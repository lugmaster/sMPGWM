using System;
using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Constants;
using FlatEarthUniverse.scripts.Network;
using Godot;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

[Serializable]
public readonly struct MovementState(
    Vector2 position,
    Vector2 velocity,
    float rotation,
    float delta,
    Vector2 inputDirection,
    float targetRotationAngle,
    bool afterBurnerPressed,
    bool enableFriction,
    long tickNumber,
    long stateId) : IEquatable<MovementState>
{
    public Vector2 Position { get; init; } = position;
    public Vector2 Velocity { get; init; } = velocity;
    public float Rotation { get; init; } = rotation;
    public float Delta { get; init; } = delta;
    public Vector2 InputDirection { get; init; } = inputDirection;
    public float TargetRotationAngle { get; init; } = targetRotationAngle;
    public bool AfterBurnerPressed { get; init; } = afterBurnerPressed;
    public bool EnableFriction { get; init; } = enableFriction;
    public long TickNumber { get; init; } = tickNumber;
    public long StateId { get; init; } = stateId;
    //Thresholds are squared already!!! 
    private const float DesyncPositionThreshold = 144f; // 12
    private const float DesyncRotationThreshold = 49f; // 7
    private const float DesyncVelocityThreshold = 250f; // 50
    private const float DesyncPositionSnapLimit = 4096f; // 64
    private const float DesyncRotationSnapLimit = 144f; // 12
    private const float DesyncVelocitySnapLimit = 10000f; // 100 
    private const float SoftLerpFactor = 0.35f;

    private const int Loglevel = -1;

    public static MovementState WithPositionVelocityRotation(Vector2 position, Vector2 velocity, float rotation,
        MovementState movementState)
    {
        return new MovementState(position, velocity, rotation, movementState.Delta, movementState.InputDirection, movementState.TargetRotationAngle,
            movementState.AfterBurnerPressed, movementState.EnableFriction, movementState.TickNumber,  movementState.StateId);
    }
    
    public override bool Equals(object obj)
    {
        return obj is MovementState other && Equals(other);
    }

    public bool Equals(MovementState other) =>
        Position.IsEqualApprox(other.Position) &&
        Velocity.IsEqualApprox(other.Velocity) &&
        Mathf.IsEqualApprox(Rotation, other.Rotation) &&
        InputDirection.IsEqualApprox(other.InputDirection) &&
        EnableFriction == other.EnableFriction &&
        Mathf.IsEqualApprox(TargetRotationAngle, other.TargetRotationAngle);
    
    public override int GetHashCode() => HashCode.Combine(Position, Velocity, Rotation, InputDirection, TargetRotationAngle);

    public override string ToString() =>
        $"Pos:{Position}, Vel:{Velocity}, Rot:{Rotation}, Del:{Delta}, Dir:{InputDirection}, Angle:{TargetRotationAngle}, AfterBurnerPressed:{AfterBurnerPressed}, EnableFriction:{EnableFriction} Tick:{TickNumber}, StateId:{StateId}";

    public static bool operator ==(MovementState left, MovementState right) => left.Equals(right);
    public static bool operator !=(MovementState left, MovementState right) => !left.Equals(right);


    public GCol.Array ToVariant()
    {
        return new GCol.Array
        {
            Position,
            Velocity,
            Rotation,
            Delta,
            InputDirection,
            TargetRotationAngle,
            AfterBurnerPressed,
            EnableFriction,
            TickNumber,
            StateId
        };
    }

    public static MovementState ToMovementState(GCol.Array convertedState)
    {
        return new MovementState(
            convertedState[0].AsVector2(), //position
            convertedState[1].AsVector2(), // velocity
            (float)convertedState[2].AsDouble(), // rotation
            (float)convertedState[3].AsDouble(), // delta
            convertedState[4].AsVector2(), // inputDirection
            (float)convertedState[5].AsDouble(), //targetRotationAngle
            convertedState[6].AsBool(), // afterBurnerPressed
            convertedState[7].AsBool(), // enableFriction
            convertedState[8].AsInt64(), // tickNumber
            convertedState[9].AsInt64() // stateId
        );
    }

    public MovementStateDesync IsRotationDesync(MovementState other)
    {
        if (Mathf.IsEqualApprox(Rotation, other.Rotation) ||
            Math.Abs(Rotation - other.Rotation) < DesyncRotationThreshold)
        {
            return MovementStateDesync.None;
        }

        return Math.Abs(Rotation - other.Rotation) < DesyncRotationSnapLimit
            ? MovementStateDesync.Minor
            : MovementStateDesync.Major;
    }

    public MovementStateDesync IsVelocityDesync(MovementState other)
    {
        if (Velocity.IsEqualApprox(other.Velocity) ||
            Velocity.DistanceSquaredTo(other.Velocity) < DesyncVelocityThreshold)
        {
            return MovementStateDesync.None;
        }

        return Velocity.DistanceSquaredTo(other.Velocity) < DesyncVelocitySnapLimit
            ? MovementStateDesync.Minor
            : MovementStateDesync.Major;
    }

    public MovementStateDesync IsPositionDesync(MovementState other)
    {
        if (Position.IsEqualApprox(other.Position))
            return MovementStateDesync.None;

        var distSquared = Position.DistanceSquaredTo(other.Position);

        if (distSquared < DesyncPositionThreshold)
            return MovementStateDesync.None;

        return distSquared < DesyncPositionSnapLimit
            ? MovementStateDesync.Minor
            : MovementStateDesync.Major;
    }

    public bool IsDesync(MovementState other)
    {
        return IsRotationDesync(other).IsDesync() || IsVelocityDesync(other).IsDesync() ||
               IsPositionDesync(other).IsDesync();
    }


    public static Vector2 LerpVector2(Vector2 from, Vector2 to)
    {
        return from.Lerp(to, SoftLerpFactor);
    }

    public static float LerpFloat(float from, float to)
    {
        return Mathf.LerpAngle(from, to, SoftLerpFactor);
    }

    public MovementState CorrectStateTo(MovementState to)
    {
        Vector2 position;
        Vector2 velocity;
        float rotation;
        switch (IsPositionDesync(to))
        {
            case MovementStateDesync.Major:
                Logger.Log($"Major Desync in position: from {Position} to {to.Position} at {NetworkTicker.GetLogInfo()}", Loglevel);
                position = to.Position;
                break;
            case MovementStateDesync.Minor:
                Logger.Log($"Minor Desync (Lerping) in position: from {Position} to {to.Position} at {NetworkTicker.GetLogInfo()}", Loglevel);
                position = LerpVector2(Position, to.Position);
                break;
            case MovementStateDesync.None:
                Logger.Log($"NO Desync in position: from {Position} to {to.Position} at {NetworkTicker.GetLogInfo()}", Loglevel);
                position = Position;
                break;
            default:
                Logger.Error("Unknown desync state");
                position = Position;
                break;
        }

        switch (IsVelocityDesync(to))
        {
            case MovementStateDesync.Major:
                Logger.Log($"Major Desync in velocity: from {Velocity} to {to.Velocity} at {NetworkTicker.GetLogInfo()}", Loglevel);
                velocity = to.Velocity;
                break;
            case MovementStateDesync.Minor:
                Logger.Log($"Minor Desync (Lerping) in velocity: from {Velocity} to {to.Velocity} at {NetworkTicker.GetLogInfo()}", Loglevel);
                velocity = LerpVector2(Velocity, to.Velocity);
                break;
            case MovementStateDesync.None:
                Logger.Log($"NO Desync in velocity: from {Velocity} to {to.Velocity} at {NetworkTicker.GetLogInfo()}", Loglevel);
                velocity = Velocity;
                break;
            default:
                Logger.Error("Unknown desync state");
                velocity = Velocity;
                break;
        }

        switch (IsRotationDesync(to))
        {
            case MovementStateDesync.Major:
                Logger.Log($"Major Desync in rotation: from {Rotation} to {to.Rotation} at {NetworkTicker.GetLogInfo()}", Loglevel);
                rotation = to.Rotation;
                break;
            case MovementStateDesync.Minor:
                Logger.Log($"Minor Desync (Lerping) in rotation: from {Rotation} to {to.Rotation} at {NetworkTicker.GetLogInfo()}", Loglevel);
                rotation = LerpFloat(Rotation, to.Rotation);
                break;
            case MovementStateDesync.None:
                Logger.Log($"NO in rotation: from {Rotation} to {to.Rotation} at {NetworkTicker.GetLogInfo()}", Loglevel);
                rotation = Rotation;
                break;
            default:
                Logger.Error("Unknown desync state");
                rotation = Rotation;
                break;
        }


        return WithPositionVelocityRotation(
            position,
            velocity,
            rotation,
            this);
    }
}