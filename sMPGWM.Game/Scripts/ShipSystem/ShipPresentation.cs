using System;
using Godot;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.ShipSystem;

/// <summary>Owns visual transforms and correction offsets. Camera policy belongs to PlayerController.</summary>
public sealed class ShipPresentation(ShipController ship)
{
    private Node2D? _visuals;
    private ShieldVisual? _shieldVisual;
    private Node2D Visuals => _visuals ??= ship.GetNode<Node2D>("ShipVisuals");
    private ShieldVisual? Shield => _shieldVisual ??= ship.GetNodeOrNull<ShieldVisual>(ShieldVisualPath);
    private Vector2 _offset;
    private float _angleOffset;
    private static readonly NodePath ShieldVisualPath = new("ShipVisuals/ShieldVisual");
    private long _lastShieldCollisionTick = -1;
    internal void PulseShieldCollision(long tick, float ageSeconds)
    {
        if (tick <= _lastShieldCollisionTick) return;
        _lastShieldCollisionTick = tick;
        PulseShieldHit(ageSeconds);
    }
    internal void PulseShieldHit(float ageSeconds) => Shield?.Pulse(ageSeconds);
    internal void ResetShieldPulse(long cutoffTick)
    {
        _lastShieldCollisionTick = cutoffTick;
        Shield?.ResetPulse();
    }
    public Vector2 Position => Visuals.GlobalPosition;
    public float Rotation => Visuals.GlobalRotation;
    public byte MovementFlags { get; set; }
    public event Action<Vector2>? Presented;
    public event Action? Snapped;
    public void Present(Vector2 position, float rotation)
    {
        Visuals.GlobalPosition = position; Visuals.GlobalRotation = rotation;
        Presented?.Invoke(position);
    }
    public void Snap()
    {
        _offset = Vector2.Zero; _angleOffset = 0;
        MovementFlags = ship.AppliedMovementFlags;
        Present(ship.GlobalPosition, ship.GlobalRotation);
        Snapped?.Invoke();
    }
    public void CorrectFrom(Vector2 displayed, float angle)
    {
        _offset = displayed - ship.GlobalPosition;
        _angleOffset = Mathf.AngleDifference(ship.GlobalRotation, angle);
        if (_offset.Length() > 100) { Snap(); return; }
        Present(displayed, angle);
    }
    public void RenderLocal(double delta)
    {
        float decay = (float)Math.Exp(-delta / .08);
        _offset *= decay; _angleOffset *= decay;
        MovementFlags = ship.AppliedMovementFlags;
        Present(ship.GlobalPosition + _offset, ship.GlobalRotation + _angleOffset);
    }
}
