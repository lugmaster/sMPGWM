using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record AbilityLoadoutDefinition(string Id, string DisplayName, string Description, IReadOnlyList<string> AbilityIds)
    : GameplayDefinition(Id, DisplayName, Description, null);
