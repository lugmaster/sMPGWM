using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.Items;

public sealed class ItemCategoryDefinition(string id, string displayName, IconDefinition icon)
{
    public string Description { get; init; } = "";
    public bool CanStack { get; init; }
    public int StackLimit { get; init; } = 1;
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public IconDefinition Icon { get; } = icon;
}
