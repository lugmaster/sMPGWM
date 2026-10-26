using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.Mocks;

namespace sMPGWM.Tests.FileLoader;

[TestFixture]
public sealed class ItemDefinitionLoadingTests
{
    [Test]
    public void PackagedItems_PreserveAllSixteenExamplesAndStackLimits()
    {
        var catalog = LoadPackaged();
        Assert.That(catalog.Definitions, Has.Count.EqualTo(16));
        foreach (var category in catalog.Categories.All)
        {
            foreach (var variant in new[] { "standard", "advanced" })
            {
                var item = catalog.Get(new($"mock_{variant}_{category.Id.Value}"));
                Assert.That(item.CategoryId, Is.EqualTo(category.Id));
                Assert.That(item.DisplayName, Is.EqualTo($"{(variant == "standard" ? "Standard" : "Advanced")} {category.DisplayName}"));
                Assert.That(item.Description, Is.Not.Empty);
                Assert.That(item.IconAssetPath, Is.Null);
                Assert.That(catalog.GetStackLimit(item.Id), Is.EqualTo(category.CanStack
                    ? variant == "standard" ? 10 : 1000 : 1));
            }
        }
        Assert.DoesNotThrow(() => PlayerMock.ValidateInventoryDefinitions(catalog));
    }

    [TestCase("Id")]
    [TestCase("DisplayName")]
    [TestCase("CategoryId")]
    public void RequiredFields_CannotBeOmitted(string field)
    {
        var data = PackagedToken();
        ((JObject)data["Items"]![0]!).Remove(field);
        AssertFailure(data.ToString());
    }

    [TestCase("Id", " ")]
    [TestCase("DisplayName", "")]
    [TestCase("CategoryId", " ")]
    [TestCase("Description", null)]
    [TestCase("MaximumStackSize", "10")]
    [TestCase("Unexpected", "value")]
    public void InvalidFields_FailSchemaValidation(string field, string? value)
    {
        var data = PackagedToken();
        data["Items"]![0]![field] = value == null ? JValue.CreateNull() : new JValue(value);
        AssertFailure(data.ToString());
    }

    [TestCase(0L)]
    [TestCase(-1L)]
    [TestCase(2147483648L)]
    public void InvalidStackLimits_FailSchemaValidation(long limit)
    {
        var data = PackagedToken();
        data["Items"]![12]!["MaximumStackSize"] = limit;
        AssertFailure(data.ToString());
    }

    [TestCase("Items: null")]
    [TestCase("Other: []")]
    [TestCase("Items: [unterminated")]
    [TestCase("Items: [null]")]
    public void InvalidDocuments_ReturnStructuredErrors(string yaml) => AssertFailure(yaml);

    [TestCase("Id", "mock_advanced_engine")]
    [TestCase("CategoryId", "unknown_category")]
    public void InvalidReferencesAndDuplicates_FailCatalogValidation(string field, string value)
    {
        var data = PackagedToken();
        data["Items"]![0]![field] = value;
        var result = AssertFailure(data.ToString());
        Assert.That(result.Error!.Title, Is.EqualTo("Invalid item definitions"));
        Assert.That(result.Error.ToDisplayString(), Does.Contain(value));
    }

    [Test]
    public void NonstackableLimitAboveOne_FailsCatalogValidation()
    {
        var data = PackagedToken();
        data["Items"]![0]!["MaximumStackSize"] = 2;
        Assert.That(AssertFailure(data.ToString()).Error!.Title, Is.EqualTo("Invalid item definitions"));
    }

    [Test]
    public void OptionalFields_SupportDefaultsNullsAndExplicitArtwork()
    {
        var result = Load("""
            Items:
              - Id: default_resource
                DisplayName: Resource
                CategoryId: resource
              - Id: explicit_resource
                DisplayName: Resource with icon
                CategoryId: resource
                MaximumStackSize: null
                IconAssetPath: res://assets/example.png
            """);
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        var catalog = result.Value!;
        Assert.That(catalog.Get(new("default_resource")).Description, Is.Empty);
        Assert.That(catalog.Get(new("default_resource")).IconAssetPath, Is.Null);
        Assert.That(catalog.GetStackLimit(new("default_resource")), Is.EqualTo(int.MaxValue));
        Assert.That(catalog.GetStackLimit(new("explicit_resource")), Is.EqualTo(int.MaxValue));
        Assert.That(catalog.Get(new("explicit_resource")).IconAssetPath, Is.EqualTo("res://assets/example.png"));
    }

    [Test]
    public void EmptyCatalog_IsValidButCannotSupplyMockReferences()
    {
        var result = Load("Items: []");
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.Throws<ArgumentException>(() => PlayerMock.ValidateInventoryDefinitions(result.Value!));
    }

    [Test]
    public void MissingLastMockReference_IsDetectedBeforeSeeding()
    {
        var data = PackagedToken();
        ((JArray)data["Items"]!).Last!.Remove();
        var result = Load(data.ToString());
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        var error = Assert.Throws<ArgumentException>(() => PlayerMock.ValidateInventoryDefinitions(result.Value!));
        Assert.That(error!.Message, Does.Contain("mock_advanced_resource"));
    }

    [Test]
    public void EachLoad_IsIndependentAndRetainsProvidedCategories()
    {
        var first = LoadPackaged();
        var data = PackagedToken();
        data["Items"]![0]!["DisplayName"] = "Modified engine";
        var second = Load(data.ToString()).Value!;
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(first.Get(new("mock_standard_engine")).DisplayName, Is.EqualTo("Standard Engine"));
        Assert.That(second.Get(new("mock_standard_engine")).DisplayName, Is.EqualTo("Modified engine"));
    }

    private static ItemCatalog LoadPackaged()
    {
        var result = Load(ReadGameData("ItemDefinitions.yml"));
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        return result.Value!;
    }

    private static LoadResult<ItemCatalog> AssertFailure(string yaml)
    {
        var result = Load(yaml);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain(FilePaths.UserItemDataYaml));
        // Syntax errors have no schema context; validation/conversion errors include it.
        if (result.Error.SchemaPath != null)
            Assert.That(result.Error.SchemaPath, Is.EqualTo(FilePaths.ItemDataYamlSchema));
        return result;
    }

    private static LoadResult<ItemCatalog> Load(string yaml)
    {
        var categories = ItemCategories.CreateDefault();
        var definitions = YamlParser.ValidateAndDeserialize<ItemDefinitions>(yaml,
            ReadGameData("ItemSchema.yml"), FilePaths.UserItemDataYaml, FilePaths.ItemDataYamlSchema);
        var result = definitions.IsSuccess
            ? definitions.Value!.ToCatalog(categories, FilePaths.UserItemDataYaml, FilePaths.ItemDataYamlSchema)
            : LoadResult<ItemCatalog>.Failure(definitions.Error!);
        if (result.IsSuccess)
            Assert.That(result.Value!.Categories, Is.SameAs(categories));
        return result;
    }

    private static JObject PackagedToken() => (JObject)YamlParser.ParseToken(
        ReadGameData("ItemDefinitions.yml"), FilePaths.ItemDataYaml).Value!;

    private static string ReadGameData(string fileName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "sMPGWM.Game", "assets", "game_data", fileName);
            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate packaged game data '{fileName}'.");
    }
}
