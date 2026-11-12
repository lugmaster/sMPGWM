using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public readonly record struct ShipInputState(Vector2 Direction, float AimAngle, bool HasAim, bool Afterburner, bool Brake, bool AutomaticBraking);
