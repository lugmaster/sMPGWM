using Godot;

namespace sMPGWM.Scripts.Movement;

public static class MovementPhysics
{
    private const float VelocityStopperSq = 5000f;
    private const float DragSpeedCoefficient = 0.005f;

    public static float RotationStep(
        float currentRotation,
        float targetRotationAngle,
        float rotationSpeedPerSecond,
        float rotationInertia,
        float delta,
        float angleEpsilon = 0.001f)
    {
        var rotationSpeedPerFrame = rotationSpeedPerSecond / rotationInertia * delta;

        var signedDeltaAngle = Mathf.PosMod(
            targetRotationAngle - currentRotation + Mathf.Pi,
            Mathf.Tau) - Mathf.Pi;

        var absDeltaAngle = Mathf.Abs(signedDeltaAngle);
        if (absDeltaAngle <= angleEpsilon)
            return currentRotation;

        var step = Mathf.Min(rotationSpeedPerFrame, absDeltaAngle);
        var signedStep = Mathf.Sign(signedDeltaAngle) * step;
        return currentRotation + signedStep;
    }

    public static Vector2 MovementStep(
        Vector2 currentVelocity,
        Vector2 inputDirection,
        Vector2 basisX,
        Vector2 basisY,
        float currentAcceleration,
        float maxSpeed,
        float currentFriction,
        float delta,
        bool enableFriction
    )
    {
        var velocity = currentVelocity;
        var accelerationDelta = currentAcceleration * delta;
        var maxSpeedSquared = maxSpeed * maxSpeed;

        var hasInput = inputDirection.LengthSquared() > 0.01f;

        float currentSpeedSquared;
        float currentSpeed;
        var speedComputed = false;

        if (hasInput)
        {
            var targetVelocity =
                (basisX * inputDirection.X + basisY * inputDirection.Y) * maxSpeed;

            velocity += (targetVelocity - velocity) * accelerationDelta;

            currentSpeedSquared = velocity.LengthSquared();
            speedComputed = true;
        }
        else
        {
            currentSpeedSquared = velocity.LengthSquared();

            if (currentSpeedSquared <= VelocityStopperSq)
                return Vector2.Zero;

            var startedAboveMaxSpeed = currentSpeedSquared > maxSpeedSquared;

            if (enableFriction || startedAboveMaxSpeed)
            {
                currentSpeed = Mathf.Sqrt(currentSpeedSquared);
                var deceleration = (currentFriction + DragSpeedCoefficient * currentSpeed) * delta;

                if (deceleration >= 1f)
                    return Vector2.Zero;

                velocity *= (1f - deceleration);

                currentSpeedSquared = velocity.LengthSquared();
                speedComputed = true;
            }
        }

        if (!speedComputed)
            currentSpeedSquared = velocity.LengthSquared();

        if (currentSpeedSquared > maxSpeedSquared)
        {
            currentSpeed = Mathf.Sqrt(currentSpeedSquared);
            var direction = velocity / currentSpeed;
            velocity = velocity.MoveToward(direction * maxSpeed, accelerationDelta);
        }

        return velocity;
    }
    
    public static float ComputeBrakeDistance(float speed, float friction, float drag, float stopperSpeed)
    {
        if (speed <= stopperSpeed) return 0f;
        if (drag > 0f)
            return (1f / drag) * Mathf.Log((friction + drag * speed) / (friction + drag * stopperSpeed));
        // drag == 0 → linear only
        return (speed - stopperSpeed) / friction;
    }

    public static float ComputeBrakeTime(float speed, float friction, float drag, float stopperSpeed)
    {
        if (speed <= stopperSpeed) return 0f;
        if (drag > 0f)
            return (1f / friction) * Mathf.Log( (speed * (friction + drag * stopperSpeed)) /
                                                (stopperSpeed * (friction + drag * speed)) );
        // drag == 0
        return (1f / friction) * Mathf.Log(speed / stopperSpeed);
    }

}