using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.GameplayDataSystem;

public abstract record GameplayDefinition(string Id, string DisplayName, string Description, IconDefinition? Icon);
