using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

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

public struct ChangedStatPayload
{
    public int StatIndex;
    public float BaseValue;
    public float MaxValue;
    public float CurrentValue;
}

public sealed class StateDelta
{
    public byte DeltaVersion { get; init; }
    public long EntityId { get; init; }
    public long NetworkTick { get; init; }
    public SimulationLane SimulationLane { get; init; }
    public DeltaPayloadFlags PayloadFlags { get; init; }
    public ulong DirtyStats { get; init; }
    public ulong ConditionFlags { get; init; }
    public ChangedStatPayload[] ChangedStats { get; init; } = [];
    public ActiveEffect[] ActiveEffects { get; init; } = [];
    public CausalRecord[] CausalRecords { get; init; } = [];
}
