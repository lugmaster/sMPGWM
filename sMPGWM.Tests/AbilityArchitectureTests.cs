using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

public sealed class AbilityArchitectureTests
{
    [Test]
    public void DefinitionsAreTypedValidatedAndImmutable()
    {
        JObject? original = null;
        var data = GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "AbilityDefinitions.yml") original = document;
        });
        var ability = data.Abilities.GetById("shieldCapacityBoost");
        Assert.That(data.Effects.GetById(ability.EffectId), Is.TypeOf<ApplyBehaviourEffect>());
        Assert.That(data.Effects.GetById("infectionProjectile"), Is.TypeOf<LaunchProjectileEffect>());
        Assert.That(data.Effects.GetById("removeDisrupted"), Is.TypeOf<SequenceEffect>());
        Assert.That(data.Behaviours.GetById("overheating").OnRemove!.EffectId, Is.EqualTo("overheatingCooldown"));
        Assert.That(data.Behaviours.GetById("maxHealthRot").PeriodicAccumulator!.IntervalMs, Is.EqualTo(1000));
        Assert.That(data.Projectiles.GetById("empBall").Speed, Is.EqualTo(800));
        Assert.That(data.DamageSources.GetById("physical").Id, Is.EqualTo("physical"));
        Assert.That(data.Conditions.GetById("dead").Id, Is.EqualTo("dead"));
        Assert.That(data.Classifications.GetById("infection").Id, Is.EqualTo("infection"));
        Assert.That(data.Stats.GetById("shield").IsResource, Is.True);
        Assert.That(data.Stats.GetById("speed").IsResource, Is.False);
        Assert.That(data.Abilities.TryGetById("missing", out _), Is.False);
        GameplayTestData.Find((JArray)original!["AbilityDefinitions"]!, ability.Id)["DisplayName"] = "changed";
        Assert.That(ability.DisplayName, Is.EqualTo("Shield Capacity Boost"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)ability.BlockedByConditions).Add("dead"));

        var categories = ItemDefinitionParser.LoadItemCategoryDefinitions(GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var items = ItemRegistry.LoadItemDefinitions(GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml"), categories, _ => null!);
        var all = new List<ItemDefinition>(items.Values);
        GameplayCatalogueValidator.ValidateItems(data, all);
        Assert.That(items["infectedCargo"].BehaviourGrants[0], Is.EqualTo(new ItemBehaviourGrant("infection", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive)));
        var invalidItem = new ItemDefinition("invalid", "Invalid", "Resource", "", false, 1, null!)
        {
            BehaviourGrants = new[] { new ItemBehaviourGrant("missing", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive) }
        };
        Assert.Throws<ArgumentException>(() => GameplayCatalogueValidator.ValidateItems(data, new[] { invalidItem }));

        var error = Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "AbilityDefinitions.yml")
                GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost")["EffectId"] = "missingEffect";
        }));
        Assert.That(error!.Message, Does.Contain("AbilityDefinitions.yml").And.Contain("shieldCapacityBoost").And.Contain("EffectId").And.Contain("missingEffect"));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "EffectDefinitions.yml") ((JArray)document["EffectDefinitions"]!).Add(document["EffectDefinitions"]![0]!.DeepClone());
        }));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "BehaviourDefinitions.yml")
                GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "infection")["OnExpireEffectId"] = "infection";
        }));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "BehaviourDefinitions.yml")
                GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "infection")["Modifiers"]![0]!["ValueKind"] = "Maximum";
        }));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "AbilityDefinitions.yml") document["AbilityDefinitions"]![0]!["UnknownField"] = true;
        }));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "EffectDefinitions.yml")
                GameplayTestData.Find((JArray)document["EffectDefinitions"]!, "shieldCapacityBoost")["Kind"] = "DealDamage";
        }));
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "EffectDefinitions.yml")
                GameplayTestData.Find((JArray)document["EffectDefinitions"]!, "disruptionArea")["TargetingId"] = "self";
        }));
    }

    [Test]
    public void AbilitiesExecuteByDefinitionWithIndependentLifetime()
    {
        var data = GameplayTestData.Load(variants: true);
        using var runtime = new AbilityRuntime(data);
        var state = new ShipRuntime(100, 40);
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost", "shieldAlias" });
        state.Grants.SetSource("item:1", new[] { "shieldVariant" });
        var context = new AbilityActivationContext(state, state, 0);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.Accepted));
        var first = state.ActiveBehaviours[0];
        Assert.That(runtime.Activate("shieldVariant", context, 200), Is.EqualTo(AbilityActivationResult.Accepted));
        var second = state.ActiveBehaviours[1];
        Assert.That(state.Shield.Capacity, Is.EqualTo(175));
        Assert.That(state.Shield.Current, Is.EqualTo(40));
        Assert.That(first.RuntimeId, Is.Not.EqualTo(second.RuntimeId));
        Assert.That(runtime.Activate("shieldAlias", context, 300), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(2));
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.EqualTo(first.RuntimeId));
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(2100));
        Assert.That(state.ActiveBehaviours[1], Is.EqualTo(second));
        state.Grants.RemoveSource("item:1");
        Assert.That(runtime.Activate("shieldVariant", context, 400), Is.EqualTo(AbilityActivationResult.AbilityNotGranted));
        Assert.That(state.ActiveBehaviours[1], Is.EqualTo(second));
        Assert.That(state.Expire(799), Is.False);
        Assert.That(state.Expire(800), Is.True);
        Assert.That(state.Shield.Capacity, Is.EqualTo(150));
        Assert.That(state.Expire(2099), Is.False);
        Assert.That(state.Expire(2100), Is.True);
        Assert.That(state.Shield.Capacity, Is.EqualTo(100));
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2100), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.Not.EqualTo(first.RuntimeId));
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(3900));

        var ally = new ShipRuntime(100, 40);
        state.SetFaction(1);
        ally.SetFaction(1);
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Target = ally, DistanceSquared = 360000 }, 2200), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(ally.ActiveBehaviours[0].Deadline, Is.EqualTo(4000));
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(3900));
        Assert.That(data.Behaviours.GetById("shieldCapacityBoost").Lifetime.DurationMs, Is.EqualTo(30000));
        state.Grants.SetSource("secondSkill", new[] { "shieldCapacityBoost" });
        state.Grants.RemoveSource("skill");
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2300), Is.EqualTo(AbilityActivationResult.Accepted));
        state.Grants.RemoveSource("secondSkill");
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2400), Is.EqualTo(AbilityActivationResult.AbilityNotGranted));
        runtime.Dispose();
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2500), Is.EqualTo(AbilityActivationResult.SessionEnded));
        using var nextSession = new AbilityRuntime(data);
        var nextState = new ShipRuntime(100, 40);
        Assert.That(nextSession.Activate("shieldCapacityBoost", new(nextState, nextState, 0), 0), Is.EqualTo(AbilityActivationResult.AbilityNotGranted));
    }

    [Test]
    public void UnsupportedAndUnauthorizedActivationsCannotMutateState()
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        var state = new ShipRuntime(100, 40);
        var context = new AbilityActivationContext(state, state, 0);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.AbilityNotGranted));
        Assert.That(runtime.Definitions.Abilities.IsSupported("delayedExplosion", out var reason), Is.False);
        Assert.That(reason, Does.Contain("delayedExplosion"));
        state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "delayedExplosion" });
        Assert.That(runtime.Activate("delayedExplosion", context, 0), Is.EqualTo(AbilityActivationResult.UnsupportedAbility));
        var silenced = new ShipRuntime(100, 40);
        silenced.Grants.SetSource("fixture", new[] { "shieldCapacityBoost" });
        silenced.SetCondition(runtime.Definitions.Conditions.GetById("silenced"), true);
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Caster = silenced }, 0), Is.EqualTo(AbilityActivationResult.BlockedByCondition));
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Target = new ShipRuntime(100, 40) }, 0), Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly));
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { DistanceSquared = 360001 }, 0), Is.EqualTo(AbilityActivationResult.OutOfRange));
        Assert.That(state.Revision, Is.EqualTo(1));
        Assert.That(state.ActiveBehaviours, Is.Empty);
        foreach (var field in new[] { "CooldownMs", "Costs", "TargetingId" })
        {
            using var unsupported = new AbilityRuntime(GameplayTestData.Load(edit: (file, document) =>
            {
                if (file != "AbilityDefinitions.yml") return;
                var ability = GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost");
                if (field == "CooldownMs") ability[field] = .5;
                else if (field == "Costs") ability[field] = new JArray(new JObject { ["StatId"] = "energy_storage", ["Amount"] = 1 });
                else ability[field] = "hostile";
            }));
            Assert.That(unsupported.Definitions.Abilities.IsSupported("shieldCapacityBoost", out _), Is.False, field);
            Assert.That(unsupported.Activate("shieldCapacityBoost", context, 1), Is.EqualTo(AbilityActivationResult.UnsupportedAbility));
        }
        Assert.That(state.Shield.Capacity, Is.EqualTo(100));
    }
}
