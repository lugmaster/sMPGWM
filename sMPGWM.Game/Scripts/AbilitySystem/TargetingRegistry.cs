using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class TargetingRegistry : DefinitionRegistry<TargetingDefinition>
{
    internal TargetingRegistry(IEnumerable<TargetingDefinition> definitions) : base(definitions, "TargetingDefinitions.yml") { }
}
