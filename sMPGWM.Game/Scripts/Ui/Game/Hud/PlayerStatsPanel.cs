using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game.Hud;

public partial class PlayerStatsPanel : PanelContainer
{
    private readonly Dictionary<StatTypes, StatBar> _bars = new();

    private VBoxContainer _barContainer = null!;
    private LivingEntityState _livingEntityState = null!;

    public override void _Ready()
    {
        _barContainer = GetNode<VBoxContainer>("%BarContainer");
        Logger.Info("PlayerStatsPanel loaded.");
    }

    public void Bind(LivingEntityState state, IReadOnlyCollection<StatTypes> visibleStats)
    {
        _livingEntityState = state ?? throw new ArgumentNullException(nameof(state));

        if (visibleStats == null)
            throw new ArgumentNullException(nameof(visibleStats));

        if (visibleStats.Count == 0)
            throw new ArgumentException("At least one visible stat is required.", nameof(visibleStats));

        RebuildBars(visibleStats);
        RefreshAllBars();
    }

    private void RebuildBars(IReadOnlyCollection<StatTypes> visibleStats)
    {
        ClearBars();

        foreach (var type in visibleStats)
        {
            var statBar = HudProvider.CreateStatBar();

            _barContainer.AddChild(statBar);

            statBar.Configure(type, GetTitle(type));

            if (!_bars.TryAdd(type, statBar))
                throw new InvalidOperationException($"Duplicate StatBar type: {type}");
        }
    }

    private void ClearBars()
    {
        _bars.Clear();

        foreach (var child in _barContainer.GetChildren().ToArray())
        {
            _barContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void RefreshAllBars()
    {
        foreach (var type in _bars.Keys)
            RefreshBar(type);
    }

    private void RefreshBar(StatTypes type)
    {
        var stat = _livingEntityState.GetStatByType(type);
        GetBar(type).Setup(stat.CurrentValue, stat.MaxValue);
    }

    public void SetBarValue(StatTypes type, double value, double maxValue)
    {
        GetBar(type).Setup(value, maxValue);
    }

    private StatBar GetBar(StatTypes type)
    {
        if (!_bars.TryGetValue(type, out var bar))
            throw new InvalidOperationException($"Missing StatBar type: {type}");

        return bar;
    }

    private static string GetTitle(StatTypes type)
    {
        return type switch
        {
            StatTypes.Health => "Health",
            StatTypes.Shield => "Shield",
            StatTypes.Energy => "Energy",
            StatTypes.HealthRegeneration => "Health Regen",
            StatTypes.ShieldRegeneration => "Shield Regen",
            StatTypes.EnergyRegeneration => "Energy Regen",
            StatTypes.Speed => "Speed",
            StatTypes.Acceleration => "Acceleration",
            StatTypes.AfterBurnerSpeed => "Afterburner Speed",
            StatTypes.AfterBurnerAcceleration => "Afterburner Acceleration",
            StatTypes.Condition => throw new ArgumentException("Condition is not a StatBar stat.", nameof(type)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}