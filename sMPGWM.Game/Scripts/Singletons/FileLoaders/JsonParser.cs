using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace sMPGWM.Scripts.Singletons.FileLoaders;

public static class JsonParser
{
    public static LoadResult<JToken> ValidateAndReturn(
        string jsonText,
        string jsonSchemaText,
        string sourcePath,
        string schemaPath)
    {
        var dataResult = ParseToken(jsonText, sourcePath, "JSON data");
        if (!dataResult.IsSuccess)
            return LoadResult<JToken>.Failure(dataResult.Error!);

        var schemaResult = ParseSchema(jsonSchemaText, schemaPath);
        if (!schemaResult.IsSuccess)
            return LoadResult<JToken>.Failure(schemaResult.Error!);

        return Validate(dataResult.Value!, schemaResult.Value!, sourcePath, schemaPath);
    }

    public static LoadResult<TDefinition> ValidateAndDeserialize<TDefinition>(
        string jsonText,
        string jsonSchemaText,
        string sourcePath,
        string schemaPath)
        where TDefinition : class
    {
        var tokenResult = ValidateAndReturn(jsonText, jsonSchemaText, sourcePath, schemaPath);
        if (!tokenResult.IsSuccess)
            return LoadResult<TDefinition>.Failure(tokenResult.Error!);

        return DefinitionDeserializer.Deserialize<TDefinition>(
            tokenResult.Value!, sourcePath, schemaPath);
    }

    private static LoadResult<JToken> ParseToken(
        string text,
        string path,
        string description)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"Empty {description}",
                "The document is empty or contains only whitespace.",
                path));
        }

        try
        {
            using var reader = new JsonTextReader(new StringReader(text))
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            };

            var token = JToken.ReadFrom(reader);

            // Reject trailing non-whitespace content / a second root value.
            if (reader.Read())
            {
                return LoadResult<JToken>.Failure(new DataLoadError(
                    $"Invalid {description}",
                    "The document contains additional content after the root value.",
                    path));
            }

            return LoadResult<JToken>.Success(token);
        }
        catch (JsonReaderException ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"Invalid {description}",
                $"Line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}",
                path,
                Exception: ex));
        }
        catch (Exception ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"{description} parsing failed",
                "An unexpected error occurred while parsing the document.",
                path,
                Exception: ex));
        }
    }

    private static LoadResult<JSchema> ParseSchema(string schemaText, string schemaPath)
    {
        if (string.IsNullOrWhiteSpace(schemaText))
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "Empty JSON schema",
                "The schema is empty or contains only whitespace.",
                SchemaPath: schemaPath));
        }

        try
        {
            return LoadResult<JSchema>.Success(JSchema.Parse(schemaText));
        }
        catch (JSchemaReaderException ex)
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "Invalid JSON schema",
                "The schema could not be parsed as JSON Schema.",
                SchemaPath: schemaPath,
                Exception: ex));
        }
        catch (JsonReaderException ex)
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "Invalid JSON schema document",
                $"Line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}",
                SchemaPath: schemaPath,
                Exception: ex));
        }
        catch (Exception ex)
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "JSON schema parsing failed",
                "An unexpected error occurred while parsing the schema.",
                SchemaPath: schemaPath,
                Exception: ex));
        }
    }

    private static LoadResult<JToken> Validate(
        JToken token,
        JSchema schema,
        string sourcePath,
        string schemaPath)
    {
        try
        {
            if (token.IsValid(schema, out IList<ValidationError> errors))
                return LoadResult<JToken>.Success(token);

            var details = string.Join(
                Environment.NewLine,
                errors.Select(FormatValidationError));

            return LoadResult<JToken>.Failure(new DataLoadError(
                "JSON schema validation failed",
                details,
                sourcePath,
                schemaPath));
        }
        catch (Exception ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                "JSON schema validation failed",
                "An unexpected error occurred while validating the JSON data.",
                sourcePath,
                schemaPath,
                ex));
        }
    }

    private static string FormatValidationError(ValidationError error)
    {
        var path = string.IsNullOrWhiteSpace(error.Path) ? "$" : error.Path;
        return $"- {path}: {error.Message}";
    }
}