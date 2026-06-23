using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Tests;

[TestFixture]
public class StatTests
{
    private sealed class TestLivingEntityLogger : ILivingEntityLogger
    {
        public readonly List<string> Messages = [];

        public void Info(string message)
        {
            Messages.Add(message);
        }
    }

    [Test]
    public void PermanentModifiers_RecalculateMaxAndApplyCurrentRules()
    {
        var stat = new Stat(100);
        var flatPenalty = new StatModifier("flat-penalty", "test", -90, ModifierType.Flat);

        stat.AddModifier(new StatModifier("flat-bonus", "test", 20, ModifierType.Flat));
        stat.AddModifier(new StatModifier("percent-bonus", "test", 0.5f, ModifierType.Percent));
        stat.AddModifier(flatPenalty);

        Assert.Multiple(() =>
        {
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(45f));
            Assert.That(stat.CurrentValue, Is.EqualTo(45f));
        });

        stat.AddModifier(new StatModifier("percent-bonus-2", "test", 0.25f, ModifierType.Percent));
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(52.5f));
            Assert.That(stat.CurrentValue, Is.EqualTo(52.5f));
        });

        stat.Reduce(25f);
        stat.Raise(100f);
        stat.Reduce(5f);
        Assert.That(stat.CurrentValue, Is.EqualTo(47.5f));

        stat.RemoveModifier(flatPenalty);
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(210f));
            Assert.That(stat.CurrentValue, Is.EqualTo(205f));
        });

        stat.AddModifier(new StatModifier("percent-penalty", "test", -0.8f, ModifierType.Percent));
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(114f));
            Assert.That(stat.CurrentValue, Is.EqualTo(114f));
        });
    }

    [Test]
    public void TemporaryModifier_Expires_AfterTick_RecalculatesMax()
    {
        var stat = new Stat(100);

        stat.AddModifier(new StatModifier("flat-bonus", "test", 20, ModifierType.Flat));
        stat.AddModifier(new StatModifier("percent-bonus", "test", 0.5f, ModifierType.Percent));
        stat.AddModifier(new TemporaryStatModifier("temp-penalty", "test", -30f, ModifierType.Flat, 2f));

        Assert.Multiple(() =>
        {
            Assert.That(stat.PermanentMaxValue, Is.EqualTo(180f));
            Assert.That(stat.MaxValue, Is.EqualTo(150f));
            Assert.That(stat.CurrentValue, Is.EqualTo(150f));
        });

        stat.Tick(1f);
        Assert.That(stat.MaxValue, Is.EqualTo(150f));

        stat.Tick(1.1f);
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(180f));
            Assert.That(stat.CurrentValue, Is.EqualTo(180f));
        });
    }

    [Test]
    public void MaxDecrease_OnlyClampsCurrent_WhenCurrentExceedsNewMax()
    {
        var stat = new Stat(100);
        stat.Reduce(10);

        stat.AddModifier(new TemporaryStatModifier("temp-penalty-1", "test", -10, ModifierType.Flat, 5f));

        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(90f));
            Assert.That(stat.CurrentValue, Is.EqualTo(90f));
        });

        stat.AddModifier(new TemporaryStatModifier("temp-penalty-2", "test", -20, ModifierType.Flat, 5f));

        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(70f));
            Assert.That(stat.CurrentValue, Is.EqualTo(70f));
        });
    }

    [Test]
    public void MaxIncrease_RaisesCurrentBySameDelta()
    {
        var stat = new Stat(100);
        stat.Reduce(40);

        stat.AddModifier(new StatModifier("flat-bonus", "test", 25, ModifierType.Flat));

        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(125f));
            Assert.That(stat.CurrentValue, Is.EqualTo(85f));
        });
    }

    [Test]
    public void SetBaseValue_UsesMaxCurrentRules()
    {
        var stat = new Stat(100);
        stat.Reduce(10);

        stat.SetBaseValue(50);
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(50f));
            Assert.That(stat.CurrentValue, Is.EqualTo(50f));
        });

        stat.SetBaseValue(80);
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(80f));
            Assert.That(stat.CurrentValue, Is.EqualTo(80f));
        });
    }

    [Test]
    public void RecurringModifier_UsesPerSecondValue()
    {
        var stat = new Stat(100);
        var recurring = new RecurringStatModifier("dot", "test", -5f, ModifierType.Flat, 4f, 2f, stat);

        stat.AddRecurringModifier(recurring);

        stat.Tick(2f);
        Assert.That(stat.CurrentValue, Is.EqualTo(90f));

        stat.Tick(2f);
        Assert.That(stat.CurrentValue, Is.EqualTo(80f));

        stat.Tick(0.1f);
        stat.Tick(2f);
        Assert.That(stat.CurrentValue, Is.EqualTo(80f));
    }

    [Test]
    public void ModifierMetadata_IsExposedThroughBreakdown()
    {
        var stat = new Stat(100);
        var modifier = new TemporaryStatModifier(
            "instance-1",
            "skill-overcharge",
            20f,
            ModifierType.Flat,
            3f,
            ModifierSourceType.Skill,
            "Overcharge",
            ModifierStackPolicy.RefreshDuration);

        stat.AddModifier(modifier);

        var breakdown = stat.CreateBreakdown(StatType.Energy);

        Assert.Multiple(() =>
        {
            Assert.That(breakdown.BaseValue, Is.EqualTo(100f));
            Assert.That(breakdown.MaxValue, Is.EqualTo(120f));
            Assert.That(breakdown.TemporaryContributions.Length, Is.EqualTo(1));
            Assert.That(breakdown.TemporaryContributions[0].InstanceId, Is.EqualTo("instance-1"));
            Assert.That(breakdown.TemporaryContributions[0].SourceId, Is.EqualTo("skill-overcharge"));
            Assert.That(breakdown.TemporaryContributions[0].DisplayName, Is.EqualTo("Overcharge"));
            Assert.That(breakdown.TemporaryContributions[0].TimeRemaining, Is.EqualTo(3f));
        });
    }

    [Test]
    public void StackPolicy_RefreshDuration_ReplacesSameSourceModifier()
    {
        var stat = new Stat(100);

        stat.AddModifier(new TemporaryStatModifier(
            "old",
            "skill-boost",
            10f,
            ModifierType.Flat,
            1f,
            ModifierSourceType.Skill,
            "Boost",
            ModifierStackPolicy.RefreshDuration));

        stat.AddModifier(new TemporaryStatModifier(
            "new",
            "skill-boost",
            10f,
            ModifierType.Flat,
            5f,
            ModifierSourceType.Skill,
            "Boost",
            ModifierStackPolicy.RefreshDuration));

        Assert.Multiple(() =>
        {
            Assert.That(stat.TemporaryModifiers, Has.Count.EqualTo(1));
            Assert.That(stat.TemporaryModifiers[0].InstanceId, Is.EqualTo("new"));
            Assert.That(stat.TemporaryModifiers[0].TimeRemaining, Is.EqualTo(5f));
            Assert.That(stat.MaxValue, Is.EqualTo(110f));
        });
    }

    [Test]
    public void EffectService_ReducesTargetedStatsAndZeroHealthChangesCondition()
    {
        var logger = new TestLivingEntityLogger();
        var service = new LivingEntityServiceCore(logger);
        service.RegisterPeer(7, LivingEntityStateFactory.CreateDefaultPlayerState(7));
        service.ApplyEffect(7, new ReduceEffect("reduce-shield", "skill-1", StatType.Shield, 25f));

        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(7).Shield.CurrentValue, Is.EqualTo(75f));
            Assert.That(service.GetState(7).Health.CurrentValue, Is.EqualTo(100f));
            Assert.That(service.GetRuntime(7).IsAlive(), Is.True);
            Assert.That(logger.Messages, Is.Not.Empty);
        });

        var conditionEvents = new List<LivingEntityConditionChanged>();
        service.ConditionChanged += conditionEvents.Add;

        service.ApplyEffect(7, new ReduceEffect("reduce-health", "skill-1", StatType.Health, 200f));

        Assert.Multiple(() =>
        {
            Assert.That(service.GetRuntime(7).IsAlive(), Is.False);
            Assert.That(service.GetRuntime(7).Condition.Contains(LivingEntityCondition.Dead), Is.True);
            Assert.That(service.GetState(7).Health.CurrentValue, Is.EqualTo(0f));
            Assert.That(service.GetState(7).Shield.CurrentValue, Is.EqualTo(0f));
            Assert.That(service.GetState(7).Energy.CurrentValue, Is.EqualTo(0f));
            Assert.That(conditionEvents.Any(e => e.Added.Contains(LivingEntityCondition.Dead)), Is.True);
        });
    }

    [Test]
    public void EffectService_RemovesTimedCondition_WhenConditionSourceExpires()
    {
        var service = new LivingEntityServiceCore(new TestLivingEntityLogger());
        service.RegisterPeer(8, LivingEntityStateFactory.CreateDefaultPlayerState(8));
        var conditionEvents = new List<LivingEntityConditionChanged>();
        service.ConditionChanged += conditionEvents.Add;

        service.ApplyEffect(8,
            new ConditionEffect("cloak-1", "skill-cloak", LivingEntityCondition.Untargetable, true, 1f));

        Assert.Multiple(() =>
        {
            Assert.That(service.GetRuntime(8).Condition.Contains(LivingEntityCondition.Untargetable), Is.True);
            Assert.That(conditionEvents, Has.Count.EqualTo(1));
            Assert.That(conditionEvents[0].Added.Contains(LivingEntityCondition.Untargetable), Is.True);
        });

        service.TickActiveEffects(8, 1.1f);

        Assert.Multiple(() =>
        {
            Assert.That(service.GetRuntime(8).Condition.Contains(LivingEntityCondition.Untargetable), Is.False);
            Assert.That(conditionEvents, Has.Count.EqualTo(2));
            Assert.That(conditionEvents[1].Removed.Contains(LivingEntityCondition.Untargetable), Is.True);
        });
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
            Assert.That(agent.LivingEntityId, Is.EqualTo(1_000_042));
            Assert.That(agent.LivingEntityType, Is.EqualTo(LivingEntityType.AgentController));
        });
    }

}
