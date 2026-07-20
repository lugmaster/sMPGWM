using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Enums.GameState;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Singletons.Autoload.Logger;


namespace sMPGWM.Scripts.Ui.Game.Hud;

public partial class HotbarControl : PanelContainer
{
    private HBoxContainer _buttonContainer = null!;
    private bool _isProcessingInput = true;
    public event Action<int>? SlotPressed;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(HotbarControl)}.{nameof(_EnterTree)} called.");
        _buttonContainer = GetNode<HBoxContainer>("%ButtonContainer");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(HotbarControl)}.{nameof(_Ready)} called.");
        ClearButtons();

        Logger.Debug($"{nameof(HotbarControl)}.{nameof(_Ready)} loaded.");
    }

    public void SetIcons(IReadOnlyList<IconDefinition> iconDefinitions)
    {
        Logger.Debug($"{nameof(HotbarControl)}.{nameof(SetIcons)} called with {nameof(iconDefinitions)} count={iconDefinitions.Count}.");
        ClearButtons();

        for (var i = 0; i < iconDefinitions.Count; i++)
        {
            var slotIndex = i;
            var icon = BaseFactory.CreateClickableIcon(iconDefinitions[i]);

            icon.Pressed += () => SlotPressed?.Invoke(slotIndex);
            icon.Disabled = !_isProcessingInput;

            _buttonContainer.AddChild(icon);
        }
    }

    private void ClearButtons()
    {
        var children = _buttonContainer.GetChildren();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is ClickableIcon child)
                child.QueueFree();
        }
    }
    
    public void ApplyGameState(MainGameState mainGameState)
    {
        Logger.Debug($"{nameof(HotbarControl)}.{nameof(ApplyGameState)} called with {nameof(mainGameState)}={mainGameState}.");
        switch (mainGameState)
        {
            case MainGameState.Active:
                ApplyActiveState();
                break;

            case MainGameState.InMenu:
            case MainGameState.Inactive:
                ApplyInactiveState();
                break;

            default:
                Logger.Error($"{nameof(HotbarControl)}.{nameof(ApplyGameState)} reached unexpected {nameof(mainGameState)}={mainGameState}.");
                throw new InvalidOperationException($"Unexpected game state: {mainGameState}");
        }

        var children = _buttonContainer.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is ClickableIcon child)
                child.Disabled = !_isProcessingInput;
        }

    }
    
    private void ApplyActiveState()
    {
        ProcessMode = ProcessModeEnum.Inherit;
        MouseFilter = MouseFilterEnum.Pass;
        _isProcessingInput = true;
    }

    private void ApplyInactiveState()
    {
        ProcessMode = ProcessModeEnum.Disabled;
        MouseFilter = MouseFilterEnum.Ignore;
        _isProcessingInput = false;
    }
}
