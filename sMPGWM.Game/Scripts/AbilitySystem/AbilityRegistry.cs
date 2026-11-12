using System;
using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityRegistry : DefinitionRegistry<AbilityDefinition>
{
    internal AbilityRegistry(IEnumerable<AbilityDefinition> definitions) : base(definitions, "AbilityDefinitions.yml") { }
    private readonly Dictionary<string, AbilityExecutionPlan> _plans = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _unsupported = new(StringComparer.Ordinal);
    private bool _prepared;
    internal void Prepare(GameplayCatalogues definitions)
    {
        if (_prepared) throw new InvalidOperationException("Abilities already prepared.");
        _prepared = true;
        foreach (var ability in definitions.Abilities.GetAll())
        {
            var reason = AbilityPreparer.Prepare(definitions, ability, out var plan);
            if (reason == null) _plans.Add(ability.Id, plan!);
            else _unsupported.Add(ability.Id, reason);
        }
    }
    internal bool TryGetPlan(string id, out AbilityExecutionPlan plan) => _plans.TryGetValue(id, out plan!);
    public bool IsSupported(string abilityId, out string reason)
    {
        if (_plans.ContainsKey(abilityId)) { reason = ""; return true; }
        reason = _unsupported.TryGetValue(abilityId, out var unsupported) ? unsupported : $"Unknown ability '{abilityId}'.";
        return false;
    }

    public IReadOnlyList<string> GetSupportDiagnostics(IEnumerable<ItemDefinition> items)
    {
        var diagnostics = new List<string>();
        foreach (var reason in _unsupported.Values) diagnostics.Add(reason);
        foreach (var item in items)
        {
            foreach (var ability in item.AbilityIds)
                if (!IsSupported(ability, out var reason)) diagnostics.Add($"Item '{item.Id}': {reason}");
            foreach (var grant in item.BehaviourGrants)
                diagnostics.Add($"Item '{item.Id}' -> behaviour '{grant.BehaviourId}': passive item behaviour grants ({grant.While}, {grant.Lifetime}) are not implemented.");
        }
        return diagnostics.AsReadOnly();
    }

}
