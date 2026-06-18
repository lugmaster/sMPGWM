using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game.PlayerHud;

public partial class PlayerStatsPanel : PanelContainer
{
    private ProgressBar _healthBar = null!;
    private ProgressBar _shieldBar = null!;
    private ProgressBar _energyBar = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _healthBar = GetNode<ProgressBar>("%HealthBar");
        _shieldBar = GetNode<ProgressBar>("%ShieldBar");
        _energyBar = GetNode<ProgressBar>("%EnergyBar");

        SetMockValues();

        Logger.Info("PlayerStatsPanel loaded.");
    }

    private void SetMockValues()
    {
        SetupBar(_healthBar, "Health", 75);
        SetupBar(_shieldBar, "Shield", 50);
        SetupBar(_energyBar, "Energy", 90);
    }

    private static void SetupBar(ProgressBar bar, string label, double value)
    {
        bar.MinValue = 0;
        bar.MaxValue = 100;
        bar.Value = value;
        bar.ShowPercentage = false;
        bar.TooltipText = label;
    }
}