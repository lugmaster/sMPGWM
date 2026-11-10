using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityLoadoutRegistry : DefinitionRegistry<AbilityLoadoutDefinition>
{
    internal AbilityLoadoutRegistry(IEnumerable<AbilityLoadoutDefinition> definitions) : base(definitions, "AbilityLoadoutDefinitions.yml") { }
}
