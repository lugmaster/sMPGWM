using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.ProjectileSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

public sealed class GameplayCatalogues
{
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
        ConditionRegistry conditions, ProjectileRegistry projectiles, DamageSourceRegistry damageSources)
    {
        Abilities = abilities; Effects = effects; Behaviours = behaviours; Targeting = targeting;
        Classifications = classifications; Stats = stats; Conditions = conditions;
        Projectiles = projectiles; DamageSources = damageSources;
        GameplayCatalogueValidator.Validate(this);
    }
}
