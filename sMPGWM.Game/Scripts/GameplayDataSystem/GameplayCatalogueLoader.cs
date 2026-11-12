using sMPGWM.Scripts.FileLoadingSystem;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.ProjectileSystem;
using sMPGWM.Scripts.StatSystem;

using static sMPGWM.Scripts.GameplayDataSystem.GameplayDefinitionParser;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayCatalogueLoader
{
    public static LoadResult<GameplayCatalogues> TryLoad(Func<CatalogueSource, JObject> read)
    {
        try { return LoadResult<GameplayCatalogues>.Success(Load(read)); }
        catch (ArgumentException error)
        {
            foreach (var source in Sources)
                if (error.Message.Contains(source.FileName, StringComparison.Ordinal))
                    return LoadResult<GameplayCatalogues>.Failure(new DataLoadError("Invalid gameplay definitions",
                        error.Message, "user://config/gameplay/" + source.FileName,
                        "res://assets/game_data/" + source.SchemaName, error));
            return LoadResult<GameplayCatalogues>.Failure(new DataLoadError("Invalid gameplay definitions", error.Message, Exception: error));
        }
    }

    private static T[] Load<T>(Catalogue<T> catalogue, Func<CatalogueSource, JObject> read) =>
        CatalogueParser.Parse(read(catalogue.Source), catalogue.Source, catalogue.ParseDefinition);

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
    public static GameplayCatalogues Load(Func<CatalogueSource, JObject> read)
    {
        return new GameplayCatalogues(
            new AbilityRegistry(Load(Abilities, read)),
            new EffectRegistry(Load(Effects, read)),
            new BehaviourRegistry(Load(Behaviours, read)),
            new TargetingRegistry(Load(Targeting, read)),
            new BehaviourClassificationRegistry(Load(Classifications, read)),
            new StatRegistry(Load(Stats, read)),
            new ConditionRegistry(Load(Conditions, read)),
            new ProjectileRegistry(Load(Projectiles, read)),
            new DamageSourceRegistry(Load(DamageSources, read)));
    }

}
