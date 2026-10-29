using sMPGWM.Scripts.Util.UiComponents;
namespace sMPGWM.Scripts.ItemSystem;

public sealed class ItemCategoryDefinition(string id, string displayName, IconDefinition icon)
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public IconDefinition Icon { get; } = icon;
}
