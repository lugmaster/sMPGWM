using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Tests;

[TestFixture]
public class LivingEntityTests
{
    private const long EntityId = 1;

    [SetUp]
    public void SetUp()
    {
        EffectDefinitionRegistry.Clear();
        RegisterCoreDefinitions();
        StateEngine.Configure(new StateEngineConfig
        {
            CausalRecordsEnabledForDeterministicTests = true,
            SnapshotRetentionTicks = 45,
            GlobalRegenerationIntervalTicks = 30
        });
        StateEngine.Reset();
    }

    [Test]
    public void DeathAndRevive_FollowDeadBlockingRules()
    {
        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        StateEngine.ProcessLivingEntity(EntityId, 1, [Effect(1, 101, 1)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(0f));
            Assert.That(state.Condition.Contains(LivingEntityCondition.Dead), Is.True);
        });

        StateEngine.ProcessLivingEntity(EntityId, 2, [Effect(2, 102, 2)], 1);
        Assert.That(state.Health.CurrentValue, Is.EqualTo(0f));

        StateEngine.ProcessLivingEntity(EntityId, 3, [Effect(3, 103, 3)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(25f));
            Assert.That(state.Condition.Contains(LivingEntityCondition.Dead), Is.False);
        });
    }

    [Test]
    public void PoisonCondition_UsesAncestorClosure_AndCleanseRemovesUnderlyingEffect()
    {
        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        StateEngine.ProcessLivingEntity(EntityId, 1, [Effect(6, 201, 1)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(95f));
            Assert.That(state.Condition.Contains(LivingEntityCondition.Poison), Is.True);
            Assert.That(state.Condition.Contains(LivingEntityCondition.DamageOverTime), Is.True);
            Assert.That(state.Condition.Contains(LivingEntityCondition.Debuff), Is.True);
        });

        StateEngine.ProcessLivingEntity(EntityId, 2, [Effect(7, 202, 2)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(90f));
            Assert.That(state.Condition.Contains(LivingEntityCondition.Poison), Is.False);
            Assert.That(state.ActiveEffectCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void IndependentStacks_ReplaceOldest_WhenMaxStacksReached()
    {
        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        StateEngine.ProcessLivingEntity(EntityId, 1, [Effect(6, 301, 1)], 1);
        StateEngine.ProcessLivingEntity(EntityId, 2, [Effect(6, 302, 2)], 1);
        StateEngine.ProcessLivingEntity(EntityId, 3, [Effect(6, 303, 3)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.ActiveEffectCount, Is.EqualTo(2));
            Assert.That(state.ActiveEffects.Take(state.ActiveEffectCount).Select(effect => effect.RuntimeEffectId), Is.EquivalentTo(new[] { 302L, 303L }));
            Assert.That(state.Health.CurrentValue, Is.EqualTo(75f));
        });
    }

    [Test]
    public void ChildEffectDepthOverflow_IsRejectedDeterministically()
    {
        RegisterDepthChainDefinitions();
        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        var delta = StateEngine.ProcessLivingEntity(EntityId, 1, [Effect(20, 401, 1)], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(94f));
            Assert.That(DeltaReader.TryRead(delta, out var read), Is.True);
            Assert.That(read.CausalRecords.Any(record => record.Type == CausalRecordType.ChildEffectRejected), Is.True);
        });
    }

    [Test]
    public void DeltaRoundTrip_AppliesFinalAuthoritativeState()
    {
        var hostState = CreateState();
        StateEngine.AddLivingEntity(hostState);
        var delta = StateEngine.ProcessLivingEntity(EntityId, 1, [Effect(8, 501, 1)], 1);

        StateEngine.Reset();
        var clientState = CreateState();
        StateEngine.AddLivingEntity(clientState);

        StateEngine.ApplyDelta(EntityId, delta);

        Assert.Multiple(() =>
        {
            Assert.That(clientState.Shield.CurrentValue, Is.EqualTo(60f));
            Assert.That(clientState.NetworkTick, Is.EqualTo(1));
        });
    }

    [Test]
    public void RollbackCorrection_ReplaysStoredEffectHistoryFromCorrectedTick()
    {
        var state = CreateState();
        StateEngine.AddLivingEntity(state);
        StateEngine.ProcessLivingEntity(EntityId, 1, [], 0);
        StateEngine.ProcessLivingEntity(EntityId, 2, [Effect(8, 601, 2)], 1);

        var corrected = CreateState();
        corrected.NetworkTick = 1;
        corrected.Health = new Stat(100f, 80f);
        var correction = new DeltaWriter().Write(
            corrected,
            SimulationLane.Fast,
            1UL << (int)StatType.Health,
            false,
            false,
            [],
            0);

        StateEngine.ApplyDelta(EntityId, correction);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.CurrentValue, Is.EqualTo(80f));
            Assert.That(state.Shield.CurrentValue, Is.EqualTo(60f));
            Assert.That(state.NetworkTick, Is.EqualTo(2));
        });
    }

    [Test]
    public void DeterministicReplay_ProducesIdenticalStateAndDelta()
    {
        var first = RunReplay();
        var second = RunReplay();

        Assert.Multiple(() =>
        {
            Assert.That(second.State.Health.CurrentValue, Is.EqualTo(first.State.Health.CurrentValue));
            Assert.That(second.State.Condition, Is.EqualTo(first.State.Condition));
            Assert.That(second.Delta.ToArray(), Is.EqualTo(first.Delta.ToArray()));
        });
    }

    private static (LivingEntityState State, PackedByteArray Delta) RunReplay()
    {
        StateEngine.Reset();
        var state = CreateState();
        StateEngine.AddLivingEntity(state);
        var effects = new[]
        {
            Effect(6, 701, 1),
            Effect(8, 702, 1)
        };
        var delta = StateEngine.ProcessLivingEntity(EntityId, 1, effects, effects.Length);
        return (state, delta);
    }

    private static LivingEntityState CreateState()
    {
        var state = new LivingEntityState(80)
        {
            EntityId = EntityId,
            LivingEntityType = LivingEntityType.MpController
        };
        state.InitializeStat(StatType.Health, 100f);
        state.InitializeStat(StatType.EnergyStorage, 100f);
        state.InitializeStat(StatType.Shield, 100f);
        state.ClearDirty();
        return state;
    }

    private static ActiveEffect Effect(int definitionId, long runtimeId, long tick)
    {
        return ActiveEffectFactory.Create(definitionId, runtimeId, 10, 20, EntityId, tick);
    }

    private static void RegisterCoreDefinitions()
    {
        EffectDefinitionRegistry.RegisterRange(
        [
            new EffectDefinition
            {
                EffectDefinitionId = 1,
                Name = "Fatal Damage",
                Target = new EffectTarget(StatType.Health, StatValueKind.Current, StatManipulation.Reduce, StatCalculation.Flat, 200f)
            },
            new EffectDefinition
            {
                EffectDefinitionId = 2,
                Name = "Normal Heal",
                Target = new EffectTarget(StatType.Health, StatValueKind.Current, StatManipulation.Raise, StatCalculation.Flat, 25f)
            },
            new EffectDefinition
            {
                EffectDefinitionId = 3,
                Name = "Revive",
                CanTriggerWhileDead = true,
                ChildEffectDefinitionIds = [4, 5]
            },
            new EffectDefinition
            {
                EffectDefinitionId = 4,
                Name = "Restore Health",
                CanTriggerWhileDead = true,
                Target = new EffectTarget(StatType.Health, StatValueKind.Current, StatManipulation.Raise, StatCalculation.Flat, 25f)
            },
            new EffectDefinition
            {
                EffectDefinitionId = 5,
                Name = "Remove Dead",
                CanTriggerWhileDead = true,
                ConditionRemoveRequest = LivingEntityCondition.Dead
            },
            new EffectDefinition
            {
                EffectDefinitionId = 6,
                Name = "Poison",
                ClassificationConditions = LivingEntityCondition.Poison,
                ConditionAddRequest = LivingEntityCondition.Poison,
                Target = new EffectTarget(StatType.Health, StatValueKind.Current, StatManipulation.Reduce, StatCalculation.Flat, 5f),
                Trigger = new EffectTrigger(EffectTriggerType.OverTime, 1),
                Duration = EffectDuration.TemporaryTicks(5),
                StackPolicy = EffectStackPolicy.Stack,
                StackBehavior = EffectStackBehavior.IndependentStacks,
                MaxStacks = 2,
                MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest
            },
            new EffectDefinition
            {
                EffectDefinitionId = 7,
                Name = "Cleanse Poison",
                EffectRemovalRule = new EffectRemovalRule
                {
                    RequiredConditions = LivingEntityCondition.Poison,
                    IncludeChildConditions = true,
                    MaxRemoveCount = 1,
                    RemoveOldestFirst = true
                }
            },
            new EffectDefinition
            {
                EffectDefinitionId = 8,
                Name = "Shield Hit",
                Target = new EffectTarget(StatType.Shield, StatValueKind.Current, StatManipulation.Reduce, StatCalculation.Flat, 40f)
            }
        ]);

        var reports = EffectValidator.ValidateRegistered();
        Assert.That(reports.Where(report => report.Severity == ValidationSeverity.Error), Is.Empty);
    }

    private static void RegisterDepthChainDefinitions()
    {
        for (var i = 20; i <= 26; i++)
        {
            EffectDefinitionRegistry.Register(new EffectDefinition
            {
                EffectDefinitionId = i,
                CanTriggerWhileDead = true,
                Target = new EffectTarget(StatType.Health, StatValueKind.Current, StatManipulation.Reduce, StatCalculation.Flat, 1f),
                ChildEffectDefinitionIds = i == 26 ? [] : [i + 1]
            });
        }
    }
}
