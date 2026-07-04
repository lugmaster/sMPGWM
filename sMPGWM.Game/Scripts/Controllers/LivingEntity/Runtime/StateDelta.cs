using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Flags identifying which sections are present in a packed state delta.
/// </summary>
[System.Flags]
public enum DeltaPayloadFlags : byte
{
    None = 0,
    HasFinalStats = 1 << 0,
    HasFinalConditions = 1 << 1,
    HasFinalEffects = 1 << 2,
    HasCausalRecords = 1 << 3,
    HasDebugRecords = 1 << 4,
    CompressedPayload = 1 << 5
}

/// <summary>
/// Final authoritative values for one changed stat in a state delta.
/// </summary>
public struct ChangedStatPayload
{
    /// <summary>
    /// Stat array index corresponding to <see cref="sMPGWM.Scripts.Enums.Game.StatType"/>.
    /// </summary>
    public int StatIndex;

    /// <summary>
    /// Authoritative base value for the changed stat.
    /// </summary>
    public float BaseValue;

    /// <summary>
    /// Authoritative max value for the changed stat.
    /// </summary>
    public float MaxValue;

    /// <summary>
    /// Authoritative current value for the changed stat.
    /// </summary>
    public float CurrentValue;
}

/// <summary>
/// Structured representation of a decoded state-engine delta payload.
/// </summary>
public sealed class StateDelta
{
    /// <summary>
    /// Gets the delta format version.
    /// </summary>
    public byte DeltaVersion { get; init; }

    /// <summary>
    /// Gets the entity id corrected or updated by the delta.
    /// </summary>
    public long EntityId { get; init; }

    /// <summary>
    /// Gets the network tick represented by the delta.
    /// </summary>
    public long NetworkTick { get; init; }

    /// <summary>
    /// Gets the simulation lane associated with the delta tick.
    /// </summary>
    public SimulationLane SimulationLane { get; init; }

    /// <summary>
    /// Gets flags describing payload sections present in the delta.
    /// </summary>
    public DeltaPayloadFlags PayloadFlags { get; init; }

    /// <summary>
    /// Gets the changed-stat mask carried by the delta.
    /// </summary>
    public ulong DirtyStats { get; init; }

    /// <summary>
    /// Gets final authoritative effective condition flags.
    /// </summary>
    public ulong ConditionFlags { get; init; }

    /// <summary>
    /// Gets final authoritative stat payloads for changed stats.
    /// </summary>
    public ChangedStatPayload[] ChangedStats { get; init; } = [];

    /// <summary>
    /// Gets final authoritative active-effect payloads when included.
    /// </summary>
    public ActiveEffect[] ActiveEffects { get; init; } = [];

    /// <summary>
    /// Gets optional causal records included for tests, debug, or replay.
    /// </summary>
    public CausalRecord[] CausalRecords { get; init; } = [];
}
