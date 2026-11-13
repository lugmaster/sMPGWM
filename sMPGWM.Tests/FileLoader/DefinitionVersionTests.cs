using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Tests.FileLoader;

public class DefinitionVersionTests
{
    [TestCase("Ships.yml", "ShipsSchema.yml")]
    [TestCase("items/engine.yml", "items/engine.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-000 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/hull.yml", "items/hull.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-001 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/shield_generator.yml", "items/shield_generator.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-002 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/reactor.yml", "items/reactor.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-003 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/light_weapon.yml", "items/light_weapon.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-004 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/medium_weapon.yml", "items/medium_weapon.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-005 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/heavy_weapon.yml", "items/heavy_weapon.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-006 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/consumable.yml", "items/consumable.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-007 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/resource.yml", "items/resource.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-008 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/battery.yml", "items/battery.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-009 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/weapon_fixture.yml", "items/weapon_fixture.schema.yml")] // TEST-SUPPORT: ITEM-SPLIT-VERSION-010 | Tests: sMPGWM.Tests.FileLoader.DefinitionVersionTests.VersionMetadataIsRequiredAndValidated
    [TestCase("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml")]
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
