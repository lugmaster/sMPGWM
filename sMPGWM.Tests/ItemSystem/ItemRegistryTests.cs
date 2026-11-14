using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class ItemRegistryTests
{
    [Test]
    public void PackagedYamlLoadsFinalDefinitionsWithResolvedCategoryNamesAndIcons()
    {
        var categories = ItemTestData.ParseCategories(Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var items = ItemTestData.ParseItems(Read("ItemDefinitions.yml", "items/ItemSchema.yml"), categories, _ => null!);
        Assert.That(items.Count, Is.EqualTo(23));
        Assert.That(items["mock_standard_medium_weapon"].CategoryId, Is.EqualTo("medium_weapon"));
        Assert.That(items["mock_advanced_medium_weapon"].CategoryId, Is.EqualTo("medium_weapon"));
        Assert.That(((WeaponFixtureItemDefinition)items["mock_standard_weapon_fixture"]).WeaponSlots, Is.EqualTo(9));
        Assert.That(items["mock_standard_weapon_fixture"].Mass, Is.EqualTo(1));
        Assert.That(typeof(WeaponFixtureItemDefinition).GetProperty("EnergyConsumption"), Is.Null);
        Assert.That(items["mock_standard_engine"].CanStack, Is.False);
        Assert.That(items["mock_standard_engine"].StackLimit, Is.EqualTo(1));
        Assert.That(items["mock_standard_consumable"].CanStack, Is.True);
        Assert.That(items["mock_standard_consumable"].StackLimit, Is.EqualTo(10));
        Assert.That(items["mock_standard_consumable"].CategoryName, Is.EqualTo(categories["consumable"].DisplayName));
    }

    [Test]
    public void MissingCategoryValuesAndMissingConsumableAbilitiesAreRejected()
    {
        var categories = ItemTestData.ParseCategories(Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var items = Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        ((JObject)items["Items"]![0]!).Remove("Speed");
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseItems(items, categories, _ => null!));
        items = Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        foreach (var item in items["Items"]!)
            if ((string?)item["CategoryId"] == "consumable") ((JObject)item).Remove("AbilityIds");
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseItems(items, categories, _ => null!));
    }

    [Test]
    public void DuplicateIdsAndUnknownCategoriesAreRejected()
    {
        var yaml = Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml");
        ((JArray)yaml["Categories"]!).Add(yaml["Categories"]![0]!.DeepClone());
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseCategories(yaml, _ => null!));
        var items = Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseItems(items, new(), _ => null!));
    }

    [Test]
    public void NonstackableItemsCannotDeclareMultipleItemsPerStack()
    {
        var categories = ItemTestData.ParseCategories(Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var yaml = Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        yaml["Items"]![0]!["StackLimit"] = 1;
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseItems(yaml, categories, _ => null!));
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
            var schema = category ? "items/ItemCategorySchema.yml" : "items/ItemSchema.yml";
            var yaml = "Version: 3.0.0\n" + (category ? "Categories" : "Items") + ":\n  - Id: resource\n    DisplayName: Test\n    Description: Test\n" +
                (category ? "" : "    CategoryId: resource\n    Mass: 1\n") + "    " + setting;
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
            var categories = ItemTestData.ParseCategories((JObject)YamlParser.ParseToken(
                "Version: 3.0.0\nCategories:\n  - Id: resource\n    DisplayName: Test\n    Description: Resource category\n    " + categorySetting, "test").Value!, key => { iconKey = key; return null!; });
            var items = ItemTestData.ParseItems((JObject)YamlParser.ParseToken(
                "Version: 3.0.0\nItems:\n  - Id: test\n    DisplayName: Test\n    Description: Resource item\n    CategoryId: resource\n    Mass: 1\n    " + itemSetting, "test").Value!, categories, _ => null!);
            var expected = itemSetting switch
            {
                "CanStack: false" => 1,
                "CanStack: true" => int.MaxValue,
                "StackLimit: 2" => 2,
                _ => categorySetting == "CanStack: true" ? int.MaxValue : categorySetting == "StackLimit: 7" ? 7 : 1
            };
            Assert.That(items["test"].StackLimit, Is.EqualTo(expected));
            Assert.That(items["test"].CanStack, Is.EqualTo(expected > 1));
            Assert.That(Icons.IsDefined(iconKey!), Is.True);
        }
    }

    [TestCase(-1d)]
    [TestCase(1.5d)]
    [TestCase(10d)]
    public void InvalidFixtureCapacityIsRejected(double capacity)
    {
        var categories = ItemTestData.ParseCategories(Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var yaml = Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        foreach (var item in yaml["Items"]!)
            if ((string?)item["CategoryId"] == "weapon_fixture") item["WeaponSlots"] = capacity;
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseItems(yaml, categories, _ => null!));
    }

    private static JObject Read(string file, string schema) => GameplayTestData.Read(file, schema);
}
