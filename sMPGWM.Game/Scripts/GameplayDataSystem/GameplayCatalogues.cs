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
    public ItemCategoryDefinitions ItemCategories { get; }
    public ItemDefinitions Items { get; }
    public ShipDefinitions Ships { get; }
    public IReadOnlyList<string> SupportDiagnostics { get; }
    public AbilityRegistry Abilities { get; }
    public EffectRegistry Effects { get; }
    public BehaviourRegistry Behaviours { get; }
    public TargetingRegistry Targeting { get; }
    public BehaviourClassificationRegistry Classifications { get; }
    public StatRegistry Stats { get; }
    public ConditionRegistry Conditions { get; }
    public ProjectileRegistry Projectiles { get; }
    public DamageSourceRegistry DamageSources { get; }

    internal GameplayCatalogues(AbilityRegistry abilities, EffectRegistry effects, BehaviourRegistry behaviours,
        TargetingRegistry targeting, BehaviourClassificationRegistry classifications, StatRegistry stats,
        ConditionRegistry conditions, ProjectileRegistry projectiles, DamageSourceRegistry damageSources,
        ItemCategoryDefinitions itemCategories, ItemDefinitions items, ShipDefinitions ships)
    {
        ItemCategories = itemCategories; Items = items; Ships = ships;
        Abilities = abilities; Effects = effects; Behaviours = behaviours; Targeting = targeting;
        Classifications = classifications; Stats = stats; Conditions = conditions;
        Projectiles = projectiles; DamageSources = damageSources;
        GameplayCatalogueValidator.Validate(this);
        GameplayCatalogueValidator.ValidateItems(this, Items.GetAll());
        CatalogueParser.WithSource(ShipCatalogueLoader.Source, () =>
        {
            EquipmentDefinitionValidator.Validate(Ships.GetAll(), Items.GetAll());
            return true;
        });
        StarterLoadout.ValidateDefinitions(Items);
        foreach (var item in Items.GetAll()) EquipmentStatCalculator.Precompile(item);
        Abilities.Prepare(this);
        SupportDiagnostics = Abilities.GetSupportDiagnostics(Items.GetAll());
    }
}
