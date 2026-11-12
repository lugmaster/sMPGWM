using System.Collections.Generic;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record AbilityGrantState(string? Name, StatContributionSource? Source, IReadOnlyList<string> AbilityIds);
