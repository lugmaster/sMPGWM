using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Tests.FileLoader;

public class DefinitionVersionTests
{
    [TestCase("Ships.yml", "ShipsSchema.yml")]
    [TestCase("ItemDefinitions.yml", "ItemSchema.yml")]
    [TestCase("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml")]
    public void VersionMetadataIsRequiredAndValidated(string file, string schema)
    {
        var directory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "../../../../sMPGWM.Game/assets/game_data"));
        var yaml = File.ReadAllText(Path.Combine(directory, file));
        var schemaText = File.ReadAllText(Path.Combine(directory, schema));
        Assert.That(yaml.Split('\n')[0].TrimEnd('\r'), Does.Match(@"^Version: [0-9]+\.[0-9]+\.[0-9]+$"));
        var result = YamlParser.ValidateAndReturn(yaml, schemaText, file, schema);
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.That((string?)result.Value!["Version"], Does.Match(@"^[0-9]+\.[0-9]+\.[0-9]+$"));

        var withoutVersion = yaml[(yaml.IndexOf('\n') + 1)..];
        Assert.That(YamlParser.ValidateAndReturn(withoutVersion, schemaText, file, schema).IsSuccess, Is.False);
        foreach (var invalid in new[] { "1", "1.0", "invalid", "null" })
            Assert.That(YamlParser.ValidateAndReturn("Version: " + invalid + "\n" + withoutVersion,
                schemaText, file, schema).IsSuccess, Is.False, invalid);
    }
}
