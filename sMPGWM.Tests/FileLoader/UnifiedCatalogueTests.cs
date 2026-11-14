using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.FileLoader;

public sealed class UnifiedCatalogueTests
{
    [Test]
    public void MissingStarterItemFailsDuringAssembly()
    {
        var error = Assert.Catch<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "engine.yml") document["Items"]![0]!["Id"] = "renamedEngine";
        }));
        Assert.That(error!.Message, Does.Contain("Starter loadout").And.Contain("mock_standard_engine"));
    }

    [Test]
    public void CompleteAssemblyReadsEverySourceOnceInDependencyOrder()
    {
        var sources = new List<CatalogueSource>();
        var data = GameplayCatalogueLoader.Load(source =>
        {
            sources.Add(source);
            return GameplayTestData.Read(source.DefaultDirectory + source.FileName, source.DefaultDirectory + source.SchemaName);
        }, _ => null!, _ => true);

        Assert.That(sources.Count, Is.EqualTo(22));
        Assert.That(sources[0], Is.EqualTo(ItemCatalogueLoader.CategorySource));
        for (var i = 0; i < ItemCatalogueLoader.ItemSources.Count; i++)
            Assert.That(sources[i + 1], Is.EqualTo(ItemCatalogueLoader.ItemSources[i]));
        Assert.That(sources[12], Is.EqualTo(ShipCatalogueLoader.Source));
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources) Assert.That(paths.Add(source.UserPath), Is.True, source.UserPath);
        Assert.That(data.Items.All, Is.Not.Empty);
        Assert.That(data.ItemCategories.All, Is.Not.Empty);
        Assert.That(data.Ships.All, Is.Not.Empty);
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out _), Is.True);
        Assert.That(data.SupportDiagnostics, Is.Not.Empty);
    }

    [TestCase("name", "ships")]
    [TestCase("Id", "Items")]
    public void CommonParserUsesSourceIdentityAndRejectsDuplicateRows(string identity, string root)
    {
        var source = new CatalogueSource("definitions.yml", "schema.yml", root, identity);
        var document = new JObject { [root] = new JArray(new JObject { [identity] = "same" }, new JObject { [identity] = "same" }) };
        var error = Assert.Throws<CatalogueLoadException>(() => CatalogueParser.Parse(document, source, row => (string)row[identity]!));
        Assert.That(error!.CatalogueSource, Is.SameAs(source));
        Assert.That(error.Message, Does.Contain(identity).And.Contain("duplicate").And.Contain("same"));
    }

    [TestCase("[3]")]
    [TestCase("[{\"Id\":3}]")]
    [TestCase("[{\"Id\":null}]")]
    public void InvalidIdentityShapeHasRowAndSourceContext(string rows)
    {
        var source = new CatalogueSource("broken.yml", "schema.yml", "Items");
        var document = new JObject { ["Items"] = JArray.Parse(rows) };
        var error = Assert.Throws<CatalogueLoadException>(() => CatalogueParser.Parse(document, source, _ => 0));
        Assert.That(error!.CatalogueSource, Is.SameAs(source));
        Assert.That(error.Message, Does.Contain("row 0").And.Contain("broken.yml"));
    }
}
