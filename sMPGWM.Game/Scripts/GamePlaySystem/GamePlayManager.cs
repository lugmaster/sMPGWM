using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : AbstractSingleton<GamePlayManager>
{
    private UiCanvas _canvas = null!;
    private InputController _inputController = null!;
    private PlayerActionController _playerActions = null!;
    private readonly PlayerInputPolicy _inputPolicy = new();
    private MainGameState _gameState = MainGameState.Inactive;

    public event Action<MainGameState>? OnGameStateChanged;

    public MainGameState GameState => _gameState;
    public ItemCategories ItemCategories { get; private set; } = null!;
    public ItemCatalog Items { get; private set; } = null!;
    public InventoryImpl InventoryImpl { get; private set; } = null!;
    public EquipmentImpl EquipmentImpl { get; private set; } = null!;
    public InventoryEquipmentTransferService ItemTransfers { get; private set; } = null!;
    public ItemHotbarState ItemHotbar { get; private set; } = null!;
    public ItemHotbarAdapter HotbarAdapter { get; private set; } = null!;

    [Export] public bool PopulateMockInventory { get; set; } = true;

    protected override void OnReady()
    {
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

        ItemCategories = categoriesResult.Value;
        var player = PlayerMock.Instance;
        Items = new ItemCatalog(ItemCategories, player.GetTestItems(ItemCategories));
        InventoryImpl = new InventoryImpl(Items,
            exception => LoggingSystem.Logger.Error($"Inventory observer failed: {exception}"));
        if (PopulateMockInventory) player.PopulateInventory(InventoryImpl);
        EquipmentImpl = new EquipmentImpl(Items,
            exception => LoggingSystem.Logger.Error($"Equipment observer failed: {exception}"));
        ItemTransfers = new InventoryEquipmentTransferService(InventoryImpl, EquipmentImpl);

        _canvas = GetNode<UiCanvas>("UiCanvas");
        _canvas.InventoryEditAvailabilityChanged += SetInventoryEditing;
        _canvas.BindInventory(InventoryImpl, EquipmentImpl, ItemTransfers);
        _inputController = InputController.Instance;
        _inputController.SetPointerInputFilter(_canvas.IsPointerOverInteractiveUi);
        var actions = player.GetHotBarSkills();
        ItemHotbar = new ItemHotbarState(
            exception => LoggingSystem.Logger.Error($"Hotbar observer failed: {exception}"));
        HotbarAdapter = new ItemHotbarAdapter(ItemHotbar, InventoryImpl, EquipmentImpl, ItemTransfers);
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
            _canvas.UnbindInventory();
            _canvas.InventoryEditAvailabilityChanged -= SetInventoryEditing;
        }
        ItemTransfers?.Dispose();
        ItemHotbar?.Dispose();
        ItemHotbar = null!;
        HotbarAdapter = null!;
        EquipmentImpl?.Dispose();
        InventoryImpl?.Dispose();
        ItemTransfers = null!;
        EquipmentImpl = null!;
        InventoryImpl = null!;
        Items = null!;
        ItemCategories = null!;
        if (_playerActions is null)
            return;

        _inputPolicy.CanControlPlayerChanged -= ApplyPlayerInputAvailability;
        _canvas.BlockingChanged -= _inputPolicy.SetHasBlockingWindow;
        _canvas.OnRequestHotbarAction -= _playerActions.RequestHotbarActivation;
    }

    private void Start() => SetGameState(MainGameState.Active);

    private void SetInventoryEditing(bool available)
    {
        _canvas.SetInventoryEditing(available && GameState == MainGameState.Active);
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
        OnGameStateChanged?.Invoke(newGameState);
    }

    private void ApplyPlayerInputAvailability(bool enabled)
    {
        _inputController.SetGameplayInputEnabled(enabled);
        _canvas.SetGameplayInputEnabled(enabled);
    }
}
