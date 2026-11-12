using Newtonsoft.Json.Linq; // TEST-SUPPORT: CATALOGUE-SHIP-001 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
using sMPGWM.Scripts.GameplayDataSystem; // TEST-SUPPORT: CATALOGUE-SHIP-002 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.FileLoader;

public class ShipDefinitionTests // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-6 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
{
    private static string Read(string name) // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-8 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-10 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        while (directory != null) // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-11 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        {
            var path = Path.Combine(directory.FullName, "sMPGWM.Game", "assets", "game_data", name); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-13 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
            if (File.Exists(path)) return File.ReadAllText(path); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-14 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
            directory = directory.Parent; // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-15 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        }
        throw new FileNotFoundException(name); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-17 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource, sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
    }

    [Test] // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-20 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    public void PackagedFighterPreservesConfiguration() // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-21 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    {
        var result = Parse(Read("Ships.yml")); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-23 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString()); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-24 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        var ship = result.Value!.Ships.Single(); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-25 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        Assert.Multiple(() => // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-26 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        {
            Assert.That(result.Value.Version, Is.EqualTo("2.0.0")); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-28 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Name, Is.EqualTo("fighter")); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-29 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.TexturePath, Is.EqualTo("res://assets/mocks/ship_fighter_reference.png")); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-30 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.ColliderPoints, Has.Count.EqualTo(20)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-31 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.ColliderPoints[0], Is.EqualTo(new double[] {27, -1})); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-32 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Stats.Speed, Is.EqualTo(50)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-33 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Stats.AfterBurnerSpeed, Is.EqualTo(50)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-34 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Stats.RotationSpeed, Is.EqualTo(2)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-35 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Stats.Acceleration, Is.EqualTo(0.1)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-36 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Stats.AfterBurnerAcceleration, Is.EqualTo(0.1)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-37 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.Mass, Is.EqualTo(1)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-38 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.WeaponSlots.Light, Is.EqualTo(2)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-39 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
            Assert.That(ship.WeaponSlots.Heavy, Is.EqualTo(2)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-40 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        });
    }

    [TestCase("Speed: 50.0", "Speed: invalid")] // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-44 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    [TestCase("Speed: 50.0", "unknown: 350.0")] // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-45 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    [TestCase("name: \"fighter\"", "name: \" \"")] // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-46 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    [TestCase("light: 2", "light: 2147483648")] // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-47 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    public void InvalidDefinitionsIdentifySource(string original, string replacement) // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-48 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    {
        var result = Parse(Read("Ships.yml").Replace(original, replacement)); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-50 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        Assert.That(result.IsSuccess, Is.False); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-51 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        Assert.That(result.Error!.ToDisplayString(), Does.Contain("Ships.yml")); // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-52 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    }

    [Test] // TEST-SUPPORT: CATALOGUE-SHIP-003 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
    public void CatalogueConversionRejectsUnknownMembersAndPreservesSource() // TEST-SUPPORT: CATALOGUE-SHIP-004 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
    {
        var token = (JObject)YamlParser.ParseToken(Read("Ships.yml"), "Ships.yml").Value!; // TEST-SUPPORT: CATALOGUE-SHIP-005 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        token["ships"]![0]!["unknown_member"] = 1; // TEST-SUPPORT: CATALOGUE-SHIP-006 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        var result = ShipDefinitionLoader.Load(token, "Ships.yml", "ShipsSchema.yml"); // TEST-SUPPORT: CATALOGUE-SHIP-007 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        Assert.That(result.IsSuccess, Is.False); // TEST-SUPPORT: CATALOGUE-SHIP-008 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        Assert.That(result.Error!.SourcePath, Is.EqualTo("Ships.yml")); // TEST-SUPPORT: CATALOGUE-SHIP-009 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        Assert.That(result.Error.SchemaPath, Is.EqualTo("ShipsSchema.yml")); // TEST-SUPPORT: CATALOGUE-SHIP-010 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
        Assert.That(result.Error.ToDisplayString(), Does.Contain("unknown_member")); // TEST-SUPPORT: CATALOGUE-SHIP-011 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.CatalogueConversionRejectsUnknownMembersAndPreservesSource
    }

    private static LoadResult<ShipDefinitionsDocument> Parse(string yaml) // TEST-SUPPORT: DATA-V2-SHIPDEFINITIONTESTS-55 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    {
        var result = YamlParser.ValidateAndReturn(yaml, Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml"); // TEST-SUPPORT: CATALOGUE-SHIP-012 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        if (!result.IsSuccess) return LoadResult<ShipDefinitionsDocument>.Failure(result.Error!); // TEST-SUPPORT: CATALOGUE-SHIP-013 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
        return ShipDefinitionLoader.Load((JObject)result.Value!, "Ships.yml", "ShipsSchema.yml"); // TEST-SUPPORT: CATALOGUE-SHIP-014 | Tests: sMPGWM.Tests.FileLoader.ShipDefinitionTests.PackagedFighterPreservesConfiguration, sMPGWM.Tests.FileLoader.ShipDefinitionTests.InvalidDefinitionsIdentifySource
    }
}
