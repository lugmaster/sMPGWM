using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Contains tunable limits and timing values used by the RPG state engine.
/// </summary>
public sealed class StateEngineConfig
{
    /// <summary>
    /// Gets the authoritative fast state-engine tick rate.
    /// </summary>
    public int StateTicksPerSecond { get; init; } = 30;

    /// <summary>
    /// Gets the divisor that maps fast ticks to slow-lane ticks.
    /// </summary>
    public int SlowSimulationDivisor { get; init; } = 2;

    /// <summary>
    /// Gets the fast-lane regeneration interval in network ticks.
    /// </summary>
    public int GlobalRegenerationIntervalTicks { get; init; } = 30;

    /// <summary>
    /// Gets the slow-lane regeneration interval in slow ticks.
    /// </summary>
    public int GlobalSlowRegenerationIntervalTicks { get; init; } = 15;

    /// <summary>
    /// Gets the maximum accepted child-effect depth per tick.
    /// </summary>
    public int MaxChildEffectDepth { get; init; } = 5;

    /// <summary>
    /// Gets the maximum queued child effects per entity per tick.
    /// </summary>
    public int MaxChildEffectsPerEntityPerTick { get; init; } = 3;

    /// <summary>
    /// Gets the maximum incoming effects accepted per entity per tick.
    /// </summary>
    public int MaxIncomingEffectsPerEntityPerTick { get; init; } = 20;

    /// <summary>
    /// Gets the maximum active effects processed per entity per tick.
    /// </summary>
    public int MaxEffectsProcessedPerEntityPerTick { get; init; } = 96;

    /// <summary>
    /// Gets the default fixed active-effect capacity for generic living entities.
    /// </summary>
    public int DefaultActiveEffectCapacity { get; init; } = 35;

    /// <summary>
    /// Gets the fixed active-effect capacity for player-controlled entities.
    /// </summary>
    public int PlayerActiveEffectCapacity { get; init; } = 80;

    /// <summary>
    /// Gets the fixed active-effect capacity for simple NPC or enemy entities.
    /// </summary>
    public int SimpleNpcActiveEffectCapacity { get; init; } = 35;

    /// <summary>
    /// Gets the requested number of network ticks retained for snapshots and effect history.
    /// </summary>
    public int SnapshotRetentionTicks { get; init; } = 45;

    /// <summary>
    /// Gets the minimum permitted snapshot-retention window.
    /// </summary>
    public int MinSnapshotRetentionTicks { get; init; } = 30;

    /// <summary>
    /// Gets the maximum permitted snapshot-retention window.
    /// </summary>
    public int MaxSnapshotRetentionTicks { get; init; } = 60;

    /// <summary>
    /// Gets the decimal precision used when comparing states during reconciliation.
    /// </summary>
    public int StatComparisonPrecisionDecimals { get; init; } = 3;

    /// <summary>
    /// Gets whether causal records are included for deterministic tests.
    /// </summary>
    public bool CausalRecordsEnabledForDeterministicTests { get; init; } = true;

    /// <summary>
    /// Gets whether causal records are included for release networking payloads.
    /// </summary>
    public bool CausalRecordsEnabledForReleaseNetworking { get; init; }

    /// <summary>
    /// Gets the default state-engine configuration.
    /// </summary>
    public static StateEngineConfig Default { get; } = new();

    /// <summary>
    /// Gets the requested snapshot retention clamped to the configured min/max bounds.
    /// </summary>
    public int ClampedSnapshotRetentionTicks =>
        Math.Clamp(SnapshotRetentionTicks, MinSnapshotRetentionTicks, MaxSnapshotRetentionTicks);
}
