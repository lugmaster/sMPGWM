using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Static authoritative RPG state engine for deterministic living-entity simulation.
/// </summary>
public static class StateEngine
{
    private static readonly Dictionary<long, LivingEntityStateTracker> Trackers = new();
    private static StateEngineConfig _config = StateEngineConfig.Default;
    private static DeltaWriter _deltaWriter = new();
    private static ActiveEffect[] _pendingChildEffects = new ActiveEffect[16];
    private static int _pendingChildEffectCount;
    private static StatModificationRecord[] _statModificationRecords = new StatModificationRecord[256];
    private static int _statModificationRecordCount;
    private static CausalRecord[] _causalRecords = new CausalRecord[256];
    private static int _causalRecordCount;
    private static float[] _oldBaseValues = new float[LivingEntityState.StatCount];
    private static float[] _oldMaxValues = new float[LivingEntityState.StatCount];
    private static float[] _oldCurrentValues = new float[LivingEntityState.StatCount];
    private static int[] _matchedRemovalIndices = new int[128];
    private static long _nextRuntimeEffectId = 1;
    private static long _nextInsertionSequence = 1;
    private static long _nextCausalSequence = 1;

    /// <summary>
    /// Gets the active state-engine configuration.
    /// </summary>
    public static StateEngineConfig Config => _config;

    /// <summary>
    /// Replaces the active configuration and resizes reusable scratch buffers as needed.
    /// </summary>
    public static void Configure(StateEngineConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _deltaWriter = new DeltaWriter();
        EnsureScratchCapacity(config.DefaultActiveEffectCapacity + config.MaxIncomingEffectsPerEntityPerTick + config.MaxChildEffectsPerEntityPerTick);
    }

    /// <summary>
    /// Clears all registered entities and resets deterministic runtime sequences.
    /// </summary>
    public static void Reset()
    {
        Trackers.Clear();
        _pendingChildEffectCount = 0;
        _statModificationRecordCount = 0;
        _causalRecordCount = 0;
        _nextRuntimeEffectId = 1;
        _nextInsertionSequence = 1;
        _nextCausalSequence = 1;
    }

    /// <summary>
    /// Adds a living-entity state to engine tracking and stores its initial snapshot.
    /// </summary>
    public static void AddLivingEntity(LivingEntityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.ClearDirty();
        Trackers[state.EntityId] = new LivingEntityStateTracker(state, _config);
        Trackers[state.EntityId].StoreSnapshot(state.NetworkTick);
    }

    /// <summary>
    /// Processes ordered active effects for one entity and returns a compact delta if state changed.
    /// </summary>
    public static PackedByteArray ProcessLivingEntity(
        long entityId,
        long networkTick,
        ActiveEffect[] activeEffects,
        int activeEffectCount)
    {
        var tracker = GetRequiredTracker(entityId);
        return ProcessLivingEntityInternal(tracker, networkTick, activeEffects, activeEffectCount, true, true);
    }

    /// <summary>
    /// Builds pull-based UI data for the latest tracked state of one entity.
    /// </summary>
    public static UiDto GetUpdateForUiLoop(long entityId)
    {
        var state = GetRequiredTracker(entityId).CurrentState;
        var stats = new UiStatDto[LivingEntityState.StatCount];
        for (var i = 0; i < LivingEntityState.StatCount; i++)
            stats[i] = new UiStatDto((StatType)i, state.CurrentValues[i], state.MaxValues[i]);

        var visibleCount = 0;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (!EffectDefinitionRegistry.TryGet(state.ActiveEffects[i].EffectDefinitionId, out var definition))
                continue;

            if (definition.Visibility is EffectVisibility.Hud or EffectVisibility.Menu)
                visibleCount++;
        }

        var visibleEffects = new UiEffectDto[visibleCount];
        var cursor = 0;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            var effect = state.ActiveEffects[i];
            if (!EffectDefinitionRegistry.TryGet(effect.EffectDefinitionId, out var definition))
                continue;

            if (definition.Visibility is not (EffectVisibility.Hud or EffectVisibility.Menu))
                continue;

            var target = definition.Target;
            var remainingTicks = definition.Duration.Type == EffectDurationType.Temporary
                ? Math.Max(0, effect.ExpireNetworkTick - state.NetworkTick)
                : -1;

            visibleEffects[cursor++] = new UiEffectDto(
                effect.RuntimeEffectId,
                definition.EffectDefinitionId,
                effect.SourceId,
                effect.OriginId,
                definition.Name,
                definition.Description,
                effect.StackCount,
                remainingTicks,
                definition.Trigger.Type,
                target?.Calculation,
                target?.TargetStat,
                definition.ClassificationConditions,
                definition.ConditionAddRequest,
                IsBlocked(state, definition),
                definition.Duration,
                definition.Trigger.IntervalTicks,
                target?.Manipulation,
                target?.Value ?? 0f);
        }

        return new UiDto
        {
            EntityId = state.EntityId,
            NetworkTick = state.NetworkTick,
            Stats = stats,
            Conditions = state.Condition,
            VisibleEffects = visibleEffects
        };
    }

    /// <summary>
    /// Applies an authoritative delta to an entity and resimulates stored ticks when required.
    /// </summary>
    public static void ApplyDelta(long entityId, PackedByteArray delta)
    {
        if (!DeltaReader.TryRead(delta, out var stateDelta))
            return;

        var tracker = GetRequiredTracker(entityId);
        var currentTick = tracker.CurrentState.NetworkTick;
        var correctionTarget = tracker.TryGetSnapshot(stateDelta.NetworkTick, out var snapshot)
            ? snapshot
            : tracker.CurrentState;

        ApplyFinalDelta(correctionTarget, stateDelta);

        if (stateDelta.NetworkTick < currentTick && tracker.TryGetSnapshot(stateDelta.NetworkTick, out snapshot))
        {
            tracker.CurrentState.CopyFrom(snapshot);
            for (var tick = stateDelta.NetworkTick + 1; tick <= currentTick; tick++)
            {
                if (tracker.TryGetEffectHistory(tick, out var history, out var count))
                    ProcessLivingEntityInternal(tracker, tick, history, count, false, false);
                else
                    ProcessLivingEntityInternal(tracker, tick, Array.Empty<ActiveEffect>(), 0, false, false);
            }
        }
        else
        {
            tracker.CurrentState.CopyFrom(correctionTarget);
        }

        tracker.DiscardOlderThan(stateDelta.NetworkTick);
    }

    /// <summary>
    /// Compares two states using configured stat precision and exact effective-condition matching.
    /// </summary>
    public static bool StatesEqualForReconciliation(LivingEntityState left, LivingEntityState right)
    {
        var scale = MathF.Pow(10, _config.StatComparisonPrecisionDecimals);
        for (var i = 0; i < LivingEntityState.StatCount; i++)
        {
            if (MathF.Round(left.BaseValues[i] * scale) != MathF.Round(right.BaseValues[i] * scale) ||
                MathF.Round(left.MaxValues[i] * scale) != MathF.Round(right.MaxValues[i] * scale) ||
                MathF.Round(left.CurrentValues[i] * scale) != MathF.Round(right.CurrentValues[i] * scale))
            {
                return false;
            }
        }

        return left.ConditionFlags == right.ConditionFlags;
    }

    private static PackedByteArray ProcessLivingEntityInternal(
        LivingEntityStateTracker tracker,
        long networkTick,
        ActiveEffect[] activeEffects,
        int activeEffectCount,
        bool storeHistory,
        bool emitDelta)
    {
        var state = tracker.CurrentState;
        EnsureScratchCapacity(state.ActiveEffectCapacity + activeEffectCount + _config.MaxChildEffectsPerEntityPerTick);
        ClearScratch();
        CaptureOldValues(state);
        var oldConditionFlags = state.ConditionFlags;

        state.NetworkTick = networkTick;
        state.ClearDirty();

        if (storeHistory)
            tracker.StoreEffectHistory(networkTick, activeEffects, activeEffectCount);

        var acceptedIncomingCount = Math.Min(activeEffectCount, _config.MaxIncomingEffectsPerEntityPerTick);
        for (var i = 0; i < acceptedIncomingCount; i++)
        {
            var incoming = activeEffects[i];
            if (incoming.TargetEntityId != state.EntityId || incoming.EffectNetworkTick != networkTick)
                continue;

            if (!EffectDefinitionRegistry.TryGet(incoming.EffectDefinitionId, out var definition))
                continue;

            PrepareEffectForAcceptance(ref incoming, definition, networkTick);
            AcceptEffect(state, ref incoming, definition);
        }

        if (activeEffectCount > acceptedIncomingCount)
            AddCausal(CausalRecordType.CapacityOverflow, 0, 0, StatType.Health, activeEffectCount - acceptedIncomingCount, networkTick);

        ProcessEffectsForTick(state, networkTick);
        CompactRemovedEffects(state, networkTick);
        ResolveStats(state, networkTick);
        RebuildConditions(state);
        MarkDirtyFromOldValues(state, oldConditionFlags);

        tracker.StoreSnapshot(networkTick);

        if (!emitDelta || (state.DirtyStats == 0 && !state.DirtyConditions && !state.DirtyEffects))
            return PackedByteArray.Empty;

        return _deltaWriter.Write(
            state,
            networkTick % _config.SlowSimulationDivisor == 0 ? SimulationLane.Slow : SimulationLane.Fast,
            state.DirtyStats,
            state.DirtyConditions,
            state.DirtyEffects,
            _causalRecords,
            _config.CausalRecordsEnabledForDeterministicTests ? _causalRecordCount : 0);
    }

    private static void ProcessEffectsForTick(LivingEntityState state, long networkTick)
    {
        var rounds = 0;
        do
        {
            _pendingChildEffectCount = 0;
            ProcessActiveEffects(state, networkTick);
            ProcessStatTriggeredEffects(state, networkTick);
            CompactRemovedEffects(state, networkTick);

            if (_pendingChildEffectCount == 0)
                break;

            var pendingCount = _pendingChildEffectCount;
            for (var i = 0; i < pendingCount; i++)
            {
                var child = _pendingChildEffects[i];
                if (!EffectDefinitionRegistry.TryGet(child.EffectDefinitionId, out var childDefinition))
                    continue;

                AcceptEffect(state, ref child, childDefinition);
            }

            rounds++;
        } while (rounds <= _config.MaxChildEffectDepth);
    }

    private static void ProcessActiveEffects(LivingEntityState state, long networkTick)
    {
        var processed = 0;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (processed >= _config.MaxEffectsProcessedPerEntityPerTick)
                break;

            ref var effect = ref state.ActiveEffects[i];
            if ((effect.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
                continue;

            if (!EffectDefinitionRegistry.TryGet(effect.EffectDefinitionId, out var definition))
            {
                MarkEffectForRemoval(ref effect);
                continue;
            }

            if (!IsLaneDue(definition, networkTick))
                continue;

            var isExpired = definition.Duration.Type == EffectDurationType.Temporary &&
                            networkTick >= effect.ExpireNetworkTick;
            if (isExpired && definition.Trigger.Type != EffectTriggerType.OnExpire)
            {
                MarkEffectForRemoval(ref effect);
                AddCausal(CausalRecordType.EffectExpired, effect.RuntimeEffectId, effect.EffectDefinitionId, StatType.Health, 0f, networkTick);
                continue;
            }

            if (!ShouldExecuteEffect(effect, definition, state, networkTick))
                continue;

            if (IsBlocked(state, definition))
            {
                effect.RuntimeFlags |= EffectRuntimeFlags.BlockedLastExecution;
                AddCausal(CausalRecordType.EffectBlocked, effect.RuntimeEffectId, effect.EffectDefinitionId, StatType.Health, 0f, networkTick);
                if (definition.BlockPolicy == EffectBlockPolicy.ConsumeOnFail && definition.Duration.Type == EffectDurationType.Once)
                    MarkEffectForRemoval(ref effect);
                continue;
            }

            ExecuteEffect(state, ref effect, definition, networkTick);
            processed++;

            if (definition.Duration.Type == EffectDurationType.Once ||
                definition.Trigger.Type == EffectTriggerType.OnExpire)
            {
                MarkEffectForRemoval(ref effect);
            }
        }
    }

    private static void ProcessStatTriggeredEffects(LivingEntityState state, long networkTick)
    {
        if (_statModificationRecordCount == 0)
            return;

        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            ref var effect = ref state.ActiveEffects[i];
            if ((effect.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
                continue;

            if (!EffectDefinitionRegistry.TryGet(effect.EffectDefinitionId, out var definition))
                continue;

            if (definition.Trigger.Type is not (EffectTriggerType.OnStatReduced or EffectTriggerType.OnStatRaised))
                continue;

            if (effect.LastExecutedNetworkTick == networkTick || IsBlocked(state, definition))
                continue;

            if (!HasMatchingStatTriggerRecord(definition))
                continue;

            ExecuteEffect(state, ref effect, definition, networkTick);
        }
    }

    private static bool HasMatchingStatTriggerRecord(EffectDefinition definition)
    {
        for (var i = 0; i < _statModificationRecordCount; i++)
        {
            var record = _statModificationRecords[i];
            if (record.TargetStat != definition.Trigger.TrackedStat)
                continue;

            if (definition.Trigger.Type == EffectTriggerType.OnStatReduced &&
                record.Manipulation == StatManipulation.Reduce)
            {
                return true;
            }

            if (definition.Trigger.Type == EffectTriggerType.OnStatRaised &&
                record.Manipulation == StatManipulation.Raise)
            {
                return true;
            }
        }

        return false;
    }

    private static void ExecuteEffect(
        LivingEntityState state,
        ref ActiveEffect effect,
        EffectDefinition definition,
        long networkTick)
    {
        effect.LastExecutedNetworkTick = networkTick;
        effect.RuntimeFlags |= EffectRuntimeFlags.ExecutedOnce;
        effect.RuntimeFlags &= ~EffectRuntimeFlags.BlockedLastExecution;
        AddCausal(CausalRecordType.EffectExecuted, effect.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, networkTick);

        if (definition.Target.HasValue)
        {
            var target = definition.Target.Value;
            if (target.TargetValueKind == StatValueKind.Current ||
                definition.Duration.Type == EffectDurationType.Once)
            {
                AddStatModificationRecord(effect, target, networkTick);
            }
        }

        if (definition.ConditionAddRequest != LivingEntityCondition.None)
        {
            if (definition.Duration.Type == EffectDurationType.Once)
                state.AddExplicitCondition(definition.ConditionAddRequest);

            AddCausal(CausalRecordType.ConditionAddAccepted, effect.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, networkTick);
        }

        if (definition.ConditionRemoveRequest != LivingEntityCondition.None)
        {
            state.RemoveExplicitConditionCascade(definition.ConditionRemoveRequest);
            AddCausal(CausalRecordType.ConditionRemoveAccepted, effect.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, networkTick);
        }

        if (definition.EffectRemovalRule != null)
            ApplyEffectRemovalRule(state, effect, definition, definition.EffectRemovalRule, networkTick);

        QueueChildEffects(state, effect, definition, networkTick);
    }

    private static void ResolveStats(LivingEntityState state, long networkTick)
    {
        for (var statIndex = 0; statIndex < LivingEntityState.StatCount; statIndex++)
        {
            var statType = (StatType)statIndex;
            var baseValue = MathF.Max(0f, state.BaseValues[statIndex]);
            var maxFlat = 0f;
            var maxPercent = 0f;
            var currentFlat = 0f;
            var currentPercent = 0f;

            CollectActiveMaxModifiers(state, statType, ref maxFlat, ref maxPercent);
            CollectStatRecords(statType, StatValueKind.Max, ref maxFlat, ref maxPercent);

            var previousMax = _oldMaxValues[statIndex];
            var newMax = MathF.Max(0f, (baseValue + maxFlat) * (1f + maxPercent));
            var current = state.CurrentValues[statIndex];
            var maxDelta = newMax - previousMax;
            if (maxDelta > 0f)
                current = MathF.Min(current + maxDelta, newMax);
            else if (current > newMax)
                current = newMax;

            CollectStatRecords(statType, StatValueKind.Current, ref currentFlat, ref currentPercent);
            current += currentFlat;
            current *= 1f + currentPercent;
            current = Math.Clamp(current, 0f, newMax);

            state.BaseValues[statIndex] = baseValue;
            state.MaxValues[statIndex] = newMax;
            state.CurrentValues[statIndex] = current;
        }

        ApplyRegeneration(state, networkTick);
    }

    private static void ApplyRegeneration(LivingEntityState state, long networkTick)
    {
        if (_config.GlobalRegenerationIntervalTicks > 0 &&
            networkTick % _config.GlobalRegenerationIntervalTicks == 0)
        {
            ApplyCurrentDelta(state, StatType.Health, state.CurrentValues[(int)StatType.HealthRegeneration]);
            ApplyCurrentDelta(state, StatType.EnergyStorage, state.CurrentValues[(int)StatType.EnergyProduction]);
            ApplyCurrentDelta(state, StatType.EnergyStorage, -state.CurrentValues[(int)StatType.EnergyConsumption]);
            ApplyCurrentDelta(state, StatType.Shield, state.CurrentValues[(int)StatType.ShieldRegeneration]);
        }

        if (_config.GlobalSlowRegenerationIntervalTicks <= 0 ||
            networkTick % _config.SlowSimulationDivisor != 0)
        {
            return;
        }

        var slowTick = networkTick / _config.SlowSimulationDivisor;
        if (slowTick % _config.GlobalSlowRegenerationIntervalTicks != 0)
            return;

        ApplyCurrentDelta(state, StatType.OxygenStorage, -state.CurrentValues[(int)StatType.OxygenConsumption]);
        ApplyCurrentDelta(state, StatType.OxygenStorage, state.CurrentValues[(int)StatType.OxygenGeneration]);
        ApplyCurrentDelta(state, StatType.FoodStorage, -state.CurrentValues[(int)StatType.FoodConsumption]);
        ApplyCurrentDelta(state, StatType.FoodStorage, state.CurrentValues[(int)StatType.FoodGeneration]);
        ApplyCurrentDelta(state, StatType.WaterStorage, -state.CurrentValues[(int)StatType.WaterConsumption]);
        ApplyCurrentDelta(state, StatType.WaterStorage, state.CurrentValues[(int)StatType.WaterGeneration]);
    }

    private static void ApplyCurrentDelta(LivingEntityState state, StatType statType, float delta)
    {
        if (delta == 0f)
            return;

        var index = (int)statType;
        state.CurrentValues[index] = Math.Clamp(
            state.CurrentValues[index] + delta,
            0f,
            state.MaxValues[index]);
    }

    private static void RebuildConditions(LivingEntityState state)
    {
        var effectGranted = 0UL;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            var effect = state.ActiveEffects[i];
            if ((effect.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
                continue;

            if (!EffectDefinitionRegistry.TryGet(effect.EffectDefinitionId, out var definition))
                continue;

            if (definition.Duration.Type != EffectDurationType.Once)
                effectGranted |= (ulong)definition.ConditionAddRequest;
        }

        var inferred = 0UL;
        if (state.CurrentValues[(int)StatType.Health] <= 0f)
            inferred |= (ulong)LivingEntityCondition.Dead;

        if (state.CurrentValues[(int)StatType.EnergyStorage] <= 0f)
            inferred |= (ulong)LivingEntityCondition.EnergyDepleted;

        state.SetDerivedConditionFlags(effectGranted | inferred);
    }

    private static void CollectActiveMaxModifiers(
        LivingEntityState state,
        StatType statType,
        ref float flat,
        ref float percent)
    {
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            var effect = state.ActiveEffects[i];
            if ((effect.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
                continue;

            if (!EffectDefinitionRegistry.TryGet(effect.EffectDefinitionId, out var definition) ||
                definition.Duration.Type == EffectDurationType.Once ||
                !definition.Target.HasValue)
            {
                continue;
            }

            var target = definition.Target.Value;
            if (target.TargetStat != statType || target.TargetValueKind != StatValueKind.Max)
                continue;

            AddModifier(target, Math.Max(1, effect.StackCount), ref flat, ref percent);
        }
    }

    private static void CollectStatRecords(
        StatType statType,
        StatValueKind valueKind,
        ref float flat,
        ref float percent)
    {
        for (var i = 0; i < _statModificationRecordCount; i++)
        {
            var record = _statModificationRecords[i];
            if (record.TargetStat != statType || record.TargetValueKind != valueKind)
                continue;

            var signed = record.Manipulation == StatManipulation.Raise ? record.Value : -record.Value;
            signed *= Math.Max(1, record.StackCount);
            if (record.Calculation == StatCalculation.Flat)
                flat += signed;
            else
                percent += signed;
        }
    }

    private static void AddModifier(EffectTarget target, int stackCount, ref float flat, ref float percent)
    {
        var signed = target.Manipulation == StatManipulation.Raise ? target.Value : -target.Value;
        signed *= stackCount;
        if (target.Calculation == StatCalculation.Flat)
            flat += signed;
        else
            percent += signed;
    }

    private static void AddStatModificationRecord(ActiveEffect effect, EffectTarget target, long networkTick)
    {
        EnsureStatRecordCapacity(_statModificationRecordCount + 1);
        _statModificationRecords[_statModificationRecordCount++] = new StatModificationRecord
        {
            SourceRuntimeEffectId = effect.RuntimeEffectId,
            EffectDefinitionId = effect.EffectDefinitionId,
            TargetStat = target.TargetStat,
            TargetValueKind = target.TargetValueKind,
            Manipulation = target.Manipulation,
            Calculation = target.Calculation,
            Value = target.Value,
            StackCount = Math.Max(1, effect.StackCount),
            AppliedNetworkTick = effect.AppliedNetworkTick,
            ExecutionNetworkTick = networkTick,
            CausalSequence = _nextCausalSequence++
        };

        AddCausal(CausalRecordType.StatModificationRecord, effect.RuntimeEffectId, effect.EffectDefinitionId, target.TargetStat, target.Value, networkTick);
    }

    private static bool ShouldExecuteEffect(
        ActiveEffect effect,
        EffectDefinition definition,
        LivingEntityState state,
        long networkTick)
    {
        return definition.Trigger.Type switch
        {
            EffectTriggerType.Immediate => (effect.RuntimeFlags & EffectRuntimeFlags.ExecutedOnce) == 0,
            EffectTriggerType.OverTime => IsIntervalDue(effect, definition, networkTick),
            EffectTriggerType.OnExpire => definition.Duration.Type == EffectDurationType.Temporary &&
                                          networkTick >= effect.ExpireNetworkTick,
            EffectTriggerType.OnConditionPresent => state.Condition.Contains(definition.Trigger.RequiredCondition) &&
                                                    IsIntervalDue(effect, definition, networkTick),
            EffectTriggerType.OnStatRaised or EffectTriggerType.OnStatReduced => false,
            _ => false
        };
    }

    private static bool IsIntervalDue(ActiveEffect effect, EffectDefinition definition, long networkTick)
    {
        if (definition.Duration.Type == EffectDurationType.Temporary && networkTick >= effect.ExpireNetworkTick)
            return false;

        var interval = Math.Max(1, definition.Trigger.IntervalTicks);
        return effect.LastExecutedNetworkTick == long.MinValue ||
               networkTick - effect.LastExecutedNetworkTick >= interval;
    }

    private static bool IsLaneDue(EffectDefinition definition, long networkTick)
    {
        return definition.SimulationLane == SimulationLane.Fast ||
               networkTick % _config.SlowSimulationDivisor == 0;
    }

    private static bool IsBlocked(LivingEntityState state, EffectDefinition definition)
    {
        var blockedBy = (ulong)definition.BlockedBy;
        if (!definition.CanTriggerWhileDead)
            blockedBy |= (ulong)LivingEntityCondition.Dead;

        return blockedBy != 0 && (state.ConditionFlags & blockedBy) != 0;
    }

    private static void AcceptEffect(
        LivingEntityState state,
        ref ActiveEffect effect,
        EffectDefinition definition)
    {
        if (definition.StackPolicy == EffectStackPolicy.Discard &&
            FindFirstDuplicate(state, effect, definition) >= 0)
        {
            return;
        }

        if (definition.StackPolicy == EffectStackPolicy.Replace)
        {
            var duplicateIndex = FindFirstDuplicate(state, effect, definition);
            if (duplicateIndex >= 0)
            {
                effect.InsertionSequence = state.ActiveEffects[duplicateIndex].InsertionSequence;
                state.ActiveEffects[duplicateIndex] = effect;
                state.DirtyEffects = true;
                AddCausal(CausalRecordType.StackReplaced, effect.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, effect.EffectNetworkTick);
                return;
            }
        }

        if (definition.StackPolicy == EffectStackPolicy.Stack &&
            definition.StackBehavior != EffectStackBehavior.IndependentStacks)
        {
            var duplicateIndex = FindFirstDuplicate(state, effect, definition);
            if (duplicateIndex >= 0)
            {
                ApplyCombinedStackBehavior(ref state.ActiveEffects[duplicateIndex], effect, definition);
                state.DirtyEffects = true;
                return;
            }
        }

        if (definition.StackPolicy == EffectStackPolicy.Stack &&
            definition.StackBehavior == EffectStackBehavior.IndependentStacks)
        {
            var stackCount = CountMatchingStacks(state, effect, definition);
            if (stackCount >= Math.Max(1, definition.MaxStacks))
            {
                if (!HandleStackOverflow(state, ref effect, definition))
                    return;
            }
        }

        AddActiveEffect(state, effect);
    }

    private static void AddActiveEffect(LivingEntityState state, ActiveEffect effect)
    {
        if (state.ActiveEffectCount >= state.ActiveEffects.Length)
        {
            state.CapacityOverflowCount++;
            AddCausal(CausalRecordType.CapacityOverflow, effect.RuntimeEffectId, effect.EffectDefinitionId, StatType.Health, 0f, effect.EffectNetworkTick);
            return;
        }

        state.ActiveEffects[state.ActiveEffectCount++] = effect;
        state.DirtyEffects = true;
        AddCausal(CausalRecordType.StackAdded, effect.RuntimeEffectId, effect.EffectDefinitionId, StatType.Health, 0f, effect.EffectNetworkTick);
    }

    private static void ApplyCombinedStackBehavior(
        ref ActiveEffect existing,
        ActiveEffect incoming,
        EffectDefinition definition)
    {
        switch (definition.StackBehavior)
        {
            case EffectStackBehavior.RefreshDuration:
                existing.ExpireNetworkTick = incoming.ExpireNetworkTick;
                existing.LastExecutedNetworkTick = long.MinValue;
                AddCausal(CausalRecordType.StackRefreshed, existing.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, incoming.EffectNetworkTick);
                break;
            case EffectStackBehavior.AddStackRefreshDuration:
                existing.StackCount = Math.Min(Math.Max(1, definition.MaxStacks), existing.StackCount + 1);
                existing.ExpireNetworkTick = incoming.ExpireNetworkTick;
                AddCausal(CausalRecordType.StackRefreshed, existing.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, existing.StackCount, incoming.EffectNetworkTick);
                break;
            case EffectStackBehavior.AddStackNoRefresh:
                existing.StackCount = Math.Min(Math.Max(1, definition.MaxStacks), existing.StackCount + 1);
                AddCausal(CausalRecordType.StackAdded, existing.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, existing.StackCount, incoming.EffectNetworkTick);
                break;
        }
    }

    private static bool HandleStackOverflow(
        LivingEntityState state,
        ref ActiveEffect effect,
        EffectDefinition definition)
    {
        switch (definition.MaxStackOverflowPolicy)
        {
            case MaxStackOverflowPolicy.RejectNewest:
                return false;
            case MaxStackOverflowPolicy.RefreshOldest:
            {
                var oldest = FindOldestMatchingStack(state, effect, definition);
                if (oldest < 0)
                    return false;

                state.ActiveEffects[oldest].ExpireNetworkTick = effect.ExpireNetworkTick;
                state.ActiveEffects[oldest].LastExecutedNetworkTick = long.MinValue;
                state.DirtyEffects = true;
                return false;
            }
            case MaxStackOverflowPolicy.ReplaceWeakest:
            case MaxStackOverflowPolicy.ReplaceOldest:
            default:
            {
                var oldest = FindOldestMatchingStack(state, effect, definition);
                if (oldest < 0)
                    return false;

                effect.InsertionSequence = state.ActiveEffects[oldest].InsertionSequence;
                state.ActiveEffects[oldest] = effect;
                state.DirtyEffects = true;
                AddCausal(CausalRecordType.StackReplaced, effect.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, effect.EffectNetworkTick);
                return false;
            }
        }
    }

    private static int FindFirstDuplicate(LivingEntityState state, ActiveEffect effect, EffectDefinition definition)
    {
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (IsSameStackIdentity(state.ActiveEffects[i], effect, definition))
                return i;
        }

        return -1;
    }

    private static int CountMatchingStacks(LivingEntityState state, ActiveEffect effect, EffectDefinition definition)
    {
        var count = 0;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (IsSameStackIdentity(state.ActiveEffects[i], effect, definition))
                count++;
        }

        return count;
    }

    private static int FindOldestMatchingStack(LivingEntityState state, ActiveEffect effect, EffectDefinition definition)
    {
        var selected = -1;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (!IsSameStackIdentity(state.ActiveEffects[i], effect, definition))
                continue;

            if (selected < 0 || IsOlder(state.ActiveEffects[i], state.ActiveEffects[selected]))
                selected = i;
        }

        return selected;
    }

    private static bool IsSameStackIdentity(ActiveEffect existing, ActiveEffect incoming, EffectDefinition incomingDefinition)
    {
        if (!EffectDefinitionRegistry.TryGet(existing.EffectDefinitionId, out var existingDefinition))
            return false;

        if (existingDefinition.StackBaseId != incomingDefinition.StackBaseId)
            return false;

        return incomingDefinition.StackScope switch
        {
            EffectStackScope.SameSource => existing.SourceId == incoming.SourceId,
            EffectStackScope.SameSourceAndOrigin => existing.SourceId == incoming.SourceId &&
                                                    existing.OriginId == incoming.OriginId,
            _ => false
        };
    }

    private static bool IsOlder(ActiveEffect left, ActiveEffect right)
    {
        if (left.AppliedNetworkTick != right.AppliedNetworkTick)
            return left.AppliedNetworkTick < right.AppliedNetworkTick;
        if (left.InsertionSequence != right.InsertionSequence)
            return left.InsertionSequence < right.InsertionSequence;
        return left.RuntimeEffectId < right.RuntimeEffectId;
    }

    private static void QueueChildEffects(
        LivingEntityState state,
        ActiveEffect parent,
        EffectDefinition definition,
        long networkTick)
    {
        for (var i = 0; i < definition.ChildEffectDefinitionIds.Length; i++)
        {
            if (parent.ChildDepth + 1 > _config.MaxChildEffectDepth ||
                _pendingChildEffectCount >= _config.MaxChildEffectsPerEntityPerTick)
            {
                AddCausal(CausalRecordType.ChildEffectRejected, parent.RuntimeEffectId, definition.EffectDefinitionId, StatType.Health, 0f, networkTick);
                continue;
            }

            if (!EffectDefinitionRegistry.TryGet(definition.ChildEffectDefinitionIds[i], out var childDefinition))
                continue;

            var child = ActiveEffectFactory.Create(
                childDefinition,
                _nextRuntimeEffectId++,
                parent.SourceId,
                parent.OriginId,
                state.EntityId,
                networkTick,
                parent.RuntimeEffectId,
                parent.ChildDepth + 1,
                i);
            child.InsertionSequence = _nextInsertionSequence++;
            _pendingChildEffects[_pendingChildEffectCount++] = child;
            AddCausal(CausalRecordType.ChildEffectQueued, child.RuntimeEffectId, child.EffectDefinitionId, StatType.Health, 0f, networkTick);
        }
    }

    private static void ApplyEffectRemovalRule(
        LivingEntityState state,
        ActiveEffect requester,
        EffectDefinition requesterDefinition,
        EffectRemovalRule rule,
        long networkTick)
    {
        var matchCount = 0;
        for (var i = 0; i < state.ActiveEffectCount; i++)
        {
            if (MatchesRemovalRule(state.ActiveEffects[i], requester, requesterDefinition, rule))
                _matchedRemovalIndices[matchCount++] = i;
        }

        var maxRemove = rule.MaxRemoveCount <= 0 ? matchCount : Math.Min(rule.MaxRemoveCount, matchCount);
        for (var removed = 0; removed < maxRemove; removed++)
        {
            var selectedMatchIndex = SelectRemovalMatch(state, matchCount, rule.RemoveOldestFirst);
            if (selectedMatchIndex < 0)
                break;

            var activeIndex = _matchedRemovalIndices[selectedMatchIndex];
            MarkEffectForRemoval(ref state.ActiveEffects[activeIndex]);
            AddCausal(CausalRecordType.EffectRemoved, state.ActiveEffects[activeIndex].RuntimeEffectId,
                state.ActiveEffects[activeIndex].EffectDefinitionId, StatType.Health, 0f, networkTick);
            _matchedRemovalIndices[selectedMatchIndex] = -1;
            state.DirtyEffects = true;
        }
    }

    private static int SelectRemovalMatch(LivingEntityState state, int matchCount, bool oldestFirst)
    {
        var selected = -1;
        for (var i = 0; i < matchCount; i++)
        {
            var activeIndex = _matchedRemovalIndices[i];
            if (activeIndex < 0)
                continue;

            if (selected < 0)
            {
                selected = i;
                continue;
            }

            var candidate = state.ActiveEffects[activeIndex];
            var current = state.ActiveEffects[_matchedRemovalIndices[selected]];
            var candidateIsOlder = IsOlder(candidate, current);
            if ((oldestFirst && candidateIsOlder) || (!oldestFirst && !candidateIsOlder))
                selected = i;
        }

        return selected;
    }

    private static bool MatchesRemovalRule(
        ActiveEffect candidate,
        ActiveEffect requester,
        EffectDefinition requesterDefinition,
        EffectRemovalRule rule)
    {
        if ((candidate.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
            return false;

        if (!EffectDefinitionRegistry.TryGet(candidate.EffectDefinitionId, out var candidateDefinition))
            return false;

        if (rule.MatchEffectDefinitionId.HasValue &&
            candidate.EffectDefinitionId != rule.MatchEffectDefinitionId.Value)
        {
            return false;
        }

        if (rule.MatchStackGroupId.HasValue &&
            candidateDefinition.StackBaseId != rule.MatchStackGroupId.Value)
        {
            return false;
        }

        if (!MatchesSourcePolicy(candidate, requester, rule))
            return false;

        var classification = (ulong)candidateDefinition.ClassificationConditions;
        if (!MatchesRequiredConditions(classification, (ulong)rule.RequiredConditions, rule.IncludeChildConditions))
            return false;

        if (rule.ExcludedConditions != LivingEntityCondition.None &&
            MatchesRequiredConditions(classification, (ulong)rule.ExcludedConditions, rule.IncludeChildConditions))
        {
            return false;
        }

        return requesterDefinition.EffectDefinitionId != candidateDefinition.EffectDefinitionId ||
               requester.RuntimeEffectId != candidate.RuntimeEffectId;
    }

    private static bool MatchesSourcePolicy(ActiveEffect candidate, ActiveEffect requester, EffectRemovalRule rule)
    {
        return rule.SourceMatchPolicy switch
        {
            SourceMatchPolicy.AnySource => true,
            SourceMatchPolicy.SameSource => candidate.SourceId == requester.SourceId,
            SourceMatchPolicy.SameSourceAndOrigin => candidate.SourceId == requester.SourceId &&
                                                     candidate.OriginId == requester.OriginId,
            SourceMatchPolicy.ExplicitSourceId => rule.MatchSourceId.HasValue &&
                                                  candidate.SourceId == rule.MatchSourceId.Value,
            SourceMatchPolicy.ExplicitSourceAndOrigin => rule.MatchSourceId.HasValue &&
                                                         rule.MatchOriginId.HasValue &&
                                                         candidate.SourceId == rule.MatchSourceId.Value &&
                                                         candidate.OriginId == rule.MatchOriginId.Value,
            _ => false
        };
    }

    private static bool MatchesRequiredConditions(ulong classification, ulong required, bool includeChildConditions)
    {
        if (required == 0)
            return true;

        var mask = required;
        while (mask != 0)
        {
            var bit = mask & (~mask + 1);
            var requiredFamily = includeChildConditions ? ConditionRegistry.ExpandWithDescendants(bit) : bit;
            if ((classification & requiredFamily) == 0)
                return false;

            mask &= ~bit;
        }

        return true;
    }

    private static void CompactRemovedEffects(LivingEntityState state, long networkTick)
    {
        var write = 0;
        for (var read = 0; read < state.ActiveEffectCount; read++)
        {
            var effect = state.ActiveEffects[read];
            if ((effect.RuntimeFlags & EffectRuntimeFlags.PendingRemoval) != 0)
            {
                state.DirtyEffects = true;
                AddCausal(CausalRecordType.EffectExpired, effect.RuntimeEffectId, effect.EffectDefinitionId, StatType.Health, 0f, networkTick);
                continue;
            }

            if (write != read)
                state.ActiveEffects[write] = effect;

            write++;
        }

        for (var i = write; i < state.ActiveEffectCount; i++)
            state.ActiveEffects[i] = default;

        state.ActiveEffectCount = write;
    }

    private static void PrepareEffectForAcceptance(
        ref ActiveEffect effect,
        EffectDefinition definition,
        long networkTick)
    {
        if (effect.RuntimeEffectId == 0)
            effect.RuntimeEffectId = _nextRuntimeEffectId++;

        if (effect.StackCount < 1)
            effect.StackCount = 1;

        if (effect.AppliedNetworkTick == 0 && networkTick != 0)
            effect.AppliedNetworkTick = networkTick;

        if (effect.LastExecutedNetworkTick == 0)
            effect.LastExecutedNetworkTick = long.MinValue;

        if (definition.Duration.Type == EffectDurationType.Temporary &&
            effect.ExpireNetworkTick == 0)
        {
            effect.ExpireNetworkTick = networkTick + definition.Duration.Ticks;
        }
        else if (definition.Duration.Type != EffectDurationType.Temporary)
        {
            effect.ExpireNetworkTick = long.MaxValue;
        }

        effect.InsertionSequence = _nextInsertionSequence++;
    }

    private static void MarkEffectForRemoval(ref ActiveEffect effect)
    {
        effect.RuntimeFlags |= EffectRuntimeFlags.PendingRemoval;
    }

    private static void ApplyFinalDelta(LivingEntityState state, StateDelta delta)
    {
        state.NetworkTick = delta.NetworkTick;
        for (var i = 0; i < delta.ChangedStats.Length; i++)
        {
            var stat = delta.ChangedStats[i];
            state.BaseValues[stat.StatIndex] = stat.BaseValue;
            state.MaxValues[stat.StatIndex] = stat.MaxValue;
            state.CurrentValues[stat.StatIndex] = stat.CurrentValue;
        }

        if ((delta.PayloadFlags & DeltaPayloadFlags.HasFinalConditions) != 0)
            state.SetEffectiveConditionFlagsFromDelta(delta.ConditionFlags);

        if ((delta.PayloadFlags & DeltaPayloadFlags.HasFinalEffects) != 0)
        {
            var count = Math.Min(delta.ActiveEffects.Length, state.ActiveEffects.Length);
            Array.Copy(delta.ActiveEffects, state.ActiveEffects, count);
            for (var i = count; i < state.ActiveEffectCount; i++)
                state.ActiveEffects[i] = default;

            state.ActiveEffectCount = count;
        }

        state.ClearDirty();
    }

    private static void CaptureOldValues(LivingEntityState state)
    {
        Array.Copy(state.BaseValues, _oldBaseValues, LivingEntityState.StatCount);
        Array.Copy(state.MaxValues, _oldMaxValues, LivingEntityState.StatCount);
        Array.Copy(state.CurrentValues, _oldCurrentValues, LivingEntityState.StatCount);
    }

    private static void MarkDirtyFromOldValues(LivingEntityState state, ulong oldConditionFlags)
    {
        for (var i = 0; i < LivingEntityState.StatCount; i++)
        {
            if (!state.BaseValues[i].Equals(_oldBaseValues[i]) ||
                !state.MaxValues[i].Equals(_oldMaxValues[i]) ||
                !state.CurrentValues[i].Equals(_oldCurrentValues[i]))
            {
                state.DirtyStats |= 1UL << i;
            }
        }

        if (state.ConditionFlags != oldConditionFlags)
            state.DirtyConditions = true;
    }

    private static void ClearScratch()
    {
        _pendingChildEffectCount = 0;
        _statModificationRecordCount = 0;
        _causalRecordCount = 0;
    }

    private static void AddCausal(
        CausalRecordType type,
        long runtimeEffectId,
        int effectDefinitionId,
        StatType statType,
        float value,
        long networkTick)
    {
        if (!_config.CausalRecordsEnabledForDeterministicTests)
            return;

        EnsureCausalCapacity(_causalRecordCount + 1);
        _causalRecords[_causalRecordCount++] = new CausalRecord
        {
            Type = type,
            RuntimeEffectId = runtimeEffectId,
            EffectDefinitionId = effectDefinitionId,
            StatType = statType,
            Value = value,
            NetworkTick = networkTick
        };
    }

    private static LivingEntityStateTracker GetRequiredTracker(long entityId)
    {
        if (Trackers.TryGetValue(entityId, out var tracker))
            return tracker;

        throw new InvalidOperationException($"No living entity state registered for entity {entityId}.");
    }

    private static void EnsureScratchCapacity(int requiredEffectCapacity)
    {
        if (_pendingChildEffects.Length < _config.MaxChildEffectsPerEntityPerTick)
            Array.Resize(ref _pendingChildEffects, _config.MaxChildEffectsPerEntityPerTick);

        if (_matchedRemovalIndices.Length < requiredEffectCapacity)
            Array.Resize(ref _matchedRemovalIndices, requiredEffectCapacity);
    }

    private static void EnsureStatRecordCapacity(int required)
    {
        if (_statModificationRecords.Length < required)
            Array.Resize(ref _statModificationRecords, Math.Max(_statModificationRecords.Length * 2, required));
    }

    private static void EnsureCausalCapacity(int required)
    {
        if (_causalRecords.Length < required)
            Array.Resize(ref _causalRecords, Math.Max(_causalRecords.Length * 2, required));
    }
}
