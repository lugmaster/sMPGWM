namespace sMPGWM.Scripts.GamePlaySystem;

internal enum SessionInitializationStage
{
    Created,
    PreparingDefinitions,
    DefinitionsReady,
    Constructing,
    AwaitingSynchronization,
    Binding,
    Ready,
    Stopped
}
