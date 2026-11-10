using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.GameplayDataSystem;

public abstract class DefinitionRegistry<T> where T : GameplayDefinition
{
    private readonly Dictionary<string, T> _byId = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<T> _all;
    protected DefinitionRegistry(IEnumerable<T> definitions, string file)
    {
        var all = new List<T>();
        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || !_byId.TryAdd(definition.Id, definition))
                throw new ArgumentException($"{file}: blank or duplicate Id '{definition.Id}'.");
            all.Add(definition);
        }
        _all = all.AsReadOnly();
    }
    public T GetById(string id) => _byId.TryGetValue(id, out var definition) ? definition :
        throw new KeyNotFoundException($"Unknown {typeof(T).Name} '{id}'.");
    public bool TryGetById(string id, out T definition) => _byId.TryGetValue(id, out definition!);
    public IReadOnlyList<T> GetAll() => _all;
}
