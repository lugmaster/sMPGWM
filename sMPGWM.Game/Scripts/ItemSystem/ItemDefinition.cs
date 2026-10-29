using sMPGWM.Scripts.Util.UiComponents;
namespace sMPGWM.Scripts.ItemSystem;

public sealed class ItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public string CategoryName { get; } = categoryName;
    public string Description { get; } = description;
    public bool CanStack { get; } = canStack;
    public int StackLimit { get; } = stackLimit;
    public IconDefinition Icon { get; } = icon;
}
