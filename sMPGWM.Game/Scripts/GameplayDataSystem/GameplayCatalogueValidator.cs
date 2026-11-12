using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayCatalogueValidator
{
    private static T Reference<T>(DefinitionRegistry<T> registry, string id, string owner, string field)
        where T : GameplayDefinition => registry.TryGetById(id, out var definition) ? definition :
        throw new ArgumentException($"{owner}, {field}: unknown reference '{id}'.");

    public static void ValidateItems(GameplayCatalogues data, IReadOnlyList<ItemDefinition> items)
    {
        foreach (var item in items)
        {
            var owner = $"ItemDefinitions.yml: item '{item.Id}'";
            foreach (var id in item.AbilityIds) Reference(data.Abilities, id, owner, "AbilityIds");
            foreach (var grant in item.BehaviourGrants) Reference(data.Behaviours, grant.BehaviourId, owner, "BehaviourGrants");
            if (item.Values.TryGetValue("DamageSource", out var source))
                Reference(data.DamageSources, (string)source, owner, "Values.DamageSource");
        }
    }

    internal static void Validate(GameplayCatalogues data)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Edge(string source, string destination)
        {
            if (!graph.TryGetValue(source, out var edges)) graph.Add(source, edges = new());
            edges.Add(destination);
        }
        void Conditions(IReadOnlyList<string> ids, string owner, string field)
        {
            foreach (var id in ids) Reference(data.Conditions, id, owner, field);
        }
        void Modifier(ModifierDefinition modifier, string owner)
        {
            var stat = Reference(data.Stats, modifier.StatId, owner, "Modifiers.StatId");
            if (modifier.ValueKind == ModifierValueKind.Maximum && !stat.IsResource)
                throw new ArgumentException($"{owner}, Modifiers.ValueKind: Maximum requires a resource pool ('{stat.Id}').");
        }
        foreach (var ability in data.Abilities.GetAll())
        {
            var owner = $"AbilityDefinitions.yml: ability '{ability.Id}'";
            var target = Reference(data.Targeting, ability.TargetingId, owner, "TargetingId");
            if (target.Radius != null) throw new ArgumentException($"{owner}, TargetingId: selected-target ability cannot use an area profile.");
            Reference(data.Effects, ability.EffectId, owner, "EffectId");
            Conditions(ability.BlockedByConditions, owner, "BlockedByConditions");
            foreach (var cost in ability.Costs)
            {
                var stat = Reference(data.Stats, cost.StatId, owner, "Costs.StatId");
                if (!stat.IsResource) throw new ArgumentException($"{owner}, Costs.StatId: requires a resource pool.");
            }
        }
        foreach (var effect in data.Effects.GetAll())
        {
            var owner = $"EffectDefinitions.yml: effect '{effect.Id}'";
            var node = "effect:" + effect.Id;
            void EffectReference(string id, string field)
            {
                Reference(data.Effects, id, owner, field);
                Edge(node, "effect:" + id);
            }
            switch (effect)
            {
                case ApplyBehaviourEffect apply:
                    Reference(data.Behaviours, apply.BehaviourId, owner, "BehaviourId");
                    Edge(node, "behaviour:" + apply.BehaviourId);
                    break;
                case SequenceEffect sequence:
                    foreach (var id in sequence.EffectIds) EffectReference(id, "EffectIds");
                    break;
                case SearchAreaEffect area:
                    var target = Reference(data.Targeting, area.TargetingId, owner, "TargetingId");
                    if (target.Radius == null) throw new ArgumentException($"{owner}, TargetingId: area search requires a radius.");
                    EffectReference(area.EffectId, "EffectId");
                    break;
                case LaunchProjectileEffect projectile:
                    Reference(data.Projectiles, projectile.ProjectileId, owner, "ProjectileId");
                    EffectReference(projectile.ImpactEffectId, "ImpactEffectId");
                    break;
                case RestoreResourceEffect restore:
                    if (!Reference(data.Stats, restore.StatId, owner, "StatId").IsResource)
                        throw new ArgumentException($"{owner}, StatId: restoration requires a resource pool.");
                    Conditions(restore.BlockedByConditions, owner, "BlockedByConditions");
                    break;
                case DealDamageEffect damage:
                    Reference(data.DamageSources, damage.DamageSourceId, owner, "DamageSourceId");
                    break;
                case RemoveBehavioursEffect remove:
                    foreach (var id in remove.Selector.ClassificationIds) Reference(data.Classifications, id, owner, "Selector.ClassificationIds");
                    foreach (var id in remove.Selector.ModifiedStatIds) Reference(data.Stats, id, owner, "Selector.ModifiedStatIds");
                    break;
            }
        }
        foreach (var behaviour in data.Behaviours.GetAll())
        {
            var owner = $"BehaviourDefinitions.yml: behaviour '{behaviour.Id}'";
            foreach (var id in behaviour.ClassificationIds) Reference(data.Classifications, id, owner, "ClassificationIds");
            Conditions(behaviour.GrantedConditions, owner, "GrantedConditions");
            Conditions(behaviour.GrantedConditionsAfterFirstTick, owner, "GrantedConditionsAfterFirstTick");
            Conditions(behaviour.BlockedByConditions, owner, "BlockedByConditions");
            foreach (var modifier in behaviour.Modifiers) Modifier(modifier, owner);
            if (behaviour.PeriodicAccumulator is { } periodic)
            {
                Modifier(periodic.Modifier, owner);
                Conditions(periodic.BlockedByConditions, owner, "PeriodicAccumulator.BlockedByConditions");
                if (behaviour.Lifetime.DurationMs is { } duration && periodic.IntervalMs >= duration)
                    throw new ArgumentException($"{owner}, PeriodicAccumulator.IntervalMs: no tick before expiration.");
            }
            void Hook(string? id, string field)
            {
                if (id == null) return;
                Reference(data.Effects, id, owner, field);
                Edge("behaviour:" + behaviour.Id, "effect:" + id);
            }
            Hook(behaviour.OnApplyEffectId, "OnApplyEffectId");
            Hook(behaviour.OnExpireEffectId, "OnExpireEffectId");
            Hook(behaviour.OnRemove?.EffectId, "OnRemove.EffectId");
            if (behaviour.OnExpireEffectId != null && behaviour.Lifetime.Kind != BehaviourLifetimeKind.Timed)
                throw new ArgumentException($"{owner}, OnExpireEffectId: requires a timed lifetime.");
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string node, int depth)
        {
            if (visited.Contains(node)) return;
            if (depth > 256) throw new ArgumentException($"Gameplay dependency graph exceeds 256 levels at '{node}'.");
            if (!visiting.Add(node)) throw new ArgumentException($"EffectDefinitions.yml / BehaviourDefinitions.yml: dependency cycle at '{node}'.");
            if (graph.TryGetValue(node, out var edges))
                foreach (var edge in edges) Visit(edge, depth + 1);
            visiting.Remove(node);
            visited.Add(node);
        }
        foreach (var node in graph.Keys) Visit(node, 0);
    }
}
