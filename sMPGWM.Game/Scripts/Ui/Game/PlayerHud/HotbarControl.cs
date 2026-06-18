using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;


namespace sMPGWM.Scripts.Ui.Game.PlayerHud;

public partial class HotbarControl : PanelContainer
{
    private const int HotbarSlotCount = 10;

    private HBoxContainer _buttonContainer = null!;

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
            var displayNumber = slotIndex == 9 ? "0" : (slotIndex + 1).ToString();

            var button = new Button
            {
                Text = displayNumber,
                CustomMinimumSize = new Vector2(56, 56),
                FocusMode = FocusModeEnum.None
            };

            button.Pressed += () => OnHotbarButtonPressed(slotIndex);

            _buttonContainer.AddChild(button);
        }
    }

    private static void OnHotbarButtonPressed(int slotIndex)
    {
        Logger.Info($"Hotbar button clicked. SlotIndex: {slotIndex}");
    }
}