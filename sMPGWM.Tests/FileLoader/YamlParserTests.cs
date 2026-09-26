using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Tests.FileLoader;

[TestFixture]
public sealed class YamlParserTests
{
    [Test]
    public void ValidateAndReturn_ValidYamlFileAgainstYamlSchema_ReturnsToken()
    {
        var result = YamlParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidYaml,
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        FileLoaderParserTestData.AssertShipToken(result.Value!);
    }

    [Test]
    public void ValidateAndDeserialize_ValidYamlFileAgainstYamlSchema_ReturnsDefinition()
    {
        var result = YamlParser.ValidateAndDeserialize<FileLoaderParserTestData.ShipDefinition>(
            FileLoaderParserTestData.ValidYaml,
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        FileLoaderParserTestData.AssertShipDefinition(result.Value!);
    }

    [Test]
    public void ValidateAndDeserialize_WhenCSharpTypeDoesNotMatchValidatedYaml_ReturnsDeserializerError()
    {
        var result = YamlParser.ValidateAndDeserialize<FileLoaderParserTestData.IncompleteShipDefinition>(
            FileLoaderParserTestData.ValidYaml,
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Definition deserialization failed",
            nameof(FileLoaderParserTestData.IncompleteShipDefinition));
    }

    [Test]
    public void ValidateAndDeserialize_InvalidYamlFile_ReturnsParseError()
    {
        var result = YamlParser.ValidateAndDeserialize<FileLoaderParserTestData.ShipDefinition>(
            "id: [unterminated",
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML data",
            "Line");
    }

    [Test]
    public void ParseToken_YamlScalarsAndCollections_ReturnJsonCompatibleTypes()
    {
        var result = YamlParser.ParseToken(
            """
            nullValue: null
            tildeNull: ~
            booleanValue: true
            integerValue: 42
            floatingValue: 12.5
            quotedInteger: "42"
            quotedBoolean: 'true'
            sequence:
              - 1
              - two
            """,
            FileLoaderParserTestData.YamlSourcePath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!["nullValue"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(result.Value!["tildeNull"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(result.Value!["booleanValue"]!.Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That(result.Value!["integerValue"]!.Type, Is.EqualTo(JTokenType.Integer));
            Assert.That(result.Value!["floatingValue"]!.Type, Is.EqualTo(JTokenType.Float));
            Assert.That(result.Value!["quotedInteger"]!.Type, Is.EqualTo(JTokenType.String));
            Assert.That(result.Value!["quotedBoolean"]!.Type, Is.EqualTo(JTokenType.String));
            Assert.That((string?)result.Value!["sequence"]?[1], Is.EqualTo("two"));
        });
    }

    [Test]
    public void ParseToken_ExplicitYamlTags_ReturnJsonCompatibleTypes()
    {
        var result = YamlParser.ParseToken(
            """
            taggedNull: !!null ""
            taggedBool: !!bool true
            taggedInt: !!int 42
            taggedFloat: !!float 12.5
            taggedString: !!str 42
            """,
            FileLoaderParserTestData.YamlSourcePath);

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!["taggedNull"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(result.Value!["taggedBool"]!.Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That(result.Value!["taggedInt"]!.Type, Is.EqualTo(JTokenType.Integer));
            Assert.That(result.Value!["taggedFloat"]!.Type, Is.EqualTo(JTokenType.Float));
            Assert.That(result.Value!["taggedString"]!.Type, Is.EqualTo(JTokenType.String));
        });
    }

    [TestCase("!!bool definitely", "Invalid boolean value")]
    [TestCase("!!int 12x", "Invalid integer value")]
    [TestCase("!!float 1x", "Invalid floating-point value")]
    public void ParseToken_InvalidExplicitYamlScalarTag_ReturnsTaggedValueError(
        string yamlText,
        string expectedMessagePart)
    {
        var result = YamlParser.ParseToken(yamlText, FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML document",
            expectedMessagePart);
    }

    [Test]
    public void ParseToken_EmptyYamlFile_ReturnsParseError()
    {
        var result = YamlParser.ParseToken("   ", FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Empty YAML document",
            "empty or contains only whitespace");
    }

    [Test]
    public void ParseToken_YamlStreamWithoutDocument_ReturnsEmptyStreamError()
    {
        var result = YamlParser.ParseToken("# comments only", FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Empty YAML document",
            "does not contain a document");
    }

    [Test]
    public void ParseToken_MultipleYamlDocuments_ReturnsParseError()
    {
        var result = YamlParser.ParseToken(
            """
            ---
            id: ship.fighter
            ---
            id: ship.bomber
            """,
            FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Multiple YAML documents",
            "Exactly one YAML document");
    }

    [Test]
    public void ParseToken_MalformedYamlFile_ReturnsParseErrorWithLineInformation()
    {
        var result = YamlParser.ParseToken(
            "id: [unterminated",
            FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML document",
            "Line");
    }

    [Test]
    public void ParseToken_DuplicateYamlProperty_ReturnsParseError()
    {
        var result = YamlParser.ParseToken(
            """
            id: ship.fighter
            id: ship.bomber
            """,
            FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML document",
            "Duplicate key");
    }

    [Test]
    public void ParseToken_YamlObjectKeyThatIsNotScalar_ReturnsParseError()
    {
        var result = YamlParser.ParseToken(
            """
            ? [ship, fighter]
            : active
            """,
            FileLoaderParserTestData.YamlSourcePath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML document",
            "YAML object keys must be non-empty strings");
    }

    [Test]
    public void ValidateAndReturn_YamlSchemaRootThatIsNotObject_ReturnsSchemaRootError()
    {
        var result = YamlParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidYaml,
            "- type: object",
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid YAML schema root",
            "object at its root");
    }

    [Test]
    public void ValidateAndReturn_InvalidYamlData_ReturnsParseErrorBeforeSchemaValidation()
    {
        var result = YamlParser.ValidateAndReturn(
            "id: [unterminated",
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitleContains(
            result,
            "Invalid YAML data",
            "Line");
    }

    [Test]
    public void ValidateAndReturn_EmptyYamlSchema_ReturnsSchemaParseError()
    {
        var result = YamlParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidYaml,
            "",
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Empty YAML schema",
            "empty or contains only whitespace");
    }

    [Test]
    public void ValidateAndReturn_InvalidYamlSchema_ReturnsSchemaError()
    {
        var result = YamlParser.ValidateAndReturn(
            FileLoaderParserTestData.ValidYaml,
            """
            type: not-a-json-schema-type
            """,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "Invalid YAML schema",
            "not a valid JSON Schema");
    }

    [Test]
    public void ValidateAndReturn_YamlSchemaThatFailsDuringValidation_ReturnsValidationError()
    {
        var result = YamlParser.ValidateAndReturn(
            "abc",
            """
            type: string
            pattern: "["
            """,
            "user://test-data/regex.yml",
            "res://test-data/regex.schema.yml");

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "YAML schema validation failed",
            "regex pattern");
    }

    [Test]
    public void ValidateAndReturn_YamlFileThatViolatesYamlSchema_ReturnsValidationErrorWithPaths()
    {
        var invalidYaml = FileLoaderParserTestData.ValidYaml.Replace("tier: 2", "tier: 0");

        var result = YamlParser.ValidateAndReturn(
            invalidYaml,
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "YAML schema validation failed",
            "tier");
        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.SourcePath, Is.EqualTo(FileLoaderParserTestData.YamlSourcePath));
            Assert.That(result.Error.SchemaPath, Is.EqualTo(FileLoaderParserTestData.YamlSchemaPath));
        });
    }

    [Test]
    public void ValidateAndReturn_YamlRootThatViolatesYamlSchema_FormatsRootPath()
    {
        var result = YamlParser.ValidateAndReturn(
            "[]",
            FileLoaderParserTestData.YamlSchema,
            FileLoaderParserTestData.YamlSourcePath,
            FileLoaderParserTestData.YamlSchemaPath);

        FileLoaderParserTestData.AssertFailureTitle(
            result,
            "YAML schema validation failed",
            "$");
    }
}
