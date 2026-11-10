using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record BehaviourRemovalHook(IReadOnlyList<BehaviourRemovalReason> ExceptReasons, string EffectId);
