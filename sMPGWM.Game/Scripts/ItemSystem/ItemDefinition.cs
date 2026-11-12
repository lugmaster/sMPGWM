using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : GameplayDefinition(id, displayName, description, icon)
{
    private static readonly System.Collections.Generic.IReadOnlyDictionary<string, object> EmptyValues =
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new System.Collections.Generic.Dictionary<string, object>());
    public System.Collections.Generic.IReadOnlyDictionary<string, object> Values { get; init; } = EmptyValues;
    public System.Collections.Generic.IReadOnlyList<string> AbilityIds { get; init; } = System.Array.Empty<string>();
    public System.Collections.Generic.IReadOnlyList<ItemBehaviourGrant> BehaviourGrants { get; init; } = System.Array.Empty<ItemBehaviourGrant>();
    public string CategoryName { get; } = categoryName;
    public string CategoryId { get; init; } = "";
    public bool CanStack { get; } = canStack;
    public int StackLimit { get; } = stackLimit;
}
