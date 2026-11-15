namespace sMPGWM.Scripts.WeaponSystem;

public readonly record struct PendingReload(int Peer, long Request, string Entity, long Generation,
    long EquipmentRevision, ulong[] Items);
