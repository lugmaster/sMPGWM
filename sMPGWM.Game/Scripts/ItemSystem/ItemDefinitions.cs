using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ItemSystem;

public sealed class ItemDefinitions : DefinitionRegistry<ItemDefinition>
{
    private readonly Dictionary<string, CatalogueSource> _sources;

    internal ItemDefinitions(ItemDefinition[] definitions, Dictionary<string, CatalogueSource> sources)
        : base(definitions, "items")
    {
        _sources = new Dictionary<string, CatalogueSource>(sources, System.StringComparer.Ordinal);
    }

    internal CatalogueSource SourceFor(string id) => _sources[id];
}
