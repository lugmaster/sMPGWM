using sMPGWM.Scripts.UtilitySystem.UiComponents;
using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record BehaviourDefinition(
    string Id, string DisplayName, string Description, IconDefinition? Icon,
    BehaviourDisposition Disposition, BehaviourLifetime Lifetime, BehaviourStacking Stacking, bool OrdinaryRemoval,
    IReadOnlyList<string> ClassificationIds, IReadOnlyList<ModifierDefinition> Modifiers,
    IReadOnlyList<string> GrantedConditions, IReadOnlyList<string> GrantedConditionsAfterFirstTick,
    IReadOnlyList<string> BlockedByConditions, BlockedBehaviourPolicy? BlockedPolicy,
    IReadOnlyList<ExecutionPhase> Interrupts, IReadOnlyList<DisabledCapability> DisabledCapabilities,
    string? OnApplyEffectId, string? OnExpireEffectId, BehaviourRemovalHook? OnRemove,
    LethalProtectionDefinition? LethalProtection, PeriodicAccumulatorDefinition? PeriodicAccumulator)
    : GameplayDefinition(Id, DisplayName, Description, Icon);
