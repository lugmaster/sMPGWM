using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.Singletons.FileLoaders;

namespace sMPGWM.Tests.FileLoader;

internal static class FileLoaderParserTestData
{
    public const string JsonSourcePath = "user://test-data/ship_definition.json";
    public const string JsonSchemaPath = "res://test-data/ship_schema.json";
    public const string YamlSourcePath = "user://test-data/ship_definition.yml";
    public const string YamlSchemaPath = "res://test-data/ship_schema.yml";

    public static string ValidJson => Read("ship_definition.json");
    public static string JsonSchema => Read("ship_schema.json");
    public static string ValidYaml => Read("ship_definition.yml");
    public static string YamlSchema => Read("ship_schema.yml");

    public static void AssertShipToken(JToken token)
    {
        Assert.Multiple(() =>
        {
            Assert.That((string?)token["id"], Is.EqualTo("ship.fighter"));
            Assert.That((string?)token["name"], Is.EqualTo("Interceptor"));
            Assert.That((int?)token["tier"], Is.EqualTo(2));
            Assert.That((bool?)token["active"], Is.True);
            Assert.That((double?)token["mass"], Is.EqualTo(12.5d));
            Assert.That(token["description"]?.Type, Is.EqualTo(JTokenType.Null));
            Assert.That((string?)token["hardpoints"]?[0]?["slot"], Is.EqualTo("nose"));
            Assert.That((bool?)token["hardpoints"]?[1]?["optional"], Is.True);
            Assert.That((int?)token["stats"]?["hull"], Is.EqualTo(120));
        });
    }

    public static void AssertShipDefinition(ShipDefinition definition)
    {
        Assert.Multiple(() =>
        {
            Assert.That(definition.Id, Is.EqualTo("ship.fighter"));
            Assert.That(definition.Name, Is.EqualTo("Interceptor"));
            Assert.That(definition.Tier, Is.EqualTo(2));
            Assert.That(definition.Active, Is.True);
            Assert.That(definition.Mass, Is.EqualTo(12.5d));
            Assert.That(definition.Description, Is.Null);
            Assert.That(definition.Hardpoints, Has.Count.EqualTo(2));
            Assert.That(definition.Hardpoints[1].Optional, Is.True);
            Assert.That(definition.Stats["shield"], Is.EqualTo(75));
        });
    }

    public static void AssertFailureTitle<T>(
        LoadResult<T> result,
        string expectedTitle,
        string expectedMessagePart)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Title, Is.EqualTo(expectedTitle));
            Assert.That(result.Error.Message, Does.Contain(expectedMessagePart));
        });
    }

    public static void AssertFailureTitleContains<T>(
        LoadResult<T> result,
        string expectedTitlePart,
        string expectedMessagePart)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Title, Does.Contain(expectedTitlePart));
            Assert.That(result.Error.Message, Does.Contain(expectedMessagePart));
        });
    }

    private static string Read(string fileName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "sMPGWM.Tests", "FileLoader", "TestData", fileName);
            if (File.Exists(path))
                return File.ReadAllText(path);

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate FileLoader test data file '{fileName}'.");
    }

    public sealed class ShipDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Tier { get; set; }
        public bool Active { get; set; }
        public double Mass { get; set; }
        public string? Description { get; set; }
        public List<HardpointDefinition> Hardpoints { get; set; } = [];
        public Dictionary<string, int> Stats { get; set; } = [];
    }

    public sealed class HardpointDefinition
    {
        public string Slot { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Optional { get; set; }
    }

    public sealed class IncompleteShipDefinition
    {
        public string Id { get; set; } = string.Empty;
    }
}
