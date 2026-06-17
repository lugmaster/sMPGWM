using System;
using Godot;
using sMPGWM.Scripts.Autoload;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.InGame;

public partial class InGameOverlay : CanvasLayer
{
    // private ProgressBar _healthBar = null!;
    // private TextureRect _radarMock = null!;

    private Control _menuLayer = null!;
    private Control _menuHost = null!;
    
    public override void _Ready()
    {
        // ProcessMode = ProcessModeEnum.Always;

        // _healthBar = GetNode<ProgressBar>("%HealthBar");
        // _radarMock = GetNode<TextureRect>("%RadarMock");
        //
        // _healthBar.MinValue = 0;
        // _healthBar.MaxValue = 100;
        // _healthBar.Value = 75;
        //
        // _radarMock.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        // _radarMock.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        
        _menuLayer = GetNode<Control>("%MenuLayer");
        _menuHost = GetNode<Control>("%MenuHost");

        InGameScreenManager.Instance.Register(_menuLayer, _menuHost);

        Logger.Info("InGameOverlay loaded");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("open_menu"))
        {
            InGameScreenManager.Instance.TogglePauseMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("open_inventory"))
        {
            InGameScreenManager.Instance.ToggleInventory();
            GetViewport().SetInputAsHandled();
            return;
        }
        
        if (@event.IsActionPressed("ui_open_map"))
        {
            InGameScreenManager.Instance.ToggleMap();
            GetViewport().SetInputAsHandled();
            return;
        }
    }
}