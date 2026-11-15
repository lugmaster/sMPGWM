using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    public const int ShotStagingCapacity = 256, PendingShotCapacity = 512;
    private readonly AcceptedShot[] _stagedShots = new AcceptedShot[ShotStagingCapacity];
    private readonly AcceptedShot[] _decodedShots = new AcceptedShot[ShotEventCodec.MaxEvents];
    private readonly AcceptedShot[] _pendingShots = new AcceptedShot[PendingShotCapacity];
    private readonly byte[][] _shotPacketBuffers = CreateShotPacketBuffers();
    private readonly ShotSequenceWindow _shotSequences = new();
    private readonly Dictionary<string, int> _weaponEntities = new(WorldTopology.MaxEntities, StringComparer.Ordinal);
    private readonly Dictionary<int, string> _weaponEntityNames = new(WorldTopology.MaxEntities);
    private int _nextWeaponEntity, _stagedShotCount, _pendingShotCount;
    private Guid _shotSession;
    private bool _hasWeaponMap;
    public long WeaponBaselineCutoff { get; private set; } = -1;
    public int ShotStagingHighWater { get; private set; }
    public long ShotStagingDrops { get; private set; }
    public int PendingShotHighWater { get; private set; }
    public long PendingShotDrops { get; private set; }
    public long ShotBytesPublished { get; private set; }
    public long ShotDedupCapacityDrops => _shotSequences.CapacityDrops;
    public int ShotDedupHighWater => _shotSequences.HighWater;
    public event Action<AcceptedShot, ShipController?, double>? ShotReceived;
    public event Action<long>? ShotPresentationCleared;

    private static byte[][] CreateShotPacketBuffers()
    {
        var buffers = new byte[ShotEventCodec.MaxEvents][];
        for (var i = 0; i < buffers.Length; i++)
            buffers[i] = new byte[ShotEventCodec.HeaderBytes + (i + 1) * ShotEventCodec.EventBytes];
        return buffers;
    }

    public int GetWeaponEntityIndex(string entity)
    {
        if (_weaponEntities.TryGetValue(entity, out var index)) return index;
        if (!IsHost() || !World.Entities.ContainsKey(entity)) return 0;
        index = checked(++_nextWeaponEntity);
        _weaponEntities.Add(entity, index); _weaponEntityNames.Add(index, entity);
        return index;
    }

    public string? ResolveNetworkEntity(int index) => _weaponEntityNames.TryGetValue(index, out var entity) ? entity : null;

    private Godot.Collections.Array WeaponEntityMap()
    {
        var rows = new Godot.Collections.Array();
        foreach (var entity in World.Entities.Keys)
            rows.Add(new Godot.Collections.Array { GetWeaponEntityIndex(entity), entity });
        return rows;
    }

    private void PublishWeaponEntityMap()
    {
        var rows = WeaponEntityMap();
        foreach (var (peer, join) in _joins.Snapshot())
            if (join.ReceivesWorld && CanSendToPeer(peer)) RpcId(peer, nameof(ReceiveWeaponEntityMap), Synchronization!.SessionWireIdentity, rows);
        // Retain only living topology entries; numeric IDs are never reused in this transport session.
        if (_weaponEntities.Count > World.Entities.Count)
        {
            var removed = new List<string>();
            foreach (var entity in _weaponEntities.Keys)
                if (!World.Entities.ContainsKey(entity)) removed.Add(entity);
            foreach (var entity in removed)
            { _weaponEntityNames.Remove(_weaponEntities[entity]); _weaponEntities.Remove(entity); }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponEntityMap(string session, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || !Guid.TryParseExact(session, "N", out var identity) || identity == Guid.Empty ||
            (_clientJoin.Phase != ClientJoinPhase.AwaitingSnapshot && identity != Synchronization.Session) ||
            rows.Count > WorldTopology.MaxEntities) return;
        var indices = new HashSet<int>(); var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in rows)
        {
            if (value.VariantType != Variant.Type.Array) return;
            var row = value.AsGodotArray();
            if (row.Count != 2 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.String ||
                row[0].AsInt64() <= 0 || row[0].AsInt64() > int.MaxValue || !Guid.TryParseExact(row[1].AsString(), "N", out _) ||
                !indices.Add(row[0].AsInt32()) || !names.Add(row[1].AsString())) return;
        }
        _weaponEntities.Clear(); _weaponEntityNames.Clear();
        foreach (var value in rows)
        {
            var row = value.AsGodotArray(); var index = row[0].AsInt32(); var name = row[1].AsString();
            _weaponEntities.Add(name, index); _weaponEntityNames.Add(index, name);
        }
        _shotSession = identity;
        _hasWeaponMap = true;
    }

    public void StageShot(AcceptedShot shot)
    {
        if (!IsHost() || Synchronization == null || shot.Session != Synchronization.Session || !ShotEventCodec.IsValid(shot)) return;
        if (_weaponEntityNames.TryGetValue(shot.Entity, out var entity) && World.Entities.TryGetValue(entity, out var ship))
            ShotReceived?.Invoke(shot, ship, shot.Tick);
        if (_stagedShotCount == ShotStagingCapacity) { ShotStagingDrops++; return; }
        _stagedShots[_stagedShotCount++] = shot;
        ShotStagingHighWater = Math.Max(ShotStagingHighWater, _stagedShotCount);
    }

    public void PublishShots()
    {
        if (!IsHost()) return;
        var recipients = _joins.Snapshot();
        var hasRecipient = false;
        foreach (var (_, join) in recipients)
            if (join.Phase == PeerJoinPhase.Ready) { hasRecipient = true; break; }
        if (!hasRecipient) { _stagedShotCount = 0; return; }
        for (var offset = 0; offset < _stagedShotCount; offset += ShotEventCodec.MaxEvents)
        {
            var count = Math.Min(ShotEventCodec.MaxEvents, _stagedShotCount - offset);
            var bytes = _shotPacketBuffers[count - 1];
            ShotEventCodec.EncodeInto(_stagedShots.AsSpan(offset, count), bytes);
            using Variant payloadValue = bytes;
            foreach (var (peer, join) in recipients)
                if (join.Phase == PeerJoinPhase.Ready && CanSendToPeer(peer))
                {
                    RpcId(peer, MethodName.ReceiveShots, (ReadOnlySpan<Variant>)[payloadValue]);
                    ShotBytesPublished += bytes.Length;
                }
        }
        _stagedShotCount = 0;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveShots(byte[] bytes)
    {
        if (!IsHostMessage() || _clientJoin.Phase != ClientJoinPhase.Ready || Synchronization == null ||
            !ShotEventCodec.TryDecode(bytes, _decodedShots, out var count)) return;
        var now = (long)Synchronization.HostEstimate;
        for (var i = 0; i < count; i++)
        {
            var shot = _decodedShots[i];
            if (!_shotSequences.Accept(shot.Session, shot.Item, shot.Sequence, shot.Tick, now)) continue;
            if (_pendingShotCount == PendingShotCapacity) { PendingShotDrops++; continue; }
            _pendingShots[_pendingShotCount++] = shot;
            PendingShotHighWater = Math.Max(PendingShotHighWater, _pendingShotCount);
        }
    }

    public void PresentShots(double localTick, double remoteTick)
    {
        for (var i = _pendingShotCount - 1; i >= 0; i--)
        {
            var shot = _pendingShots[i];
            ShipController? ship = null;
            if (_weaponEntityNames.TryGetValue(shot.Entity, out var entity)) World.Entities.TryGetValue(entity, out ship);
            var timeline = ship?.LocallyControlled == true ? localTick : remoteTick;
            var expired = shot.Tick <= WeaponBaselineCutoff || (ship == null ? localTick - shot.Tick > 30 : timeline - shot.Tick > 30);
            if (!expired && (ship == null || timeline < shot.Tick)) continue;
            _pendingShots[i] = _pendingShots[--_pendingShotCount];
            _pendingShots[_pendingShotCount] = default;
            if (!expired && ship != null && shot.Muzzle < ship.Runtime.WeaponFixturePoints.Count &&
                ship.ControllingPlayer?.ControlGeneration == shot.Generation)
                ShotReceived?.Invoke(shot, ship, timeline);
        }
    }

    public void ClearShotPresentation(Guid session, long cutoff)
    {
        if (_shotSession != session) { _weaponEntities.Clear(); _weaponEntityNames.Clear(); _nextWeaponEntity = 0; _hasWeaponMap = false; }
        _shotSession = session; WeaponBaselineCutoff = cutoff;
        _pendingShotCount = 0; _stagedShotCount = 0;
        Array.Clear(_pendingShots); Array.Clear(_stagedShots);
        _shotSequences.Clear(session, cutoff);
        ShotPresentationCleared?.Invoke(cutoff);
    }

    private void ClearWeapons()
    {
        ClearShotPresentation(Guid.Empty, -1);
        ClearWeaponStates();
        ShotReceived = null; ShotPresentationCleared = null;
    }
}
