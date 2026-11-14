using Godot;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public readonly record struct AcceptedShot(long Tick, ItemEntityId ItemId, ulong Sequence, string EntityId,
    long ControlGeneration, string ProfileId, byte MuzzleIndex, Vector2 Origin, Vector2 End,
    Vector2 Normal, ShotResultFlags Flags, string TargetEntityId = "");
