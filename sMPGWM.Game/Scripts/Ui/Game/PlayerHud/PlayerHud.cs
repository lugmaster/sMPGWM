using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game.PlayerHud;

public partial class PlayerHud : Control
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Logger.Info("PlayerHud loaded.");
    }
}