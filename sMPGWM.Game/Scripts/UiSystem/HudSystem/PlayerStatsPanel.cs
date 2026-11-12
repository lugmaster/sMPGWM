using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerStatsPanel : PanelContainer
{
    private NetworkManager? _network;
    private StatBar _shield = null!;
    private StatBar _energy = null!;
    private Label _availability = null!;
    private long _revision = -1;
    private string _entity = "";
    private double _current = double.NaN;
    private double _capacity = double.NaN;

    public override void _Ready()
    {
        _shield = GetNode<StatBar>("%BarContainer/StatBar");
        _shield.SetTitle("Shield");
        _energy = GetNode<StatBar>("%BarContainer/EnergyBar");
        _energy.SetTitle("Energy");
        _availability = GetNode<Label>("%BarContainer/Availability");
        Refresh();
    }

    public void Bind(NetworkManager? network)
    {
        _network = network;
        _entity = "";
        _revision = -1;
        _current = _capacity = double.NaN;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        if (_shield == null) return;
        var ship = _network?.World.LocalPlayer?.Participant?.ControlledShip;
        var entity = ship?.EntityId ?? "";
        var current = ship?.Runtime.Shield.Current ?? 0;
        var capacity = ship?.Stats.ShieldCapacity ?? 0;
        var revision = ship?.Runtime.Revision ?? 0;
        if (entity == _entity && current == _current && capacity == _capacity && revision == _revision) return;
        _revision = revision;
        _entity = entity;
        _current = current;
        _capacity = capacity;
        _shield.Setup(current, capacity);
        _energy.Setup(ship?.Runtime.Energy.Current ?? 0, ship?.Runtime.Energy.Capacity ?? 0);
        var state = ship?.Runtime;
        _availability.Text = state?.Components.Power == null ? "Power unavailable - shield unprotected" :
            state.EnergyDepleted ? "Power depleted — shield unprotected" :
            state?.ShieldProtectionAvailable == true ? "Powered — shield protective" :
            state != null && state.ShieldDowntimeDeadline > state.AuthoritativeTick ? "Powered — shield downtime" :
            "Powered — shield unavailable";
        _shield.Modulate = state?.ShieldProtectionAvailable == true ? Colors.White : new Color(0.65f, 0.65f, 0.65f);
    }

    public override void _ExitTree() => Bind(null);
}
