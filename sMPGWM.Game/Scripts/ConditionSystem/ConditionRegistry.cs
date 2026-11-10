using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ConditionSystem;

public sealed class ConditionRegistry : DefinitionRegistry<ConditionDefinition>
{
    internal ConditionRegistry(IEnumerable<ConditionDefinition> definitions) : base(definitions, "Conditions.yml") { }
}
