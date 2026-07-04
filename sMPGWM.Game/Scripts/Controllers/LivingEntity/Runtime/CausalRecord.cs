using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Optional debug/replay record that explains why state changed during effect processing.
/// </summary>
public struct CausalRecord
{
    /// <summary>
    /// Causal event category.
    /// </summary>
    public CausalRecordType Type;

    /// <summary>
    /// Runtime effect id associated with the event.
    /// </summary>
    public long RuntimeEffectId;

    /// <summary>
    /// Effect definition id associated with the event.
    /// </summary>
    public int EffectDefinitionId;

    /// <summary>
    /// Stat associated with the event when applicable.
    /// </summary>
    public StatType StatType;

    /// <summary>
    /// Numeric value associated with the event when applicable.
    /// </summary>
    public float Value;

    /// <summary>
    /// Network tick on which the causal event occurred.
    /// </summary>
    public long NetworkTick;
}
