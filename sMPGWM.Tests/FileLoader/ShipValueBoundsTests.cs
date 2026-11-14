using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.FileLoader;

public sealed class ShipValueBoundsTests
{
    [Test]
    public void MassCannotExceedRuntimeMassLimit()
    {
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        document["ships"]![0]!["mass"] = (double)float.MaxValue;
        AssertSchema(document, false);
        var parsed = ShipDefinitionLoader.Load(document, "test", "ShipsSchema.yml");
        Assert.That(parsed.IsSuccess, Is.True);
        Assert.Throws<ArgumentException>(() => ShipRegistry.Build(parsed.Value!, _ => true));
        document["ships"]![0]!["mass"] = (double)(float.MaxValue / 2);
        AssertSchema(document, true);
        parsed = ShipDefinitionLoader.Load(document, "test", "ShipsSchema.yml");
        Assert.DoesNotThrow(() => ShipRegistry.Build(parsed.Value!, _ => true));
    }

    [TestCase(1e100)]
    [TestCase(-1e100)]
    public void ColliderCoordinatesMustFitGodotVectors(double coordinate)
    {
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        document["ships"]![0]!["collider_points"]![0]![0] = coordinate;
        AssertSchema(document, false);
    }

    private static void AssertSchema(JObject document, bool valid)
    {
        var result = YamlParser.ValidateAndReturn(document.ToString(),
            File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, "ShipsSchema.yml")), "test", "ShipsSchema.yml");
        Assert.That(result.IsSuccess, Is.EqualTo(valid), result.Error?.ToDisplayString());
    }
}
