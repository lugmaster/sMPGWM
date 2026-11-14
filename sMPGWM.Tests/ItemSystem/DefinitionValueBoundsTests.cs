using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

public sealed class DefinitionValueBoundsTests
{
    private static readonly string[] Categories =
    [
        "engine", "hull", "shield_generator", "reactor", "light_weapon", "medium_weapon", "heavy_weapon",
        "consumable", "resource", "battery", "weapon_fixture"
    ];

    [TestCase("Speed")]
    [TestCase("AfterBurnerSpeed")]
    [TestCase("RotationSpeed")]
    [TestCase("Acceleration")]
    [TestCase("AfterBurnerAcceleration")]
    public void MovementMustFitRuntimeFloatBeforeEquipmentCreation(string field)
    {
        var document = Read("engine");
        document["Items"]![0]![field] = 1e100;
        AssertRejected(document, "engine");
        document["Items"]![0]![field] = (double)float.MaxValue;
        AssertAccepted(document, "engine");
    }

    [TestCaseSource(nameof(Categories))]
    public void MassCannotExceedRuntimeMassLimit(string category)
    {
        var document = Read(category);
        document["Items"]![0]!["Mass"] = (double)float.MaxValue;
        AssertRejected(document, category);
        document["Items"]![0]!["Mass"] = (double)(float.MaxValue / 2);
        AssertAccepted(document, category);
    }

    [Test]
    public void ShieldDowntimeMustFitRuntimeTickConversion()
    {
        var document = Read("shield_generator");
        document["Items"]![0]!["DownTime"] = 1e100;
        AssertRejected(document, "shield_generator");
        document["Items"]![0]!["DownTime"] = long.MaxValue / 60d;
        AssertAccepted(document, "shield_generator");
    }

    [TestCaseSource(nameof(Categories))]
    public void OptionalIconsAcceptNullAndValidKeysButRejectMalformedKeys(string category)
    {
        var document = Read(category);
        var invalidIcons = category == "engine"
            ? new[] { "", " ", "MissingGroup", ".MissingGroup", "Software.", "Software.Invalid.Key", "Software.WarningSignCircleQuestionMarkHelp\n" }
            : new[] { " ", "Software.Invalid.Key", "Software.WarningSignCircleQuestionMarkHelp\n" };
        foreach (var icon in invalidIcons)
        {
            document["Items"]![0]!["Icon"] = icon;
            AssertRejected(document, category);
        }
        document["Items"]![0]!["Icon"] = null;
        AssertAccepted(document, category);
        document["Items"]![0]!["Icon"] = "Software.WarningSignCircleQuestionMarkHelp";
        AssertAccepted(document, category);
    }

    [TestCaseSource(nameof(Categories))]
    public void GameplayReferencesRespectProtocolIdentifierLimit(string category)
    {
        var document = Read(category);
        document["Items"]![0]!["AbilityIds"] = new JArray(new string('a', 65));
        AssertRejected(document, category);
        document["Items"]![0]!["AbilityIds"] = new JArray(new string('a', 64));
        AssertAccepted(document, category);
        document["Items"]![0]!["BehaviourGrants"] = new JArray(new JObject
        {
            ["BehaviourId"] = new string('b', 65), ["While"] = "Equipped", ["Lifetime"] = "WhileSourceActive"
        });
        AssertRejected(document, category);
        document["Items"]![0]!["BehaviourGrants"]![0]!["BehaviourId"] = new string('b', 64);
        AssertAccepted(document, category);
    }

    [TestCase("ReloadDuration")]
    [TestCase("EnergyPerShot")]
    public void WeaponSupplySchemasRejectNumericOverflow(string field)
    {
        foreach (var category in new[] { "light_weapon", "medium_weapon", "heavy_weapon" })
        {
            var document = Read(category);
            document["Items"]![0]!["Supply"] = field == "ReloadDuration"
                ? new JObject { ["Mode"] = "Ammunition", ["AmmoCapacity"] = 10, [field] = double.MaxValue }
                : new JObject { ["Mode"] = "Energy", [field] = double.MaxValue };
            AssertAccepted(document, category);
            var yaml = document.ToString().Replace(double.MaxValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture), "1e999", StringComparison.Ordinal);
            foreach (var schema in new[] { $"items/{category}.schema.yml", "items/ItemSchema.yml" })
            {
                var result = YamlParser.ValidateAndReturn(yaml, File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, schema)), "test", schema);
                Assert.That(result.IsSuccess, Is.False, schema);
            }
        }
    }

    private static JObject Read(string category) => GameplayTestData.Read($"items/{category}.yml", $"items/{category}.schema.yml");

    private static void AssertAccepted(JObject document, string category)
    {
        AssertSchemas(document, category, true);
        Assert.DoesNotThrow(() => Parse(document));
    }

    private static void AssertRejected(JObject document, string category)
    {
        AssertSchemas(document, category, false);
        Assert.Throws<ArgumentException>(() => Parse(document));
    }

    private static void AssertSchemas(JObject document, string category, bool valid)
    {
        foreach (var schema in new[] { $"items/{category}.schema.yml", "items/ItemSchema.yml" })
        {
            var result = YamlParser.ValidateAndReturn(document.ToString(),
                File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, schema)), "test", schema);
            Assert.That(result.IsSuccess, Is.EqualTo(valid), result.Error?.ToDisplayString() ?? schema);
        }
    }

    private static void Parse(JObject document)
    {
        var categories = ItemDefinitionParser.LoadItemCategoryDefinitions(
            GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        ItemDefinitionParser.LoadItemDefinitions(document, categories, _ => null!);
    }
}
