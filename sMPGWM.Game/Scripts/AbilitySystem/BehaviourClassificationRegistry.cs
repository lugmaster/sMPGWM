using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class BehaviourClassificationRegistry : DefinitionRegistry<BehaviourClassificationDefinition>
{
    internal BehaviourClassificationRegistry(IEnumerable<BehaviourClassificationDefinition> definitions) : base(definitions, "BehaviourClassificationDefinitions.yml") { }
}
