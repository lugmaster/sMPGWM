using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

public struct StatModificationRecord
{
    public long SourceRuntimeEffectId;
    public int EffectDefinitionId;
    public StatType TargetStat;
    public StatValueKind TargetValueKind;
    public StatManipulation Manipulation;
    public StatCalculation Calculation;
    public float Value;
    public int StackCount;
    public long AppliedNetworkTick;
    public long ExecutionNetworkTick;
    public long CausalSequence;
}
