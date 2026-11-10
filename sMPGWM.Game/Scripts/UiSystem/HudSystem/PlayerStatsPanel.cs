using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerStatsPanel : PanelContainer
{
    private NetworkManager? _network;
    private StatBar _shield = null!;
    private string _entity = "";
    private double _current = double.NaN;
    private double _capacity = double.NaN;
    private double _elapsed;

    public override void _Ready()
    {
        _shield = GetNode<StatBar>("%BarContainer/StatBar");
        _shield.SetTitle("Shield");
        _shield.Setup(0, 0);
    }

    public void Bind(NetworkManager? network)
    {
        _network = network;
        _entity = "";
        _current = _capacity = double.NaN;
        _elapsed = .2;
        if (_shield != null) _shield.Setup(0, 0);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < .2) return;
        _elapsed = 0;
        var ship = _network?.World.LocalPlayer?.Participant?.ControlledShip;
        var entity = ship?.EntityId ?? "";
        var current = ship?.Gameplay.Shield.Current ?? 0;
        var capacity = ship?.Gameplay.Shield.Capacity ?? 0;
        if (entity == _entity && current == _current && capacity == _capacity) return;
        _entity = entity;
        _current = current;
        _capacity = capacity;
        _shield.Setup(current, capacity);
    }

    public override void _ExitTree() => Bind(null);
}
