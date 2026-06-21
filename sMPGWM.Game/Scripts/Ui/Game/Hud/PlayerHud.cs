using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Enums.Game;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game.Hud;

public partial class PlayerHud : Control
{
    private Control _statsAnchor = null!;
    private Control _radarAnchor = null!;
    private Control _hotbarAnchor = null!;
    
    private PlayerStatsPanel _statsPanel = null!;
    private RadarControl _radarControl = null!;
    private HotbarControl _hotbarControl = null!;

    public override void _Ready()
    {
        _statsAnchor = GetNode<Control>("%StatsAnchor");
        _radarAnchor = GetNode<Control>("%RadarAnchor");
        _hotbarAnchor = GetNode<Control>("%HotbarAnchor");
        _statsPanel = GetNode<PlayerStatsPanel>("%PlayerStatsPanel");
        _radarControl = GetNode<RadarControl>("%RadarControl");
        _hotbarControl = GetNode<HotbarControl>("%HotbarControl");

        GetViewport().SizeChanged += ApplyLayout;
        ApplyLayout();
        Logger.Info("PlayerHud loaded.");
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= ApplyLayout;
    }

    private void ApplyLayout()
    {
        // TODO implement later
    }
    
    public void Bind(LivingEntityState state, IReadOnlyCollection<StatTypes> visibleStats)
    {
        _statsPanel.Bind(state, visibleStats);
    }
}