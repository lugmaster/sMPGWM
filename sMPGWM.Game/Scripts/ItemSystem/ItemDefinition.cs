using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : GameplayDefinition(id, displayName, description, icon)
{
    public System.Collections.Generic.IReadOnlyDictionary<string, object> Values { get; init; } = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new System.Collections.Generic.Dictionary<string, object>());
    public System.Collections.Generic.IReadOnlyList<string> AbilityIds { get; init; } = System.Array.Empty<string>();
    public System.Collections.Generic.IReadOnlyList<ItemBehaviourGrant> BehaviourGrants { get; init; } = System.Array.Empty<ItemBehaviourGrant>();
    public string CategoryName { get; } = categoryName;
    public bool CanStack { get; } = canStack;
    public int StackLimit { get; } = stackLimit;
}
