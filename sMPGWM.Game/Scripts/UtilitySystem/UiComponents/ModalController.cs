using System;
using Godot;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Scripts.UtilitySystem.UiComponents;

public partial class ModalController : AbstractSingleton<ModalController>
{
    private CanvasLayer? _loadingLayer;
    private LoadingScreen? _loadingScreen;
    private Func<(double Percent, string Message)>? _loadingProgress;
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
        ShowLoadingScreen(() => (0, "Loading game..."));
    }

    public void ShowLoadingScreen(Func<(double Percent, string Message)> progressProvider)
    {
        _loadingProgress = progressProvider;
        if (_loadingLayer != null) return;
        _loadingLayer = new CanvasLayer { Layer = int.MaxValue, ProcessMode = ProcessModeEnum.Always };
        _loadingScreen = GD.Load<PackedScene>("res://scenes/ui/loading_screen.tscn").Instantiate<LoadingScreen>();
        AddChild(_loadingLayer);
        _loadingLayer.AddChild(_loadingScreen);
        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);
        _Process(0);
    }

    public override void _Process(double delta)
    {
        if (_loadingScreen == null || _loadingProgress == null) return;
        var progress = _loadingProgress();
        _loadingScreen.UpdateProgress(progress.Percent, progress.Message);
    }

    public void HideLoadingScreen()
    {
        _loadingLayer?.Hide();
        _loadingLayer?.QueueFree();
        _loadingLayer = null;
        _loadingScreen = null;
        _loadingProgress = null;
        SetProcess(false);
    }
}
