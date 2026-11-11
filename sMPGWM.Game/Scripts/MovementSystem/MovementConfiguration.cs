using System;

namespace sMPGWM.Scripts.MovementSystem;

public readonly record struct MovementConfiguration(float Speed, float AfterBurnerSpeed, float RotationSpeed,
    float Acceleration, float AfterBurnerAcceleration, float Mass, bool Enabled)
{
    public bool IsValid => Valid(Speed) && Valid(AfterBurnerSpeed) && Valid(RotationSpeed) &&
        Valid(Acceleration) && Valid(AfterBurnerAcceleration) && Valid(Mass);

    private static bool Valid(float value) => float.IsFinite(value) && value >= 0;
}
