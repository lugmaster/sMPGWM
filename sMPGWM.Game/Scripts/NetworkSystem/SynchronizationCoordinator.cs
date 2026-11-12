using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.World;

namespace sMPGWM.Scripts.NetworkSystem;

/// <summary>Orders input, fixed movement and completed-step publication; delegates client algorithms.</summary>
public partial class SynchronizationCoordinator : Node
{
    public PlayerController Player { get; init; } = null!;
    public GameWorld World { get; init; } = null!;
    public NetworkManager Network { get; set; } = null!;
    public Guid Session { get; private set; }
    public long CompletedTick { get; private set; }
    public long PredictionTick => _prediction.Tick;
    public double Rtt => _clock.Rtt;
    public double Jitter => _clock.Jitter;
    public int Lead => _clock.Lead;
    public double HostEstimate => _clock.HostEstimate;
    private bool _active;
    private readonly NetworkClock _clock = new(Time.GetTicksMsec);
    private readonly ClientSynchronizationState _client = new(Time.GetTicksMsec);
    private readonly LocalMovementPrediction _prediction = new();
    private readonly RemoteMovementPresentation _remote = new();
    private readonly Dictionary<int, CommandQueue> _queues = new();
    private readonly Dictionary<int, (string, long)> _bindings = new();
    private readonly Dictionary<int, ulong> _traffic = new();
    private readonly List<MovementSample> _snapshotSamples = new(MovementWire.MaxPlayers);
    private readonly List<int> _departedPeers = new(MovementWire.MaxPlayers);

    public void Activate()
    {
        if (Engine.PhysicsTicksPerSecond != 60) { Network.FailSession("Synchronization requires 60 Hz physics."); return; }
        if (Network.IsHost()) Session = Guid.NewGuid();
        else if (!_client.HasBaseline) { Network.FailSession("Missing movement baseline."); return; }
        Player.UsePredictionCamera(!Network.IsHost());
        _active = true; _client.Activate();
    }

    public byte[] Baseline()
    {
        _snapshotSamples.Clear();
        foreach (var pair in World.Players)
            if (pair.Value.ControlledShip != null)
                _snapshotSamples.Add(MovementStateAdapter.Capture(pair.Key, pair.Value,
                    _queues.TryGetValue(pair.Key, out var queue) ? queue.LatestArrival : CompletedTick));
        return MovementWire.Snapshot(Session, CompletedTick, _snapshotSamples);
    }

    public void AcceptCommands(int peer, byte[] bytes)
    {
        if (!_active || !Network.IsHost() || !Network.MovementReady(peer)) return;
        ulong now = Time.GetTicksMsec();
        if (_traffic.TryGetValue(peer, out var last) && now - last < 8) return;
        _traffic[peer] = now;
        if (!MovementWire.ReadCommands(bytes, out var session, out var entity, out var generation, out var commands) ||
            session != Session || !World.Players.TryGetValue(peer, out var owner) ||
            owner.ControlledShip?.EntityId != entity || owner.ControlGeneration != generation) return;
        var queue = Queue(peer, owner);
        foreach (var command in commands) queue.Accept(command, CompletedTick);
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
            foreach (var (peer, owner) in World.Players)
            {
                var ship = owner.ControlledShip; if (ship == null) continue;
                var q = Queue(peer, owner);
                if (peer == 1) q.Accept(new(tick, ship.InputState), CompletedTick);
                ship.SetInput(q.Consume(tick));
            }
            Network.AdvanceGameplay(tick);
            foreach (var owner in World.Players.Values) owner.ControlledShip?.Simulate(1.0 / 60);
            CompletedTick = tick;
            Network.PublishGameplay(tick);
            if (tick % 2 == 0) Network.PublishMovement(Baseline());
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
                Network.SendMovementCommands(MovementWire.Commands(Session, local.EntityId,
                    Player.Participant!.ControlGeneration, _prediction.RecentCommands()));
        }
        if (PredictionTick >= Math.Floor(HostEstimate) + Lead) _client.FinishWarmup();
    }

    public void TimingReply(long token, long hostTick)
    {
        if (_active && !_clock.Reply(token, hostTick)) Recover();
    }

    public bool Receive(byte[] bytes, bool baseline = false, long request = 0)
    {
        if (!MovementWire.ReadSnapshot(bytes, out var session, out var tick, out var samples)) return false;
        foreach (var sample in samples)
            if (World.Players.TryGetValue(sample.Peer, out var owner) && owner.ControlledShip is { } ship &&
                ship.EntityId == sample.Entity && owner.ControlGeneration == sample.Generation &&
                (sample.ConfigurationRevision < ship.MovementConfigurationRevision ||
                 sample.ConfigurationRevision == ship.MovementConfigurationRevision && sample.Configuration != ship.MovementConfiguration))
                return false;
        if (baseline)
        {
            if (!_client.AcceptsBaseline(session, request)) return false;
            if (samples.Count != World.Players.Count(p => p.Value.ControlledShip != null) || samples.Any(s =>
                !World.Players.TryGetValue(s.Peer, out var p) || p.ControlledShip?.EntityId != s.Entity ||
                p.ControlGeneration != s.Generation)) return false;
            Session = session;
            _client.ApplyBaseline(session, tick);
            _prediction.Reset(tick); _remote.Reset(tick); _clock.ClearProbes();
        }
        else if (!_active || !_client.ObserveSnapshot(session, tick)) return false;
        CompletedTick = tick;
        _clock.Observe(tick, baseline);
        // Warm-up snapshots refresh liveness and remote proxies but cannot reconcile an unfinished history.
        bool reconcile = baseline || _client.Phase == ClientSynchronizationPhase.Predicting;
        MovementSnapshotApplication.Apply(samples, World.Players,
            (owner, sample) => _remote.Apply(sample.Peer, owner.ControlledShip!, tick, sample, baseline),
            (owner, sample) =>
            {
                if (baseline || sample.ConfigurationRevision != owner.ControlledShip!.MovementConfigurationRevision)
                {
                    _prediction.ApplyBaseline(tick, sample, owner.ControlledShip!);
                    _client.RestartWarmup();
                }
                else if (reconcile && !_prediction.Reconcile(sample.Peer, owner, tick, sample)) Recover();
            });
        return true;
    }

    public override void _Process(double delta)
    {
        if (!_active || Network.IsHost() || !_client.CanPresent) return;
        _remote.Render(delta, _clock.PresentationTick, World.Players);
        Player.Participant?.ControlledShip?.Presentation.RenderLocal(delta);
    }

    private void Recover()
    {
        if (!_client.BeginRecovery()) return;
        _prediction.Reset(PredictionTick); _remote.Reset(CompletedTick); _clock.ClearProbes();
        Player.Participant?.ControlledShip?.ClearInput();
        Network.RequestMovementBaseline(_client.Request);
    }
    public void BindingChanged(int peer)
    {
        ResetCommands(peer); _remote.Remove(peer);
        if (peer == Multiplayer.GetUniqueId())
        {
            _prediction.Reset(CompletedTick);
            Player.Participant?.ControlledShip?.Presentation.Snap();
            if (_active) Recover();
        }
    }
    public void ResetCommands(int peer) { _queues.Remove(peer); _bindings.Remove(peer); }
    public void Stop()
    {
        _active = false; ProcessMode = ProcessModeEnum.Disabled; _client.Stop();
        _queues.Clear(); _bindings.Clear(); _traffic.Clear();
        _prediction.Reset(PredictionTick); _remote.Reset(CompletedTick); _clock.ClearProbes();
    }
    public override void _ExitTree() => Stop();
}
