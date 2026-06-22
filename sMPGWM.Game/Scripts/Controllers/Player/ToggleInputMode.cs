using Godot;

namespace sMPGWM.Scripts.Controllers.Player;

public sealed class ToggleInputMode(string action, bool initial = false) : IInputMode
{
    private readonly bool _initial = initial;
    private bool _enabled = initial;

    public void Update()
    {
        if (Input.IsActionJustPressed(action))
            _enabled = !_enabled;
    }

    public bool IsActive() => _enabled;

    public void ResetState() => _enabled = _initial;
}