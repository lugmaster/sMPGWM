using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Tests;

[TestFixture]
public class StatTests
{
    private const long EntityId = 7;

    [SetUp]
    public void SetUp()
    {
        EffectDefinitionRegistry.Clear();
        StateEngine.Configure(new StateEngineConfig
        {
            CausalRecordsEnabledForDeterministicTests = true,
            GlobalRegenerationIntervalTicks = 30,
            SnapshotRetentionTicks = 45
        });
        StateEngine.Reset();
    }

    [Test]
    public void TemporaryMaxModifier_RecalculatesFromActiveEffectsAndCompensatesCurrent()
    {
        Register(new EffectDefinition
        {
            EffectDefinitionId = 1,
            Name = "Shield Booster",
            Target = new EffectTarget(StatType.Shield, StatValueKind.Max, StatManipulation.Raise, StatCalculation.Flat, 50f),
            Duration = EffectDuration.TemporaryTicks(3),
            StackPolicy = EffectStackPolicy.Replace,
            StackBehavior = EffectStackBehavior.RefreshDuration
        });

        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        var effect = CreateEffect(1, 100, 1);
        StateEngine.ProcessLivingEntity(EntityId, 1, [effect], 1);

        Assert.Multiple(() =>
        {
            Assert.That(state.Shield.MaxValue, Is.EqualTo(150f));
            Assert.That(state.Shield.CurrentValue, Is.EqualTo(150f));
        });

        StateEngine.ProcessLivingEntity(EntityId, 2, [], 0);
        StateEngine.ProcessLivingEntity(EntityId, 3, [], 0);
        StateEngine.ProcessLivingEntity(EntityId, 4, [], 0);

        Assert.Multiple(() =>
        {
            Assert.That(state.Shield.MaxValue, Is.EqualTo(100f));
            Assert.That(state.Shield.CurrentValue, Is.EqualTo(100f));
        });
    }

    [Test]
    public void AdditivePercentageModifiers_RecalculateMaxWithoutMultiplication()
    {
        Register(new EffectDefinition
        {
            EffectDefinitionId = 1,
            Target = new EffectTarget(StatType.Health, StatValueKind.Max, StatManipulation.Raise, StatCalculation.Percentage, 0.2f),
            Duration = EffectDuration.TemporaryTicks(5),
            StackPolicy = EffectStackPolicy.Stack,
            StackBehavior = EffectStackBehavior.IndependentStacks,
            MaxStacks = 3
        });
        Register(new EffectDefinition
        {
            EffectDefinitionId = 2,
            StackGroupId = 1,
            Target = new EffectTarget(StatType.Health, StatValueKind.Max, StatManipulation.Raise, StatCalculation.Percentage, 0.3f),
            Duration = EffectDuration.TemporaryTicks(5),
            StackPolicy = EffectStackPolicy.Stack,
            StackBehavior = EffectStackBehavior.IndependentStacks,
            MaxStacks = 3
        });

        var state = CreateState();
        StateEngine.AddLivingEntity(state);

        var effects = new[]
        {
            CreateEffect(1, 101, 1),
            CreateEffect(2, 102, 1)
        };
        StateEngine.ProcessLivingEntity(EntityId, 1, effects, effects.Length);

        Assert.Multiple(() =>
        {
            Assert.That(state.Health.MaxValue, Is.EqualTo(150f));
            Assert.That(state.Health.CurrentValue, Is.EqualTo(150f));
        });
    }

    [Test]
    public void Regeneration_UsesNetworkTickIntervals()
    {
        var state = CreateState();
        state.Health = new Stat(100f, 50f);
        state.HealthRegeneration = new Stat(10f);
        StateEngine.AddLivingEntity(state);

        StateEngine.ProcessLivingEntity(EntityId, 29, [], 0);
        Assert.That(state.Health.CurrentValue, Is.EqualTo(50f));

        StateEngine.ProcessLivingEntity(EntityId, 30, [], 0);
        Assert.That(state.Health.CurrentValue, Is.EqualTo(60f));
    }

    [Test]
    public void Factory_UsesPeerIdForPlayersAndReservedRangeForAgents()
    {
        var player = LivingEntityStateFactory.CreateDefaultPlayerState(42);
        var agent = LivingEntityStateFactory.CreateDefaultAgentState(42);

        Assert.Multiple(() =>
        {
            Assert.That(player.LivingEntityId, Is.EqualTo(42));
            Assert.That(player.LivingEntityType, Is.EqualTo(LivingEntityType.MpController));
            Assert.That(player.ActiveEffectCapacity, Is.GreaterThanOrEqualTo(70));
            Assert.That(agent.LivingEntityId, Is.EqualTo(1_000_042));
            Assert.That(agent.LivingEntityType, Is.EqualTo(LivingEntityType.AgentController));
        });
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

    private static ActiveEffect CreateEffect(int definitionId, long runtimeId, long tick)
    {
        return ActiveEffectFactory.Create(definitionId, runtimeId, 10, 20, EntityId, tick);
    }

    private static void Register(EffectDefinition definition)
    {
        EffectDefinitionRegistry.Register(definition);
        var reports = EffectValidator.ValidateRegistered();
        Assert.That(reports.Where(report => report.Severity == ValidationSeverity.Error), Is.Empty);
    }
}
