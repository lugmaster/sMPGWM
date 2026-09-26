namespace sMPGWM.Scripts.GameFlowSystem;

/// <summary>
///  
///  StartMenu
///     -> Join / Host
///  
///  Loading  
///     -> World synchronized / make game ready  
///  
///  InGame  
///     -> User leaves OR connection recovery failed  
///  
///  Disconnecting  
///     -> Session / world cleanup complete  
///  
///  StartMenu
/// 
/// </summary>  
public enum GameFlowState
{
    StartMenu,
    Loading,
    InGame,
    Disconnecting
}