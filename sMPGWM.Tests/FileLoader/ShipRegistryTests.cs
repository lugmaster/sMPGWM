using Godot;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.ShipSystem;
namespace sMPGWM.Tests.FileLoader;
public class ShipRegistryTests
{
    private static ShipDefinition Definition(string name = "fighter") => new()
    {
        Name = name, Description = "A test ship.", TexturePath = "texture",
        ColliderPoints = new() { new double[] {0, 0}, new double[] {10, 0}, new double[] {10, 10}, new double[] {5, 5}, new double[] {0, 10} },
        Health = 150,
        Mass = 1
    };
    [Test]
    public void RegistryCopiesCollectionsAndUsesExactNames()
    {
        var definition = Definition();
        var data = ShipDefinitionParser.Create(definition, _ => true);
        var rows = new[] { data, ShipDefinitionParser.Create(Definition("Fighter"), _ => true) };
        var registry = new ShipDefinitions(rows);
        rows[0] = rows[1];
        definition.ColliderPoints[0][0] = 99;
        definition.Health = 99;
        definition.Mass = 99;
        definition.Description = "Changed after loading.";
        definition.Stats = definition.Stats with { Speed = 99 };
        Assert.That(registry.GetAll(), Has.Count.EqualTo(2));
        Assert.That(registry.TryGetById("FIGHTER", out _), Is.False);
        Assert.That(registry.GetById("fighter").ColliderPoints[0], Is.EqualTo(Vector2.Zero));
        Assert.That(registry.GetById("fighter").Health, Is.EqualTo(150));
        Assert.That(registry.GetById("fighter").Mass, Is.EqualTo(1));
        Assert.That(registry.GetById("fighter").StatModifiers.Speed, Is.Zero);
        Assert.That(registry.GetById("fighter").Description, Is.EqualTo("A test ship."));
        Assert.Throws<NotSupportedException>(() => ((IList<Vector2>)data.ColliderPoints)[0] = Vector2.One);
        Assert.Throws<NotSupportedException>(() => ((IList<ShipData>)registry.GetAll())[0] = rows[1]);
    }
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.MaxValue)]
    public void InvalidNumbersFail(double speed)
    {
        var definition = Definition(); definition.Stats = definition.Stats with { Speed = speed };
        Assert.Throws<ArgumentException>(() => ShipDefinitionParser.Create(definition, _ => true));
    }
    [Test]
    public void DuplicateNamesAndUnavailableTexturesFail()
    {
        var document = new JObject { ["ships"] = new JArray(JObject.FromObject(Definition()), JObject.FromObject(Definition())) };
        var error = Assert.Catch<ArgumentException>(() => ShipCatalogueLoader.Load(_ => document, _ => true));
        Assert.That(error!.Message, Does.Contain("Ships.yml").And.Contain("fighter").And.Contain("duplicate"));
        Assert.Throws<ArgumentException>(() => ShipDefinitionParser.Create(Definition(), _ => false));
        var definition = Definition(); definition.Mass = -1;
        Assert.Throws<ArgumentException>(() => ShipDefinitionParser.Create(definition, _ => true));
    }
    [Test]
    public void InvalidGeometryFails()
    {
        Assert.That(ShipDefinitionParser.IsUsablePolygon(new[] { Vector2.Zero, Vector2.One, Vector2.Zero }), Is.False);
        Assert.That(ShipDefinitionParser.IsUsablePolygon(new[] { Vector2.Zero, Vector2.One, Vector2.One * 2 }), Is.False);
        Assert.That(ShipDefinitionParser.IsUsablePolygon(new[] { Vector2.Zero, new Vector2(10,10), new Vector2(0,10), new Vector2(10,0) }), Is.False);
    }

    [TestCase(0d)]
    [TestCase(-1d)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void InvalidHealthFails(double health)
    {
        var definition = Definition();
        definition.Health = health;
        Assert.Throws<ArgumentException>(() => ShipDefinitionParser.Create(definition, _ => true));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n")]
    public void InvalidDescriptionsFailBeforeRegistryCreation(string? description)
    {
        var definition = Definition();
        definition.Description = description!;
        var error = Assert.Throws<ArgumentException>(() => ShipDefinitionParser.Create(definition, _ => true));
        Assert.That(error!.Message, Does.Contain("description"));
    }
}
