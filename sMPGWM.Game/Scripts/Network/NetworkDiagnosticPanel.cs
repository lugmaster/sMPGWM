using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.scripts.Network;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Network;

// Presentation only; the session and transports do not create UI controls.
public partial class NetworkDiagnosticPanel : CanvasLayer
{
    public NetworkManager SessionManager { get; init; } = null!;
    private Label _status = null!;
    private Label _lobby = null!;

    public override void _Ready()
    {
        Layer = 100;
        var panel = new VBoxContainer { Position = new Vector2(20, 20) };
        _status = new Label();
        _lobby = new Label();
        var leave = new Button { Text = "Return to menu" };
        leave.Pressed += () => GameFlowManager.Instance.QuitToStartMenu("Left diagnostic session.");
        panel.AddChild(_status);
        panel.AddChild(leave);
        panel.AddChild(new Label { Text = $"Logs: {ProjectSettings.GlobalizePath(Logger.LogFilePath)}" });
        panel.AddChild(_lobby);
        AddChild(panel);
        SessionManager.Session.Changed += Refresh;
        SessionManager.DiagnosticCompleted += Completed;
        Refresh(SessionManager.Session.State);
    }

    private void Completed(DiagnosticResult result) => Refresh(SessionManager.Session.State);

    private void Refresh(SessionState state)
    {
        _status.Text = SessionManager.DiagnosticResult?.Reason ??
            $"Steam {SessionManager.NetworkRole}: {state} (gameplay inactive)";
        _lobby.Text = SessionManager.LobbyId is { } lobby ? $"Private lobby ID: {lobby}" : "";
    }

    public override void _ExitTree()
    {
        SessionManager.Session.Changed -= Refresh;
        SessionManager.DiagnosticCompleted -= Completed;
    }
}
