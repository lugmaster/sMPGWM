using System.Collections.Generic;
using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;


namespace sMPGWM.Scripts.Ui.Game.Hud;

public partial class HotbarControl : PanelContainer
{
    private const int HotbarSlotCount = 10;

    private HBoxContainer _buttonContainer = null!;
    private readonly List<Button> _buttons = new();

    public override void _Ready()
    {
        _buttonContainer = GetNode<HBoxContainer>("%ButtonContainer");

        CreateButtons();

        Logger.Info("HotbarControl loaded.");
    }

    private void CreateButtons()
    {
        for (var i = 0; i < HotbarSlotCount; i++)
        {
            var slotIndex = i;

            var button = new Button
            {
                Text = slotIndex == 9 ? "0" : (slotIndex + 1).ToString(),
                FocusMode = FocusModeEnum.None,
                // SizeFlagsHorizontal = SizeFlags.ExpandFill,
                // SizeFlagsVertical = SizeFlags.ExpandFill
            };

            button.Pressed += () => OnHotbarButtonPressed(slotIndex);

            _buttons.Add(button);
            _buttonContainer.AddChild(button);
        }
    }

    private static void OnHotbarButtonPressed(int slotIndex)
    {
        Logger.Info($"Hotbar button clicked. SlotIndex: {slotIndex}");
    }
}