using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.FileLoader;

public class CatalogueDiagnosticsTests
{
    [TestCase("category", "CategoryId", "missing_category")]
    [TestCase("duplicate", "Id", "duplicate")]
    [TestCase("numeric", "Speed", "invalid value")]
    [TestCase("stacking", "StackLimit", "inconsistent")]
    public void ItemFailuresPreserveSourceDefinitionAndField(string mutation, string field, string detail)
    {
        var categories = ItemDefinitionParser.LoadItemCategoryDefinitions(GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var document = GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        var row = document["Items"]![0]!;
        var id = (string)row["Id"]!;
        switch (mutation)
        {
            case "category": row["CategoryId"] = "missing_category"; break;
            case "duplicate": ((JArray)document["Items"]!).Add(row.DeepClone()); break;
            case "numeric": row["Values"]!["Speed"] = -1; break;
            case "stacking": ((JObject)row).Remove("CanStack"); row["StackLimit"] = 1; break;
        }
        var result = CatalogueParser.Load(() => ItemDefinitionParser.LoadItemDefinitions(document, categories, _ => null!),
            "user://custom/ItemDefinitions.yml", "res://assets/game_data/items/ItemSchema.yml");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("user://custom/ItemDefinitions.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("res://assets/game_data/items/ItemSchema.yml"));
        Assert.That(result.Error.Message, Does.Contain(id).And.Contain(field).And.Contain(detail));
    }

    [Test]
    public void CategoryDuplicatesHaveContext()
    {
        var document = GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml");
        var row = document["Categories"]![0]!;
        ((JArray)document["Categories"]!).Add(row.DeepClone());
        var error = Assert.Throws<ArgumentException>(() => ItemDefinitionParser.LoadItemCategoryDefinitions(document, _ => null!));
        Assert.That(error!.Message, Does.Contain("items/ItemCategoryDefinitions.yml").And.Contain("items/ItemCategorySchema.yml").And.Contain((string)row["Id"]!).And.Contain("duplicate"));
    }

    [Test]
    public void GameplayReferencesIdentifySourceAndField()
    {
        var result = GameplayCatalogueLoader.TryLoad(source =>
        {
            var document = GameplayTestData.Read(source.FileName, source.SchemaName);
            if (source.RootKey == "AbilityDefinitions") document[source.RootKey]![0]!["TargetingId"] = "missing_targeting";
            return document;
        });
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("user://config/gameplay/AbilityDefinitions.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("res://assets/game_data/AbilitySchema.yml"));
        Assert.That(result.Error.Message, Does.Contain("TargetingId").And.Contain("missing_targeting"));
    }

    [Test]
    public void ProgrammingFailuresAreNotReportedAsContentErrors()
    {
        Assert.Throws<InvalidOperationException>(() => CatalogueParser.Load<int>(() => throw new InvalidOperationException("bug"), "source", "schema"));
    }
}
