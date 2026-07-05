using sMPGWM.Scripts.Gameplay.Tags;

namespace sMPGWM.Tests.TagSystem;

internal static class TagSystemFixtureFactory
{
    public const string EntityCondition = "EntityCondition";
    public const string EffectClassification = "EffectClassification";
    public const string ActionClassification = "ActionClassification";
    public const string ItemClassification = "ItemClassification";
    public const string TargetClassification = "TargetClassification";
    public const string CooldownGroup = "CooldownGroup";
    public const string DamageKind = "DamageKind";
    public const string FactionRelation = "FactionRelation";

    public static GameplayTagRegistry CreateSmallGameplayRegistry()
    {
        return CreateSmallGameplayBuilder().CompileAndFreeze();
    }

    public static GameplayTagRegistryBuilder CreateSmallGameplayBuilder()
    {
        var builder = CreateDomainOnlyBuilder();

        RegisterEntityConditions(builder);
        RegisterEffectClassifications(builder);
        RegisterActionClassifications(builder);
        RegisterItemClassifications(builder);
        RegisterTargetClassifications(builder);
        RegisterCooldownGroups(builder);
        RegisterDamageKinds(builder);
        RegisterFactionRelations(builder);
        RegisterQueries(builder);

        return builder;
    }

    public static GameplayTagRegistryBuilder CreateDomainOnlyBuilder()
    {
        return new GameplayTagRegistryBuilder()
            .RegisterDomain(EntityCondition, "Condition", "Mutable state metadata on living entities.")
            .RegisterDomain(EffectClassification, "Effect", "Metadata on effects used for blocking, removal, triggers, and filtering.")
            .RegisterDomain(ActionClassification, "Action", "Metadata on usable actions.")
            .RegisterDomain(ItemClassification, "Item", "Metadata on items and equipment.")
            .RegisterDomain(TargetClassification, "Target", "Targeting filter metadata.")
            .RegisterDomain(CooldownGroup, "Cooldown", "Shared cooldown identity metadata.")
            .RegisterDomain(DamageKind, "Damage", "Damage taxonomy.")
            .RegisterDomain(FactionRelation, "Faction", "Relationship or disposition metadata.");
    }

    public static GameplayTagId Tag(GameplayTagRegistry registry, string canonicalName)
    {
        return registry.GetTagId(canonicalName);
    }

    public static TagQueryId Query(GameplayTagRegistry registry, string queryKey)
    {
        return registry.GetQueryId(queryKey);
    }

    private static void RegisterEntityConditions(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(EntityCondition, "Condition.Dead")
            .RegisterTag(EntityCondition, "Condition.Poisoned")
            .RegisterTag(EntityCondition, "Condition.PoisonExposure")
            .RegisterTag(EntityCondition, "Condition.Invulnerable")
            .RegisterTag(EntityCondition, "Condition.Incurable")
            .RegisterTag(EntityCondition, "Condition.EnergyDepleted")
            .RegisterTag(EntityCondition, "Condition.EnergyDrained")
            .RegisterTag(EntityCondition, "Condition.Overheated")
            .RegisterTag(EntityCondition, "Condition.Infected")
            .RegisterTag(EntityCondition, "Condition.Cursed")
            .RegisterTag(EntityCondition, "Condition.Untargetable");
    }

    private static void RegisterEffectClassifications(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(EffectClassification, "Effect.Damage")
            .RegisterTag(EffectClassification, "Effect.Damage.Poison")
            .RegisterTag(EffectClassification, "Effect.Damage.Poison.Dot")
            .RegisterTag(EffectClassification, "Effect.Damage.Thermal")
            .RegisterTag(EffectClassification, "Effect.Buff")
            .RegisterTag(EffectClassification, "Effect.Buff.AttackSpeed")
            .RegisterTag(EffectClassification, "Effect.Buff.EnergyProduction")
            .RegisterTag(EffectClassification, "Effect.Buff.ShieldCapacity")
            .RegisterTag(EffectClassification, "Effect.Debuff")
            .RegisterTag(EffectClassification, "Effect.Cleanse")
            .RegisterTag(EffectClassification, "Effect.Emp")
            .RegisterTag(EffectClassification, "Effect.Overheating")
            .RegisterTag(EffectClassification, "Effect.OverheatingCooldown")
            .RegisterTag(EffectClassification, "Effect.Immunity")
            .RegisterTag(EffectClassification, "Effect.Immunity.Poison")
            .RegisterTag(EffectClassification, "Effect.Immunity.Corrosion")
            .RegisterTag(EffectClassification, "Effect.Immunity.Curse")
            .RegisterTag(EffectClassification, "Effect.CannotBeHealed")
            .RegisterTag(EffectClassification, "Effect.CannotBeRegenerated")
            .RegisterTag(EffectClassification, "Effect.Corroding")
            .RegisterTag(EffectClassification, "Effect.Fire")
            .RegisterTag(EffectClassification, "Effect.Infection");
    }

    private static void RegisterActionClassifications(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(ActionClassification, "Action.Skill")
            .RegisterTag(ActionClassification, "Action.Offensive")
            .RegisterTag(ActionClassification, "Action.Defensive")
            .RegisterTag(ActionClassification, "Action.Projectile")
            .RegisterTag(ActionClassification, "Action.Item");
    }

    private static void RegisterItemClassifications(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(ItemClassification, "Item.Weapon")
            .RegisterTag(ItemClassification, "Item.Weapon.Laser")
            .RegisterTag(ItemClassification, "Item.Consumable")
            .RegisterTag(ItemClassification, "Item.Equipment")
            .RegisterTag(ItemClassification, "Item.Equipment.ShieldGenerator");
    }

    private static void RegisterTargetClassifications(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(TargetClassification, "Target.LivingEntity")
            .RegisterTag(TargetClassification, "Target.Hostile")
            .RegisterTag(TargetClassification, "Target.Friendly")
            .RegisterTag(TargetClassification, "Target.Self");
    }

    private static void RegisterCooldownGroups(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(CooldownGroup, "Cooldown.Weapon")
            .RegisterTag(CooldownGroup, "Cooldown.Weapon.Light")
            .RegisterTag(CooldownGroup, "Cooldown.Weapon.Heavy");
    }

    private static void RegisterDamageKinds(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(DamageKind, "Damage.Kinetic")
            .RegisterTag(DamageKind, "Damage.Thermal")
            .RegisterTag(DamageKind, "Damage.Explosive")
            .RegisterTag(DamageKind, "Damage.Poison");
    }

    private static void RegisterFactionRelations(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterTag(FactionRelation, "Faction.Hostile")
            .RegisterTag(FactionRelation, "Faction.Friendly")
            .RegisterTag(FactionRelation, "Faction.Neutral");
    }

    private static void RegisterQueries(GameplayTagRegistryBuilder builder)
    {
        builder
            .RegisterQuery(
                "query.effect.cleanse_poison_removal",
                EffectClassification,
                all: ["Effect.Damage.Poison"],
                none: ["Effect.Immunity.Poison"])
            .RegisterQuery(
                "query.effect.is_damage",
                EffectClassification,
                all: ["Effect.Damage"])
            .RegisterQuery(
                "query.effect.exact_damage",
                EffectClassification,
                all: ["Effect.Damage"],
                matchMode: TagQueryMatchMode.Direct)
            .RegisterQuery(
                "query.condition.dead_or_incurable",
                EntityCondition,
                any: ["Condition.Dead", "Condition.Incurable"])
            .RegisterQuery(
                "query.effect.not_poison_immune",
                EffectClassification,
                none: ["Effect.Immunity.Poison"],
                matchAll: true);
    }
}
