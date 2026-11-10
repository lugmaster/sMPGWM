using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityRegistry : DefinitionRegistry<AbilityDefinition>
{
    internal AbilityRegistry(IEnumerable<AbilityDefinition> definitions) : base(definitions, "AbilityDefinitions.yml") { }
}
