using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.FileLoader;

public class ShipDefinitionTests
{
    private static string Read(string name)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var path = Path.Combine(directory.FullName, "sMPGWM.Game", "assets", "game_data", name);
            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new FileNotFoundException(name);
    }

    [Test]
    public void PackagedFighterPreservesConfiguration()
    {
        var result = Parse(Read("Ships.yml"));
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        var ship = result.Value!.GetById("fighter");
        Assert.Multiple(() =>
        {
            Assert.That((string)GameplayTestData.Read("Ships.yml", "ShipsSchema.yml")["Version"]!, Is.EqualTo("2.0.0"));
            Assert.That(ship.Name, Is.EqualTo("fighter"));
            Assert.That(ship.Description, Is.EqualTo("An agile combat ship with bonuses to speed, acceleration, and handling."));
            Assert.That(ship.TexturePath, Is.EqualTo("res://assets/mocks/ship_fighter_reference.png"));
            Assert.That(ship.ColliderPoints, Has.Count.EqualTo(20));
            Assert.That(ship.ColliderPoints[0], Is.EqualTo(new Godot.Vector2(27, -1)));
            Assert.That(ship.StatModifiers.Speed, Is.EqualTo(50));
            Assert.That(ship.StatModifiers.AfterBurnerSpeed, Is.EqualTo(50));
            Assert.That(ship.StatModifiers.RotationSpeed, Is.EqualTo(2));
            Assert.That(ship.StatModifiers.Acceleration, Is.EqualTo(0.1));
            Assert.That(ship.StatModifiers.AfterBurnerAcceleration, Is.EqualTo(0.1));
            Assert.That(ship.Health, Is.EqualTo(150));
            Assert.That(ship.Mass, Is.EqualTo(1));
        });
    }

    [TestCase("Speed: 50.0", "Speed: invalid")]
    [TestCase("Speed: 50.0", "unknown: 350.0")]
    [TestCase("name: \"fighter\"", "name: \" \"")]
    [TestCase("health: 150", "")]
    [TestCase("health: 150", "health: 0")]
    [TestCase("health: 150", "health: -1")]
    [TestCase("health: 150", "health: null")]
    [TestCase("health: 150", "health: invalid")]
    public void InvalidDefinitionsIdentifySource(string original, string replacement)
    {
        var result = Parse(Read("Ships.yml").Replace(original, replacement));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain("Ships.yml"));
    }

    [Test]
    public void CatalogueConversionRejectsUnknownMembersAndPreservesSource()
    {
        var token = (JObject)YamlParser.ParseToken(Read("Ships.yml"), "Ships.yml").Value!;
        token["ships"]![0]!["unknown_member"] = 1;
        var result = Convert(token);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("Ships.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("ShipsSchema.yml"));
        Assert.That(result.Error.ToDisplayString(), Does.Contain("unknown_member"));
    }

    private static LoadResult<ShipDefinitions> Convert(JObject document)
    {
        try { return LoadResult<ShipDefinitions>.Success(ShipCatalogueLoader.Load(_ => document, _ => true)); }
        catch (CatalogueLoadException error)
        {
            return LoadResult<ShipDefinitions>.Failure(new DataLoadError("Invalid ship definitions", error.Message,
                "Ships.yml", "ShipsSchema.yml", error));
        }
    }

    private static LoadResult<ShipDefinitions> Parse(string yaml)
    {
        var result = YamlParser.ValidateAndReturn(yaml, Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml");
        if (!result.IsSuccess) return LoadResult<ShipDefinitions>.Failure(result.Error!);
        return Convert((JObject)result.Value!);
    }

    [TestCase("missing")]
    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("whitespace")]
    public void DescriptionsMustBePresentAndNonblank(string mutation)
    {
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        var ship = (JObject)document["ships"]![0]!;
        if (mutation == "missing") ship.Remove("description");
        else if (mutation == "null") ship["description"] = JValue.CreateNull();
        else ship["description"] = mutation switch
        {
            "empty" => "",
            _ => " \t\r\n"
        };
        var result = YamlParser.ValidateAndReturn(document.ToString(), Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain("description"));
        if (mutation is "missing" or "null")
            Assert.That(Convert(document).IsSuccess, Is.False);
    }
}
