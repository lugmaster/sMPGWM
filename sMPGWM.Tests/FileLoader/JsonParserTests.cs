using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using sMPGWM.Scripts.Singletons.FileLoaders;

namespace sMPGWM.Tests.FileLoader;

[TestFixture]
public sealed class JsonParserTests
{
    [Test]
    public void ValidateAndReturn_ValidJsonFileAgainstJsonSchema_ReturnsToken()
    {
        var result = JsonParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidJson,
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        FileLoaderParserTestData.AssertShipToken(result.Value!);
    }

    [Test]
    public void ValidateAndDeserialize_ValidJsonFileAgainstJsonSchema_ReturnsDefinition()
    {
        var result = JsonParser.ValidateAndDeserialize<FileLoaderParserTestData.ShipDefinition>(
            FileLoaderParserTestData.ValidJson,
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        FileLoaderParserTestData.AssertShipDefinition(result.Value!);
    }

    [Test]
    public void ValidateAndDeserialize_WhenCSharpTypeDoesNotMatchValidatedJson_ReturnsDeserializerError()
    {
        var result = JsonParser.ValidateAndDeserialize<FileLoaderParserTestData.IncompleteShipDefinition>(
            FileLoaderParserTestData.ValidJson,
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Definition deserialization failed",
            nameof(FileLoaderParserTestData.IncompleteShipDefinition));
    }

    [Test]
    public void ValidateAndDeserialize_InvalidJsonFile_ReturnsParseError()
    {
        var result = JsonParser.ValidateAndDeserialize<FileLoaderParserTestData.ShipDefinition>(
            "{ \"id\": ",
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid JSON data",
            "Line");
    }

    [Test]
    public void ValidateAndReturn_EmptyJsonFile_ReturnsParseError()
    {
        var result = JsonParser.ValidateAndReturn(
            "   ",
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Empty JSON data",
            "empty or contains only whitespace");
    }

    [Test]
    public void ValidateAndReturn_MalformedJsonFile_ReturnsParseErrorWithLineInformation()
    {
        var result = JsonParser.ValidateAndReturn(
            "{ \"id\": ",
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid JSON data",
            "Line");
    }

    [Test]
    public void ValidateAndReturn_JsonFileWithSecondRootValue_ReturnsParseError()
    {
        var result = JsonParser.ValidateAndReturn(
            """{ "id": "ship.fighter" } { "id": "ship.bomber" }""",
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid JSON data",
            "additional content");
    }

    [Test]
    public void ValidateAndReturn_EmptyJsonSchema_ReturnsSchemaError()
    {
        var result = JsonParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidJson,
            "",
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Empty JSON schema",
            "empty or contains only whitespace");
    }

    [TestCase("""{ "type": "not-a-json-schema-type" }""")]
    public void ValidateAndReturn_InvalidJsonSchema_ReturnsSchemaError(string schemaText)
    {
        var result = JsonParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidJson,
            schemaText,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "JSON schema",
            "schema");
    }

    [Test]
    public void ValidateAndReturn_MalformedJsonSchemaDocument_ReturnsSchemaDocumentError()
    {
        var result = JsonParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidJson,
            "not json",
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid JSON schema document",
            "Line");
    }

    [Test]
    public void ValidateAndReturn_JsonFileThatViolatesJsonSchema_ReturnsValidationErrorWithPaths()
    {
        var invalidJson = FileLoaderParserTestData.ValidJson.Replace("\"tier\": 2", "\"tier\": 0");

        var result = JsonParser.ValidateAndReturn(
            invalidJson,
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "JSON schema validation failed",
            "tier");
        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.SourcePath, Is.EqualTo(FileLoaderParserTestData.JsonSourcePath));
            Assert.That(result.Error.SchemaPath, Is.EqualTo(FileLoaderParserTestData.JsonSchemaPath));
        });
    }

    [Test]
    public void ValidateAndReturn_JsonRootThatViolatesJsonSchema_FormatsRootPath()
    {
        var result = JsonParser.ValidateAndReturn(
            "[]",
            FileLoaderParserTestData.JsonSchema,
            FileLoaderParserTestData.JsonSourcePath,
            FileLoaderParserTestData.JsonSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "JSON schema validation failed",
            "$");
    }

    [Test]
    public void ValidateAndReturn_JsonSchemaThatFailsDuringValidation_ReturnsValidationError()
    {
        var result = JsonParser.ValidateAndReturn(
            "\"abc\"",
            """{ "type": "string", "pattern": "[" }""",
            "user://test-data/regex.json",
            "res://test-data/regex.schema.json");

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "JSON schema validation failed",
            "regex pattern");
    }

    [Test]
    public void Validate_DefensiveNullToken_ReturnsUnexpectedValidationError()
    {
        var result = InvokePrivateValidate(null, JSchema.Parse("""{ "type": "object" }"""));

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "JSON schema validation failed",
            "unexpected error");
    }

    [Test]
    public void ValidateAndReturn_JsonNumbersAreNotParsedAsDates()
    {
        var result = JsonParser.ValidateAndReturn(
            """{ "timestamp": "2026-07-20T20:00:00Z", "ratio": 0.5 }""",
            """
            {
              "type": "object",
              "required": [ "timestamp", "ratio" ],
              "properties": {
                "timestamp": { "type": "string" },
                "ratio": { "type": "number" }
              }
            }
            """,
            "user://test-data/time.json",
            "res://test-data/time.schema.json");

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!["timestamp"]!.Type, Is.EqualTo(JTokenType.String));
            Assert.That(result.Value!["ratio"]!.Type, Is.EqualTo(JTokenType.Float));
        });
    }

    private static LoadResult<JToken> InvokePrivateValidate(JToken? token, JSchema schema)
    {
        var method = typeof(JsonParser).GetMethod(
            "Validate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        return (LoadResult<JToken>)method!.Invoke(
            null,
            [token, schema, "user://test-data/private.json", "res://test-data/private.schema.json"])!;
    }
}
