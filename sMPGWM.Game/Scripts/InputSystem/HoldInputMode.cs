using Godot;

namespace sMPGWM.Scripts.InputSystem;

public sealed class HoldInputMode(string action, bool initial = false) : IInputMode
{
    private readonly bool _initial = initial;
    private bool _isHeld = initial;

    public void Update()
    {
        if (_isHeld && Input.IsActionJustReleased(action))
            _isHeld = false;
        if (!_isHeld && Input.IsActionPressed(action))
            _isHeld = true;
    }

    public bool IsActive() => _isHeld;

    public void ResetState() => _isHeld = _initial;
}