using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.ProjectileSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

public sealed class GameplayCatalogues
{
    public DefinitionSet<ItemCategoryDefinition> ItemCategories { get; }
    public DefinitionSet<ItemDefinition> Items { get; }
    public DefinitionSet<ShipData> Ships { get; }
    public IReadOnlyList<string> SupportDiagnostics { get; }
    public AbilityDefinitions Abilities { get; }
    public DefinitionSet<EffectDefinition> Effects { get; }
    public DefinitionSet<BehaviourDefinition> Behaviours { get; }
    public DefinitionSet<TargetingDefinition> Targeting { get; }
    public DefinitionSet<BehaviourClassificationDefinition> Classifications { get; }
    public DefinitionSet<StatDefinition> Stats { get; }
    public DefinitionSet<ConditionDefinition> Conditions { get; }
    public DefinitionSet<ProjectileDefinition> Projectiles { get; }
    public DefinitionSet<DamageSourceDefinition> DamageSources { get; }

    internal GameplayCatalogues(AbilityDefinitions abilities, DefinitionSet<EffectDefinition> effects, DefinitionSet<BehaviourDefinition> behaviours,
        DefinitionSet<TargetingDefinition> targeting, DefinitionSet<BehaviourClassificationDefinition> classifications, DefinitionSet<StatDefinition> stats,
        DefinitionSet<ConditionDefinition> conditions, DefinitionSet<ProjectileDefinition> projectiles, DefinitionSet<DamageSourceDefinition> damageSources,
        DefinitionSet<ItemCategoryDefinition> itemCategories, DefinitionSet<ItemDefinition> items, DefinitionSet<ShipData> ships)
    {
        ItemCategories = itemCategories; Items = items; Ships = ships;
        Abilities = abilities; Effects = effects; Behaviours = behaviours; Targeting = targeting;
        Classifications = classifications; Stats = stats; Conditions = conditions;
        Projectiles = projectiles; DamageSources = damageSources;
        GameplayCatalogueValidator.Validate(this);
        GameplayCatalogueValidator.ValidateItems(this, Items.All);
        CatalogueParser.WithSource(ShipCatalogueLoader.Source, () =>
        {
            EquipmentDefinitionValidator.Validate(Ships.All, Items.All);
            return true;
        });
        StartLoadout.ValidateDefinitions(Items);
        foreach (var item in Items.All)
        {
            EquipmentStatCalculator.Precompile(item);
            WeaponSystem.WeaponDefinitionCompiler.Prepare(item);
        }
        Abilities.Prepare(this);
        SupportDiagnostics = Abilities.GetSupportDiagnostics(Items.All);
    }
}
