using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record SequenceEffect(string Id, string DisplayName, string Description, IReadOnlyList<string> EffectIds, bool ContinueOnFailure)
    : EffectDefinition(Id, DisplayName, Description);
