using System.Collections.Generic;
using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Network;
using Godot;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public class MovementStateTracker
{
    private Queue<MovementState> _pastStates = new(30);
    private MovementState _current, _lastSent, _lastReceived;
    private const int LogLevel = -1;
    private long _generatedStateId;

    public MovementStateTracker(Vector2 position, Vector2 velocity, float rotation, float delta, Vector2 inputDirection, float targetRotationAngle, bool afterBurnerPressed, bool enableFriction, long tick)
    {
        var state = new MovementState(position, velocity, rotation, delta, inputDirection, targetRotationAngle, afterBurnerPressed, enableFriction, tick, _generatedStateId++);
        _lastReceived = _lastSent = _current = state;
    }

    // --- Public Update Methods ---

    public void UpdateCurrentState(MovementState state)
    {
        Logger.Log($"Enqueueing current state: {state} at {NetworkTicker.GetLogInfo()}", LogLevel);
        _pastStates.Enqueue(_current);
        while (_pastStates.Count > 30) _pastStates.Dequeue();
        _current = state;
    }

    public void UpdateLastStateReceived(MovementState movementState) => _lastReceived = movementState;

    public void UpdateLastStateSentFromCurrent() => _lastSent = _current;

    public void UpdateCurrentStateFromLastReceived(long tick)
    {
        _current = new MovementState(
            _lastReceived.Position,
            _lastReceived.Velocity,
            _lastReceived.Rotation,
            _lastReceived.Delta,
            _lastReceived.InputDirection,
            _lastReceived.TargetRotationAngle,
            _lastReceived.AfterBurnerPressed,
            _lastReceived.EnableFriction,
            tick,
            _lastReceived.StateId);
    }

    public void ReApplyCorrectedStates(Queue<MovementState> correctedStates)
    {
        _pastStates = correctedStates;
    }

    public void ClearState() => _pastStates.Clear();

    // --- State Query Methods ---

    public List<MovementState> GetStatesToCorrect()
    {
        Logger.Log(
            $"States before Collection ({_pastStates.Count}) since last Correction:\n{string.Join(" ,\n", _pastStates)} \nat {NetworkTicker.GetLogInfo()}",
            LogLevel);

        var result = new List<MovementState>(_pastStates.Count);
        var desyncFound = false;

        while (_pastStates.TryPeek(out var pastState))
        {
            if (pastState.StateId < _lastReceived.StateId)
            {
                _pastStates.Dequeue();
                continue;
            }

            if (pastState.StateId == _lastReceived.StateId)
            {
                if (pastState.IsDesync(_lastReceived))
                {
                    desyncFound = true;
                    result.Add(_lastReceived);
                }

                _pastStates.Dequeue();
            }

            break;
        }

        if (desyncFound)
        {
            while (_pastStates.TryDequeue(out var rest))
            {
                result.Add(rest);
            }
        }
        Logger.Log(
            $"Collected states ({result.Count}) for Correction:\n{string.Join(" ,\n", result)} \nat {NetworkTicker.GetLogInfo()}",
            LogLevel);
        return result;
    }

    public bool HasLastStateSentChangedToCurrent() => _lastSent != _current;

    public bool HasLastStateReceivedChangedToCurrent() => _lastReceived != _current;

    public bool ShouldApplySnap() =>
        _pastStates.Count == 0 || Mathf.Abs(_lastReceived.TickNumber - _pastStates.Peek().TickNumber) > 30;

    // --- State Accessors ---

    public MovementState GetCurrentState() => _current;

    public MovementState GetLastStateReceived() => _lastReceived;

    // --- Utility ---

    public long GenerateStateId() => _generatedStateId++;

    public static MovementState CreateStateSnapShot(
        MpController mpController,
        Vector2 inputDirection,
        float targetRotationAngle,
        bool afterBurnerPressed = false,
        bool enableFriction = false,
        float delta = -1f,
        long tickNumber = -1,
        long stateId = -1)
    {
        return new MovementState(
            mpController.GlobalPosition,
            mpController.Velocity,
            mpController.Rotation,
            delta,
            inputDirection,
            targetRotationAngle,
            afterBurnerPressed,
            enableFriction,
            tickNumber,
            stateId);
    }

    public void ApplyStateSnapShot(MpController mpController, MovementState state)
    {
        mpController.GlobalPosition = state.Position;
        mpController.Velocity = state.Velocity;
        mpController.Rotation = state.Rotation;
    }
    
    public void ReinitializeForRespawn(Vector2 position, Vector2 velocity, float rotation, float delta, Vector2 inputDirection, float targetRotationAngle, bool afterBurnerPressed, bool enableFriction, long tick)
    {
        ClearState();
        var state = new MovementState(position, velocity, rotation, delta, inputDirection, targetRotationAngle, afterBurnerPressed, enableFriction, tick, _generatedStateId);
        _current = state;
    }
}
