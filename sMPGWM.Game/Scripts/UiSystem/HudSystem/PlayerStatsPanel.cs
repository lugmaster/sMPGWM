using Godot;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerStatsPanel : PanelContainer
{
    private NetworkManager? _network;
    private Util.UiComponents.StatBar _bar = null!;
    private Label _target = null!;
    private Label _feedback = null!;
    private string _localEntity = "";
    private string _lastShield = "";
    private string _lastTarget = "";
    private double _elapsed;
    private Button _self = null!;
    private Button _ally = null!;
    public override void _Ready()
    {
        var container = GetNode<VBoxContainer>("%BarContainer");
        _bar = container.GetNode<Util.UiComponents.StatBar>("StatBar");
        _bar.SetTitle("Shield");
        _target = new Label(); _feedback = new Label();
        _self = new Button { Text = "Target self" };
        _ally = new Button { Text = "Next ally" };
        container.AddChild(_target); container.AddChild(_feedback);
        container.AddChild(_self); container.AddChild(_ally);
        _self.Pressed += SelectSelf; _ally.Pressed += NextAlly;
    }
    public void Bind(NetworkManager? network)
    {
        if (_network != null) _network.AbilityFeedback -= Feedback;
        _network = network;
        if (_network != null) _network.AbilityFeedback += Feedback;
        if (_feedback != null) _feedback.Text = "";
    }
    private void Feedback(string text) => _feedback.Text = text;
    private void SelectSelf() { if (_network != null) _network.SelectedTarget = ""; }
    private void NextAlly()
    {
        if (_network == null) return;
        var next = _network.SelectedTarget.Length == 0;
        foreach (var player in _network.World.Players.Values)
        {
            var ship = player.ControlledShip;
            if (ship == null || ship.LocallyControlled) continue;
            if (next) { _network.SelectedTarget = ship.EntityId; return; }
            if (_network.SelectedTarget == ship.EntityId) next = true;
        }
        SelectSelf();
    }
    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < .2 || _network == null) return;
        _elapsed = 0;
        var local = _network.World.LocalPlayer?.Participant?.ControlledShip;
        if (local == null) { _target.Text = ""; return; }
        if (_localEntity != local.EntityId) { _localEntity = local.EntityId; SelectSelf(); _feedback.Text = ""; }
        var enabled = _network.World.LocalPlayer!.InputPolicy.CanControlPlayer;
        _self.Disabled = _ally.Disabled = !enabled;
        var shieldText = $"Shield: {local.Gameplay.Shield.Current:0.#} / {local.Gameplay.Shield.Capacity:0.#}";
        if (shieldText != _lastShield)
        {
            _lastShield = shieldText;
            _bar.Setup(local.Gameplay.Shield.Current, local.Gameplay.Shield.Capacity);
        }
        var target = local;
        if (_network.SelectedTarget.Length > 0 && !_network.World.Entities.TryGetValue(_network.SelectedTarget, out target))
        { SelectSelf(); target = local; }
        var state = target.Gameplay;
        var tick = _network.IsHost() ? _network.Synchronization!.CompletedTick : _network.Synchronization!.HostEstimate;
        var remaining = System.Math.Max(0, (state.Deadline - tick) / 60d);
        var targetText = $"Target: {(target == local ? "Self" : target.EntityId[..6])}  Shield {state.Shield.Current:0.#} / {state.Shield.Capacity:0.#}" +
            (state.BehaviourInstance != 0 ? $"\nShield Capacity Boost: {remaining:0.0}s" : "");
        if (targetText != _lastTarget) _target.Text = _lastTarget = targetText;
    }
    public override void _ExitTree() => Bind(null);
}
