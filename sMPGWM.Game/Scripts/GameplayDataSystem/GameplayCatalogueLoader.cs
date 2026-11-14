using sMPGWM.Scripts.FileLoadingSystem;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.ProjectileSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

using static sMPGWM.Scripts.GameplayDataSystem.GameplayDefinitionParser;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayCatalogueLoader
{
    internal static LoadResult<GameplayCatalogues> TryLoad(Func<CatalogueSource, JObject> read,
        Func<string, IconDefinition> resolveIcon, Func<string, bool> textureAvailable)
    {
        try { return LoadResult<GameplayCatalogues>.Success(Load(read, resolveIcon, textureAvailable)); }
        catch (CatalogueLoadException error)
        {
            return LoadResult<GameplayCatalogues>.Failure(new DataLoadError("Invalid gameplay definitions",
                error.Message, error.CatalogueSource.UserPath, error.CatalogueSource.SchemaPath, error));
        }
        catch (ArgumentException error)
        {
            return LoadResult<GameplayCatalogues>.Failure(new DataLoadError("Invalid gameplay definitions", error.Message, Exception: error));
        }
    }

    private static readonly Catalogue<AbilityDefinition> Abilities = new(
        new("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions"), Ability);
    private static readonly Catalogue<EffectDefinition> Effects = new(
        new("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions"), Effect);
    private static readonly Catalogue<BehaviourDefinition> Behaviours = new(
        new("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions"), Behaviour);
    private static readonly Catalogue<TargetingDefinition> Targeting = new(
        new("TargetingDefinitions.yml", "TargetingSchema.yml", "TargetingDefinitions"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
            EnumValue<TargetRelation>(r, "Relation"), EnumValue<TargetLifeState>(r, "LifeState"),
            (bool)r["MechanicalOnly"]!, OptionalNumber(r, "MaxRange"), OptionalNumber(r, "Radius")));
    private static readonly Catalogue<BehaviourClassificationDefinition> Classifications = new(
        new("BehaviourClassificationDefinitions.yml", "BehaviourClassificationSchema.yml", "BehaviourClassifications"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));
    private static readonly Catalogue<StatDefinition> Stats = new(
        new("Attributes.yml", "AttributeSchema.yml", "Stats"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), IsResource(Text(r, "Id"))));
    private static readonly Catalogue<ConditionDefinition> Conditions = new(
        new("Conditions.yml", "ConditionSchema.yml", "Conditions"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));
    private static readonly Catalogue<ProjectileDefinition> Projectiles = new(
        new("ProjectileDefinitions.yml", "ProjectileSchema.yml", "ProjectileDefinitions"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
            Number(r, "Speed"), Number(r, "MaxLifetimeMs"),
            EnumValue<ProjectileCollision>(r, "Collision"), EnumValue<ProjectileExpiration>(r, "ExpireWithoutImpact")));
    private static readonly Catalogue<DamageSourceDefinition> DamageSources = new(
        new("DamageSources.yml", "DamageSourceSchema.yml", "DamageSources"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));

    public static IReadOnlyList<CatalogueSource> Sources { get; } = Array.AsReadOnly(new[]
    {
        Abilities.Source, Effects.Source, Behaviours.Source, Targeting.Source, Classifications.Source,
        Stats.Source, Conditions.Source, Projectiles.Source, DamageSources.Source
    });

    // The reader validates each document against its schema before returning it.
    internal static GameplayCatalogues Load(Func<CatalogueSource, JObject> read,
        Func<string, IconDefinition> resolveIcon, Func<string, bool> textureAvailable)
    {
        var categories = ItemCatalogueLoader.LoadCategories(read, resolveIcon);
        var items = ItemCatalogueLoader.LoadItems(read, categories, resolveIcon);
        var ships = ShipCatalogueLoader.Load(read, textureAvailable);
        return new GameplayCatalogues(
            new AbilityDefinitions(CatalogueParser.Load(Abilities, read), Abilities.Source),
            new DefinitionSet<EffectDefinition>(CatalogueParser.Load(Effects, read), Effects.Source),
            new DefinitionSet<BehaviourDefinition>(CatalogueParser.Load(Behaviours, read), Behaviours.Source),
            new DefinitionSet<TargetingDefinition>(CatalogueParser.Load(Targeting, read), Targeting.Source),
            new DefinitionSet<BehaviourClassificationDefinition>(CatalogueParser.Load(Classifications, read), Classifications.Source),
            new DefinitionSet<StatDefinition>(CatalogueParser.Load(Stats, read), Stats.Source),
            new DefinitionSet<ConditionDefinition>(CatalogueParser.Load(Conditions, read), Conditions.Source),
            new DefinitionSet<ProjectileDefinition>(CatalogueParser.Load(Projectiles, read), Projectiles.Source),
            new DefinitionSet<DamageSourceDefinition>(CatalogueParser.Load(DamageSources, read), DamageSources.Source), categories, items, ships);
    }

}
