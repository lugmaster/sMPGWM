using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;


namespace sMPGWM.Scripts.Ui.Game.Hud;

public partial class HotbarControl : PanelContainer
{
    private HBoxContainer _buttonContainer = null!;
    private bool _isProcessingInput = true;
    public event Action<int>? SlotPressed;

    public override void _Ready()
    {
        _buttonContainer = GetNode<HBoxContainer>("%ButtonContainer");

        ClearButtons();

        Logger.Info("HotbarControl loaded.");
    }

    public void SetIcons(IReadOnlyList<IconDefinition> iconDefinitions)
    {
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
        foreach (var child in _buttonContainer.GetChildren().OfType<ClickableIcon>().ToList())
        {
            child.QueueFree();
        }
    }

    public void SetInputProcessing(bool isProcessing)
    {
        _isProcessingInput = isProcessing;
        ProcessMode = isProcessing ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        MouseFilter = isProcessing ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        foreach (var child in _buttonContainer.GetChildren().OfType<ClickableIcon>())
            child.Disabled = !isProcessing;
    }
}
