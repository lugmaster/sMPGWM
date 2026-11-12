using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemCategoryDefinition(string id, string displayName, IconDefinition icon)
    : GameplayDefinition(id, displayName, "", icon)
{
    public System.Collections.Generic.IReadOnlyList<string> Values { get; init; } = System.Array.Empty<string>();
    public bool CanStack { get; init; }
    public int StackLimit { get; init; } = 1;
}
