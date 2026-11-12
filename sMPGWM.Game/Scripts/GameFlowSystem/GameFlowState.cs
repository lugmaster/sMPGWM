namespace sMPGWM.Scripts.GameFlowSystem;

// StartMenu -> Loading -> InGame -> Disconnecting -> StartMenu.
// Loading ends only after SessionInitialization verifies prepared state and local bindings.
// Clients also require complete authoritative baselines and host confirmation.
// Failure or leaving during Loading also enters Disconnecting.
public enum GameFlowState
{
    StartMenu,
    Loading,
    InGame,
    Disconnecting
}
