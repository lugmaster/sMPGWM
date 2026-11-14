using System;
using Godot;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly ShieldCollisionEvent[] _shieldCollisionEvents = new ShieldCollisionEvent[ShieldCollisionEventCodec.MaxEvents];
    private int _shieldCollisionEventCount;

    private void RecordShieldCollision(string entity, long tick)
    {
        var contact = new ShieldCollisionEvent(tick, entity);
        PresentShieldCollision(contact);
        for (var i = 0; i < _shieldCollisionEventCount; i++)
        {
            if (_shieldCollisionEvents[i].EntityId != entity) continue;
            _shieldCollisionEvents[i] = contact;
            return;
        }
        if (_shieldCollisionEventCount < _shieldCollisionEvents.Length)
            _shieldCollisionEvents[_shieldCollisionEventCount++] = contact;
    }

    private void PublishShieldCollisions()
    {
        if (_shieldCollisionEventCount == 0) return;
        var bytes = ShieldCollisionEventCodec.Encode(Synchronization!.Session, _shieldCollisionEvents.AsSpan(0, _shieldCollisionEventCount));
        foreach (var (peer, join) in _joins.Snapshot())
            if (join.Phase == PeerJoinPhase.Ready) RpcId(peer, nameof(ReceiveShieldCollisions), bytes);
        _shieldCollisionEventCount = 0;
        Array.Clear(_shieldCollisionEvents);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveShieldCollisions(byte[] bytes)
    {
        if (!IsHostMessage() || !WeaponBaselineReady ||
            !ShieldCollisionEventCodec.Decode(bytes, out var session, out var contacts) || session != Synchronization!.Session) return;
        foreach (var contact in contacts)
        {
            if (contact.Tick <= _shotCutoff || contact.Tick < Synchronization.CompletedTick - 120 ||
                contact.Tick > Synchronization.CompletedTick + 120) continue;
            PresentShieldCollision(contact);
        }
    }
}
