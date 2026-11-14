using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.VfxSystem;

public partial class ShieldVisual : Node2D
{
    public const string ShaderPath = "res://assets/vfx/shield_outline.gdshader";
    public const float PulseDuration = .15f;
    private static readonly StringName PulseParameter = "pulse_strength";
    private ShaderMaterial? _material;
    private Rect2 _bounds;
    private float _remaining;

    public void Initialize(ShieldGeometry geometry)
    {
        if (_material != null) throw new InvalidOperationException("Shield visual is already initialized.");
        Position = geometry.Center;
        RotationDegrees = geometry.RotationDegrees;
        ZIndex = 1;
        var padding = new Vector2(10, 10);
        _bounds = new Rect2(-geometry.Radii - padding, (geometry.Radii + padding) * 2);
        _material = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(ShaderPath)
            ?? throw new InvalidOperationException("Shield outline shader is unavailable.") };
        _material.SetShaderParameter("radii", geometry.Radii);
        Material = _material;
        ResetPulse();
        QueueRedraw();
    }

    public void Pulse(float ageSeconds = 0)
    {
        if (_material == null || !float.IsFinite(ageSeconds)) return;
        _remaining = Math.Max(_remaining, PulseDuration - Math.Max(0, ageSeconds));
        if (_remaining <= 0) return;
        Visible = true;
        SetProcess(true);
        UpdateStrength();
    }

    public override void _Process(double delta)
    {
        _remaining = Math.Max(0, _remaining - (float)delta);
        if (_remaining == 0) ResetPulse();
        else UpdateStrength();
    }

    private void UpdateStrength()
    {
        var strength = _remaining / PulseDuration;
        _material?.SetShaderParameter(PulseParameter, strength * strength);
    }

    public void ResetPulse()
    {
        _remaining = 0;
        _material?.SetShaderParameter(PulseParameter, 0f);
        Visible = false;
        SetProcess(false);
    }

    public override void _Draw() => DrawRect(_bounds, Colors.White);

    public override void _ExitTree()
    {
        ResetPulse();
        Material = null;
        _material?.Dispose();
        _material = null;
    }
}
