using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class EffectRegistry : DefinitionRegistry<EffectDefinition>
{
    internal EffectRegistry(IEnumerable<EffectDefinition> definitions) : base(definitions, "EffectDefinitions.yml") { }
}
