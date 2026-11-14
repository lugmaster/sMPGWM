using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public sealed record ShieldGeometry(ShieldGeometryShape Shape, Vector2 Center, Vector2 Radii, float RotationDegrees);
