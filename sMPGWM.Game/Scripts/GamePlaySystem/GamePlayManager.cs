using sMPGWM.Scripts.PlayerControllerSystem;
using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UtilitySystem;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : AbstractSingleton<GamePlayManager>
{
    private GameSession? _session;
    private SessionCoordinator? _coordinator;
    private MainGameState _gameState = MainGameState.Inactive;
    public PlayerController LocalPlayer => _session?.Player!;
    public MessageController MessageController => _session?.Messages!;
    public MainGameState GameState => _gameState;

    public RestoredWorldState CaptureState()
    {
        if (_coordinator?.State != SessionState.Active || _session == null)
            throw new InvalidOperationException("Only an active authoritative session can be captured.");
        return _session.CaptureState();
    }

    protected override void OnReady()
    {
        var flow = GameFlowManager.Instance;
        _coordinator = flow.Session;
        if (_coordinator == null)
        {
            flow.QuitToStartMenuBlocking("No session startup was supplied.");
            return;
        }
        _coordinator.Start(scope =>
        {
            var startup = flow.ConsumeNetworkStartup()
                ?? throw new InvalidOperationException("No network startup was supplied.");
            var canvas = GetNode<UiCanvas>("UiCanvas");
            var world = GetNode<Node>("WorldViewPortContainer/WorldViewport/WorldScene");
            // Clear published references after every other resource has been released.
            scope.Defer(() => _session = null);
            _session = GameSessionFactory.Create(scope, this, canvas, world, startup, _coordinator);
            flow.StateChanged += NetworkStateChanged;
            scope.Defer(() => flow.StateChanged -= NetworkStateChanged);
            scope.Defer(StopSession);
            NetworkStateChanged(flow.GameFlowState);
            // Start on the next frame after the scene has completed readiness.
            CallDeferred(nameof(StartNetwork));
        });
    }

    private void StartNetwork()
    {
        if (_coordinator?.State == SessionState.Starting) _session?.Network.CreateNetwork();
    }

    protected override void OnExitTree() => _coordinator?.Stop();
    private void StopSession() => SetGameState(MainGameState.Inactive);
    private void NetworkStateChanged(GameFlowState state) =>
        SetGameState(state == GameFlowState.InGame ? MainGameState.Active : MainGameState.Inactive);

    private void SetGameState(MainGameState newGameState)
    {
        if (!Enum.IsDefined(newGameState))
            throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null);
        if (newGameState == _gameState) return;
        _gameState = newGameState;
        if (_session != null && GodotObject.IsInstanceValid(_session.Player) && _session.Player.IsInsideTree())
            _session.Player.SetSessionActive(newGameState == MainGameState.Active);
        _session?.Messages.SetActive(newGameState == MainGameState.Active);
    }
}
