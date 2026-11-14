using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace sMPGWM.Scripts.GameplayDataSystem;

internal static class CatalogueParser
{
    internal static T[] Load<T>(Catalogue<T> catalogue, Func<CatalogueSource, JObject> read) =>
        WithSource(catalogue.Source, () => Parse(read(catalogue.Source), catalogue.Source, catalogue.ParseDefinition));

    internal static T WithSource<T>(CatalogueSource source, Func<T> parse)
    {
        try { return parse(); }
        catch (CatalogueLoadException) { throw; }
        catch (Exception error) when (error is ArgumentException or FormatException or InvalidCastException or OverflowException)
        {
            throw new CatalogueLoadException(source, error.Message, error);
        }
    }

    public static T[] Parse<T>(JObject document, CatalogueSource source, Func<JToken, T> parse)
    {
        if (document[source.RootKey] is not JArray rows)
            throw new CatalogueLoadException(source, $"{source.RootKey}: expected an array.");
        var result = new T[rows.Count];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < result.Length; i++)
        {
            string? id = null;
            try
            {
                if (rows[i] is not JObject row) throw new ArgumentException("Expected an object.");
                if (row[source.IdentityField]?.Type != JTokenType.String)
                    throw new ArgumentException($"{source.IdentityField}: required string.");
                id = (string?)row[source.IdentityField];
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException($"{source.IdentityField}: required nonempty string.");
                if (!ids.Add(id)) throw new ArgumentException($"{source.IdentityField}: duplicate definition '{id}'.");
                result[i] = parse(rows[i]);
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidCastException or OverflowException)
            {
                throw new CatalogueLoadException(source, $"row {i}, definition '{id}': {error.Message}", error);
            }
        }
        return result;
    }
}
