using System;
using sMPGWM.Scripts.MessageSystem;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Definitions;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;
using sMPGWM.Scripts.Util.UiComponents;

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
    public ItemCategories ItemCategories { get; private set; } = null!;
    public ItemCatalog Items { get; private set; } = null!;
    public InventoryController InventoryController { get; private set; } = null!;
    public EquipmentController EquipmentController { get; private set; } = null!;
    public InventoryEquipmentTransferService ItemTransfers { get; private set; } = null!;
    public ItemHotbarState ItemHotbar { get; private set; } = null!;
    public ItemHotbarAdapter HotbarAdapter { get; private set; } = null!;

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
        // TODO pack initialization into speaking methods to make the start up more understandable
        var definitionsResult = FileLoader.LoadYamlDefinition<ItemCategoryDefinitions>(
            FilePaths.UserItemCategoryDataYaml, FilePaths.ItemCategoryDataYaml, FilePaths.ItemCategoryDataYamlSchema);
        if (!definitionsResult.IsSuccess)
        {
            FileLoader.AbortLoad(definitionsResult.Error!);
            return;
        }

        var categoriesResult = definitionsResult.Value!.ToCategories(
            FilePaths.UserItemCategoryDataYaml, FilePaths.ItemCategoryDataYamlSchema);
        if (!categoriesResult.IsSuccess)
        {
            FileLoader.AbortLoad(categoriesResult.Error!);
            return;
        }

        var itemsResult = FileLoader.LoadYamlDefinition<ItemDefinitions>(
            FilePaths.UserItemDataYaml, FilePaths.ItemDataYaml, FilePaths.ItemDataYamlSchema);
        if (!itemsResult.IsSuccess)
        {
            FileLoader.AbortLoad(itemsResult.Error!);
            return;
        }

        var catalogResult = itemsResult.Value!.ToCatalog(categoriesResult.Value!,
            FilePaths.UserItemDataYaml, FilePaths.ItemDataYamlSchema);
        if (!catalogResult.IsSuccess)
        {
            FileLoader.AbortLoad(catalogResult.Error!);
            return;
        }

        if (PopulateMockInventory)
        {
            try
            {
                PlayerMock.ValidateInventoryDefinitions(catalogResult.Value!);
            }
            catch (ArgumentException exception)
            {
                FileLoader.AbortLoad(new DataLoadError("Invalid mock inventory definitions", exception.Message,
                    FilePaths.UserItemDataYaml, FilePaths.ItemDataYamlSchema, exception));
                return;
            }
        }

        ItemCategories = categoriesResult.Value;
        var player = PlayerMock.Instance;
        Items = catalogResult.Value!;
        InventoryController = new InventoryController(Items,
            exception => LoggingSystem.Logger.Error($"Inventory observer failed: {exception}"));
        if (PopulateMockInventory) player.PopulateInventory(InventoryController);
        EquipmentController = new EquipmentController(Items,
            exception => LoggingSystem.Logger.Error($"Equipment observer failed: {exception}"));
        ItemTransfers = new InventoryEquipmentTransferService(InventoryController, EquipmentController);

        _canvas = GetNode<UiCanvas>("UiCanvas");
        Messages = new MessageStore(exception => LoggingSystem.Logger.Error($"Message observer failed: {exception}"));
        _acknowledgements = new MessageAcknowledgements(Messages);
        _acknowledgements.Changed += RefreshAcknowledgement;
        _canvas.MessageAcknowledged += AcknowledgeMessage;
        Messages.RegisterSender(new MessageSender("system", "System"));
        Messages.Receive("welcome", "system", "Welcome", "Welcome, this is test message", MessageType.NonBlocking);
        _canvas.BindMessages(Messages);
        _canvas.InventoryEditAvailabilityChanged += SetInventoryEditing;
        BindInventoryPresentation(InventoryController, EquipmentController, Items, ItemTransfers);
        _inputController = InputController.Instance;
        _inputController.SetPointerInputFilter(_canvas.IsPointerOverInteractiveUi);
        var actions = player.GetHotBarSkills();
        ItemHotbar = new ItemHotbarState(
            exception => LoggingSystem.Logger.Error($"Hotbar observer failed: {exception}"));
        HotbarAdapter = new ItemHotbarAdapter(ItemHotbar, InventoryController, EquipmentController, ItemTransfers);
        _playerActions = new PlayerActionController(
            _inputPolicy, position => HotbarAdapter.Activate(position,
                key => player.OnHotbarSlotPressed(int.Parse(key)),
                request => LoggingSystem.Logger.Info($"Item hotbar activation requested: {request}")),
            () => _inputController.IsGameplayInputAvailable);

        _canvas.OnRequestHotbarAction += _playerActions.RequestHotbarActivation;
        _canvas.BlockingChanged += _inputPolicy.SetHasBlockingWindow;
        _inputPolicy.CanControlPlayerChanged += ApplyPlayerInputAvailability;

        _canvas.BindHotbar(ItemHotbar, HotbarAdapter, Items, actions);
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
        ItemHotbar?.Dispose();
        ItemHotbar = null!;
        HotbarAdapter = null!;
        EquipmentController?.Dispose();
        InventoryController?.Dispose();
        ItemTransfers = null!;
        EquipmentController = null!;
        InventoryController = null!;
        Items = null!;
        ItemCategories = null!;
        if (_playerActions is null)
            return;

        _inputPolicy.CanControlPlayerChanged -= ApplyPlayerInputAvailability;
        _canvas.BlockingChanged -= _inputPolicy.SetHasBlockingWindow;
        _canvas.OnRequestHotbarAction -= _playerActions.RequestHotbarActivation;
    }

    private void Start() => SetGameState(MainGameState.Active);

    internal void BindInventoryPresentation(InventoryController inventory, EquipmentController equipment, ItemCatalog catalog,
        InventoryEquipmentTransferService? transfers = null)
    {
        _canvas.UnbindInventory();
        _presentationTransfers = transfers;
        _canvas.BindInventory(inventory, equipment, catalog, transfers);
    }

    private void SetInventoryEditing(bool available)
    {
        _presentationTransfers?.RevokeEditSession();
        var session = available && GameState == MainGameState.Active && _presentationTransfers != null
            ? _presentationTransfers.OpenEditSession() : Guid.Empty;
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
