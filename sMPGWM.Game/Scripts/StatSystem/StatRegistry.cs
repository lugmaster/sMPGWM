using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.StatSystem;

public sealed class StatRegistry : DefinitionRegistry<StatDefinition>
{
    internal StatRegistry(IEnumerable<StatDefinition> definitions) : base(definitions, "Attributes.yml") { }
}
