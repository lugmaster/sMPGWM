using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Effects;

public abstract record LivingEntityEffect(string EffectInstanceId, string SourceId);

public sealed record ReduceEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    float Amount)
    : LivingEntityEffect(EffectInstanceId, SourceId);

public sealed record RaiseEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    float Amount)
    : LivingEntityEffect(EffectInstanceId, SourceId);

public sealed record StatModifierEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    StatModifier Modifier)
    : LivingEntityEffect(EffectInstanceId, SourceId);

public sealed record ConditionEffect(
    string EffectInstanceId,
    string SourceId,
    LivingEntityCondition Condition,
    bool IsApplied,
    float Duration = 0f)
    : LivingEntityEffect(EffectInstanceId, SourceId);
