using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

internal static class CatalogueParser
{
    public static LoadResult<T> Load<T>(Func<T> parse, string sourcePath, string schemaPath)
    {
        try { return LoadResult<T>.Success(parse()); }
        catch (ArgumentException error)
        {
            return LoadResult<T>.Failure(new DataLoadError("Invalid catalogue definitions", error.Message,
                sourcePath, schemaPath, error));
        }
    }

    public static T[] Parse<T>(JObject document, CatalogueSource source, Func<JToken, T> parse)
    {
        if (document[source.RootKey] is not JArray rows)
            throw new ArgumentException($"{source.FileName} ({source.SchemaName}): {source.RootKey}: expected an array.");
        var result = new T[rows.Count];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < result.Length; i++)
        {
            var id = (string?)rows[i]["Id"];
            try
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id: required nonempty string.");
                if (!ids.Add(id)) throw new ArgumentException($"Id: duplicate definition '{id}'.");
                result[i] = parse(rows[i]);
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidCastException or OverflowException)
            {
                throw new ArgumentException($"{source.FileName} ({source.SchemaName}): definition '{id}': {error.Message}", error);
            }
        }
        return result;
    }
}
