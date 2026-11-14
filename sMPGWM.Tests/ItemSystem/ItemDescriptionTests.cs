using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

public sealed class ItemDescriptionTests
{
    [TestCase("engine")]
    [TestCase("hull")]
    [TestCase("shield_generator")]
    [TestCase("reactor")]
    [TestCase("battery")]
    [TestCase("weapon_fixture")]
    [TestCase("light_weapon")]
    [TestCase("medium_weapon")]
    [TestCase("heavy_weapon")]
    [TestCase("consumable")]
    [TestCase("resource")]
    public void EveryItemSchemaRequiresNonblankDescriptions(string category)
    {
        var document = GameplayTestData.Read($"items/{category}.yml", $"items/{category}.schema.yml");
        var row = (JObject)document["Items"]![0]!;
        foreach (var mutation in new[] { "missing", "blank" })
        {
            Invalidate(row, mutation);
            AssertRejected(document, $"items/{category}.schema.yml");
            AssertRejected(document, "items/ItemSchema.yml");
        }
    }

    [TestCase("missing")]
    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("blank")]
    [TestCase("number")]
    public void ParsersRejectInvalidDescriptionsAtTheLoadingBoundary(string mutation)
    {
        var categoryDocument = GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml");
        var categories = ItemDefinitionParser.LoadItemCategoryDefinitions(categoryDocument, _ => null!);
        Invalidate((JObject)categoryDocument["Categories"]![0]!, mutation);
        AssertRejected(categoryDocument, "items/ItemCategorySchema.yml");
        var categoryError = Assert.Throws<ArgumentException>(() => ItemDefinitionParser.LoadItemCategoryDefinitions(categoryDocument, _ => null!));
        Assert.That(categoryError!.Message, Does.Contain("Description"));

        var document = GameplayTestData.Read("items/resource.yml", "items/resource.schema.yml");
        Invalidate((JObject)document["Items"]![0]!, mutation);
        AssertRejected(document, "items/resource.schema.yml");
        AssertRejected(document, "items/ItemSchema.yml");
        var itemError = Assert.Throws<ArgumentException>(() => ItemRegistry.LoadItemDefinitions(document, categories, _ => null!));
        Assert.That(itemError!.Message, Does.Contain("Description"));
    }

    private static void Invalidate(JObject row, string mutation)
    {
        switch (mutation)
        {
            case "missing": row.Remove("Description"); break;
            case "null": row["Description"] = null; break;
            case "empty": row["Description"] = ""; break;
            case "blank": row["Description"] = " \t\r\n"; break;
            case "number": row["Description"] = 42; break;
        }
    }

    private static void AssertRejected(JObject document, string schema)
    {
        var result = YamlParser.ValidateAndReturn(document.ToString(),
            File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, schema)), "test", schema);
        Assert.That(result.IsSuccess, Is.False, schema);
        Assert.That(result.Error!.Exception, Is.Null, result.Error.ToDisplayString());
        if (schema != "items/ItemSchema.yml") Assert.That(result.Error.Message, Does.Contain("Description"));
    }
}
