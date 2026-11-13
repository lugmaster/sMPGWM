using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public readonly record struct WeaponStateSnapshot(ItemEntityId ItemId, long Revision, int Rounds,
    double NextFireTick, long ReloadStartTick, long ReloadDeadline, ulong ShotSequence);
