using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.InputSystem;

public partial class InputController : AbstractSingleton<InputController>
{
    public Vector2 Direction { get; private set; } = Vector2.Zero;
    
    public bool ActionButtonPressed { get; private set; }
    public bool AlternateActionPressed { get; private set; }
    public bool MainMenuJustPressed { get; private set; }
    public bool InventoryMenuJustPressed { get; private set; }
    public bool MapMenuJustPressed { get; private set; }
    public bool AfterBurnerPressed { get; private set; }
    public bool TargetSwitchJustReleased { get; private set; }
    public bool TargetNearestSwitchJustReleased { get; private set; }
    public bool AutoBreakingEnabled { get; private set; }
    public bool BreakPressed { get; private set; }
    
    
    private readonly ToggleInputMode _enableAutoBreaking = new("EnableAutoBreaking", true);
    private readonly HoldInputMode _actionInput = new("Action");
    private readonly HoldInputMode _afterBurnerInput = new("AfterBurner");
    private readonly HoldInputMode _alternateActionInput = new("AlternateAction");
    private readonly HoldInputMode _breakInput = new("Break");
    private readonly List<IInputMode> _inputModes = [];
    
    private bool _gameplayInputEnabled;
    private bool _wasInputAvailable;
    private Func<bool> _isPointerOverUi = () => false;

    public bool IsGameplayInputAvailable => _gameplayInputEnabled && IsWindowFocused;

    public void SetPointerInputFilter(Func<bool> isPointerOverUi) => _isPointerOverUi = isPointerOverUi;

    protected override void OnReady()
    {
        Logger.Debug(
            $"{nameof(InputController)}.{nameof(OnReady)} setting up for authority {GetMultiplayerAuthority()} on peer {Multiplayer.GetUniqueId()}.");
        _inputModes.Add(_enableAutoBreaking);
        _inputModes.Add(_actionInput);
        _inputModes.Add(_alternateActionInput);
        _inputModes.Add(_afterBurnerInput);
        _inputModes.Add(_breakInput);
        IsWindowFocused = DisplayServer.WindowIsFocused();
        ResetInputState();
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        _gameplayInputEnabled = enabled;
        if (!enabled)
        {
            ResetInputState();
            _wasInputAvailable = false;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsGameplayInputAvailable)
        {
            if (_wasInputAvailable) ResetInputState();
            _wasInputAvailable = false;
            return;
        }

        _wasInputAvailable = true;
        var pointerBlocked = _isPointerOverUi();
        for (var index = 0; index < _inputModes.Count; index++)
        {
            if (pointerBlocked && (_inputModes[index] == _actionInput || _inputModes[index] == _alternateActionInput))
            {
                _inputModes[index].ResetState();
                continue;
            }
            _inputModes[index].Update();
        }

        Direction = Input.GetVector("Backwards", "Forwards", "StrafeLeft", "StrafeRight");
        UpdateInputs();
        DetectTargetSwitchPressed();
    }

    private void UpdateInputs()
    {
        ActionButtonPressed = _actionInput.IsActive();
        AutoBreakingEnabled = _enableAutoBreaking.IsActive();
        AlternateActionPressed = _alternateActionInput.IsActive();
        AfterBurnerPressed = _afterBurnerInput.IsActive();
        BreakPressed = _breakInput.IsActive();
    }

    // target nearest switch relies on shift + key input. 
    // with this implementation it worked. otherwise most of the time only target switch was triggered. 
    private void DetectTargetSwitchPressed()
    {
        if (Input.IsActionJustReleased("TargetNearestSwitch"))
        {
            TargetNearestSwitchJustReleased = true;
            TargetSwitchJustReleased = false;
            return;
        }

        if (Input.IsActionJustReleased("TargetSwitch"))
        {
            TargetNearestSwitchJustReleased = false;
            TargetSwitchJustReleased = true;
            return;
        }

        TargetNearestSwitchJustReleased = false;
        TargetSwitchJustReleased = false;
    }

    private void ResetInputState()
    {
        for (var i = 0; i < _inputModes.Count; i++)
            _inputModes[i].ResetState();

        Direction = Vector2.Zero;
        TargetSwitchJustReleased = false;
        TargetNearestSwitchJustReleased = false;
        UpdateInputs();
    }
    
    public static bool IsWindowFocused { get; private set; } = true;


    public override void _Notification(int what)
    {
        const int notificationWmFocusIn = 1004;
        const int notificationWmFocusOut = 1005;

        if (what is notificationWmFocusIn or notificationWmFocusOut)
        {
            IsWindowFocused = DisplayServer.WindowIsFocused();
            if (!IsWindowFocused)
            {
                ResetInputState();
                _wasInputAvailable = false;
            }
        }
    }
}
