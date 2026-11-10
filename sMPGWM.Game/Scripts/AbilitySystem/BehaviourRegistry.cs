using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class BehaviourRegistry : DefinitionRegistry<BehaviourDefinition>
{
    internal BehaviourRegistry(IEnumerable<BehaviourDefinition> definitions) : base(definitions, "BehaviourDefinitions.yml") { }
}
