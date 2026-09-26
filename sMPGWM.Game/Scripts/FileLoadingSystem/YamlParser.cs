using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace sMPGWM.Scripts.FileLoadingSystem;

public static class YamlParser
{
    public static LoadResult<JToken> ValidateAndReturn(
        string yamlText,
        string yamlSchemaText,
        string sourcePath,
        string schemaPath)
    {
        var dataResult = ParseToken(yamlText, sourcePath, "YAML data");
        if (!dataResult.IsSuccess)
            return LoadResult<JToken>.Failure(dataResult.Error!);

        var schemaTokenResult = ParseToken(yamlSchemaText, schemaPath, "YAML schema");
        if (!schemaTokenResult.IsSuccess)
            return LoadResult<JToken>.Failure(schemaTokenResult.Error!);

        if (schemaTokenResult.Value is not JObject schemaObject)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                "Invalid YAML schema root",
                "The YAML schema must contain an object at its root.",
                SchemaPath: schemaPath));
        }

        var schemaResult = CreateSchema(schemaObject, schemaPath);
        if (!schemaResult.IsSuccess)
            return LoadResult<JToken>.Failure(schemaResult.Error!);

        return Validate(dataResult.Value!, schemaResult.Value!, sourcePath, schemaPath);
    }

    public static LoadResult<TDefinition> ValidateAndDeserialize<TDefinition>(
        string yamlText,
        string yamlSchemaText,
        string sourcePath,
        string schemaPath)
        where TDefinition : class
    {
        var tokenResult = ValidateAndReturn(yamlText, yamlSchemaText, sourcePath, schemaPath);
        if (!tokenResult.IsSuccess)
            return LoadResult<TDefinition>.Failure(tokenResult.Error!);

        return DefinitionDeserializer.Deserialize<TDefinition>(
            tokenResult.Value!, sourcePath, schemaPath);
    }

    public static LoadResult<JToken> ParseToken(
        string yamlText,
        string sourcePath,
        string description = "YAML document")
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"Empty {description}",
                "The document is empty or contains only whitespace.",
                sourcePath));
        }

        try
        {
            using var reader = new StringReader(yamlText);
            var stream = new YamlStream();
            stream.Load(reader);

            if (stream.Documents.Count == 0)
            {
                return LoadResult<JToken>.Failure(new DataLoadError(
                    $"Empty {description}",
                    "The YAML stream does not contain a document.",
                    sourcePath));
            }

            if (stream.Documents.Count > 1)
            {
                return LoadResult<JToken>.Failure(new DataLoadError(
                    "Multiple YAML documents",
                    "Exactly one YAML document per file is supported.",
                    sourcePath));
            }

            return LoadResult<JToken>.Success(ConvertNode(stream.Documents[0].RootNode));
        }
        catch (YamlException ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"Invalid {description}",
                $"Line {ex.Start.Line}, column {ex.Start.Column}: {ex.Message}",
                sourcePath,
                Exception: ex));
        }
        catch (Exception ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                $"{description} parsing failed",
                "An unexpected error occurred while parsing the YAML document.",
                sourcePath,
                Exception: ex));
        }
    }

    private static JToken ConvertNode(YamlNode node) => node switch
    {
        YamlMappingNode mapping => ConvertMapping(mapping),
        YamlSequenceNode sequence => ConvertSequence(sequence),
        YamlScalarNode scalar => ConvertScalar(scalar),
        _ => throw new YamlException(
            node.Start,
            node.End,
            $"Unsupported YAML node type: {node.GetType().Name}. " +
            "Aliases, anchors and custom node types are not supported.")
    };

    private static JObject ConvertMapping(YamlMappingNode mapping)
    {
        var result = new JObject();

        foreach (var pair in mapping.Children)
        {
            if (pair.Key is not YamlScalarNode keyNode ||
                string.IsNullOrWhiteSpace(keyNode.Value))
            {
                throw new YamlException(
                    pair.Key.Start,
                    pair.Key.End,
                    "YAML object keys must be non-empty strings.");
            }

            var key = keyNode.Value;
            if (result.ContainsKey(key))
            {
                throw new YamlException(
                    keyNode.Start,
                    keyNode.End,
                    $"Duplicate YAML property '{key}'.");
            }

            result.Add(key, ConvertNode(pair.Value));
        }

        return result;
    }

    private static JArray ConvertSequence(YamlSequenceNode sequence)
    {
        var result = new JArray();
        foreach (var child in sequence.Children)
            result.Add(ConvertNode(child));
        return result;
    }

    private static JToken ConvertScalar(YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (value is null)
            return JValue.CreateNull();

        var tag = scalar.Tag.IsEmpty || scalar.Tag.IsNonSpecific
            ? string.Empty
            : scalar.Tag.Value;
        switch (tag)
        {
            case "tag:yaml.org,2002:null":
                return JValue.CreateNull();
            case "tag:yaml.org,2002:bool":
                return new JValue(ParseBoolean(value, scalar));
            case "tag:yaml.org,2002:int":
                return new JValue(ParseInteger(value, scalar));
            case "tag:yaml.org,2002:float":
                return new JValue(ParseFloat(value, scalar));
            case "tag:yaml.org,2002:str":
                return new JValue(value);
        }

        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted)
            return new JValue(value);

        if (value == "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return JValue.CreateNull();

        if (bool.TryParse(value, out var booleanValue))
            return new JValue(booleanValue);

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var integerValue))
            return new JValue(integerValue);

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var floatingPointValue))
            return new JValue(floatingPointValue);

        return new JValue(value);
    }

    private static bool ParseBoolean(string value, YamlScalarNode scalar)
    {
        if (bool.TryParse(value, out var result))
            return result;

        throw new YamlException(scalar.Start, scalar.End,
            $"Invalid boolean value '{value}'.");
    }

    private static long ParseInteger(string value, YamlScalarNode scalar)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var result))
            return result;

        throw new YamlException(scalar.Start, scalar.End,
            $"Invalid integer value '{value}'.");
    }

    private static double ParseFloat(string value, YamlScalarNode scalar)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var result))
            return result;

        throw new YamlException(scalar.Start, scalar.End,
            $"Invalid floating-point value '{value}'.");
    }

    private static LoadResult<JSchema> CreateSchema(JObject schemaObject, string schemaPath)
    {
        try
        {
            var schemaJson = schemaObject.ToString(Formatting.None);
            return LoadResult<JSchema>.Success(JSchema.Parse(schemaJson));
        }
        catch (JSchemaReaderException ex)
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "Invalid YAML schema",
                "The YAML document is valid YAML, but it is not a valid JSON Schema.",
                SchemaPath: schemaPath,
                Exception: ex));
        }
        catch (Exception ex)
        {
            return LoadResult<JSchema>.Failure(new DataLoadError(
                "YAML schema parsing failed",
                "An unexpected error occurred while creating the JSON Schema.",
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
                errors.Select(error =>
                {
                    var path = string.IsNullOrWhiteSpace(error.Path) ? "$" : error.Path;
                    return $"- {path}: {error.Message}";
                }));

            return LoadResult<JToken>.Failure(new DataLoadError(
                "YAML schema validation failed",
                details,
                sourcePath,
                schemaPath));
        }
        catch (Exception ex)
        {
            return LoadResult<JToken>.Failure(new DataLoadError(
                "YAML schema validation failed",
                "An unexpected error occurred while validating the YAML data.",
                sourcePath,
                schemaPath,
                ex));
        }
    }
}
