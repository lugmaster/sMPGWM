using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Tests;

[TestFixture]
public class LivingEntityTests
{
    private const int PlayerPeerId = 1;
    private const int OtherPeerA = 2;
    private const int OtherPeerB = 3;
    private const int OtherPeerC = 4;

    [Test]
    public void PlayerState_RemainsValid_WhenApplyingEffectsAndRecurringEnergyChanges()
    {
        var service = CreateServiceWithFourLivingEntities();
        var conditionEvents = new List<LivingEntityConditionChanged>();
        service.ConditionChanged += conditionEvents.Add;

        service.ApplyEffect(PlayerPeerId,
            new StatModifierEffect(
                "speed-boost-1",
                "engine-upgrade",
                StatType.Speed,
                new StatModifier(
                    "speed-mod-1",
                    "engine-upgrade",
                    25f,
                    ModifierType.Flat,
                    ModifierSourceType.Item,
                    "Engine Upgrade")));

        service.ApplyEffect(PlayerPeerId,
            new ConditionEffect(
                "invulnerable-shield-1",
                "shield-skill",
                LivingEntityCondition.Invulnerable,
                true,
                0.5f));

        service.ApplyEffect(PlayerPeerId, new ReduceEffect("shield-reduce-1", "test", StatType.Shield, 40f));
        service.ApplyEffect(PlayerPeerId, new RaiseEffect("shield-raise-1", "test", StatType.Shield, 10f));
        service.ApplyEffect(PlayerPeerId, new ReduceEffect("energy-reduce-1", "test", StatType.Energy, 30f));

        Assert.Multiple(() =>
        {
            var runtime = service.GetRuntime(PlayerPeerId);
            Assert.That(runtime.State.Speed.MaxValue, Is.EqualTo(25f));
            Assert.That(runtime.State.Shield.CurrentValue, Is.EqualTo(70f));
            Assert.That(runtime.State.Energy.CurrentValue, Is.EqualTo(70f));
            Assert.That(runtime.Condition.Contains(LivingEntityCondition.Invulnerable), Is.True);
            Assert.That(runtime.Condition.Contains(LivingEntityCondition.EnergyDepleted), Is.False);
        });

        var energy = service.GetState(PlayerPeerId).Energy;
        service.ApplyEffect(PlayerPeerId,
            new StatModifierEffect(
                "energy-drain-effect",
                "drain-aura",
                StatType.Energy,
                new RecurringStatModifier(
                    "energy-drain-mod",
                    "drain-aura",
                    -20f,
                    ModifierType.Flat,
                    2f,
                    1f,
                    energy,
                    ModifierSourceType.Skill,
                    "Drain Aura")));

        service.TickActiveEffects(PlayerPeerId, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(50f));
            Assert.That(service.GetRuntime(PlayerPeerId).Condition.Contains(LivingEntityCondition.EnergyDepleted),
                Is.False);
        });

        service.TickActiveEffects(PlayerPeerId, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(30f));
            Assert.That(service.GetRuntime(PlayerPeerId).Condition.Contains(LivingEntityCondition.EnergyDepleted),
                Is.False);
        });

        service.ApplyEffect(PlayerPeerId, new ReduceEffect("energy-reduce-2", "test", StatType.Energy, 15f));
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(15f));
            Assert.That(service.GetRuntime(PlayerPeerId).Condition.Contains(LivingEntityCondition.EnergyDepleted),
                Is.False);
        });

        service.ApplyEffect(PlayerPeerId, new ReduceEffect("energy-reduce-3", "test", StatType.Energy, 15f));
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(0f));
            Assert.That(service.GetRuntime(PlayerPeerId).Condition.Contains(LivingEntityCondition.EnergyDepleted),
                Is.True);
            Assert.That(
                conditionEvents.Any(e => e.Added.Contains(LivingEntityCondition.EnergyDepleted)),
                Is.True);
        });

        service.ApplyEffect(PlayerPeerId,
            new StatModifierEffect(
                "energy-restore-effect",
                "battery-regen",
                StatType.Energy,
                new RecurringStatModifier(
                    "energy-restore-mod",
                    "battery-regen",
                    12.5f,
                    ModifierType.Flat,
                    2f,
                    1f,
                    service.GetState(PlayerPeerId).Energy,
                    ModifierSourceType.Skill,
                    "Battery Regen")));

        service.TickActiveEffects(PlayerPeerId, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(12.5f));
            Assert.That(service.GetRuntime(PlayerPeerId).Condition.Contains(LivingEntityCondition.EnergyDepleted),
                Is.False);
            Assert.That(
                conditionEvents.Any(e => e.Removed.Contains(LivingEntityCondition.EnergyDepleted)),
                Is.True);
        });

        service.TickActiveEffects(PlayerPeerId, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(service.GetState(PlayerPeerId).Energy.CurrentValue, Is.EqualTo(25f));
            Assert.That(service.GetRuntime(PlayerPeerId).IsAlive(), Is.True);
            Assert.That(service.GetRuntime(OtherPeerA).IsAlive(), Is.True);
            Assert.That(service.GetRuntime(OtherPeerB).IsAlive(), Is.True);
            Assert.That(service.GetRuntime(OtherPeerC).IsAlive(), Is.True);
        });
    }

    [Test]
    [Explicit("Requires Godot runtime. LivingEntityService delta APIs allocate Godot.Collections.Array, which crashes in the plain NUnit host.")]
    public void ServiceDeltas_AppliedToRegisteredPeers_UpdateRuntimeState()
    {
        var host = CreateServiceWithFourLivingEntities();
        var client = CreateServiceWithFourLivingEntities();
        var clientEvents = new List<LivingEntityConditionChanged>();
        client.ConditionChanged += clientEvents.Add;

        host.ApplyEffect(OtherPeerA, new ReduceEffect("energy-a", "test", StatType.Energy, 35f));
        host.ApplyEffect(OtherPeerB,
            new ConditionEffect("invisible-b", "test", LivingEntityCondition.Invisible, true));
        host.ApplyEffect(OtherPeerC, new ReduceEffect("health-c", "test", StatType.Health, 100f));

        client.ApplyDelta(host.GetDelta(OtherPeerA, includePermanent: false));
        client.ApplyDelta(host.GetDelta(OtherPeerB, includePermanent: false));
        client.ApplyDelta(host.GetDelta(OtherPeerC, includePermanent: false));

        Assert.Multiple(() =>
        {
            Assert.That(client.GetRuntime(OtherPeerA).State.Energy.CurrentValue, Is.EqualTo(65f));
            Assert.That(client.GetRuntime(OtherPeerB).Condition.Contains(LivingEntityCondition.Invisible), Is.True);
            Assert.That(client.GetRuntime(OtherPeerC).Condition.Contains(LivingEntityCondition.Dead), Is.True);
            Assert.That(client.GetRuntime(OtherPeerC).State.Health.CurrentValue, Is.EqualTo(0f));
            Assert.That(clientEvents.Any(e => e.Added.Contains(LivingEntityCondition.Invisible)), Is.True);
            Assert.That(clientEvents.Any(e => e.Added.Contains(LivingEntityCondition.Dead)), Is.True);
        });
    }

    private static LivingEntityServiceCore CreateServiceWithFourLivingEntities()
    {
        var service = new LivingEntityServiceCore();
        service.RegisterPeer(PlayerPeerId, LivingEntityStateFactory.CreateDefaultPlayerState(PlayerPeerId));
        service.RegisterPeer(OtherPeerA, LivingEntityStateFactory.CreateDefaultPlayerState(OtherPeerA));
        service.RegisterPeer(OtherPeerB, LivingEntityStateFactory.CreateDefaultPlayerState(OtherPeerB));
        service.RegisterPeer(OtherPeerC, LivingEntityStateFactory.CreateDefaultPlayerState(OtherPeerC));
        return service;
    }
}
