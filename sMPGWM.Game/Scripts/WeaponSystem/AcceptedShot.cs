using System;
using Godot;

namespace sMPGWM.Scripts.WeaponSystem;

public readonly record struct AcceptedShot(Guid Session, long Tick, ulong Item, int Entity,
    long Generation, long Sequence, int Profile, int Muzzle, Vector2 Origin, Vector2 Endpoint,
    Vector2 Normal, bool Hit, bool ShieldContact);
