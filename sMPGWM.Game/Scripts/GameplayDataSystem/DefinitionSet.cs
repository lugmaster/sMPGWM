using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.GameplayDataSystem;

public class DefinitionSet<T> where T : GameplayDefinition
{
    private readonly Dictionary<string, T> _byId;
    private readonly CatalogueSource? _source;
    private readonly Dictionary<string, CatalogueSource>? _sources;

    public IReadOnlyList<T> All { get; }

    internal DefinitionSet(T[] definitions, string file, IReadOnlyDictionary<string, CatalogueSource>? sources = null)
    {
        var owned = (T[])definitions.Clone();
        _byId = new Dictionary<string, T>(owned.Length, StringComparer.Ordinal);
        foreach (var definition in owned)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || !_byId.TryAdd(definition.Id, definition))
                throw new ArgumentException($"{file}: blank or duplicate Id '{definition.Id}'.");
        }
        All = Array.AsReadOnly(owned);
        if (sources != null) _sources = new Dictionary<string, CatalogueSource>(sources, StringComparer.Ordinal);
    }

    internal DefinitionSet(T[] definitions, CatalogueSource source) : this(definitions, source.UserPath)
    {
        _source = source;
    }

    public T GetById(string id) => _byId.TryGetValue(id, out var definition) ? definition :
        throw new KeyNotFoundException($"Unknown {typeof(T).Name} '{id}'.");

    public bool TryGetById(string id, out T definition) => _byId.TryGetValue(id, out definition!);

    internal CatalogueSource? SourceFor(string id) => _byId.ContainsKey(id)
        ? _source ?? (_sources != null && _sources.TryGetValue(id, out var source) ? source : null)
        : null;
}
