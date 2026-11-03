using System;
using sMPGWM.Scripts.EquipmentSystem;
using Godot;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UiSystem;
namespace sMPGWM.Scripts.GamePlaySystem;

public partial class PlayerController : Node
{
    public PlayerInputPolicy InputPolicy { get; } = new();
    public MpController? Participant { get; private set; }
    public InputController Input { get; } = new() { Name = "InputController" };
    private UiCanvas _canvas = null!;
    private InventoryEquipmentTransferService _transfer = null!;
    private PlayerActionController _actions = null!;
    private bool _active;
    private bool _authorityReady;
    private bool _shutdown;
    private ShipController? _ship;
    private Camera2D _camera = null!;

    public void Initialize(UiCanvas canvas, InventoryEquipmentTransferService transfer, Action<int> activate, Node world)
    {
        _canvas = canvas; _transfer = transfer;
        Input.ProcessPhysicsPriority = -200;
        AddChild(Input);
        Input.SetPointerInputFilter(canvas.IsPointerOverInteractiveUi);
        _actions = new(InputPolicy, activate, () => Input.IsGameplayInputAvailable);
        canvas.OnRequestHotbarAction += _actions.RequestHotbarActivation;
        canvas.BlockingChanged += InputPolicy.SetHasBlockingWindow;
        canvas.InventoryEditAvailabilityChanged += SetInventoryEditing;
        InputPolicy.CanControlPlayerChanged += ApplyAvailability;
        // TODO #ShipStory use default values - i never initiated a camera with that much parameters. defend if you think any is really needed.
        _camera = new Camera2D { Name = "LocalCamera", PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 5f, IgnoreRotation = true,
            ProcessPriority = 100, ProcessCallback = Camera2D.Camera2DProcessCallback.Idle };
        world.AddChild(_camera);
        // TODO #ShipStory what are these adjustions for?
        ProcessPhysicsPriority = -100;
        // TODO #ShipStory what are these adjustions for?
        ProcessPriority = 50;
        InputPolicy.SetHasBlockingWindow(canvas.HasBlockingWindow);
        ApplyAvailability(false);
    }
    public void BindParticipant(MpController participant)
    {
        if (Participant == participant) return;
        if (participant.ControllingPeerId != Multiplayer.GetUniqueId())
            throw new InvalidOperationException("Cannot bind a remote participant.");
        UnbindParticipant();
        SetMultiplayerAuthority(participant.ControllingPeerId);
        _camera.SetMultiplayerAuthority(participant.ControllingPeerId);
        _authorityReady = true;
        Participant = participant;
        participant.ControlledShipChanged += ShipChanged;
        ShipChanged(null, participant.ControlledShip);
        ApplyAvailability(InputPolicy.CanControlPlayer);
    }
    public void UnbindParticipant()
    {
        if (_shutdown) return;
        if (Participant != null) Participant.ControlledShipChanged -= ShipChanged;
        ShipChanged(_ship, null);
        Participant = null;
        Input.SetGameplayInputEnabled(false);
        _authorityReady = false;
    }
    private void ShipChanged(ShipController? previous, ShipController? next)
    {
        if (_ship == next) return;
        if (_ship != null)
        {
            _ship.Teleported -= SnapCamera;
            _ship.TreeExiting -= TargetExiting;
            _ship.SetLocalControl(false);
        }
        if (next == null && _camera != null && _camera.IsInsideTree())
        {
            _camera.GlobalPosition = _camera.GetScreenCenterPosition();
            _camera.ResetSmoothing();
        }
        _ship = next;
        Input.SetGameplayInputEnabled(false);
        if (next != null)
        {
            next.SetLocalControl(true);
            next.Teleported += SnapCamera;
            next.TreeExiting += TargetExiting;
            SnapCamera();
        }
        ApplyAvailability(InputPolicy.CanControlPlayer);
    }
    private void TargetExiting() => ShipChanged(_ship, null);
    private void SnapCamera()
    {
        if (_ship == null) return;
        _camera.GlobalPosition = _ship.DisplayedPosition;
        _camera.ResetSmoothing();
        _camera.ForceUpdateScroll();
    }
    public override void _Process(double delta)
    {
        if (_ship != null) _camera.GlobalPosition = _ship.DisplayedPosition;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_ship == null) return;
        if (!Input.IsGameplayInputAvailable || !IsMultiplayerAuthority()) { _ship.ClearInput(); return; }
        var aim = _ship.GetGlobalMousePosition() - _ship.GlobalPosition;
        _ship.SetInput(new(Input.Direction, aim.Angle(), !_canvas.IsPointerOverInteractiveUi() && aim.LengthSquared() > 0,
            Input.AfterBurnerPressed, Input.BreakPressed, Input.AutoBreakingEnabled));
    }
    public void SetSessionActive(bool active)
    {
        _active = active;
        if (!active) SetInventoryEditing(false);
        InputPolicy.SetSessionActive(active);
        _canvas.SetNavigationEnabled(active);
    }
    private void SetInventoryEditing(bool available)
    {
        _transfer.RevokeEditSession();
        _canvas.SetInventoryEditSession(available && _active ? _transfer.OpenEditSession() : 0UL);
    }
    private void ApplyAvailability(bool enabled)
    {
        if (_shutdown) return;
        Input.SetGameplayInputEnabled(enabled && _authorityReady);
        _canvas.SetGameplayInputEnabled(enabled && _authorityReady);
        if (!enabled) _ship?.ClearInput();
    }
    public void Shutdown()
    {
        if (_shutdown) return;
        UnbindParticipant();
        _shutdown = true;
        InputPolicy.CanControlPlayerChanged -= ApplyAvailability;
        _canvas.BlockingChanged -= InputPolicy.SetHasBlockingWindow;
        _canvas.InventoryEditAvailabilityChanged -= SetInventoryEditing;
        _canvas.OnRequestHotbarAction -= _actions.RequestHotbarActivation;
        _canvas.UnbindInventory();
        _transfer.RevokeEditSession();
        if (GodotObject.IsInstanceValid(_camera)) _camera.QueueFree();
    }
    public override void _ExitTree() => Shutdown();
}
