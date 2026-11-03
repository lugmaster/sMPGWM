using System;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.HotbarSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : AbstractSingleton<GamePlayManager>
{
    private UiCanvas _canvas = null!;
    public PlayerController LocalPlayer { get; private set; } = null!;
    private MainGameState _gameState = MainGameState.Inactive;
    private double _mockMessageGameplaySeconds;
    private double _nextMockMessageLogSeconds = 3.0;
    private bool _mockAcknowledgementSent;

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
        LocalPlayer = new PlayerController { Name = "PlayerController" };
        MessageController = new MessageController(_canvas, LocalPlayer.InputPolicy,
            exception => Logger.Error($"Message observer failed: {exception}"));
        MessageMock.PopulateWelcomeMessage(MessageController);
        _canvas.BindInventory(InventoryController, EquipmentController, IeTransferService);
        var actions = player.GetHotBarSkills();
        HotbarController = new ItemHotbarController(InventoryController, EquipmentController, IeTransferService,
            exception => Logger.Error($"Hotbar observer failed: {exception}"));
        var world = GetNode<Node>("WorldViewPortContainer/WorldViewport/WorldScene");
        LocalPlayer.Initialize(_canvas, IeTransferService, position => HotbarController.Activate(position,
            key => player.OnHotbarSlotPressed(int.Parse(key)),
            request => Logger.Info($"Item hotbar activation requested: {request}")), world);
        AddChild(LocalPlayer);
        _canvas.BindHotbar(HotbarController, actions);
        LocalPlayer.SetSessionActive(GameState == MainGameState.Active);
        GameFlowManager.Instance.StateChanged += NetworkStateChanged;
        NetworkStateChanged(GameFlowManager.Instance.GameFlowState);

        // Gameplay is ready before networking can spawn players or publish session events.
        world.AddChild(new MpManager { Name = nameof(MpManager), LocalPlayer = LocalPlayer });
        AddChild(new NetworkManager { Name = nameof(NetworkManager) });
    }

    public override void _Process(double delta)
    {
        if (GameState != MainGameState.Active || _mockAcknowledgementSent) return;

        _mockMessageGameplaySeconds += delta;
        if (_mockMessageGameplaySeconds >= _nextMockMessageLogSeconds)
        {
            Logger.Info($"Mock message timer: {_mockMessageGameplaySeconds:F1}/30 seconds of active gameplay.");
            _nextMockMessageLogSeconds += 3.0;
        }
        if (_mockMessageGameplaySeconds < 30.0) return;

        _mockAcknowledgementSent = true;
        Logger.Info("Mock message timer: sending acknowledgement message.");
        MessageMock.SendAcknowledgementMessage(MessageController);
        Logger.Info("Mock message timer: send completed.");
    }

    protected override void OnExitTree()
    {
        LocalPlayer?.Shutdown();
        GameFlowManager.Instance.StateChanged -= NetworkStateChanged;
        if (_canvas != null)
        {
            MessageController?.Dispose();
            _canvas.UnbindInventory();
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
    }

    public void StopSession()
    {
        if (_canvas != null && LocalPlayer != null) SetGameState(MainGameState.Inactive);
    }

    private void NetworkStateChanged(GameFlowState state)
    {
        SetGameState(state == GameFlowState.InGame ? MainGameState.Active : MainGameState.Inactive);
    }

    public void SetGameState(MainGameState newGameState)
    {
        if (!Enum.IsDefined(newGameState))
            throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null);
        if (newGameState == _gameState) return;

        _gameState = newGameState;
        LocalPlayer.SetSessionActive(newGameState == MainGameState.Active);
        MessageController.SetActive(newGameState == MainGameState.Active);
        OnGameStateChanged?.Invoke(newGameState);
    }

}
