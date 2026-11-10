using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Items;

public sealed class ItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
{
    public System.Collections.Generic.IReadOnlyDictionary<string, object> Values { get; init; } = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new System.Collections.Generic.Dictionary<string, object>());
    public System.Collections.Generic.IReadOnlyList<string> AbilityIds { get; init; } = System.Array.Empty<string>();
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public string CategoryName { get; } = categoryName;
    public string Description { get; } = description;
    public bool CanStack { get; } = canStack;
    public int StackLimit { get; } = stackLimit;
    public IconDefinition Icon { get; } = icon;
}
