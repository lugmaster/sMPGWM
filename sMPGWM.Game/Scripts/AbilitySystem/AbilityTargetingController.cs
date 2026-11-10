using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.AbilitySystem;

public partial class AbilityTargetingController : Node
{
    private readonly NetworkManager _network;
    private readonly PlayerController _player;
    private readonly UiCanvas _canvas;
    private EquipmentAbilityHotbarBinding? _binding;
    private long _source;
    private string _caster = "";
    private long _generation;
    private bool _waitForRelease;
    public bool IsTargeting => _binding != null;
    public bool BlocksPointerActions => IsTargeting || _waitForRelease;

    public AbilityTargetingController(NetworkManager network, PlayerController player, UiCanvas canvas)
    {
        _network = network;
        _player = player;
        _canvas = canvas;
    }

    public override void _Ready()
    {
        _player.InputPolicy.CanControlPlayerChanged += AvailabilityChanged;
        _network.LocalItems.Equipment.Changed += EquipmentChanged;
    }

    public void Begin(EquipmentAbilityHotbarBinding binding)
    {
        Cancel();
        if (!_player.Input.IsGameplayInputAvailable || _player.Participant?.ControlledShip is not { } ship) return;
        var equipment = _network.LocalItems.Equipment;
        var slot = equipment.FindSlot(binding.SlotKey);
        if (slot < 0 || equipment.Snapshot.Slots[slot] is not { } item ||
            !_network.LocalItems.Provides(binding.SlotKey, checked((long)item.Id.Value), binding.AbilityId) ||
            !_network.Abilities.IsSupported(binding.AbilityId, out _)) return;
        _binding = binding;
        _source = checked((long)item.Id.Value);
        _caster = ship.EntityId;
        _generation = _player.Participant.ControlGeneration;
        _waitForRelease = Input.IsMouseButtonPressed(MouseButton.Left);
        var texture = Cursors.GreenTargetCursor;
        Input.SetCustomMouseCursor(texture, Input.CursorShape.Arrow, texture.GetSize() / 2);
        _canvas.ShowGameplayFeedback("Select your ship or an allied ship. Right-click or Escape to cancel.");
    }

    public void Cancel()
    {
        if (_binding == null) return;
        _binding = null;
        _waitForRelease = Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsMouseButtonPressed(MouseButton.Right);
        Input.SetCustomMouseCursor(null);
        _canvas.ShowGameplayFeedback("");
    }

    private bool ContextValid() => _player.Input.IsGameplayInputAvailable &&
        _player.Participant?.ControlledShip?.EntityId == _caster && _player.Participant.ControlGeneration == _generation &&
        _binding != null && _network.LocalItems.Provides(_binding.SlotKey, _source, _binding.AbilityId);

    public override void _Process(double delta)
    {
        if (_waitForRelease && !Input.IsMouseButtonPressed(MouseButton.Left) && !Input.IsMouseButtonPressed(MouseButton.Right))
            _waitForRelease = false;
        if (IsTargeting && !ContextValid()) Cancel();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsTargeting) return;
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } ||
            input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            Cancel();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (!IsTargeting || input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) return;
        if (_waitForRelease || _canvas.IsPointerOverInteractiveUi()) return;
        GetViewport().SetInputAsHandled();
        if (!ContextValid()) { Cancel(); return; }
        var caster = _player.Participant!.ControlledShip!;
        var target = ShipTargetPicker.Pick(_network.World, caster.GetCanvasTransform().AffineInverse() * mouse.Position);
        if (target == null) { _canvas.ShowGameplayFeedback("Select a living allied ship."); return; }
        var ability = _network.Abilities.Definitions.Abilities.GetById(_binding!.AbilityId);
        var targeting = _network.Abilities.Definitions.Targeting.GetById(ability.TargetingId);
        var context = new AbilityActivationContext(caster.Gameplay, target.Gameplay,
            caster.GlobalPosition.DistanceSquaredTo(target.GlobalPosition));
        if (AbilityEligibility.Rejection(ability, targeting, context) is { } rejection)
        { _canvas.ShowGameplayFeedback(AbilityActivationResultText.Format(rejection)); return; }
        var binding = _binding;
        var source = _source;
        Cancel();
        _network.ActivateAbility(binding.AbilityId, target.EntityId, binding.SlotKey, source);
    }

    private void AvailabilityChanged(bool available) { if (!available) Cancel(); }
    private void EquipmentChanged(EquipmentSnapshot _) { if (IsTargeting && !ContextValid()) Cancel(); }
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut) Cancel();
    }
    public override void _ExitTree()
    {
        Cancel();
        _player.InputPolicy.CanControlPlayerChanged -= AvailabilityChanged;
        _network.LocalItems.Equipment.Changed -= EquipmentChanged;
    }
}
