using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class ItemRegistryTests
{
    [Test]
    [Category("Godot")]
    public void EntitiesShareTheRegistryDefinitionAndHaveDistinctIdentities()
    {
        var first = ItemRegistry.CreateEntity("mock_standard_engine");
        var second = ItemRegistry.CreateEntity("mock_standard_engine");
        Assert.That(first.Definition, Is.SameAs(second.Definition).And.SameAs(ItemRegistry.GetById("mock_standard_engine")));
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        Assert.That(first.Definition.Icon.IconTexture, Is.Not.Null);
        Assert.That(first.Definition.Icon, Is.SameAs(ItemRegistry.GetCategoryById("engine").Icon));
        var all = ItemRegistry.GetAll();
        Assert.That(ItemRegistry.GetAll(), Is.SameAs(all));
        Assert.Throws<NotSupportedException>(() => ((IList<ItemDefinition>)ItemRegistry.GetAll()).Clear());
    }

    [TestCase("mock_standard_engine", 0)]
    [TestCase("mock_standard_engine", 2)]
    [TestCase("mock_standard_consumable", -1)]
    [TestCase("mock_standard_consumable", 11)]
    [Category("Godot")]
    public void InvalidEntityCountsAreRejected(string id, int count) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemRegistry.CreateEntity(id, count));

    [Test]
    public void PackagedYamlLoadsFinalDefinitionsWithResolvedCategoryNamesAndIcons()
    {
        var categories = ItemRegistry.LoadItemCategoryDefinitions(Read("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml"), _ => null!);
        var items = ItemRegistry.LoadItemDefinitions(Read("ItemDefinitions.yml", "ItemSchema.yml"), categories, _ => null!);
        Assert.That(items.Count, Is.EqualTo(16));
        Assert.That(items["mock_standard_engine"].CanStack, Is.False);
        Assert.That(items["mock_standard_engine"].StackLimit, Is.EqualTo(1));
        Assert.That(items["mock_standard_consumable"].CanStack, Is.True);
        Assert.That(items["mock_standard_consumable"].StackLimit, Is.EqualTo(10));
        Assert.That(items["mock_standard_consumable"].CategoryName, Is.EqualTo(categories["consumable"].DisplayName));
    }

    [Test]
    public void DuplicateIdsAndUnknownCategoriesAreRejected()
    {
        var yaml = Read("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml");
        ((JArray)yaml["Categories"]!).Add(yaml["Categories"]![0]!.DeepClone());
        Assert.Throws<ArgumentException>(() => ItemRegistry.LoadItemCategoryDefinitions(yaml, _ => null!));
        var items = Read("ItemDefinitions.yml", "ItemSchema.yml");
        Assert.Throws<KeyNotFoundException>(() => ItemRegistry.LoadItemDefinitions(items, new(), _ => null!));
    }

    [Test]
    public void NonstackableItemsCannotDeclareMultipleItemsPerStack()
    {
        var categories = ItemRegistry.LoadItemCategoryDefinitions(Read("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml"), _ => null!);
        var yaml = Read("ItemDefinitions.yml", "ItemSchema.yml");
        yaml["Items"]![0]!["StackLimit"] = 2;
        Assert.Throws<ArgumentException>(() => ItemRegistry.LoadItemDefinitions(yaml, categories, _ => null!));
    }

    private static JObject Read(string file, string schema)
    {
        var directory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../sMPGWM.Game/assets/game_data"));
        var result = YamlParser.ValidateAndReturn(File.ReadAllText(Path.Combine(directory, file)),
            File.ReadAllText(Path.Combine(directory, schema)), file, schema);
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        return (JObject)result.Value!;
    }
}
