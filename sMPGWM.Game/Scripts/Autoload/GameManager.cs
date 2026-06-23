using System;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Enums.GameState;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Mocks;
using sMPGWM.Scripts.Ui.Game;

namespace sMPGWM.Scripts.Autoload;

public partial class GameManager : AbstractSingleton<GameManager>
{
    private UiCanvas _canvas = null!;
    private Node2D _world = null!;

    private MainGameState _gameState = MainGameState.Inactive;

    public event Action<MainGameState> OnGameStateChanged;

    private MainGameState GameState
    {
        get => _gameState;
        set => SetGameState(value);
    }

    protected override void OnReady()
    {
        _canvas = GetNode<UiCanvas>("UiCanvas");
        _world = GetNode<Node2D>(
            "WorldViewPortContainer/WorldViewport/WorldScene");
        _canvas.OnRequestHotbarAction += ConsumeOnHotbarSlotPressed;
        _canvas.OnRequestMenuAction += ConsumeMenuAction;
        CallDeferred(nameof(Start));
    }
    
    // private const double WaitingTime = 30.0f;
    //
    // private double _passedTime;
    // private bool _fired;
    // private bool _firedBack;
    //
    // public override void _Process(double delta)
    // {
    //     if (_fired && _firedBack)
    //     {
    //         return;
    //     }
    //
    //     _passedTime += delta;
    //
    //     if (!_fired && _passedTime >= WaitingTime)
    //     {
    //         _fired = true;
    //         Logger.Info($"Fired: {_passedTime} - Setting state to {_gameState}");
    //         SetGameState(MainGameState.Inactive);
    //     }
    //
    //     if (!_firedBack && _passedTime >= WaitingTime * 2)
    //     {
    //         _firedBack = true;
    //         Logger.Info($"FiredBack: {_passedTime} - Setting state to {_gameState}");
    //         SetGameState(MainGameState.Active);
    //     }
    // }

    private void Start()
    {
        GameState = MainGameState.Active;
    }


    private void SetGameState(MainGameState newGameState)
    {
        if (newGameState == GameState) return;
        _gameState = newGameState;
        OnGameStateChanged?.Invoke(newGameState);
    }

    public void ConsumeMenuAction(MenuActionType type)
    {
        Logger.Info($"Consume menu action {type}");
        switch ((_gameState, type))
        {
            case (MainGameState.Active, MenuActionType.OpenInventoryMenu):
                _canvas.ApplyMenuAction(type);
                SetGameState(MainGameState.InMenu);
                break;

            case (MainGameState.Active, MenuActionType.OpenMainMenu):
                _canvas.ApplyMenuAction(type);
                SetGameState(MainGameState.InMenu);
                break;

            case (MainGameState.Active, MenuActionType.OpenMapMenu):
                _canvas.ApplyMenuAction(type);
                SetGameState(MainGameState.InMenu);
                break;
            case (MainGameState.Active, MenuActionType.CloseOverlayMenu):
                break;

            case (MainGameState.InMenu, MenuActionType.OpenInventoryMenu):
                _canvas.ApplyMenuAction(type);
                break;

            case (MainGameState.InMenu, MenuActionType.OpenMainMenu):
                _canvas.ApplyMenuAction(type);
                break;

            case (MainGameState.InMenu, MenuActionType.OpenMapMenu):
                _canvas.ApplyMenuAction(type);
                break;
            case (MainGameState.InMenu, MenuActionType.CloseOverlayMenu):
                _canvas.ApplyMenuAction(type);
                SetGameState(MainGameState.Active);
                break;

            case (MainGameState.Inactive, MenuActionType.CloseOverlayMenu):
                _canvas.ApplyMenuAction(type);
                break;
            case (MainGameState.Inactive, _):
                break;
        }
    }

    public void ConsumeOnHotbarSlotPressed(int slotNumber)
    {
        Logger.Info($"Consume hotbar slot {slotNumber}");
        switch (_gameState)
        {
            case MainGameState.Active:
                PlayerMock.Instance.OnHotbarSlotPressed(slotNumber);
                break;
            case MainGameState.InMenu:
            case MainGameState.Inactive:
                // Intentionally ignore menu requests.
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(_gameState), _gameState, null);
        }
    }
}