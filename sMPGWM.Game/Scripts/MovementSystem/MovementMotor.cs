using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public sealed class MovementMotor
{
    private MovementConfiguration _configuration;
    private float _rotationInertia;
    private float _friction;

    public void Configure(MovementConfiguration configuration)
    {
        if (!configuration.IsValid) throw new ArgumentException("Invalid movement configuration.");
        _configuration = configuration;
        _rotationInertia = (float)Math.Min(float.MaxValue, 1d + configuration.Mass * 1.05d);
        _friction = (float)(0.15d / (1d + configuration.Mass * 0.95d));
    }

    public float Rotate(float rotation, ShipInputState input, float delta) => _configuration.Enabled && input.HasAim
        ? MovementPhysics.RotationStep(rotation, input.AimAngle, _configuration.RotationSpeed, _rotationInertia, delta)
        : rotation;

    public Vector2 Drive(Vector2 velocity, Vector2 basisX, Vector2 basisY, ShipInputState input, float delta)
    {
        if (!_configuration.Enabled) return velocity;
        var acceleration = input.Afterburner ? _configuration.AfterBurnerAcceleration : _configuration.Acceleration;
        return MovementPhysics.MovementStep(velocity, input.Brake ? Vector2.Zero : input.Direction,
            basisX, basisY, acceleration, input.Afterburner ? _configuration.AfterBurnerSpeed : _configuration.Speed,
            _friction, delta, input.Brake || input.AutomaticBraking);
    }
}
