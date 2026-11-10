using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.CombatSystem;

public sealed class DamageSourceRegistry : DefinitionRegistry<DamageSourceDefinition>
{
    internal DamageSourceRegistry(IEnumerable<DamageSourceDefinition> definitions) : base(definitions, "DamageSources.yml") { }
}
