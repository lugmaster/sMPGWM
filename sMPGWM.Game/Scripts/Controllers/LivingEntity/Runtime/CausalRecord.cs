using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

public struct CausalRecord
{
    public CausalRecordType Type;
    public long RuntimeEffectId;
    public int EffectDefinitionId;
    public StatType StatType;
    public float Value;
    public long NetworkTick;
}
