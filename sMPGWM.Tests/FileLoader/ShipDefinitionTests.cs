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
        var ship = result.Value!.Ships.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Version, Is.EqualTo("2.0.0"));
            Assert.That(ship.Name, Is.EqualTo("fighter"));
            Assert.That(ship.TexturePath, Is.EqualTo("res://assets/mocks/ship_fighter_reference.png"));
            Assert.That(ship.ColliderPoints, Has.Count.EqualTo(20));
            Assert.That(ship.ColliderPoints[0], Is.EqualTo(new double[] {27, -1}));
            Assert.That(ship.Stats.Speed, Is.EqualTo(50));
            Assert.That(ship.Stats.AfterBurnerSpeed, Is.EqualTo(50));
            Assert.That(ship.Stats.RotationSpeed, Is.EqualTo(2));
            Assert.That(ship.Stats.Acceleration, Is.EqualTo(0.1));
            Assert.That(ship.Stats.AfterBurnerAcceleration, Is.EqualTo(0.1));
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
        var result = ShipDefinitionLoader.Load(token, "Ships.yml", "ShipsSchema.yml");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("Ships.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("ShipsSchema.yml"));
        Assert.That(result.Error.ToDisplayString(), Does.Contain("unknown_member"));
    }

    private static LoadResult<ShipDefinitionsDocument> Parse(string yaml)
    {
        var result = YamlParser.ValidateAndReturn(yaml, Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml");
        if (!result.IsSuccess) return LoadResult<ShipDefinitionsDocument>.Failure(result.Error!);
        return ShipDefinitionLoader.Load((JObject)result.Value!, "Ships.yml", "ShipsSchema.yml");
    }
}
