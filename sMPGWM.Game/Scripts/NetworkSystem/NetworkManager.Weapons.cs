using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, ulong> _publishedWeapons = new();
    private readonly AcceptedShot[] _shots = new AcceptedShot[256];
    private int _shotCount;
    private bool _weaponEventsBound;
    private bool _hasWeaponBaseline;
    private long _shotCutoff = -1;
    private long _weaponRequest;
    private readonly Dictionary<ItemEntityId, (ulong Sequence, ulong Window)> _shotWindows = new();
    private readonly Dictionary<int, Queue<(long Request, int Result)>> _weaponHistory = new();
    private readonly Dictionary<int, long> _latestWeaponRequest = new();
    private readonly HashSet<(int Peer, long Request)> _pendingWeapons = new();
    public bool WeaponBaselineReady => IsHost() || _hasWeaponBaseline;
    public event Action<AcceptedShot>? ShotReceived;
    public event Action<int>? WeaponFeedback;

    internal void AdvanceWeaponPresentation(double tick) => TickWeaponPresentation(tick);
    internal void BindWeaponEvents()
    {
        if (_weaponEventsBound) return;
        _weaponEventsBound = true;
        Simulation.Weapons.ShotProduced += RecordShot;
        InitializeWeaponPresentation();
    }
    private void RecordShot(AcceptedShot shot)
    {
        PresentShot(shot); ShotReceived?.Invoke(shot);
        if (_shotCount < _shots.Length) _shots[_shotCount++] = shot;
    }
    private void PublishWeapons(long tick)
    {
        foreach (var (peer, join) in _joins.Snapshot())
            if (join.ReceivesWorld && World.Players.TryGetValue(peer, out var owner) &&
                (!_publishedWeapons.TryGetValue(peer, out var revision) || revision != WeaponRevision(owner))) SendWeapons(peer);
        for (var start = 0; start < _shotCount; start += ShotEventCodec.MaxEvents)
        {
            var bytes = ShotEventCodec.Encode(Synchronization!.Session, _shots.AsSpan(start, Math.Min(ShotEventCodec.MaxEvents, _shotCount - start)));
            foreach (var (peer, join) in _joins.Snapshot())
                if (join.Phase == PeerJoinPhase.Ready) RpcId(peer, nameof(ReceiveShots), bytes);
        }
        _shotCount = 0;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveShots(byte[] bytes)
    {
        if (!IsHostMessage() || !WeaponBaselineReady || !ShotEventCodec.Decode(bytes, out var session, out var shots) || session != Synchronization!.Session) return;
        foreach (var shot in shots)
        {
            if (shot.Tick <= _shotCutoff || shot.Tick < Synchronization.CompletedTick - 120 || shot.Tick > Synchronization.CompletedTick + 120) continue;
            if (_shotWindows.TryGetValue(shot.ItemId, out var previous))
            {
                if (shot.Sequence > previous.Sequence)
                {
                    var shift = shot.Sequence - previous.Sequence;
                    _shotWindows[shot.ItemId] = (shot.Sequence, shift >= 64 ? 1UL : (previous.Window << (int)shift) | 1UL);
                }
                else
                {
                    var distance = previous.Sequence - shot.Sequence;
                    if (distance >= 64 || (previous.Window & (1UL << (int)distance)) != 0) continue;
                    _shotWindows[shot.ItemId] = (previous.Sequence, previous.Window | (1UL << (int)distance));
                }
            }
            else
            {
                if (_shotWindows.Count >= 1024) continue;
                _shotWindows.Add(shot.ItemId, (shot.Sequence, 1));
            }
            PresentShot(shot); ShotReceived?.Invoke(shot);
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
        if (_latestWeaponRequest.TryGetValue(peer, out var last) && request <= last) { ReplyWeapon(peer, request, 1); return; }
        _latestWeaponRequest[peer] = request;
        if (_pendingWeapons.Count >= 32) { CompleteWeapon(peer, request, 2); return; }
        _pendingWeapons.Add((peer, request));
        if (!Simulation.QueueWeaponCommand(peer, entity, generation, revision, slot, item == 0 ? default : ItemEntityId.FromNetwork(item), (byte)group,
            result => CompleteWeapon(peer, request, result))) CompleteWeapon(peer, request, 2);
    }
    private void CompleteWeapon(int peer, long request, int result)
    {
        _pendingWeapons.Remove((peer, request));
        if (!_weaponHistory.TryGetValue(peer, out var history)) _weaponHistory.Add(peer, history = new());
        if (history.Count == 32) history.Dequeue();
        history.Enqueue((request, result)); ReplyWeapon(peer, request, result);
    }
    private void ReplyWeapon(int peer, long request, int result)
    {
        if (peer == 1) WeaponFeedback?.Invoke(result);
        else RpcId(peer, nameof(ReceiveWeaponResult), Synchronization!.Session.ToString("N"), request, result);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponResult(string session, long request, int result)
    {
        if (IsHostMessage() && session == Synchronization!.Session.ToString("N") && request == _weaponRequest && result >= 0 && result <= 2) WeaponFeedback?.Invoke(result);
    }
    private ulong WeaponRevision(PlayerSession owner)
    {
        var revision = unchecked((ulong)owner.ControlGeneration * 1099511628211UL);
        if (owner.ControlledShip is { } ship)
        {
            revision ^= (ulong)ship.Loadout.Weapons.ConfigurationRevision;
            Accumulate(ship.Equipment.Container);
        }
        Accumulate(owner.Inventory.Container);
        return revision;
        void Accumulate(ItemContainer container)
        {
            revision = unchecked((revision ^ (ulong)container.Revision) * 1099511628211UL);
            foreach (var id in container.Slots)
                if (id is { } item && World.Items.Weapons.TryGet(item, out var state))
                    revision = unchecked((revision ^ item.Value ^ (ulong)state.Revision) * 1099511628211UL);
        }
    }
    private bool SendWeapons(int peer)
    {
        if (!World.Players.TryGetValue(peer, out var owner)) return false;
        var payload = WeaponStateCodec.Encode(owner, World.Items);
        if (RpcId(peer, nameof(ReceiveWeapons), Synchronization!.Session.ToString("N"), payload) != Error.Ok)
        { FailSession("Weapon state send failed."); return false; }
        _publishedWeapons[peer] = WeaponRevision(owner);
        return true;
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeapons(string session, byte[] bytes)
    {
        if (!IsHostMessage() || !_clientJoin.HasWorld || session != Synchronization!.Session.ToString("N") || World.LocalPlayer?.Participant is not { } owner) return;
        if (!WeaponStateCodec.Apply(bytes, owner, World.Items, out var applied)) { FailSession("Invalid weapon state snapshot."); return; }
        if (!applied) return;
        _hasWeaponBaseline = true;
        InitializeWeaponPresentation();
        if (_hasItemBaseline && _clientJoin.ApplyItems(Time.GetTicksMsec()))
            if (RpcId(1, nameof(ConfirmSpawnReady), session, owner.ControlledShip?.EntityId ?? "", owner.ControlGeneration) != Error.Ok)
                FailSession("Weapon baseline acknowledgement failed.");
    }
    private void ClearWeapons()
    {
        _publishedWeapons.Clear(); _shotCount = 0; _hasWeaponBaseline = false; _shotWindows.Clear(); _weaponHistory.Clear(); _pendingWeapons.Clear(); _latestWeaponRequest.Clear();
        ShotReceived = null; WeaponFeedback = null; DisposeWeaponPresentation();
    }
}
