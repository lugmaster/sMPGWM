using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.HotbarSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : AbstractSingleton<GamePlayManager>
{
    private UiCanvas _canvas = null!;
    private InputController _inputController = null!;
    private PlayerActionController _playerActionController = null!;
    private readonly PlayerInputPolicy _inputPolicy = new();
    private MainGameState _gameState = MainGameState.Inactive;

    public event Action<MainGameState>? OnGameStateChanged;

    public MainGameState GameState => _gameState;
    public ItemRequestService ItemRequestService { get; private set; } = null!;
    public ItemController ItemController { get; private set; } = null!;
    public InventoryController InventoryController { get; private set; } = null!;
    public EquipmentController EquipmentController { get; private set; } = null!;
    private InventoryEquipmentTransferService IeTransferService { get; set; } = null!;
    public ItemHotbarController HotbarController { get; private set; } = null!;

    public MessageController MessageController { get; private set; } = null!;

    [Export] public bool PopulateMockInventory { get; set; } = true;

    protected override void OnReady()
    {
        var player = PlayerMock.Instance;
        ItemRequestService = new ItemRequestService();
        ItemController = new ItemController(ItemRequestService);
        InventoryController = new InventoryController(ItemRequestService);
        if (PopulateMockInventory) player.PopulateInventory(InventoryController);
        EquipmentController = new EquipmentController(InventoryController.ItemRequestService);
        IeTransferService = new InventoryEquipmentTransferService(InventoryController, EquipmentController);

        _canvas = GetNode<UiCanvas>("UiCanvas");
        MessageController = new MessageController(_canvas, _inputPolicy,
            exception => Logger.Error($"Message observer failed: {exception}"));
        MessageMock.PopulateWelcomeMessage(MessageController);
        _canvas.InventoryEditAvailabilityChanged += SetInventoryEditing;
        _canvas.BindInventory(InventoryController, EquipmentController, IeTransferService);
        _inputController = InputController.Instance;
        _inputController.SetPointerInputFilter(_canvas.IsPointerOverInteractiveUi);
        var actions = player.GetHotBarSkills();
        HotbarController = new ItemHotbarController(InventoryController, EquipmentController, IeTransferService,
            exception => Logger.Error($"Hotbar observer failed: {exception}"));
        _playerActionController = new PlayerActionController(
            _inputPolicy, position => HotbarController.Activate(position,
                key => player.OnHotbarSlotPressed(int.Parse(key)),
                request => Logger.Info($"Item hotbar activation requested: {request}")),
            () => _inputController.IsGameplayInputAvailable);

        _canvas.OnRequestHotbarAction += _playerActionController.RequestHotbarActivation;
        _canvas.BlockingChanged += _inputPolicy.SetHasBlockingWindow;
        _inputPolicy.CanControlPlayerChanged += ApplyPlayerInputAvailability;

        _canvas.BindHotbar(HotbarController, actions);
        _inputPolicy.SetHasBlockingWindow(_canvas.HasBlockingWindow);
        _inputPolicy.SetSessionActive(GameState == MainGameState.Active);
        _canvas.SetNavigationEnabled(GameState == MainGameState.Active);
        ApplyPlayerInputAvailability(_inputPolicy.CanControlPlayer);
        GameFlowManager.Instance.StateChanged += NetworkStateChanged;
        NetworkStateChanged(GameFlowManager.Instance.GameFlowState);
    }

    protected override void OnExitTree()
    {
        GameFlowManager.Instance.StateChanged -= NetworkStateChanged;
        if (_canvas != null)
        {
            MessageController?.Dispose();
            _canvas.UnbindInventory();
            _canvas.InventoryEditAvailabilityChanged -= SetInventoryEditing;
        }
        IeTransferService?.Dispose();
        HotbarController?.Dispose();
        HotbarController = null!;
        EquipmentController?.Dispose();
        InventoryController?.Dispose();
        IeTransferService = null!;
        EquipmentController = null!;
        InventoryController = null!;
        ItemController = null!;
        ItemRequestService = null!;
        if (_playerActionController is null)
            return;

        _inputPolicy.CanControlPlayerChanged -= ApplyPlayerInputAvailability;
        _canvas.BlockingChanged -= _inputPolicy.SetHasBlockingWindow;
        _canvas.OnRequestHotbarAction -= _playerActionController.RequestHotbarActivation;
    }

    public void StopSession()
    {
        if (_canvas != null && _inputController != null) SetGameState(MainGameState.Inactive);
    }

    private void NetworkStateChanged(GameFlowState state)
    {
        SetGameState(state == GameFlowState.InGame ? MainGameState.Active : MainGameState.Inactive);
    }

    private void SetInventoryEditing(bool available)
    {
        IeTransferService?.RevokeEditSession();
        var session = available && GameState == MainGameState.Active && IeTransferService != null
            ? IeTransferService.OpenEditSession() : 0UL;
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
        MessageController.SetActive(newGameState == MainGameState.Active);
        OnGameStateChanged?.Invoke(newGameState);
    }

    private void ApplyPlayerInputAvailability(bool enabled)
    {
        _inputController.SetGameplayInputEnabled(enabled);
        _canvas.SetGameplayInputEnabled(enabled);
    }
}
