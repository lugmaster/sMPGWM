using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.Singletons;
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
    private readonly ToggleInputMode _showCombatMetricsInputMode = new("ShowCombatMetrics");
    private readonly List<IInputMode> _inputModes = [];
    
    private bool _resetApplied;

    private bool _wasTargetNearestSwitchPressed;

    private MainGameState _mainGameState = MainGameState.Inactive;

    protected override void OnReady()
    {
        Logger.Debug(
            $"{nameof(InputController)}.{nameof(OnReady)} setting up for authority {GetMultiplayerAuthority()} on peer {Multiplayer.GetUniqueId()}.");
        _inputModes.Add(_enableAutoBreaking);
        _inputModes.Add(_actionInput);
        _inputModes.Add(_alternateActionInput);
        _inputModes.Add(_afterBurnerInput);
        _inputModes.Add(_breakInput);
        _inputModes.Add(_showCombatMetricsInputMode);
    }

    public void ApplyUiState(MainGameState mainGameState)
    {
        Logger.Debug($"{nameof(InputController)}.{nameof(ApplyUiState)} called with {nameof(mainGameState)}={mainGameState}.");
        _mainGameState = mainGameState;
    }

    public override void _Process(double delta)
    {
        // skip all inputs if not inside game window
        if (GlobalStateManager.IsWindowFocused)
        {
            // leave this as it is. will be later enabled for stable builds
            // Input.MouseMode = Input.MouseModeEnum.Confined;
        }
        else
        {
            if (!_resetApplied)
            {
                ResetInputState();
            }
            return;
        }
        if (_mainGameState == MainGameState.Active)
        {
            for (var i = 0; i < _inputModes.Count; i++)
                _inputModes[i].Update();

            Direction = Input.GetVector("Backwards", "Forwards", "StrafeLeft", "StrafeRight");
            UpdateInputs();
            DetectTargetSwitchPressed();
        }
        else
        {
            if (!_resetApplied)
            {
                ResetInputState();
            }
        }
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
        _resetApplied = true;
    }
}
