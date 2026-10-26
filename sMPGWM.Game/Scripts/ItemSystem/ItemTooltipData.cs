#nullable enable

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemTooltipData(
    string DisplayName,
    string CategoryName,
    string Description,
    bool CanStack,
    int EffectiveStackLimit,
    int? ConfiguredStackLimit);
