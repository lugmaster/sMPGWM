using Godot;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerStatsPanel : PanelContainer
{
    // private readonly Dictionary<StatType, StatBar> _bars = new();

    private VBoxContainer _barContainer = null!;
    // private LivingEntityState _livingEntityState = null!;
    private bool _isProcessingInput = true;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(PlayerStatsPanel)}.{nameof(_EnterTree)} called.");
        _barContainer = GetNode<VBoxContainer>("%BarContainer");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(PlayerStatsPanel)}.{nameof(_Ready)} called.");
        Logger.Debug($"{nameof(PlayerStatsPanel)}.{nameof(_Ready)} loaded.");
    }

    // public void Bind(Object state, //LivingEntityState 
    //     IReadOnlyCollection<StatType> visibleStats)
    // {
    //     Logger.Debug($"{nameof(PlayerStatsPanel)}.{nameof(Bind)} called.");
    //     if (state == null)
    //     {
    //         Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(Bind)} received null {nameof(state)}.");
    //         throw new ArgumentNullException(nameof(state));
    //     }
    //
    //     // _livingEntityState = state;
    //
    //     if (visibleStats == null)
    //     {
    //         Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(Bind)} received null {nameof(visibleStats)}.");
    //         throw new ArgumentNullException(nameof(visibleStats));
    //     }
    //
    //     if (visibleStats.Count == 0)
    //     {
    //         Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(Bind)} received empty {nameof(visibleStats)}.");
    //         throw new ArgumentException("At least one visible stat is required.", nameof(visibleStats));
    //     }
    //
    //     RebuildBars(visibleStats);
    //     RefreshAllBars();
    // }
    //
    // private void RebuildBars(IReadOnlyCollection<StatType> visibleStats)
    // {
    //     ClearBars();
    //
    //     foreach (var type in visibleStats)
    //     {
    //         var statBar = HudProvider.CreateStatBar();
    //
    //         _barContainer.AddChild(statBar);
    //
    //         statBar.Configure(type, GetTitle(type));
    //
    //         if (!_bars.TryAdd(type, statBar))
    //         {
    //             Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(RebuildBars)} reached duplicate {nameof(StatType)}={type}.");
    //             throw new InvalidOperationException($"Duplicate StatBar type: {type}");
    //         }
    //     }
    // }

    private void ClearBars()
    {
        // _bars.Clear();

        var children = _barContainer.GetChildren();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is not Node child)
            {
                Logger.Warning($"{nameof(PlayerStatsPanel)}.{nameof(ClearBars)} found a non-node child entry.");
                continue;
            }

            _barContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    // private void RefreshAllBars()
    // {
    //     foreach (var type in _bars.Keys)
    //         RefreshBar(type);
    // }

    // private void RefreshBar(StatType type)
    // {
    //     // var stat = _livingEntityState.GetStatByType(type);
    //     // GetBar(type).Setup(stat.CurrentValue, stat.MaxValue);
    // }
    //
    // public void SetBarValue(StatType type, double value, double maxValue)
    // {
    //     GetBar(type).Setup(value, maxValue);
    // }
    //
    // private StatBar GetBar(StatType type)
    // {
    //     if (!_bars.TryGetValue(type, out var bar))
    //     {
    //         Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(GetBar)} reached missing {nameof(StatType)}={type}.");
    //         throw new InvalidOperationException($"Missing StatBar type: {type}");
    //     }
    //
    //     return bar;
    // }
    //
    // private static string GetTitle(StatType type)
    // {
    //     return type switch
    //     {
    //         StatType.Health => "Health",
    //         StatType.Shield => "Shield",
    //         StatType.EnergyStorage => "Energy",
    //         StatType.Armor => "Armor",
    //         StatType.HealthRegeneration => "Health Regen",
    //         StatType.ShieldRegeneration => "Shield Regen",
    //         StatType.EnergyProduction => "Energy Production",
    //         StatType.EnergyConsumption => "Energy Consumption",
    //         StatType.Speed => "Speed",
    //         StatType.Acceleration => "Acceleration",
    //         StatType.OxygenStorage => "Oxygen",
    //         StatType.FoodStorage => "Food",
    //         StatType.WaterStorage => "Water",
    //         StatType.CriticalHitChance => "Critical Chance",
    //         StatType.CriticalHitBonusDamage => "Critical Damage",
    //         StatType.AttackSpeed => "Attack Speed",
    //         StatType.ShieldRechargeCooldown => "Shield Cooldown",
    //         StatType.OxygenConsumption => "Oxygen Use",
    //         StatType.OxygenGeneration => "Oxygen Generation",
    //         StatType.FoodConsumption => "Food Use",
    //         StatType.FoodGeneration => "Food Generation",
    //         StatType.WaterConsumption => "Water Use",
    //         StatType.WaterGeneration => "Water Generation",
    //         _ => throw CreateUnsupportedTitleException(type)
    //     };
    // }
    //
    // private static ArgumentException CreateConditionTitleException(StatType type)
    // {
    //     Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(GetTitle)} received {nameof(type)}={type}; condition is not a stat bar stat.");
    //     return new ArgumentException("Condition is not a StatBar stat.", nameof(type));
    // }
    //
    // private static ArgumentOutOfRangeException CreateUnsupportedTitleException(StatType type)
    // {
    //     Logger.Error($"{nameof(PlayerStatsPanel)}.{nameof(GetTitle)} received unsupported {nameof(type)}={type}.");
    //     return new ArgumentOutOfRangeException(nameof(type), type, null);
    // }
}
