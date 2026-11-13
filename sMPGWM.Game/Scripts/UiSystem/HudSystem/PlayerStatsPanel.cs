using System;
using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerStatsPanel : PanelContainer
{
    private NetworkManager? _network;
    private StatBar _shield = null!;
    private StatBar _energy = null!;
    private StatBar _health = null!;
    private readonly Label[] _values = new Label[4];
    private readonly double[] _displayed = { double.NaN, double.NaN, double.NaN, double.NaN };
    private double _shieldCurrent = double.NaN;
    private double _shieldCapacity = double.NaN;
    private double _energyCurrent = double.NaN;
    private double _energyCapacity = double.NaN;
    private double _healthCurrent = double.NaN;
    private double _healthCapacity = double.NaN;

    public override void _Ready()
    {
        var bars = GetNode<Container>("%BarContainer");
        var grid = GetNode<Container>("%GridContainer");
        RemovePlaceholders(bars);
        RemovePlaceholders(grid);
        _health = AddBar(bars, "Health");
        _shield = AddBar(bars, "Shield");
        _energy = AddBar(bars, "Energy");
        AddValue(grid, 0, "Current velocity", Icons.Arrows.MediaControlsFastForward);
        AddValue(grid, 1, "Hull", Icons.Travel.SpaceshipJetShooter);
        AddValue(grid, 2, "Net energy production per second", Icons.Software.BatteryPowerLevelCharging);
        AddValue(grid, 3, "Mass", Icons.Sports.FitnessKettlebellWeight);
        Refresh();
    }

    private static void RemovePlaceholders(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static StatBar AddBar(Node container, string title)
    {
        var bar = HudProvider.CreateStatBar();
        bar.Name = title + "Bar";
        container.AddChild(bar);
        bar.SetTitle(title);
        return bar;
    }

    private void AddValue(Node container, int index, string title, IconDefinition icon)
    {
        var entry = HudProvider.CreateIconWithLabel();
        container.AddChild(entry);
        entry.TooltipText = title;
        entry.GetNode<TextureRect>("Icon").Texture = icon.IconTexture;
        _values[index] = entry.GetNode<Label>("Value");
    }

    public void Bind(NetworkManager? network)
    {
        _network = network;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        if (_shield == null) return;
        var ship = _network?.World.LocalPlayer?.Participant?.ControlledShip;
        var state = ship?.Runtime;
        UpdateBar(_health, state?.Health.Current ?? 0, state?.Health.Capacity ?? 0,
            ref _healthCurrent, ref _healthCapacity);
        UpdateBar(_shield, state?.Shield.Current ?? 0, state?.Shield.Capacity ?? 0,
            ref _shieldCurrent, ref _shieldCapacity);
        UpdateBar(_energy, state?.Energy.Current ?? 0, state?.Energy.Capacity ?? 0,
            ref _energyCurrent, ref _energyCapacity);
        SetValue(0, ship?.State.Velocity.Length() ?? 0);
        SetValue(1, ship?.Stats.Armor ?? 0);
        SetValue(2, state == null ? 0 : state.Stats.EnergyProduction - state.EnergyConsumption);
        SetValue(3, ship?.Stats.Mass ?? 0);
        _shield.Modulate = state?.ShieldProtectionAvailable == true ? Colors.White : new Color(0.65f, 0.65f, 0.65f);
    }

    private static void UpdateBar(StatBar bar, double current, double capacity, ref double previousCurrent, ref double previousCapacity)
    {
        if (current == previousCurrent && capacity == previousCapacity) return;
        bar.Setup(current, capacity);
        previousCurrent = current;
        previousCapacity = capacity;
    }

    private void SetValue(int index, double value)
    {
        value = Math.Round(value, 1);
        if (_displayed[index] == value) return;
        _displayed[index] = value;
        _values[index].Text = index == 2 ? $"{value:+0.#;-0.#;0} /s" : $"{value:0.#}";
    }

    public override void _ExitTree() => Bind(null);
}
