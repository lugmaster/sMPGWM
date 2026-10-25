using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;


namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class HotbarControl : PanelContainer
{
    private HBoxContainer _buttonContainer = null!;
    private bool _isProcessingInput;
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
    
    public void SetGameplayInputEnabled(bool enabled)
    {
        _isProcessingInput = enabled;
        var children = _buttonContainer.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is ClickableIcon child)
                child.Disabled = !_isProcessingInput;
        }

    }
    
}
