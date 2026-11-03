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
            Assert.That(ship.Name, Is.EqualTo("fighter"));
            Assert.That(ship.TexturePath, Is.EqualTo("res://assets/mocks/ship_fighter_reference.png"));
            Assert.That(ship.ColliderPoints, Has.Count.EqualTo(20));
            Assert.That(ship.ColliderPoints[0], Is.EqualTo(new double[] {27, -1}));
            Assert.That(ship.Stats.Speed, Is.EqualTo(350));
            Assert.That(ship.Stats.AfterBurnerSpeed, Is.EqualTo(450));
            Assert.That(ship.Stats.RotationSpeed, Is.EqualTo(5));
            Assert.That(ship.Stats.Acceleration, Is.EqualTo(1.3));
            Assert.That(ship.Stats.AfterBurnerAcceleration, Is.EqualTo(1.8));
            Assert.That(ship.Stats.ShipMass, Is.EqualTo(1));
            Assert.That(ship.WeaponSlots.Light, Is.EqualTo(2));
            Assert.That(ship.WeaponSlots.Heavy, Is.EqualTo(2));
        });
    }

    [TestCase("Speed: 350.0", "Speed: invalid")]
    [TestCase("Speed: 350.0", "unknown: 350.0")]
    [TestCase("name: \"fighter\"", "name: \" \"")]
    [TestCase("light: 2", "light: 2147483648")]
    public void InvalidDefinitionsIdentifySource(string original, string replacement)
    {
        var result = Parse(Read("Ships.yml").Replace(original, replacement));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain("Ships.yml"));
    }

    private static LoadResult<ShipDefinitionsDocument> Parse(string yaml) =>
        YamlParser.ValidateAndDeserialize<ShipDefinitionsDocument>(yaml, Read("ShipsSchema.yml"),
            "Ships.yml", "ShipsSchema.yml");
}
