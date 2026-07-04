using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Scratch-buffer record produced by effects before stat resolution.
/// </summary>
public struct StatModificationRecord
{
    /// <summary>
    /// Runtime effect id that produced the modification.
    /// </summary>
    public long SourceRuntimeEffectId;

    /// <summary>
    /// Effect definition id that produced the modification.
    /// </summary>
    public int EffectDefinitionId;

    /// <summary>
    /// Stat affected by the modification.
    /// </summary>
    public StatType TargetStat;

    /// <summary>
    /// Value bucket affected by the modification.
    /// </summary>
    public StatValueKind TargetValueKind;

    /// <summary>
    /// Whether the modification raises or reduces the value.
    /// </summary>
    public StatManipulation Manipulation;

    /// <summary>
    /// Whether the modification is flat or additive percentage.
    /// </summary>
    public StatCalculation Calculation;

    /// <summary>
    /// Magnitude of the modification.
    /// </summary>
    public float Value;

    /// <summary>
    /// Stack count applied to the modification.
    /// </summary>
    public int StackCount;

    /// <summary>
    /// Network tick when the source effect was applied.
    /// </summary>
    public long AppliedNetworkTick;

    /// <summary>
    /// Network tick when the modification was executed.
    /// </summary>
    public long ExecutionNetworkTick;

    /// <summary>
    /// Stable causal ordering sequence for this modification.
    /// </summary>
    public long CausalSequence;
}
