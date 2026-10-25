using System;
using Godot;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.Mocks;

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

    protected override void OnReady()
    {
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
