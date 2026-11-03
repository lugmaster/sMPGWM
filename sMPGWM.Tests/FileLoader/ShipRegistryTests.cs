using Godot;
using sMPGWM.Scripts.ShipSystem;
namespace sMPGWM.Tests.FileLoader;
public class ShipRegistryTests
{
    private static ShipDefinition Definition(string name = "fighter") => new()
    {
        Name = name, TexturePath = "texture",
        ColliderPoints = new() { new double[] {0, 0}, new double[] {10, 0}, new double[] {10, 10}, new double[] {5, 5}, new double[] {0, 10} },
        Stats = new() { ShipMass = 1 }
    };
    [Test]
    public void RegistryCopiesCollectionsAndUsesExactNames()
    {
        var definition = Definition();
        var document = new ShipDefinitionsDocument { Ships = new() { definition, Definition("Fighter") } };
        var registry = ShipRegistry.Build(document, _ => true);
        definition.ColliderPoints[0][0] = 99;
        definition.Stats.ShipMass = 99;
        Assert.That(registry, Has.Count.EqualTo(2));
        Assert.That(registry.ContainsKey("FIGHTER"), Is.False);
        Assert.That(registry["fighter"].ColliderPoints[0], Is.EqualTo(Vector2.Zero));
        Assert.That(registry["fighter"].Stats.ShipMass, Is.EqualTo(1));
        Assert.Throws<NotSupportedException>(() => ((IList<Vector2>)registry["fighter"].ColliderPoints)[0] = Vector2.One);
    }
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.MaxValue)]
    [TestCase(-1d)]
    public void InvalidNumbersFail(double speed)
    {
        var definition = Definition(); definition.Stats.Speed = speed;
        Assert.Throws<ArgumentException>(() => ShipRegistry.Build(new() { Ships = new() { definition } }, _ => true));
    }
    [Test]
    public void DuplicateNamesAndUnavailableTexturesFail()
    {
        Assert.Throws<ArgumentException>(() => ShipRegistry.Build(new() { Ships = new() { Definition(), Definition() } }, _ => true));
        Assert.Throws<ArgumentException>(() => ShipRegistry.Build(new() { Ships = new() { Definition() } }, _ => false));
        var definition = Definition(); definition.Stats.ShipMass = 0;
        Assert.Throws<ArgumentException>(() => ShipRegistry.Build(new() { Ships = new() { definition } }, _ => true));
    }
    [Test]
    public void InvalidGeometryFails()
    {
        Assert.That(ShipRegistry.IsUsablePolygon(new[] { Vector2.Zero, Vector2.One, Vector2.Zero }), Is.False);
        Assert.That(ShipRegistry.IsUsablePolygon(new[] { Vector2.Zero, Vector2.One, Vector2.One * 2 }), Is.False);
        Assert.That(ShipRegistry.IsUsablePolygon(new[] { Vector2.Zero, new Vector2(10,10), new Vector2(0,10), new Vector2(10,0) }), Is.False);
    }
}
