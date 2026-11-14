using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, WeaponPublicationState> _publishedWeapons = new();
    private readonly AcceptedShot[] _shots = new AcceptedShot[256];
    private readonly AcceptedShot[] _receivedShots = new AcceptedShot[ShotEventCodec.MaxEvents];
    private int _shotCount;
    private bool _weaponEventsBound;
    private bool _hasWeaponBaseline;
    private long _shotCutoff = -1;
    private long _weaponRequest;
    private readonly ShotHistory _shotWindows = new(1024);
    private readonly Dictionary<int, Queue<(long Request, WeaponCommandResult Result)>> _weaponHistory = new();
    private readonly Dictionary<int, long> _latestWeaponRequest = new();
    private readonly HashSet<(int Peer, long Request)> _pendingWeapons = new();
    public bool WeaponBaselineReady => IsHost() || _hasWeaponBaseline;
    public event Action<AcceptedShot>? ShotReceived;
    public event Action<WeaponCommandResult>? WeaponFeedback;

    internal void AdvanceWeaponPresentation(double tick) => TickWeaponPresentation(tick);
    internal void BindWeaponEvents()
    {
        if (_weaponEventsBound) return;
        _weaponEventsBound = true;
        Simulation.Weapons.ShotProduced += RecordShot;
        Simulation.ShieldCollisionProduced += RecordShieldCollision;
    }
    private void RecordShot(AcceptedShot shot)
    {
        ShotReceived?.Invoke(shot);
        if (_shotCount < _shots.Length) _shots[_shotCount++] = shot;
    }
    private void PublishWeapons(long tick)
    {
        foreach (var (peer, join) in _joins.Snapshot())
        {
            if (!join.ReceivesWorld || !World.Players.TryGetValue(peer, out var owner)) continue;
            if (!_publishedWeapons.TryGetValue(peer, out var published) || !published.MatchesBinding(owner))
            {
                if (!SendWeapons(peer)) return;
                continue;
            }
            var delta = WeaponStateDeltaCodec.Encode(owner, World.Items, published);
            if (delta == null) continue;
            if (RpcId(peer, nameof(ReceiveWeaponDelta), Synchronization!.Session.ToString("N"), delta) != Error.Ok)
            { FailSession("Weapon delta send failed."); return; }
            published.Capture(owner, World.Items);
        }
        for (var start = 0; start < _shotCount; start += ShotEventCodec.MaxEvents)
        {
            var bytes = ShotEventCodec.Encode(Synchronization!.Session, _shots.AsSpan(start, Math.Min(ShotEventCodec.MaxEvents, _shotCount - start)));
            foreach (var (peer, join) in _joins.Snapshot())
                if (join.Phase == PeerJoinPhase.Ready) RpcId(peer, nameof(ReceiveShots), bytes);
        }
        _shotCount = 0;
        PublishShieldCollisions();
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveShots(byte[] bytes)
    {
        if (!IsHostMessage() || !WeaponBaselineReady || !ShotEventCodec.Decode(bytes, _receivedShots, out var session, out var count) || session != Synchronization!.Session) return;
        foreach (var shot in _receivedShots.AsSpan(0, count))
        {
            if (shot.Tick <= _shotCutoff || shot.Tick < Synchronization.CompletedTick - 120 || shot.Tick > Synchronization.CompletedTick + 120) continue;
            if (!_shotWindows.TryAccept(shot.ItemId, shot.Sequence, shot.Tick)) continue;
            ShotReceived?.Invoke(shot);
        }
    }
    internal void WeaponBaselineCutoff(long tick)
    {
        _shotCutoff = tick; _shotWindows.Clear(); ClearWeaponPresentation(tick);
    }
    public void RequestWeaponReload() => RequestWeaponChange("", default, 0);
    public void RequestWeaponGroup(string slot, ItemEntityId item, byte group) => RequestWeaponChange(slot, item, group);
    private void RequestWeaponChange(string slot, ItemEntityId item, byte group)
    {
        if (_stopped || !WeaponBaselineReady || Synchronization == null || World.LocalPlayer?.Participant is not { ControlledShip: { } ship } owner) return;
        var request = ++_weaponRequest;
        if (IsHost()) QueueWeaponRequest(owner.ControllingPeerId, Synchronization.Session.ToString("N"), request, ship.EntityId,
            owner.ControlGeneration, ship.Loadout.Weapons.ConfigurationRevision, slot, (long)item.Value, group);
        else RpcId(1, nameof(RequestWeaponCommand), Synchronization.Session.ToString("N"), request, ship.EntityId,
            owner.ControlGeneration, ship.Loadout.Weapons.ConfigurationRevision, slot, (long)item.Value, (int)group);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestWeaponCommand(string session, long request, string entity, long generation, long revision, string slot, long item, int group)
        => QueueWeaponRequest(Multiplayer.GetRemoteSenderId(), session, request, entity, generation, revision, slot, item, group);
    private void QueueWeaponRequest(int peer, string session, long request, string entity, long generation, long revision, string slot, long item, int group)
    {
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            request <= 0 || entity.Length != 32 || generation < 0 || revision <= 0 || slot.Length > 64 || item < 0 || group < 0 || group > 2) return;
        if (_weaponHistory.TryGetValue(peer, out var history))
            foreach (var entry in history) if (entry.Request == request) { ReplyWeapon(peer, request, entry.Result); return; }
        if (_pendingWeapons.Contains((peer, request))) return;
        if (_latestWeaponRequest.TryGetValue(peer, out var last) && request <= last) { ReplyWeapon(peer, request, WeaponCommandResult.Rejected); return; }
        _latestWeaponRequest[peer] = request;
        if (_pendingWeapons.Count >= 32) { CompleteWeapon(peer, request, WeaponCommandResult.QueueFull); return; }
        _pendingWeapons.Add((peer, request));
        var kind = slot.Length == 0 && item == 0 ? WeaponCommandKind.Reload : WeaponCommandKind.AssignGroup;
        if (!Simulation.QueueWeaponCommand(peer, entity, generation, revision, kind, slot, item == 0 ? default : ItemEntityId.FromNetwork(item), (byte)group,
            result => CompleteWeapon(peer, request, result))) CompleteWeapon(peer, request, WeaponCommandResult.QueueFull);
    }
    private void CompleteWeapon(int peer, long request, WeaponCommandResult result)
    {
        _pendingWeapons.Remove((peer, request));
        if (!_weaponHistory.TryGetValue(peer, out var history)) _weaponHistory.Add(peer, history = new());
        if (history.Count == 32) history.Dequeue();
        history.Enqueue((request, result)); ReplyWeapon(peer, request, result);
    }
    private void ReplyWeapon(int peer, long request, WeaponCommandResult result)
    {
        if (peer == 1) WeaponFeedback?.Invoke(result);
        else RpcId(peer, nameof(ReceiveWeaponResult), Synchronization!.Session.ToString("N"), request, (int)result);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponResult(string session, long request, int result)
    {
        if (IsHostMessage() && session == Synchronization!.Session.ToString("N") && request == _weaponRequest &&
            result >= (int)WeaponCommandResult.Accepted && result <= (int)WeaponCommandResult.QueueFull)
            WeaponFeedback?.Invoke((WeaponCommandResult)result);
    }
    private bool SendWeapons(int peer)
    {
        if (!World.Players.TryGetValue(peer, out var owner)) return false;
        var payload = WeaponStateCodec.Encode(owner, World.Items);
        if (RpcId(peer, nameof(ReceiveWeapons), Synchronization!.Session.ToString("N"), payload) != Error.Ok)
        { FailSession("Weapon state send failed."); return false; }
        if (!_publishedWeapons.TryGetValue(peer, out var published))
            _publishedWeapons.Add(peer, published = new WeaponPublicationState());
        published.Capture(owner, World.Items);
        return true;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeapons(string session, byte[] bytes)
    {
        if (!IsHostMessage() || !_clientJoin.HasWorld || session != Synchronization!.Session.ToString("N") || World.LocalPlayer?.Participant is not { } owner) return;
        if (!WeaponStateCodec.Apply(bytes, owner, World.Items, out var applied)) { FailSession("Invalid weapon state snapshot."); return; }
        if (!applied) return;
        _hasWeaponBaseline = true;
        if (_hasItemBaseline && _clientJoin.ApplyItems(Time.GetTicksMsec()))
            if (RpcId(1, nameof(ConfirmSpawnReady), session, owner.ControlledShip?.EntityId ?? "", owner.ControlGeneration) != Error.Ok)
                FailSession("Weapon baseline acknowledgement failed.");
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponDelta(string session, byte[] bytes)
    {
        if (!IsHostMessage() || !_clientJoin.HasWorld || !_hasWeaponBaseline ||
            session != Synchronization!.Session.ToString("N") || World.LocalPlayer?.Participant is not { } owner) return;
        if (!WeaponStateDeltaCodec.Apply(bytes, owner, World.Items)) FailSession("Invalid weapon state delta.");
    }
    private void ClearWeapons()
    {
        _shieldCollisionEventCount = 0;
        Array.Clear(_shieldCollisionEvents);
        Array.Clear(_receivedShots);
        _publishedWeapons.Clear(); _shotCount = 0; _hasWeaponBaseline = false; _shotWindows.Clear(); _weaponHistory.Clear(); _pendingWeapons.Clear(); _latestWeaponRequest.Clear();
        ShotReceived = null; WeaponFeedback = null; ClearWeaponPresentation(long.MaxValue);
    }
}
