using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UtilitySystem;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using PlayerMock = sMPGWM.Scripts.UtilitySystem.Mocks.PlayerMock;

namespace sMPGWM.Scripts.GamePlaySystem;

public partial class GamePlayManager : UtilitySystem.AbstractSingleton<GamePlayManager>
{
    private GameSession? _session;
    private SessionCoordinator? _coordinator;
    private MainGameState _gameState = MainGameState.Inactive;
    public PlayerControllerSystem.PlayerController LocalPlayer => _session?.Player!;
    public MessageController MessageController => _session?.Messages!;
    public MainGameState GameState => _gameState;
    public event Action<MainGameState>? OnGameStateChanged;
    [Export] public bool PopulateMockInventory { get; set; } = true;

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
            IGameFixture fixture = GameConfig.Development
                ? new DevelopmentGameFixture(PlayerMock.Instance, PopulateMockInventory)
                : new EmptyGameFixture();
            // Clear published references after every other resource has been released.
            scope.Defer(() => _session = null);
            _session = GameSessionFactory.Create(scope, this, canvas, world, startup, _coordinator, fixture);
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

    public override void _Process(double delta)
    {
        if (GameState == MainGameState.Active && _session != null)
            _session.Fixture.Tick(delta, _session.Messages);
    }

    protected override void OnExitTree() => _coordinator?.Stop();
    public void StopSession() => SetGameState(MainGameState.Inactive);
    private void NetworkStateChanged(GameFlowState state) =>
        SetGameState(state == GameFlowState.InGame ? MainGameState.Active : MainGameState.Inactive);

    public void SetGameState(MainGameState newGameState)
    {
        if (!Enum.IsDefined(newGameState))
            throw new ArgumentOutOfRangeException(nameof(newGameState), newGameState, null);
        if (newGameState == _gameState) return;
        _gameState = newGameState;
        if (_session != null && GodotObject.IsInstanceValid(_session.Player) && _session.Player.IsInsideTree())
            _session.Player.SetSessionActive(newGameState == MainGameState.Active);
        _session?.Messages.SetActive(newGameState == MainGameState.Active);
        OnGameStateChanged?.Invoke(newGameState);
    }
}
