using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class ItemRegistryTests
{
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
        yaml["Items"]![0]!["StackLimit"] = 1;
        Assert.Throws<ArgumentException>(() => ItemRegistry.LoadItemDefinitions(yaml, categories, _ => null!));
    }

    [TestCase("", true)]
    [TestCase("CanStack: true", true)]
    [TestCase("CanStack: false", true)]
    [TestCase("StackLimit: 2", true)]
    [TestCase("StackLimit: 2147483646", true)]
    [TestCase("StackLimit: 2147483647", false)]
    [TestCase("StackLimit: 0", false)]
    [TestCase("StackLimit: 1", false)]
    [TestCase("StackLimit: -1", false)]
    [TestCase("StackLimit: 2.5", false)]
    [TestCase("CanStack: true\n    StackLimit: 2", false)]
    public void StackingSchemas(string setting, bool valid)
    {
        var directory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../sMPGWM.Game/assets/game_data"));
        foreach (var category in new[] { false, true })
        {
            var schema = category ? "ItemCategorySchema.yml" : "ItemSchema.yml";
            var yaml = (category ? "Categories" : "Items") + ":\n  - Id: test\n    DisplayName: Test\n    Description: Test\n" +
                (category ? "" : "    CategoryId: test\n") + "    " + setting;
            var result = YamlParser.ValidateAndReturn(yaml, File.ReadAllText(Path.Combine(directory, schema)), "test", schema);
            Assert.That(result.IsSuccess, Is.EqualTo(valid), yaml);
        }
    }

    [Test]
    public void DefaultsAndOverridesResolveBeforeUse()
    {
        foreach (var categorySetting in new[] { "", "CanStack: true", "StackLimit: 7" })
        foreach (var itemSetting in new[] { "", "CanStack: false", "CanStack: true", "StackLimit: 2" })
        {
            string? iconKey = null;
            var categories = ItemRegistry.LoadItemCategoryDefinitions((JObject)YamlParser.ParseToken(
                "Categories:\n  - Id: test\n    DisplayName: Test\n    " + categorySetting, "test").Value!, key => { iconKey = key; return null!; });
            var items = ItemRegistry.LoadItemDefinitions((JObject)YamlParser.ParseToken(
                "Items:\n  - Id: test\n    DisplayName: Test\n    CategoryId: test\n    " + itemSetting, "test").Value!, categories, _ => null!);
            var expected = itemSetting switch
            {
                "CanStack: false" => 1,
                "CanStack: true" => int.MaxValue,
                "StackLimit: 2" => 2,
                _ => categorySetting == "CanStack: true" ? int.MaxValue : categorySetting == "StackLimit: 7" ? 7 : 1
            };
            Assert.That(items["test"].StackLimit, Is.EqualTo(expected));
            Assert.That(items["test"].CanStack, Is.EqualTo(expected > 1));
            Assert.That(sMPGWM.Scripts.Util.UiComponents.Icons.IsDefined(iconKey!), Is.True);
        }
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
