using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.CollisionSystem;

public readonly record struct HitscanContact(bool Hit, Vector2 Position, Vector2 Normal, ShipController? Target);
