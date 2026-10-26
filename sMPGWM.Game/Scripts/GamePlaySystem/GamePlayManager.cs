using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.InputSystem;
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
    public InventorySystem.InventorySystem Inventory { get; private set; } = null!;
    public IReadOnlyDictionary<ItemCategoryId, IconDefinition> CategoryIcons { get; private set; } = null!;

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

        var categoryIcons = new Dictionary<ItemCategoryId, IconDefinition>();
        foreach (var category in categoriesResult.Value!.All)
        {
            if (category.IconKey is null)
                continue;

            try
            {
                categoryIcons.Add(category.Id, Icons.Get(category.IconKey));
            }
            catch (ArgumentException exception)
            {
                FileLoader.AbortLoad(new DataLoadError("Invalid category icon",
                    $"Category {category.Id}: {exception.Message}", FilePaths.UserItemCategoryDataYaml,
                    FilePaths.ItemCategoryDataYamlSchema, exception));
                return;
            }
        }

        ItemCategories = categoriesResult.Value;
        CategoryIcons = new ReadOnlyDictionary<ItemCategoryId, IconDefinition>(categoryIcons);
        Items = new ItemCatalog(ItemCategories, Array.Empty<ItemDefinition>());
        Inventory = new InventorySystem.InventorySystem(Items,
            exception => LoggingSystem.Logger.Error($"Inventory observer failed: {exception}"));

        _canvas = GetNode<UiCanvas>("UiCanvas");
        _inputController = InputController.Instance;
        _inputController.SetPointerInputFilter(_canvas.IsPointerOverInteractiveUi);
        var player = PlayerMock.Instance;
        _playerActions = new PlayerActionController(
            _inputPolicy, player.OnHotbarSlotPressed, () => _inputController.IsGameplayInputAvailable);

        _canvas.OnRequestHotbarAction += _playerActions.RequestHotbarActivation;
        _canvas.BlockingChanged += _inputPolicy.SetHasBlockingWindow;
        _inputPolicy.CanControlPlayerChanged += ApplyPlayerInputAvailability;

        _canvas.SetHotbarIcons(player.GetHotBarSkills());
        _inputPolicy.SetHasBlockingWindow(_canvas.HasBlockingWindow);
        _inputPolicy.SetSessionActive(GameState == MainGameState.Active);
        _canvas.SetNavigationEnabled(GameState == MainGameState.Active);
        ApplyPlayerInputAvailability(_inputPolicy.CanControlPlayer);
        CallDeferred(nameof(Start));
    }

    protected override void OnExitTree()
    {
        Inventory?.Dispose();
        Inventory = null!;
        Items = null!;
        ItemCategories = null!;
        CategoryIcons = null!;
        if (_playerActions is null)
            return;

        _inputPolicy.CanControlPlayerChanged -= ApplyPlayerInputAvailability;
        _canvas.BlockingChanged -= _inputPolicy.SetHasBlockingWindow;
        _canvas.OnRequestHotbarAction -= _playerActions.RequestHotbarActivation;
    }

    private void Start() => SetGameState(MainGameState.Active);

    public void SetGameState(MainGameState newGameState)
    {
        if (!Enum.IsDefined(newGameState))
            throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null);
        if (newGameState == _gameState) return;

        _gameState = newGameState;
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
