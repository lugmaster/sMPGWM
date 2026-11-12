using sMPGWM.Scripts.UtilitySystem.UiComponents;
using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record AbilityDefinition(
    string Id, string DisplayName, string Description, IconDefinition? Icon,
    string TargetingId, string EffectId, ActivationKind Activation, double CooldownMs,
    IReadOnlyList<AbilityCost> Costs, AbilityAvailability Availability, IReadOnlyList<string> BlockedByConditions)
    : GameplayDefinition(Id, DisplayName, Description, Icon);
