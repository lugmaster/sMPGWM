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
        var document = GameplayTestData.Read("items/engine.yml", "items/engine.schema.yml");
        var row = document["Items"]![0]!;
        var id = (string)row["Id"]!;
        switch (mutation)
        {
            case "category": row["CategoryId"] = "missing_category"; break;
            case "duplicate": ((JArray)document["Items"]!).Add(row.DeepClone()); break;
            case "numeric": row["Speed"] = -1; break;
            case "stacking": ((JObject)row).Remove("CanStack"); row["StackLimit"] = 1; break;
        }
        var result = GameplayCatalogueLoader.TryLoad(source => source.FileName == "engine.yml" ? document : Read(source),
            _ => null!, _ => true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("user://config/items/engine.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("res://assets/game_data/items/engine.schema.yml"));
        Assert.That(result.Error.Message, Does.Contain(id).And.Contain(field).And.Contain(detail));
    }

    [Test]
    public void CategoryDuplicatesHaveContext()
    {
        var document = GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml");
        var row = document["Categories"]![0]!;
        ((JArray)document["Categories"]!).Add(row.DeepClone());
        var error = Assert.Catch<ArgumentException>(() => ItemTestData.ParseCategories(document, _ => null!));
        Assert.That(error!.Message, Does.Contain("items/ItemCategoryDefinitions.yml").And.Contain("items/ItemCategorySchema.yml").And.Contain((string)row["Id"]!).And.Contain("duplicate"));
    }

    [Test]
    public void GameplayReferencesIdentifySourceAndField()
    {
        var result = GameplayCatalogueLoader.TryLoad(source =>
        {
            var document = Read(source);
            if (source.RootKey == "AbilityDefinitions") document[source.RootKey]![0]!["TargetingId"] = "missing_targeting";
            return document;
        }, _ => null!, _ => true);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.SourcePath, Is.EqualTo("user://config/gameplay/AbilityDefinitions.yml"));
        Assert.That(result.Error.SchemaPath, Is.EqualTo("res://assets/game_data/AbilitySchema.yml"));
        Assert.That(result.Error.Message, Does.Contain("TargetingId").And.Contain("missing_targeting"));
    }

    [Test]
    public void ProgrammingFailuresAreNotReportedAsContentErrors()
    {
        var catalogue = new Catalogue<int>(new CatalogueSource("source", "schema", "Rows"), _ => 1);
        Assert.Throws<InvalidOperationException>(() => CatalogueParser.Load(catalogue,
            _ => throw new InvalidOperationException("bug")));
    }

    private static JObject Read(CatalogueSource source) =>
        GameplayTestData.Read(source.DefaultDirectory + source.FileName, source.DefaultDirectory + source.SchemaName);
}
