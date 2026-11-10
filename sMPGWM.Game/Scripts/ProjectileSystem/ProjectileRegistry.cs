using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ProjectileSystem;

public sealed class ProjectileRegistry : DefinitionRegistry<ProjectileDefinition>
{
    internal ProjectileRegistry(IEnumerable<ProjectileDefinition> definitions) : base(definitions, "ProjectileDefinitions.yml") { }
}
