using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.scripts.Network;

namespace sMPGWM.Scripts.Network;

/// <summary>All movement is ordered here; snapshots describe completed steps, never world rollback.</summary>
public partial class SynchronizationCoordinator : Node
{
    public PlayerController Player { get; init; } = null!;
    public MpManager World { get; init; } = null!;
    public NetworkManager Network { get; set; } = null!;
    public Guid Session { get; private set; }
    public long CompletedTick { get; private set; }
    public long PredictionTick { get; private set; }
    public long PublicationCount { get; private set; }
    public double MaximumPublicationMilliseconds { get; private set; }
    public long Corrections { get; private set; }
    public long Recoveries { get; private set; }
    public long Underruns { get; private set; }
    public int MaximumHistory { get; private set; }
    public int MaximumQueue { get; private set; }
    public int MaximumReplaySteps { get; private set; }
    public double MaximumReplayMilliseconds { get; private set; }
    public float MaximumCorrectionDistance { get; private set; }
    public long ArrivalAge { get; private set; }
    public long FallbackCount => _queues.Values.Sum(q => q.FallbackCount);
    public double Rtt { get; private set; }
    public double Jitter { get; private set; }
    public int Lead => Math.Clamp((int)Math.Ceiling((Rtt / 2 + Jitter * 2) * 60) + 3, 3, 24);
    public double HostEstimate => _hostAtReceipt + (Time.GetTicksMsec() - _receivedAt) * .06;
    public event Action<long>? StepCompleted;
    public event Action<long>? SnapshotDue;
    private bool _active, _baseline, _recovering, _warming;
    private long _lastSnapshot = -1, _request;
    private ulong _snapshotAt, _receivedAt, _lastFrame, _recoveryAt, _lastProbe;
    private double _hostAtReceipt, _cursor;
    private readonly Dictionary<int, long> _controlGenerations = new();
    private readonly Dictionary<int, CommandQueue> _queues = new();
    private readonly Dictionary<int, (string, long)> _bindings = new();
    private readonly SortedDictionary<long, (MovementCommand Command, MovementSample State)> _history = new();
    private readonly Dictionary<int, List<(long Tick, MovementSample State)>> _remote = new();
    private readonly Dictionary<int, ulong> _traffic = new();
    private readonly Dictionary<long, ulong> _probes = new();
    private Vector2 _visualOffset;
    private float _angleOffset;
    private MovementSample? _localIdentity;

    public void Activate()
    {
        if (Engine.PhysicsTicksPerSecond != 60) { Network.FailSession("Synchronization requires 60 Hz physics."); return; }
        if (Network.IsHost()) { Session = Guid.NewGuid(); _baseline = true; }
        if (!_baseline) { Network.FailSession("Missing movement baseline."); return; }
        Player.UsePredictionCamera(!Network.IsHost());
        _active = true; _lastFrame = Time.GetTicksMsec();
    }

    private MovementSample Capture(int peer, MpController controller) =>
        new(peer, controller.ControlledShip!.EntityId, controller.ControlGeneration,
            controller.ControlledShip.DiscontinuityRevision, controller.ControlledShip.GlobalPosition,
            controller.ControlledShip.GlobalRotation, controller.ControlledShip.Velocity,
            controller.ControlledShip.InputState, _queues.TryGetValue(peer, out var q) ? q.LatestArrival : CompletedTick, controller.ControlledShip.AppliedMovementFlags);

    public byte[] Baseline() => MovementWire.Snapshot(Session, CompletedTick,
        World.MpControllers.Where(p => p.Value.ControlledShip != null).Select(p => Capture(p.Key, p.Value)).ToArray());

    public void AcceptCommands(int peer, byte[] bytes)
    {
        if (!_active || !Network.IsHost() || !Network.MovementReady(peer)) return;
        ulong now = Time.GetTicksMsec();
        if (_traffic.TryGetValue(peer, out var last) && now - last < 8) return;
        _traffic[peer] = now;
        if (!MovementWire.ReadCommands(bytes, out var session, out var entity, out var generation, out var commands) ||
            session != Session || !World.MpControllers.TryGetValue(peer, out var owner) ||
            owner.ControlledShip?.EntityId != entity || owner.ControlGeneration != generation) return;
        var queue = Queue(peer, owner);
        foreach (var command in commands) queue.Accept(command, CompletedTick);
        MaximumQueue = Math.Max(MaximumQueue, queue.Count);
    }

    private CommandQueue Queue(int peer, MpController owner)
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
        ulong now = Time.GetTicksMsec();
        bool stalled = now - _lastFrame > 500; _lastFrame = now;
        if (CompletedTick >= MovementWire.MaxTick) { Network.FailSession("Movement tick range exhausted."); return; }
        PruneEntities();
        if (Network.IsHost())
        {
            foreach (var (peer, owner) in World.MpControllers)
            {
                if (owner.ControlledShip == null)
                {
                    if (!_controlGenerations.TryGetValue(peer, out var old) || old != owner.ControlGeneration)
                    {
                        ResetCommands(peer);
                        World.RemoveConstructionRecord(peer);
                        Network.PublishControlRelease(peer, owner.ControlGeneration);
                        _controlGenerations[peer] = owner.ControlGeneration;
                    }
                    continue;
                }
                if (_controlGenerations.TryGetValue(peer, out var generation) && generation != owner.ControlGeneration)
                {
                    ResetCommands(peer);
                    Network.PublishControlBinding(World.CurrentRecord(peer), owner.ControlGeneration);
                }
                _controlGenerations[peer] = owner.ControlGeneration;
            }
            Player.SampleMovementInput();
            long tick = CompletedTick + 1;
            foreach (var (peer, owner) in World.MpControllers)
            {
                var ship = owner.ControlledShip; if (ship == null) continue;
                var q = Queue(peer, owner);
                if (peer == 1) q.Accept(new(tick, ship.InputState), CompletedTick);
                ship.SetInput(q.Consume(tick)); ship.Simulate(1.0 / 60);
            }
            CompletedTick = tick; StepCompleted?.Invoke(tick);
            if (tick % 2 == 0)
            {
                ulong started = Time.GetTicksUsec();
                PublicationCount++; Network.PublishMovement(Baseline()); SnapshotDue?.Invoke(tick);
                MaximumPublicationMilliseconds = Math.Max(MaximumPublicationMilliseconds, (Time.GetTicksUsec() - started) / 1000.0);
            }
            return;
        }
        if (stalled || now - _snapshotAt > 1000) Recover();
        now = Time.GetTicksMsec();
        if (_recovering)
        {
            if (now - _recoveryAt > 5000) { Network.FailSession("Movement recovery timed out."); return; }
            if (now - _lastProbe > 1000) { _lastProbe = now; Network.RequestMovementBaseline(_request); }
            return;
        }
        if (now - _lastProbe > 1000)
        {
            _lastProbe = now;
            _probes.Clear(); _probes[(long)now] = now;
            Network.SendTimingProbe((long)now);
        }
        var local = Player.Participant?.ControlledShip;
        if (local == null) { _warming = false; return; }
        if (_history.Count >= 128 || Math.Abs(PredictionTick - HostEstimate) > 32) { Recover(); return; }
        Player.SampleMovementInput();
        // At most two fixed steps per callback. Extra steps fill a slowly increasing lead.
        long target = (long)Math.Floor(HostEstimate) + Lead;
        int steps = PredictionTick < target - 1 ? 2 : PredictionTick > target + 1 ? 0 : 1;
        for (int n = 0; n < steps; n++)
        {
            long tick = ++PredictionTick;
            var command = new MovementCommand(tick, local.InputState);
            local.Simulate(1.0 / 60);
            _history[tick] = (command, Capture(Multiplayer.GetUniqueId(), Player.Participant!));
            MaximumHistory = Math.Max(MaximumHistory, _history.Count);
            if (tick % 2 == 0)
                Network.SendMovementCommands(MovementWire.Commands(Session, local.EntityId,
                    Player.Participant!.ControlGeneration, _history.Values.TakeLast(6).Select(h => h.Command).ToArray()));
        }
        if (PredictionTick >= Math.Floor(HostEstimate) + Lead) _warming = false;
    }

    public void TimingReply(long token, long hostTick)
    {
        if (!_probes.Remove(token, out var sent) || hostTick < 0 || hostTick > MovementWire.MaxTick) return;
        double measured = (Time.GetTicksMsec() - sent) / 1000.0;
        if (measured > 2) { Recover(); return; }
        Jitter += (Math.Abs(measured - Rtt) - Jitter) * .2;
        Rtt = Rtt == 0 ? measured : Rtt + (measured - Rtt) * .2;
        double estimate = hostTick + measured * 30;
        _hostAtReceipt = HostEstimate + Math.Clamp(estimate - HostEstimate, -1, 1);
        _receivedAt = Time.GetTicksMsec();
    }

    public bool Receive(byte[] bytes, bool baseline = false, long request = 0)
    {
        if (!MovementWire.ReadSnapshot(bytes, out var session, out var tick, out var samples)) return false;
        if (baseline)
        {
            if (_baseline && (session != Session || request != _request || !_recovering)) return false;
        }
        else if (!_active || _recovering || _warming || session != Session || tick <= _lastSnapshot) return false;
        // Validate the whole baseline before mutating any ship.
        if (baseline && (samples.Count != World.MpControllers.Count(p => p.Value.ControlledShip != null) || samples.Any(s =>
            !World.MpControllers.TryGetValue(s.Peer, out var p) || p.ControlledShip?.EntityId != s.Entity ||
            p.ControlGeneration > s.Generation))) return false;
        if (baseline) { Session = session; _history.Clear(); _remote.Clear(); _visualOffset = Vector2.Zero; _angleOffset = 0; }
        _snapshotAt = Time.GetTicksMsec();
        _lastSnapshot = tick; CompletedTick = tick;
        _hostAtReceipt = baseline ? tick : HostEstimate + Math.Clamp(tick + Rtt * 30 - HostEstimate, -1, 1);
        _receivedAt = Time.GetTicksMsec();
        if (baseline) { PredictionTick = tick; _cursor = tick; _baseline = true; _recovering = false; _warming = true; }
        if (baseline) foreach (var sample in samples) World.MpControllers[sample.Peer].ApplyGeneration(sample.Generation);
        foreach (var sample in samples)
        {
            if (!World.MpControllers.TryGetValue(sample.Peer, out var owner) ||
                owner.ControlledShip?.EntityId != sample.Entity || owner.ControlGeneration != sample.Generation) continue;
            var ship = owner.ControlledShip;
            if (ship.LocallyControlled)
            {
                if (baseline) { Restore(ship, sample); ship.Present(sample.Position, sample.Rotation); ship.GetNodeOrNull<Camera2D>("LocalCamera")?.ResetSmoothing(); _localIdentity = sample; }
                else Reconcile(ship, tick, sample);
            }
            else
            {
                var oldVisual = ship.DisplayedPosition;
                float oldAngle = ship.GetNode<Node2D>("ShipVisuals").GlobalRotation;
                Restore(ship, sample); // Collision proxy always uses newest authority, never delayed visuals.
                if (!_remote.TryGetValue(sample.Peer, out var buffer)) _remote[sample.Peer] = buffer = new();
                bool reset = baseline || buffer.Count == 0 || buffer[^1].State.Revision != sample.Revision ||
                    buffer[^1].State.Generation != sample.Generation || buffer[^1].State.Entity != sample.Entity;
                if (reset) buffer.Clear();
                buffer.Add((tick, sample));
                if (buffer.Count > 64) buffer.RemoveAt(0);
                ship.Present(reset ? sample.Position : oldVisual, reset ? sample.Rotation : oldAngle);
            }
        }
        return true;
    }

    private static void Restore(ShipController ship, MovementSample sample) =>
        ship.Restore(sample.Position, sample.Rotation, sample.Velocity, sample.Input);

    private void Reconcile(ShipController ship, long tick, MovementSample authoritative)
    {
        bool discontinuity = _localIdentity is { } identity &&
            (identity.Entity != authoritative.Entity || identity.Generation != authoritative.Generation || identity.Revision != authoritative.Revision);
        _localIdentity = authoritative;
        if (discontinuity || !_history.TryGetValue(tick, out var predicted) || PredictionTick - tick > 64)
        { Recover(); return; }
        ArrivalAge = Math.Max(0, tick - authoritative.Arrival);
        var state = predicted.State;
        bool differs = state.Position.DistanceTo(authoritative.Position) > .1f ||
            state.Velocity.DistanceTo(authoritative.Velocity) > .1f ||
            Math.Abs(Mathf.AngleDifference(state.Rotation, authoritative.Rotation)) > .001f ||
            state.Input != authoritative.Input;
        if (differs)
        {
            ulong replayStarted = Time.GetTicksUsec();
            MaximumReplaySteps = Math.Max(MaximumReplaySteps, (int)(PredictionTick - tick));
            MaximumCorrectionDistance = Math.Max(MaximumCorrectionDistance, state.Position.DistanceTo(authoritative.Position));
            var displayed = ship.DisplayedPosition;
            float angle = ship.GetNode<Node2D>("ShipVisuals").GlobalRotation;
            Restore(ship, authoritative);
            for (long t = tick + 1; t <= PredictionTick; t++)
            {
                if (!_history.TryGetValue(t, out var saved)) { Recover(); return; }
                ship.SetInput(saved.Command.Input); ship.Simulate(1.0 / 60);
                _history[t] = (saved.Command, Capture(Multiplayer.GetUniqueId(), Player.Participant!));
            }
            _visualOffset = displayed - ship.GlobalPosition;
            _angleOffset = Mathf.AngleDifference(ship.GlobalRotation, angle);
            if (_visualOffset.Length() > 100) { _visualOffset = Vector2.Zero; _angleOffset = 0; }
            MaximumReplayMilliseconds = Math.Max(MaximumReplayMilliseconds, (Time.GetTicksUsec() - replayStarted) / 1000.0);
            Corrections++;
        }
        foreach (long key in _history.Keys.TakeWhile(t => t <= tick).ToArray()) _history.Remove(key);
    }

    public override void _Process(double delta)
    {
        if (!_active || Network.IsHost() || _recovering) return;
        // HostEstimate already includes estimated inbound transit; subtract it exactly once.
        double target = HostEstimate - Rtt * 30 - 4;
        _cursor += Math.Clamp(target - _cursor, 0, delta * 66);
        foreach (var (peer, buffer) in _remote)
        {
            if (buffer.Count == 0 || !World.MpControllers.TryGetValue(peer, out var owner) || owner.ControlledShip == null) continue;
            while (buffer.Count > 2 && buffer[1].Tick <= _cursor) buffer.RemoveAt(0);
            var lower = buffer[0]; var upper = buffer.Count > 1 ? buffer[1] : lower;
            if (_cursor >= upper.Tick) Underruns++;
            var presented = MovementPresentation.Sample(lower.Tick, lower.State, upper.Tick, upper.State, _cursor);
            owner.ControlledShip.PresentedMovementFlags = presented.AppliedFlags;
            owner.ControlledShip.Present(presented.Position, presented.Rotation);
        }
        var local = Player.Participant?.ControlledShip;
        if (local != null)
        {
            float decay = (float)Math.Exp(-delta / .08);
            _visualOffset *= decay; _angleOffset *= decay;
            local.Present(local.GlobalPosition + _visualOffset, local.GlobalRotation + _angleOffset);
        }
    }

    private void Recover()
    {
        if (_recovering) return;
        GD.Print($"SYNC recover tick={CompletedTick} prediction={PredictionTick} history={_history.Count} request={_request + 1}");
        _recovering = true; Recoveries++; _request++; _recoveryAt = Time.GetTicksMsec(); _lastProbe = _recoveryAt;
        _history.Clear(); _remote.Clear(); _probes.Clear(); Player.Participant?.ControlledShip?.ClearInput();
        Network.RequestMovementBaseline(_request);
    }
    private void PruneEntities()
    {
        foreach (int peer in _queues.Keys.Concat(_remote.Keys).Concat(_traffic.Keys).Distinct().ToArray())
            if (!World.MpControllers.ContainsKey(peer)) { _controlGenerations.Remove(peer); _queues.Remove(peer); _bindings.Remove(peer); _remote.Remove(peer); _traffic.Remove(peer); }
    }
    public void BindingChanged(int peer)
    {
        _remote.Remove(peer);
        if (peer == Multiplayer.GetUniqueId())
        {
            _history.Clear(); _visualOffset = Vector2.Zero; _angleOffset = 0; _localIdentity = null;
            if (_active) Recover();
        }
    }
    public void ResetCommands(int peer) { _queues.Remove(peer); _bindings.Remove(peer); }
    public void Stop()
    {
        _active = false; ProcessMode = ProcessModeEnum.Disabled;
        _controlGenerations.Clear(); _queues.Clear(); _bindings.Clear(); _history.Clear(); _remote.Clear(); _traffic.Clear(); _probes.Clear();
        StepCompleted = null; SnapshotDue = null;
    }
    public override void _ExitTree() => Stop();
}
