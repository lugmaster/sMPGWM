using sMPGWM.Scripts.AbilitySystem;
using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;
using PlayerController = sMPGWM.Scripts.PlayerControllerSystem.PlayerController;

namespace sMPGWM.Scripts.NetworkSystem;

/// <summary>Orders input, fixed movement and completed-step publication; delegates client algorithms.</summary>
public partial class SynchronizationCoordinator : Node
{
    public PlayerController Player { get; init; } = null!;
    public GameWorld World { get; init; } = null!;
    public NetworkManager Network { get; set; } = null!;
    private Guid _session;
    public Guid Session
    {
        get => _session;
        private set
        {
            if (_session == value) return;
            _session = value;
            SessionWireIdentity = value.ToString("N");
        }
    }
    public string SessionWireIdentity { get; private set; } = Guid.Empty.ToString("N");
    public long CompletedTick { get; private set; }
    public long PredictionTick => _prediction.Tick;
    // TEST-SUPPORT: NETWORK-METRICS-001
    // Tests: sMPGWM.Tests.PerformanceProcessTests
    public double Rtt => _clock.Rtt;
    // TEST-SUPPORT: NETWORK-METRICS-002
    // Tests: sMPGWM.Tests.PerformanceProcessTests
    public double Jitter => _clock.Jitter;
    public int Lead => _clock.Lead;
    public double HostEstimate => _clock.HostEstimate;
    public double PresentationTick => Network.IsHost() ? CompletedTick : _clock.PresentationTick;
    public double WeaponLocalPresentationTick => Network.IsHost() ? CompletedTick : _clock.HostEstimate;
    private bool _active;
    private Func<int, bool>? _gameplayReady;
    private Action<int, long, AbilityActivationResult>? _abilityCompleted;
    private readonly NetworkClock _clock = new(Time.GetTicksMsec);
    private readonly ClientSynchronizationState _client = new(Time.GetTicksMsec);
    private readonly LocalMovementPrediction _prediction = new();
    private readonly RemoteMovementPresentation _remote = new();
    private readonly Dictionary<int, CommandQueue> _queues = new(MovementWire.MaxPlayers);
    private readonly Dictionary<int, (string, long)> _bindings = new(MovementWire.MaxPlayers);
    private readonly Dictionary<int, ulong> _traffic = new(MovementWire.MaxPlayers);
    private readonly List<MovementSample> _snapshotSamples = new(MovementWire.MaxPlayers);
    private readonly List<int> _departedPeers = new(MovementWire.MaxPlayers);
    private readonly List<MovementSample> _receivedSamples = new(MovementWire.MaxPlayers);
    private readonly MovementCommand[] _receivedCommands = new MovementCommand[MovementWire.MaxCommands];
    private readonly MovementCommand[] _recentCommands = new MovementCommand[MovementWire.MaxCommands];
    private readonly byte[][] _snapshotBuffers = CreateBuffers(MovementWire.MaxPlayers + 1, MovementWire.SnapshotHeaderBytes, MovementWire.SampleBytes);
    private readonly byte[][] _commandBuffers = CreateBuffers(MovementWire.MaxCommands + 1, MovementWire.CommandHeaderBytes, MovementWire.CommandBytes);
    private Func<string, int>? _entityIndex;
    private Func<int, string?>? _resolveEntity;
    private Action<PlayerSession, MovementSample>? _applyRemote;
    private Action<PlayerSession, MovementSample>? _applyLocal;
    private long _receiveTick;
    private bool _receiveBaseline, _receiveReconcile;

    private static byte[][] CreateBuffers(int count, int header, int row)
    {
        var buffers = new byte[count][];
        for (var i = 0; i < count; i++) buffers[i] = new byte[header + i * row];
        return buffers;
    }


    public void Activate()
    {
        if (Engine.PhysicsTicksPerSecond != 60) { Network.FailSession("Synchronization requires 60 Hz physics."); return; }
        if (Network.IsHost())
        {
            Session = Guid.NewGuid();
            CompletedTick = World.InitialCompletedTick;
            Network.ClearShotPresentation(Session, CompletedTick);
            foreach (var ship in World.Ships) Network.GetWeaponEntityIndex(ship.EntityId);
        }
        else if (!_client.HasBaseline) { Network.FailSession("Missing movement baseline."); return; }
        Player.UsePredictionCamera(!Network.IsHost());
        if (Network.IsHost())
            foreach (var owner in World.Participants)
                if (owner.ControlledShip != null) Queue(owner.ControllingPeerId, owner);
        _active = true; _client.Activate();
    }

    public byte[] Baseline()
    {
        _snapshotSamples.Clear();
        foreach (var owner in World.Participants)
            if (owner.ControlledShip != null)
            {
                if (Network.IsHost()) Queue(owner.ControllingPeerId, owner);
                _snapshotSamples.Add(MovementStateAdapter.Capture(owner.ControllingPeerId, owner,
                    _queues.TryGetValue(owner.ControllingPeerId, out var queue) ? queue.LatestArrival : CompletedTick));
            }
        var bytes = _snapshotBuffers[_snapshotSamples.Count];
        MovementWire.SnapshotInto(Session, CompletedTick, _snapshotSamples, _entityIndex ??= Network.GetWeaponEntityIndex, bytes);
        return bytes;
    }

    public void AcceptCommands(int peer, byte[] bytes)
    {
        if (!_active || !Network.IsHost() || !Network.MovementReady(peer)) return;
        ulong now = Time.GetTicksMsec();
        if (_traffic.TryGetValue(peer, out var last) && now - last < 8) return;
        _traffic[peer] = now;
        if (!MovementWire.ReadCommands(bytes, _receivedCommands, out var session, out var entity, out var generation, out var count) ||
            session != Session || !World.Players.TryGetValue(peer, out var owner) ||
            owner.ControlledShip == null || Network.GetWeaponEntityIndex(owner.ControlledShip.EntityId) != entity || owner.ControlGeneration != generation) return;
        var queue = Queue(peer, owner);
        for (var i = 0; i < count; i++) queue.Accept(_receivedCommands[i], CompletedTick);
    }

    private CommandQueue Queue(int peer, PlayerSession owner)
    {
        var binding = (owner.ControlledShip!.EntityId, owner.ControlGeneration);
        if (!_bindings.TryGetValue(peer, out var previous) || previous != binding)
        {
            _bindings[peer] = binding; _queues[peer] = new();
        }
        return _queues[peer];
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_active) return;
        if (CompletedTick >= MovementWire.MaxTick || PredictionTick >= MovementWire.MaxTick - 2)
        { Network.FailSession("Movement tick range exhausted."); return; }
        _departedPeers.Clear();
        foreach (var peer in _queues.Keys)
            if (!World.Players.TryGetValue(peer, out var owner) || owner.ControlledShip == null)
                _departedPeers.Add(peer);
        foreach (var peer in _traffic.Keys)
            if (!_queues.ContainsKey(peer) && (!World.Players.TryGetValue(peer, out var owner) || owner.ControlledShip == null))
                _departedPeers.Add(peer);
        foreach (var peer in _departedPeers) { ResetCommands(peer); _traffic.Remove(peer); }
        if (Network.IsHost())
        {
            Network.PublishWorldTopology();
            Player.SampleMovementInput();
            long tick = CompletedTick + 1;
            foreach (var owner in World.Participants)
            {
                var peer = owner.ControllingPeerId;
                var ship = owner.ControlledShip; if (ship == null) continue;
                var q = Queue(peer, owner);
                if (peer == 1) q.Accept(new(tick, ship.InputState), CompletedTick);
                ship.SetInput(q.Consume(tick));
            }
            Network.Simulation.Step(tick, _gameplayReady ??= Network.MovementReady,
                _abilityCompleted ??= Network.CompleteAbility);
            CompletedTick = tick;
            Network.PublishGameplay(tick);
            if (tick % 2 == 0)
            {
                Network.PublishMovement(Baseline());
                Network.PublishShots();
            }
            return;
        }
        if (_client.NeedsRecovery()) Recover();
        if (_client.Recovering)
        {
            if (_client.TimedOut) { Network.FailSession("Movement recovery timed out."); return; }
            if (_client.RetryDue()) Network.RequestMovementBaseline(_client.Request);
            return;
        }
        if (_clock.TryProbe(out var token)) Network.SendTimingProbe(token);
        var local = Player.Participant?.ControlledShip;
        if (local == null) { _client.FinishWarmup(); return; }
        if (_prediction.AtCapacity || Math.Abs(PredictionTick - HostEstimate) > 32) { Recover(); return; }
        Player.SampleMovementInput();
        long target = (long)Math.Floor(HostEstimate) + Lead;
        int steps = PredictionTick < target - 1 ? 2 : PredictionTick > target + 1 ? 0 : 1;
        for (int n = 0; n < steps; n++)
        {
            _prediction.Step(Multiplayer.GetUniqueId(), Player.Participant!);
            if (PredictionTick % 2 == 0)
            {
                var count = _prediction.CopyRecentCommands(_recentCommands);
                var entity = Network.GetWeaponEntityIndex(local.EntityId);
                if (count == 0 || entity == 0) continue;
                var bytes = _commandBuffers[count];
                MovementWire.CommandsInto(Session, entity, Player.Participant!.ControlGeneration, _recentCommands.AsSpan(0, count), bytes);
                Network.SendMovementCommands(bytes);
            }
        }
        if (PredictionTick >= Math.Floor(HostEstimate) + Lead) _client.FinishWarmup();
    }

    public void TimingReply(long token, long hostTick)
    {
        if (_active && !_clock.Reply(token, hostTick)) Recover();
    }

    public bool Receive(byte[] bytes, bool baseline = false, long request = 0)
    {
        var samples = _receivedSamples;
        if (!MovementWire.ReadSnapshot(bytes, _resolveEntity ??= Network.ResolveNetworkEntity, samples, out var session, out var tick)) return false;
        foreach (var sample in samples)
            if (World.Players.TryGetValue(sample.Peer, out var owner) && owner.ControlledShip is { } ship &&
                ship.EntityId == sample.Entity && owner.ControlGeneration == sample.Generation &&
                (sample.ConfigurationRevision < ship.MovementConfigurationRevision ||
                 sample.ConfigurationRevision == ship.MovementConfigurationRevision && sample.Configuration != ship.MovementConfiguration))
                return false;
        if (baseline)
        {
            if (!_client.AcceptsBaseline(session, request)) return false;
            var expected = 0;
            foreach (var owner in World.Participants) if (owner.ControlledShip != null) expected++;
            if (samples.Count != expected) return false;
            foreach (var sample in samples)
                if (!World.Players.TryGetValue(sample.Peer, out var owner) || owner.ControlledShip?.EntityId != sample.Entity ||
                    owner.ControlGeneration != sample.Generation) return false;
            Session = session;
            Network.ClearShotPresentation(session, tick);
            Player.ResetFireInput();
            _client.ApplyBaseline(session, tick);
            _prediction.Reset(tick); _remote.Reset(tick); _clock.ClearProbes();
        }
        else if (!_client.ObserveSnapshot(session, tick)) return false;
        CompletedTick = tick;
        _clock.Observe(tick, baseline);
        // Warm-up snapshots refresh liveness and remote proxies but cannot reconcile an unfinished history.
        bool reconcile = baseline || _client.Phase == ClientSynchronizationPhase.Predicting;
        _receiveTick = tick; _receiveBaseline = baseline; _receiveReconcile = reconcile;
        MovementSnapshotApplication.Apply(samples, World.Players,
            _applyRemote ??= ApplyRemoteSample, _applyLocal ??= ApplyLocalSample);
        return true;
    }

    private void ApplyRemoteSample(PlayerSession owner, MovementSample sample) =>
        _remote.Apply(sample.Peer, owner.ControlledShip!, _receiveTick, sample, _receiveBaseline);

    private void ApplyLocalSample(PlayerSession owner, MovementSample sample)
    {
        if (_receiveBaseline || !_active || sample.ConfigurationRevision != owner.ControlledShip!.MovementConfigurationRevision)
        {
            _prediction.ApplyBaseline(_receiveTick, sample, owner.ControlledShip!);
            _client.RestartWarmup();
        }
        else if (_receiveReconcile && !_prediction.Reconcile(sample.Peer, owner, _receiveTick, sample)) Recover();
    }

    public override void _Process(double delta)
    {
        if (!_active || Network.IsHost() || !_client.CanPresent) return;
        Network.PresentShots(_clock.HostEstimate, _clock.PresentationTick);
        _remote.Render(delta, _clock.PresentationTick, World.Players);
        Player.Participant?.ControlledShip?.Presentation.RenderLocal(delta);
    }

    private void Recover()
    {
        if (!_client.BeginRecovery()) return;
        _prediction.Reset(PredictionTick); _remote.Reset(CompletedTick); _clock.ClearProbes();
        Player.Participant?.ControlledShip?.ClearInput();
        Player.ResetFireInput();
        Network.ClearShotPresentation(Session, CompletedTick);
        Network.RequestMovementBaseline(_client.Request);
    }
    public void BindingChanged(int peer)
    {
        Network.Simulation.RemovePeer(peer);
        ResetCommands(peer); _remote.Remove(peer);
        if (peer == Multiplayer.GetUniqueId())
        {
            _prediction.Reset(CompletedTick);
            Player.Participant?.ControlledShip?.Presentation.Snap();
            if (_active) Recover();
        }
    }
    public void ResetCommands(int peer)
    {
        Network.CancelPendingReloads(peer);
        _queues.Remove(peer); _bindings.Remove(peer);
        if (World.Players.TryGetValue(peer, out var owner)) owner.ControlledShip?.ClearInput();
    }
    public void ResetCommandsForRecovery(int peer)
    {
        ResetCommands(peer);
        if (!World.Players.TryGetValue(peer, out var owner) || owner.ControlledShip is not { } ship) return;
        _bindings[peer] = (ship.EntityId, owner.ControlGeneration);
        // Previously issued commands may still be in flight anywhere inside the 32-tick admission window.
        _queues[peer] = new CommandQueue(CompletedTick + 32);
    }
    public void Stop()
    {
        _active = false; ProcessMode = ProcessModeEnum.Disabled; _client.Stop();
        _queues.Clear(); _bindings.Clear(); _traffic.Clear();
        _prediction.Reset(PredictionTick); _remote.Reset(CompletedTick); _clock.ClearProbes();
    }
    public override void _ExitTree() => Stop();
}
