using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Tests;

public sealed class GameplayDefinitionSchemaTests
{
    [Test]
    public void IdentifiersRejectTrailingNewlines()
    {
        foreach (var source in GameplayCatalogueLoader.Sources)
        {
            var document = GameplayTestData.Read(source.FileName, source.SchemaName);
            var shapes = new HashSet<string>();
            foreach (JObject row in (JArray)document[source.RootKey]!)
            {
                if (!shapes.Add((string?)row["Kind"] ?? "")) continue;
                var original = (string)row["Id"]!;
                row["Id"] = original + "\n";
                AssertSchema(document, source.SchemaName, false);
                row["Id"] = original;
            }
        }
    }

    [TestCase("1.0.0\n")]
    [TestCase(" \t")]
    [TestCase("invalid")]
    public void GameplayVersionsRequireCompleteVersionStrings(string version)
    {
        foreach (var source in GameplayCatalogueLoader.Sources)
        {
            var document = GameplayTestData.Read(source.FileName, source.SchemaName);
            document["Version"] = version;
            AssertSchema(document, source.SchemaName, false);
        }
    }

    [TestCase("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions")]
    public void IconKeysRejectTrailingNewlines(string file, string schema, string root)
    {
        var document = GameplayTestData.Read(file, schema);
        var row = document[root]![0]!;
        row["Icon"] = "Software.WarningSignCircleQuestionMarkHelp\n";
        AssertSchema(document, schema, false);
    }

    [TestCase("DisplayName")]
    [TestCase("Description")]
    public void EveryGameplaySchemaBranchRequiresNonblankText(string field)
    {
        foreach (var source in GameplayCatalogueLoader.Sources)
        {
            var document = GameplayTestData.Read(source.FileName, source.SchemaName);
            var shapes = new HashSet<string>();
            foreach (JObject row in (JArray)document[source.RootKey]!)
            {
                if (!shapes.Add((string?)row["Kind"] ?? "")) continue;
                var original = row[field]!.DeepClone();
                row[field] = " \t\r\n";
                AssertSchema(document, source.SchemaName, false);
                row[field] = original;
            }
            AssertSchema(document, source.SchemaName, true);
        }
    }

    [TestCase("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions")]
    [TestCase("Conditions.yml", "ConditionSchema.yml", "Conditions")]
    public void ReplicatedDefinitionIdsHaveProtocolLengthBounds(string file, string schema, string root)
    {
        var document = GameplayTestData.Read(file, schema);
        document[root]![0]!["Id"] = new string('a', 64);
        AssertSchema(document, schema, true);
        document[root]![0]!["Id"] = new string('a', 65);
        AssertSchema(document, schema, false);
    }

    [TestCase("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions", "revive", "BlockedByConditions[0]")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions", "disrupted", "GrantedConditions[0]")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions", "disrupted", "BlockedByConditions[0]")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions", "maxHealthRot", "GrantedConditionsAfterFirstTick[0]")]
    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions", "maxHealthRot", "PeriodicAccumulator.BlockedByConditions[0]")]
    [TestCase("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions", "shieldRecharge", "BlockedByConditions[0]")]
    [TestCase("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions", "applyDisrupted", "BehaviourId")]
    public void ReferencesUseTheSameIdBounds(string file, string schema, string root, string id, string path)
    {
        var document = GameplayTestData.Read(file, schema);
        var row = GameplayTestData.Find((JArray)document[root]!, id);
        var token = row.SelectToken(path)!;
        Assert.That(token, Is.Not.Null, path);
        token.Replace(new JValue(new string('a', 64)));
        AssertSchema(document, schema, true);
        row.SelectToken(path)!.Replace(new JValue(new string('a', 65)));
        AssertSchema(document, schema, false);
        row.SelectToken(path)!.Replace(new JValue("validId\n"));
        AssertSchema(document, schema, false);
    }

    [TestCase("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions", "infection", "Stacking.MaxStacks")]
    [TestCase("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions", "removeInfection", "MaxCount")]
    public void CountsCannotOverflowRuntimeIntegers(string file, string schema, string root, string id, string field)
    {
        var document = GameplayTestData.Read(file, schema);
        var row = GameplayTestData.Find((JArray)document[root]!, id);
        row.SelectToken(field)!.Replace(new JValue(int.MaxValue));
        AssertSchema(document, schema, true);
        row.SelectToken(field)!.Replace(new JValue((long)int.MaxValue + 1));
        AssertSchema(document, schema, false);
    }

    [Test]
    public void ExpirationHookRequiresATimedLifetime()
    {
        var document = GameplayTestData.Read("BehaviourDefinitions.yml", "BehaviourSchema.yml");
        var row = GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "delayedExplosion");
        AssertSchema(document, "BehaviourSchema.yml", true);
        row["Lifetime"] = new JObject { ["Kind"] = "UntilRemoved" };
        AssertSchema(document, "BehaviourSchema.yml", false);
        row.Remove("OnExpireEffectId");
        AssertSchema(document, "BehaviourSchema.yml", true);
    }

    [Test]
    public void TargetRangeCanBeSquaredWithoutOverflow()
    {
        var document = GameplayTestData.Read("TargetingDefinitions.yml", "TargetingSchema.yml");
        var row = document["TargetingDefinitions"]![0]!;
        var maximum = 1.3407807929942596e154;
        Assert.That(double.IsFinite(maximum * maximum), Is.True);
        row["MaxRange"] = maximum;
        AssertSchema(document, "TargetingSchema.yml", true);
        row["MaxRange"] = double.MaxValue;
        AssertSchema(document, "TargetingSchema.yml", false);
    }

    private static void AssertSchema(JObject document, string schema, bool valid)
    {
        var result = YamlParser.ValidateAndReturn(document.ToString(),
            File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, schema)), "test", schema);
        Assert.That(result.IsSuccess, Is.EqualTo(valid), $"{schema}: {document}\n{result.Error?.ToDisplayString()}");
    }
}
