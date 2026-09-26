using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sMPGWM.Scripts.FileLoadingSystem;

public static class DefinitionDeserializer
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(
        new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            FloatParseHandling = FloatParseHandling.Double,
            DateParseHandling = DateParseHandling.None
        });

    public static LoadResult<TDefinition> Deserialize<TDefinition>(
        JToken token,
        string sourcePath,
        string schemaPath)
        where TDefinition : class
    {
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var definition = token.ToObject<TDefinition>(Serializer);
            if (definition is null)
            {
                return LoadResult<TDefinition>.Failure(new DataLoadError(
                    "Definition deserialization failed",
                    $"The validated data could not be converted to {typeof(TDefinition).Name}.",
                    sourcePath,
                    schemaPath));
            }

            return LoadResult<TDefinition>.Success(definition);
        }
        catch (JsonSerializationException ex)
        {
            return LoadResult<TDefinition>.Failure(new DataLoadError(
                "Definition deserialization failed",
                $"The validated data does not match the C# type {typeof(TDefinition).Name}.",
                sourcePath,
                schemaPath,
                ex));
        }
        catch (Exception ex)
        {
            return LoadResult<TDefinition>.Failure(new DataLoadError(
                "Unexpected deserialization error",
                $"An unexpected error occurred while creating {typeof(TDefinition).Name}.",
                sourcePath,
                schemaPath,
                ex));
        }
    }
}
