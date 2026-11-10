using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.Items;

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-TEST-001 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable, sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime, sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState

public sealed class AbilityArchitectureTests // TEST-SUPPORT: ABILITY-TEST-002 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable, sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime, sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
{
    [Test] // TEST-SUPPORT: ABILITY-TEST-003 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
    public void DefinitionsAreTypedValidatedAndImmutable() // TEST-SUPPORT: ABILITY-TEST-004 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
    {
        JObject? original = null; // TEST-SUPPORT: ABILITY-TEST-005 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        var data = GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-006 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "AbilityDefinitions.yml") original = document; // TEST-SUPPORT: ABILITY-TEST-007 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        }); // TEST-SUPPORT: ABILITY-TEST-008 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        var ability = data.Abilities.GetById("shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-TEST-009 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Abilities.GetAll(), Is.SameAs(data.Abilities.GetAll())); // TEST-SUPPORT: ABILITY-TEST-010 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Effects.GetById(ability.EffectId), Is.TypeOf<ApplyBehaviourEffect>()); // TEST-SUPPORT: ABILITY-TEST-011 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Effects.GetById("infectionProjectile"), Is.TypeOf<LaunchProjectileEffect>()); // TEST-SUPPORT: ABILITY-TEST-012 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Effects.GetById("removeDisrupted"), Is.TypeOf<SequenceEffect>()); // TEST-SUPPORT: ABILITY-TEST-013 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Behaviours.GetById("overheating").OnRemove!.EffectId, Is.EqualTo("overheatingCooldown")); // TEST-SUPPORT: ABILITY-TEST-014 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Behaviours.GetById("maxHealthRot").PeriodicAccumulator!.IntervalMs, Is.EqualTo(1000)); // TEST-SUPPORT: ABILITY-TEST-015 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Projectiles.GetById("empBall").Speed, Is.EqualTo(800)); // TEST-SUPPORT: ABILITY-TEST-016 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.DamageSources.GetById("physical").Id, Is.EqualTo("physical")); // TEST-SUPPORT: ABILITY-TEST-017 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Conditions.GetById("dead").Id, Is.EqualTo("dead")); // TEST-SUPPORT: ABILITY-TEST-018 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Classifications.GetById("infection").Id, Is.EqualTo("infection")); // TEST-SUPPORT: ABILITY-TEST-019 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Stats.GetById("shield").IsResource, Is.True); // TEST-SUPPORT: ABILITY-TEST-020 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Stats.GetById("speed").IsResource, Is.False); // TEST-SUPPORT: ABILITY-TEST-021 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(data.Abilities.TryGetById("missing", out _), Is.False); // TEST-SUPPORT: ABILITY-TEST-022 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        GameplayTestData.Find((JArray)original!["AbilityDefinitions"]!, ability.Id)["DisplayName"] = "changed"; // TEST-SUPPORT: ABILITY-TEST-023 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(ability.DisplayName, Is.EqualTo("Shield Capacity Boost")); // TEST-SUPPORT: ABILITY-TEST-024 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<NotSupportedException>(() => ((IList<string>)ability.BlockedByConditions).Add("dead")); // TEST-SUPPORT: ABILITY-TEST-025 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable

        var categories = ItemRegistry.LoadItemCategoryDefinitions(GameplayTestData.Read("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml"), _ => null!); // TEST-SUPPORT: ABILITY-TEST-026 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        var items = ItemRegistry.LoadItemDefinitions(GameplayTestData.Read("ItemDefinitions.yml", "ItemSchema.yml"), categories, _ => null!); // TEST-SUPPORT: ABILITY-TEST-027 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        var all = new List<ItemDefinition>(items.Values); // TEST-SUPPORT: ABILITY-TEST-028 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        GameplayCatalogueValidator.ValidateItems(data, all); // TEST-SUPPORT: ABILITY-TEST-029 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(items["infectedCargo"].BehaviourGrants[0], Is.EqualTo(new ItemBehaviourGrant("infection", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive))); // TEST-SUPPORT: ABILITY-TEST-030 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        var invalidItem = new ItemDefinition("invalid", "Invalid", "Resource", "", false, 1, null!) // TEST-SUPPORT: ABILITY-TEST-031 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            BehaviourGrants = new[] { new ItemBehaviourGrant("missing", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive) } // TEST-SUPPORT: ABILITY-TEST-032 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        };
        Assert.Throws<ArgumentException>(() => GameplayCatalogueValidator.ValidateItems(data, new[] { invalidItem })); // TEST-SUPPORT: ABILITY-TEST-033 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable

        var error = Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-034 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "AbilityDefinitions.yml") // TEST-SUPPORT: ABILITY-TEST-035 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
                GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost")["EffectId"] = "missingEffect"; // TEST-SUPPORT: ABILITY-TEST-036 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-037 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.That(error!.Message, Does.Contain("AbilityDefinitions.yml").And.Contain("shieldCapacityBoost").And.Contain("EffectId").And.Contain("missingEffect")); // TEST-SUPPORT: ABILITY-TEST-038 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-039 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "EffectDefinitions.yml") ((JArray)document["EffectDefinitions"]!).Add(document["EffectDefinitions"]![0]!.DeepClone()); // TEST-SUPPORT: ABILITY-TEST-040 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-041 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-042 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "BehaviourDefinitions.yml") // TEST-SUPPORT: ABILITY-TEST-043 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
                GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "infection")["OnExpireEffectId"] = "infection"; // TEST-SUPPORT: ABILITY-TEST-044 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-045 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-046 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "BehaviourDefinitions.yml") // TEST-SUPPORT: ABILITY-TEST-047 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
                GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "infection")["Modifiers"]![0]!["ValueKind"] = "Maximum"; // TEST-SUPPORT: ABILITY-TEST-048 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-049 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-050 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "AbilityDefinitions.yml") document["AbilityDefinitions"]![0]!["UnknownField"] = true; // TEST-SUPPORT: ABILITY-TEST-051 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-052 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-053 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "EffectDefinitions.yml") // TEST-SUPPORT: ABILITY-TEST-054 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
                GameplayTestData.Find((JArray)document["EffectDefinitions"]!, "shieldCapacityBoost")["Kind"] = "DealDamage"; // TEST-SUPPORT: ABILITY-TEST-055 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-056 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-057 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        {
            if (file == "EffectDefinitions.yml") // TEST-SUPPORT: ABILITY-TEST-058 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
                GameplayTestData.Find((JArray)document["EffectDefinitions"]!, "disruptionArea")["TargetingId"] = "self"; // TEST-SUPPORT: ABILITY-TEST-059 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
        })); // TEST-SUPPORT: ABILITY-TEST-060 | Tests: sMPGWM.Tests.AbilityArchitectureTests.DefinitionsAreTypedValidatedAndImmutable
    }

    [Test] // TEST-SUPPORT: ABILITY-TEST-061 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
    public void AbilitiesExecuteByDefinitionWithIndependentLifetime() // TEST-SUPPORT: ABILITY-TEST-062 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
    {
        var data = GameplayTestData.Load(variants: true); // TEST-SUPPORT: ABILITY-TEST-063 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        using var runtime = new AbilityRuntime(data); // TEST-SUPPORT: ABILITY-TEST-064 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-TEST-065 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost", "shieldAlias" }); // TEST-SUPPORT: ABILITY-TEST-066 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.SetSource("item:1", new[] { "shieldVariant" }); // TEST-SUPPORT: ABILITY-TEST-067 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        var context = new AbilityActivationContext(state, state, 0); // TEST-SUPPORT: ABILITY-TEST-068 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-069 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        var first = state.ActiveBehaviours[0]; // TEST-SUPPORT: ABILITY-TEST-070 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldVariant", context, 200), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-071 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        var second = state.ActiveBehaviours[1]; // TEST-SUPPORT: ABILITY-TEST-072 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Shield.Capacity, Is.EqualTo(175)); // TEST-SUPPORT: ABILITY-TEST-073 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: ABILITY-TEST-074 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(first.RuntimeId, Is.Not.EqualTo(second.RuntimeId)); // TEST-SUPPORT: ABILITY-TEST-075 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldAlias", context, 300), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-076 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(2)); // TEST-SUPPORT: ABILITY-TEST-077 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.EqualTo(first.RuntimeId)); // TEST-SUPPORT: ABILITY-TEST-078 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(2100)); // TEST-SUPPORT: ABILITY-TEST-079 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[1], Is.EqualTo(second)); // TEST-SUPPORT: ABILITY-TEST-080 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.RemoveSource("item:1"); // TEST-SUPPORT: ABILITY-TEST-081 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldVariant", context, 400), Is.EqualTo(AbilityActivationResult.AbilityNotGranted)); // TEST-SUPPORT: ABILITY-TEST-082 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[1], Is.EqualTo(second)); // TEST-SUPPORT: ABILITY-TEST-083 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Expire(799), Is.False); // TEST-SUPPORT: ABILITY-TEST-084 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Expire(800), Is.True); // TEST-SUPPORT: ABILITY-TEST-085 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: ABILITY-TEST-086 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Expire(2099), Is.False); // TEST-SUPPORT: ABILITY-TEST-087 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Expire(2100), Is.True); // TEST-SUPPORT: ABILITY-TEST-088 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-TEST-089 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2100), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-090 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.Not.EqualTo(first.RuntimeId)); // TEST-SUPPORT: ABILITY-TEST-091 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(3900)); // TEST-SUPPORT: ABILITY-TEST-092 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime

        var ally = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-TEST-093 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.SetFaction(1); // TEST-SUPPORT: HARDEN-EXISTING-001 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        ally.SetFaction(1); // TEST-SUPPORT: HARDEN-EXISTING-002 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Target = ally, DistanceSquared = 360000 }, 2200), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-094 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(ally.ActiveBehaviours[0].Deadline, Is.EqualTo(4000)); // TEST-SUPPORT: ABILITY-TEST-095 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(3900)); // TEST-SUPPORT: ABILITY-TEST-096 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(data.Behaviours.GetById("shieldCapacityBoost").Lifetime.DurationMs, Is.EqualTo(30000)); // TEST-SUPPORT: ABILITY-TEST-097 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.SetSource("secondSkill", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-TEST-098 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.RemoveSource("skill"); // TEST-SUPPORT: ABILITY-TEST-099 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2300), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: ABILITY-TEST-100 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        state.Grants.RemoveSource("secondSkill"); // TEST-SUPPORT: ABILITY-TEST-101 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2400), Is.EqualTo(AbilityActivationResult.AbilityNotGranted)); // TEST-SUPPORT: ABILITY-TEST-102 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        runtime.Dispose(); // TEST-SUPPORT: ABILITY-TEST-103 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 2500), Is.EqualTo(AbilityActivationResult.SessionEnded)); // TEST-SUPPORT: ABILITY-TEST-104 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        using var nextSession = new AbilityRuntime(data); // TEST-SUPPORT: ABILITY-TEST-105 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        var nextState = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-TEST-106 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
        Assert.That(nextSession.Activate("shieldCapacityBoost", new(nextState, nextState, 0), 0), Is.EqualTo(AbilityActivationResult.AbilityNotGranted)); // TEST-SUPPORT: ABILITY-TEST-107 | Tests: sMPGWM.Tests.AbilityArchitectureTests.AbilitiesExecuteByDefinitionWithIndependentLifetime
    }

    [Test] // TEST-SUPPORT: ABILITY-TEST-108 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
    public void UnsupportedAndUnauthorizedActivationsCannotMutateState() // TEST-SUPPORT: ABILITY-TEST-109 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: ABILITY-TEST-110 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-TEST-111 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        var context = new AbilityActivationContext(state, state, 0); // TEST-SUPPORT: ABILITY-TEST-112 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.AbilityNotGranted)); // TEST-SUPPORT: ABILITY-TEST-113 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.IsSupported("delayedExplosion", out var reason), Is.False); // TEST-SUPPORT: ABILITY-TEST-114 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(reason, Does.Contain("delayedExplosion")); // TEST-SUPPORT: ABILITY-TEST-115 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "delayedExplosion" }); // TEST-SUPPORT: ABILITY-TEST-116 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.Activate("delayedExplosion", context, 0), Is.EqualTo(AbilityActivationResult.UnsupportedAbility)); // TEST-SUPPORT: ABILITY-TEST-117 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        var silenced = new ShipGameplayState(100, 40); // TEST-SUPPORT: HARDEN-EXISTING-003 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        silenced.Grants.SetSource("fixture", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: HARDEN-EXISTING-004 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        silenced.SetCondition(runtime.Definitions.Conditions.GetById("silenced"), true); // TEST-SUPPORT: HARDEN-EXISTING-005 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Caster = silenced }, 0), Is.EqualTo(AbilityActivationResult.BlockedByCondition)); // TEST-SUPPORT: ABILITY-TEST-118 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { Target = new ShipGameplayState(100, 40) }, 0), Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly)); // TEST-SUPPORT: ABILITY-TEST-119 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(runtime.Activate("shieldCapacityBoost", context with { DistanceSquared = 360001 }, 0), Is.EqualTo(AbilityActivationResult.OutOfRange)); // TEST-SUPPORT: ABILITY-TEST-120 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(state.Revision, Is.EqualTo(1)); // TEST-SUPPORT: ABILITY-TEST-121 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        Assert.That(state.ActiveBehaviours, Is.Empty); // TEST-SUPPORT: ABILITY-TEST-122 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        foreach (var field in new[] { "CooldownMs", "Costs", "TargetingId" }) // TEST-SUPPORT: ABILITY-TEST-123 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        {
            using var unsupported = new AbilityRuntime(GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: ABILITY-TEST-124 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
            {
                if (file != "AbilityDefinitions.yml") return; // TEST-SUPPORT: ABILITY-TEST-125 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
                var ability = GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost"); // TEST-SUPPORT: ABILITY-TEST-126 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
                if (field == "CooldownMs") ability[field] = .5; // TEST-SUPPORT: ABILITY-TEST-127 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
                else if (field == "Costs") ability[field] = new JArray(new JObject { ["StatId"] = "energy_storage", ["Amount"] = 1 }); // TEST-SUPPORT: ABILITY-TEST-128 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
                else ability[field] = "hostile"; // TEST-SUPPORT: ABILITY-TEST-129 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
            })); // TEST-SUPPORT: ABILITY-TEST-130 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
            Assert.That(unsupported.IsSupported("shieldCapacityBoost", out _), Is.False, field); // TEST-SUPPORT: ABILITY-TEST-131 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
            Assert.That(unsupported.Activate("shieldCapacityBoost", context, 1), Is.EqualTo(AbilityActivationResult.UnsupportedAbility)); // TEST-SUPPORT: ABILITY-TEST-132 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
        }
        Assert.That(state.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-TEST-133 | Tests: sMPGWM.Tests.AbilityArchitectureTests.UnsupportedAndUnauthorizedActivationsCannotMutateState
    }
}
