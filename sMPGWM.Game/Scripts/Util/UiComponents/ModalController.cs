using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class ModalController : AbstractSingleton<ModalController>
{
    private readonly PackedScene _alertScene = GD.Load<PackedScene>("res://scenes/ui/player_hud/alert_modal.tscn");

    public void ShowAlert(Texture2D titleIcon, string titleLabel, string message, Action onConfirmed)
    {
        // Own a canvas so alerts also work when the game's UiCanvas is unavailable (e.g. loading failed).
        // The layer is freed with the alert; keep this unless a shared, persistent UI host becomes useful.
        var layer = new CanvasLayer { Layer = 100, ProcessMode = ProcessModeEnum.Always };
        var alert = _alertScene.Instantiate<AlertModal>();
        alert.Configure(titleIcon, titleLabel, message);
        alert.OnMenuClosed += onConfirmed;
        alert.TreeExited += layer.QueueFree;
        AddChild(layer);
        layer.AddChild(alert);
    }

    public void ShowLoadinScreen()
    {
        // implement instantiating LoadingScreen in canvas from ModalController. This Canvas should be most top Ui Element that is possible, as it will per godot editor settings block all mouse input. 
        // the layer should be destroyed with the loading screen after it is finished. Caller of loadingScreen should provide a function that provides messages and percent value for progressbar. 
        // goal: well known loading screen that shows progress and one or more messages. 
    }
}
