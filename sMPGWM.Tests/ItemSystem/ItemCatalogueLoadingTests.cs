using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

public sealed class ItemCatalogueLoadingTests
{
    [Test]
    public void SourcesPreserveOrderAndProduceTypedDefinitionsWithoutMutatingDocuments()
    {
        var reads = new List<string>();
        var documents = new Dictionary<string, JObject>();
        var categories = ItemCatalogueLoader.LoadCategories(Read, _ => null!);
        var items = ItemCatalogueLoader.LoadItems(source =>
        {
            reads.Add(source.UserPath);
            var document = Read(source);
            documents.Add(source.FileName, document);
            return document;
        }, categories, _ => null!);
        Assert.That(items.All.Count, Is.EqualTo(23));
        Assert.That(items.All[0].Id, Is.EqualTo("mock_standard_engine"));
        Assert.That(items.All[^1].Id, Is.EqualTo("mock_standard_weapon_fixture"));
        Assert.That(items.GetById("mock_standard_engine"), Is.TypeOf<EngineItemDefinition>());
        Assert.That(items.GetById("mock_standard_consumable").StackLimit, Is.EqualTo(10));
        Assert.That(reads, Is.EqualTo(new[]
        {
            "user://config/items/engine.yml", "user://config/items/hull.yml", "user://config/items/shield_generator.yml",
            "user://config/items/reactor.yml", "user://config/items/light_weapon.yml", "user://config/items/medium_weapon.yml",
            "user://config/items/heavy_weapon.yml", "user://config/items/consumable.yml", "user://config/items/resource.yml",
            "user://config/items/battery.yml", "user://config/items/weapon_fixture.yml"
        }));
        foreach (var source in ItemCatalogueLoader.ItemSources)
            Assert.That(JToken.DeepEquals(documents[source.FileName], Read(source)), Is.True, source.FileName);
        Assert.Throws<NotSupportedException>(() => ((IList<ItemDefinition>)items.All).Clear());
    }

    [Test]
    public void DuplicateAcrossDocumentsNamesBothSourcesAndTheDefinition()
    {
        var categories = ItemCatalogueLoader.LoadCategories(Read, _ => null!);
        var error = Assert.Throws<CatalogueLoadException>(() => ItemCatalogueLoader.LoadItems(source =>
        {
            var document = Read(source);
            if (source.FileName == "hull.yml") document["Items"]![0]!["Id"] = "mock_standard_engine";
            return document;
        }, categories, _ => null!));
        Assert.That(error!.CatalogueSource.FileName, Is.EqualTo("hull.yml"));
        Assert.That(error.Message, Does.Contain("hull.yml").And.Contain("engine.yml").And.Contain("mock_standard_engine"));
    }

    [Test]
    public void MismatchedVersionNamesTheActualSource()
    {
        var categories = ItemCatalogueLoader.LoadCategories(Read, _ => null!);
        var error = Assert.Throws<CatalogueLoadException>(() => ItemCatalogueLoader.LoadItems(source =>
        {
            var document = Read(source);
            if (source.FileName == "hull.yml") document["Version"] = "4.0.0";
            return document;
        }, categories, _ => null!));
        Assert.That(error!.CatalogueSource.FileName, Is.EqualTo("hull.yml"));
        Assert.That(error.Message, Does.Contain("Version").And.Contain("mismatch"));
    }

    private static JObject Read(CatalogueSource source) =>
        GameplayTestData.Read(source.DefaultDirectory + source.FileName, source.DefaultDirectory + source.SchemaName);
}
