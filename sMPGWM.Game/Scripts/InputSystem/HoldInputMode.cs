using Godot;

namespace sMPGWM.Scripts.InputSystem;

public sealed class HoldInputMode(string action, bool initial = false) : IInputMode
{
    private readonly bool _initial = initial;
    private bool _isHeld = initial;
    private bool _waitingForRelease;

    public void Update() => Update(Input.IsActionPressed(action));

    public void Update(bool isPressed)
    {
        if (_waitingForRelease)
        {
            if (!isPressed) _waitingForRelease = false;
            return;
        }

        _isHeld = isPressed;
    }

    public bool IsActive() => _isHeld;

    public void ResetState()
    {
        _isHeld = _initial;
        _waitingForRelease = true;
    }
}
