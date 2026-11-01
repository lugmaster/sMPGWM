namespace sMPGWM.Scripts.GameFlowSystem;

// StartMenu -> Loading -> InGame -> Disconnecting -> StartMenu.
// Host: Loading ends after world readiness and lobby creation.
// Client: Loading ends after a validated hello/reply RPC round trip.
// Failure or leaving during Loading also enters Disconnecting.
public enum GameFlowState
{
    StartMenu,
    Loading,
    InGame,
    Disconnecting
}
