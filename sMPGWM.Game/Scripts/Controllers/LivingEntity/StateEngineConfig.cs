using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class StateEngineConfig
{
    public int StateTicksPerSecond { get; init; } = 30;
    public int SlowSimulationDivisor { get; init; } = 2;
    public int GlobalRegenerationIntervalTicks { get; init; } = 30;
    public int GlobalSlowRegenerationIntervalTicks { get; init; } = 15;
    public int MaxChildEffectDepth { get; init; } = 5;
    public int MaxChildEffectsPerEntityPerTick { get; init; } = 3;
    public int MaxIncomingEffectsPerEntityPerTick { get; init; } = 20;
    public int MaxEffectsProcessedPerEntityPerTick { get; init; } = 96;
    public int DefaultActiveEffectCapacity { get; init; } = 35;
    public int PlayerActiveEffectCapacity { get; init; } = 80;
    public int SimpleNpcActiveEffectCapacity { get; init; } = 35;
    public int SnapshotRetentionTicks { get; init; } = 45;
    public int MinSnapshotRetentionTicks { get; init; } = 30;
    public int MaxSnapshotRetentionTicks { get; init; } = 60;
    public int StatComparisonPrecisionDecimals { get; init; } = 3;
    public bool CausalRecordsEnabledForDeterministicTests { get; init; } = true;
    public bool CausalRecordsEnabledForReleaseNetworking { get; init; }

    public static StateEngineConfig Default { get; } = new();

    public int ClampedSnapshotRetentionTicks =>
        Math.Clamp(SnapshotRetentionTicks, MinSnapshotRetentionTicks, MaxSnapshotRetentionTicks);
}
