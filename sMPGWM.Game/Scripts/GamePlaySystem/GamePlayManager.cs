using System;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.HotbarSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : AbstractSingleton<GamePlayManager>
{
    private UiCanvas _canvas = null!;
    private InventoryEquipmentTransferService? _presentationTransfers;
    private InputController _inputController = null!;
    private PlayerActionController _playerActions = null!;
    private readonly PlayerInputPolicy _inputPolicy = new();
    private MainGameState _gameState = MainGameState.Inactive;

    public event Action<MainGameState>? OnGameStateChanged;

    public MainGameState GameState => _gameState;
    public ItemRequestService ItemRequestService { get; private set; } = null!;
    public ItemController ItemController { get; private set; } = null!;
    public InventoryController InventoryController { get; private set; } = null!;
    public EquipmentController EquipmentController { get; private set; } = null!;
    public InventoryEquipmentTransferService ItemTransfers { get; private set; } = null!;
    public ItemHotbarController HotbarController { get; private set; } = null!;

    public MessageStore Messages { get; private set; } = null!;
    private MessageAcknowledgements _acknowledgements = null!;

    private void RefreshAcknowledgement()
    {
        var required = _acknowledgements.Current;
        _inputPolicy.SetHasPendingAcknowledgement(required != null);
        if (GameState == MainGameState.Active) _canvas.PresentRequiredMessage(required);
    }

    private void AcknowledgeMessage() => _acknowledgements.AcknowledgeCurrent();

    [Export] public bool PopulateMockInventory { get; set; } = true;

    protected override void OnReady()
    {
        var player = PlayerMock.Instance;
        ItemRequestService = new ItemRequestService();
        ItemController = new ItemController(ItemRequestService);
        InventoryController = new InventoryController(ItemRequestService);
        if (PopulateMockInventory) player.PopulateInventory(InventoryController);
        EquipmentController = new EquipmentController(InventoryController.ItemRequestService);
        ItemTransfers = new InventoryEquipmentTransferService(InventoryController, EquipmentController);

        _canvas = GetNode<UiCanvas>("UiCanvas");
        Messages = new MessageStore(exception => Logger.Error($"Message observer failed: {exception}"));
        _acknowledgements = new MessageAcknowledgements(Messages);
        _acknowledgements.Changed += RefreshAcknowledgement;
        _canvas.MessageAcknowledged += AcknowledgeMessage;
        Messages.RegisterSender(new MessageSender("system", "System"));
        Messages.Receive("welcome", "system", "Welcome", "Welcome, this is test message", MessageType.NonBlocking);
        _canvas.BindMessages(Messages);
        _canvas.InventoryEditAvailabilityChanged += SetInventoryEditing;
        BindInventoryPresentation(InventoryController, EquipmentController, ItemTransfers);
        _inputController = InputController.Instance;
        _inputController.SetPointerInputFilter(_canvas.IsPointerOverInteractiveUi);
        var actions = player.GetHotBarSkills();
        HotbarController = new ItemHotbarController(InventoryController, EquipmentController, ItemTransfers,
            exception => Logger.Error($"Hotbar observer failed: {exception}"));
        _playerActions = new PlayerActionController(
            _inputPolicy, position => HotbarController.Activate(position,
                key => player.OnHotbarSlotPressed(int.Parse(key)),
                request => Logger.Info($"Item hotbar activation requested: {request}")),
            () => _inputController.IsGameplayInputAvailable);

        _canvas.OnRequestHotbarAction += _playerActions.RequestHotbarActivation;
        _canvas.BlockingChanged += _inputPolicy.SetHasBlockingWindow;
        _inputPolicy.CanControlPlayerChanged += ApplyPlayerInputAvailability;

        _canvas.BindHotbar(HotbarController, actions);
        _inputPolicy.SetHasBlockingWindow(_canvas.HasBlockingWindow);
        _inputPolicy.SetSessionActive(GameState == MainGameState.Active);
        _canvas.SetNavigationEnabled(GameState == MainGameState.Active);
        ApplyPlayerInputAvailability(_inputPolicy.CanControlPlayer);
        CallDeferred(nameof(Start));
    }

    protected override void OnExitTree()
    {
        if (_canvas != null)
        {
            _canvas.MessageAcknowledged -= AcknowledgeMessage;
            _canvas.EndMessageSession();
            _acknowledgements?.Dispose();
            Messages?.Dispose();
            _inputPolicy.SetHasPendingAcknowledgement(false);
            _canvas.UnbindInventory();
            _canvas.InventoryEditAvailabilityChanged -= SetInventoryEditing;
        }
        _presentationTransfers = null;
        ItemTransfers?.Dispose();
        HotbarController?.Dispose();
        HotbarController = null!;
        EquipmentController?.Dispose();
        InventoryController?.Dispose();
        ItemTransfers = null!;
        EquipmentController = null!;
        InventoryController = null!;
        ItemController = null!;
        ItemRequestService = null!;
        if (_playerActions is null)
            return;

        _inputPolicy.CanControlPlayerChanged -= ApplyPlayerInputAvailability;
        _canvas.BlockingChanged -= _inputPolicy.SetHasBlockingWindow;
        _canvas.OnRequestHotbarAction -= _playerActions.RequestHotbarActivation;
    }

    private void Start() => SetGameState(MainGameState.Active);

    internal void BindInventoryPresentation(InventoryController inventory, EquipmentController equipment, InventoryEquipmentTransferService? transfers = null)
    {
        _canvas.UnbindInventory();
        _presentationTransfers = transfers;
        _canvas.BindInventory(inventory, equipment, transfers);
    }

    private void SetInventoryEditing(bool available)
    {
        _presentationTransfers?.RevokeEditSession();
        var session = available && GameState == MainGameState.Active && _presentationTransfers != null
            ? _presentationTransfers.OpenEditSession() : 0UL;
        _canvas.SetInventoryEditSession(session);
    }

    public void SetGameState(MainGameState newGameState)
    {
        if (!Enum.IsDefined(newGameState))
            throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null);
        if (newGameState == _gameState) return;

        _gameState = newGameState;
        if (newGameState != MainGameState.Active) SetInventoryEditing(false);
        _inputPolicy.SetSessionActive(newGameState == MainGameState.Active);
        _canvas.SetNavigationEnabled(newGameState == MainGameState.Active);
        if (newGameState == MainGameState.Active) RefreshAcknowledgement();
        OnGameStateChanged?.Invoke(newGameState);
    }

    private void ApplyPlayerInputAvailability(bool enabled)
    {
        _inputController.SetGameplayInputEnabled(enabled);
        _canvas.SetGameplayInputEnabled(enabled);
    }
}
