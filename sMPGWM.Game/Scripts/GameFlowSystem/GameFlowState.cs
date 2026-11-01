namespace sMPGWM.Scripts.GameFlowSystem;

// StartMenu -> Loading -> InGame -> Disconnecting -> StartMenu.
// Host: Loading ends after transport readiness and local player spawning.
// Client: Loading ends after applying initial spawns and host confirmation.
// Failure or leaving during Loading also enters Disconnecting.
public enum GameFlowState
{
    StartMenu,
    Loading,
    InGame,
    Disconnecting
}
