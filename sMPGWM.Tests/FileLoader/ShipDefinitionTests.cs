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
            Assert.That((string)GameplayTestData.Read("Ships.yml", "ShipsSchema.yml")["Version"]!, Is.EqualTo("3.0.0"));
            Assert.That(ship.Name, Is.EqualTo("fighter"));
            Assert.That(ship.Description, Is.EqualTo("An agile combat ship with bonuses to speed, acceleration, and handling."));
            Assert.That(ship.TexturePath, Is.EqualTo("res://assets/mocks/ship_fighter_reference.png"));
            Assert.That(ship.ColliderPoints, Has.Count.EqualTo(20));
            Assert.That(ship.ColliderPoints[0], Is.EqualTo(new Godot.Vector2(27, -1)));
            Assert.That(ship.WeaponFixturePoints, Is.EqualTo(new[] { new Godot.Vector2(1, -15), new Godot.Vector2(1, 15) }));
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

    [TestCase(null)]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("[[]]")]
    [TestCase("[[1]]")]
    [TestCase("[[1, 2, 3]]")]
    [TestCase("[1, 2]")]
    [TestCase("[null]")]
    [TestCase("[[\"1\", 2]]")]
    [TestCase("[[1, true]]")]
    [TestCase("[[1, null]]")]
    [TestCase("[[1, 1e100]]")]
    [TestCase("[[-1e100, 1]]")]
    public void SchemaRequiresAtLeastOneNumericCoordinatePair(string? points)
    {
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        var ship = (JObject)document["ships"]![0]!;
        if (points == null) ship.Remove("weapon_fixture_points");
        else ship["weapon_fixture_points"] = JToken.Parse(points);

        var result = YamlParser.ValidateAndReturn(document.ToString(), Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain("weapon_fixture_points"));
        if (points is null or "null")
            Assert.That(Convert(document).IsSuccess, Is.False);
    }

    [TestCase("[[0, 0]]", 1)]
    [TestCase("[[1, -15], [1, 15]]", 2)]
    [TestCase("[[-1.5, 2.25], [0, 0], [3, -4]]", 3)]
    public void FixturePointsReachRuntimeInAuthoredOrder(string points, int count)
    {
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        var coordinates = JArray.Parse(points);
        document["ships"]![0]!["weapon_fixture_points"] = coordinates;
        var result = Parse(document.ToString());
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        var data = result.Value!.GetById("fighter");
        using var runtime = new ShipRuntime(data);
        Assert.That(runtime.WeaponFixturePoints, Has.Count.EqualTo(count));
        Assert.That(runtime.WeaponFixturePoints, Is.SameAs(data.WeaponFixturePoints));
        for (var i = 0; i < count; i++)
            Assert.That(runtime.WeaponFixturePoints[i], Is.EqualTo(new Godot.Vector2((float)coordinates[i][0]!, (float)coordinates[i][1]!)));
    }

    private static LoadResult<DefinitionSet<ShipData>> Convert(JObject document)
    {
        try { return LoadResult<DefinitionSet<ShipData>>.Success(ShipCatalogueLoader.Load(_ => document, _ => true)); }
        catch (CatalogueLoadException error)
        {
            return LoadResult<DefinitionSet<ShipData>>.Failure(new DataLoadError("Invalid ship definitions", error.Message,
                "Ships.yml", "ShipsSchema.yml", error));
        }
    }

    private static LoadResult<DefinitionSet<ShipData>> Parse(string yaml)
    {
        var result = YamlParser.ValidateAndReturn(yaml, Read("ShipsSchema.yml"), "Ships.yml", "ShipsSchema.yml");
        if (!result.IsSuccess) return LoadResult<DefinitionSet<ShipData>>.Failure(result.Error!);
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
