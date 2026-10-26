using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.FileLoader;

[TestFixture]
public sealed class ItemCategoryLoadingTests
{
    [Test]
    public void PackagedDefinitions_LoadAllCategoriesAndStackingMetadata()
    {
        var result = Load(ReadGameData("ItemCategoryDefinitions.yml"));

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.That(result.Value!.All, Is.EqualTo(ItemCategories.CreateDefault().All));
    }

    [Test]
    public void CustomCategory_LoadsOptionalIconAndSortsByDisplayOrder()
    {
        var data = PackagedToken();
        ((JArray)data["Categories"]!).Add(new JObject
        {
            ["Id"] = "artifact",
            ["DisplayName"] = "Ancient Artifact",
            ["DisplayOrder"] = -1,
            ["CanStack"] = false,
            ["IconAssetPath"] = "res://assets/ui/icons/artifact.png"
        });

        var result = Load(data.ToString());

        Assert.That(result.IsSuccess, Is.True, result.Error?.ToDisplayString());
        Assert.That(result.Value!.All, Has.Count.EqualTo(9));
        Assert.That(result.Value.All[0].Id.Value, Is.EqualTo("artifact"));
        Assert.That(result.Value.All[0].IconAssetPath, Is.EqualTo("res://assets/ui/icons/artifact.png"));
    }

    [TestCase("Id")]
    [TestCase("DisplayName")]
    [TestCase("DisplayOrder")]
    [TestCase("CanStack")]
    public void RequiredCategoryFields_CannotBeOmitted(string field)
    {
        var data = PackagedToken();
        ((JObject)data["Categories"]![0]!).Remove(field);

        AssertLoadFailure(data.ToString());
    }

    [TestCase("Id", " ")]
    [TestCase("DisplayName", "")]
    [TestCase("DisplayOrder", "first")]
    [TestCase("CanStack", "false")]
    public void InvalidFieldValues_FailSchemaValidation(string field, string value)
    {
        var data = PackagedToken();
        data["Categories"]![0]![field] = value;

        AssertLoadFailure(data.ToString());
    }

    [TestCase("Categories: []")]
    [TestCase("Categories: null")]
    [TestCase("Other: []")]
    [TestCase("Categories: [unterminated")]
    public void InvalidDocuments_ReturnStructuredErrors(string yaml)
    {
        AssertLoadFailure(yaml);
    }

    [Test]
    public void DuplicateCategoryIds_FailDomainValidationWithSourceDetails()
    {
        var data = PackagedToken();
        data["Categories"]![1]!["Id"] = "engine";

        var result = Load(data.ToString());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error!.Title, Is.EqualTo("Invalid item categories"));
        Assert.That(result.Error.ToDisplayString(), Does.Contain("engine"));
        Assert.That(result.Error.ToDisplayString(), Does.Contain(FilePaths.UserItemCategoryDataYaml));
    }

    [Test]
    public void EachLoad_CreatesAnIndependentCatalog()
    {
        var first = Load(ReadGameData("ItemCategoryDefinitions.yml"));
        var data = PackagedToken();
        data["Categories"]![0]!["DisplayName"] = "Modified Engine";
        var second = Load(data.ToString());

        Assert.That(second.IsSuccess, Is.True, second.Error?.ToDisplayString());
        Assert.That(first.Value!.Get(ItemCategories.Engine).DisplayName, Is.EqualTo("Engine"));
        Assert.That(second.Value!.Get(ItemCategories.Engine).DisplayName, Is.EqualTo("Modified Engine"));
        Assert.That(second.Value, Is.Not.SameAs(first.Value));
    }

    private static LoadResult<ItemCategories> Load(string yaml)
    {
        var definitions = YamlParser.ValidateAndDeserialize<ItemCategoryDefinitions>(yaml,
            ReadGameData("ItemCategorySchema.yml"), FilePaths.UserItemCategoryDataYaml,
            FilePaths.ItemCategoryDataYamlSchema);
        return definitions.IsSuccess
            ? definitions.Value!.ToCategories(FilePaths.UserItemCategoryDataYaml, FilePaths.ItemCategoryDataYamlSchema)
            : LoadResult<ItemCategories>.Failure(definitions.Error!);
    }

    private static JObject PackagedToken() => (JObject)YamlParser.ParseToken(
        ReadGameData("ItemCategoryDefinitions.yml"), FilePaths.ItemCategoryDataYaml).Value!;

    private static void AssertLoadFailure(string yaml)
    {
        var result = Load(yaml);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error!.ToDisplayString(), Does.Contain(FilePaths.UserItemCategoryDataYaml));
    }

    private static string ReadGameData(string fileName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "sMPGWM.Game", "assets", "game_data", fileName);
            if (File.Exists(path))
                return File.ReadAllText(path);
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate packaged game data '{fileName}'.");
    }
}
