using System.Reflection;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using File = System.IO.File;

namespace sMPGWM.Tests.FileLoader;

[Category("Godot")]
[NonParallelizable]
public class DefinitionPreparationTests
{
    private static readonly FieldInfo PreparedData = typeof(GameplayDefinitions)
        .GetField("Data", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Test]
    public void StartupPublishesAllDefinitionsAndCompatibleSourceCapture()
    {
        Assert.DoesNotThrow(GameConfig.EnsureCompatibilityData);
        Assert.That(ItemRegistry.GetAll(), Is.Not.Empty);
        Assert.That(ItemRegistry.GetAllCategories(), Is.Not.Empty);
        Assert.That(ShipRegistry.GetAll(), Is.Not.Empty);
        var inputs = GetInputs();
        Assert.That(inputs, Has.Count.EqualTo(45));
        Assert.That(inputs["GameplayProtocol"], Is.EqualTo("6:complete-initial-state-readiness"));
        Assert.That(inputs.ContainsKey("ItemSchema.yml"), Is.False);
        Assert.That(inputs.ContainsKey("ItemDefinitions.yml"), Is.False);
        Assert.That(GameConfig.GameDataChecksum, Is.EqualTo(ChecksumGenerator.Compute(inputs)));
        foreach (var pair in inputs)
        {
            if (pair.Key == "GameplayProtocol") continue;
            Assert.That(pair.Key.Contains('/'), Is.False);
            var root = ProjectSettings.GlobalizePath("res://assets/game_data");
            var files = System.IO.Directory.GetFiles(root, pair.Key, System.IO.SearchOption.AllDirectories);
            Assert.That(files, Has.Length.EqualTo(1), pair.Key);
            Assert.That(ChecksumGenerator.Hash(pair.Value), Is.EqualTo(ChecksumGenerator.Hash(File.ReadAllText(files[0]))), pair.Key);
        }
    }

    [Test]
    public void UninitializedLookupsAndCompatibilityChecksNeverLoadDefinitions()
    {
        var previous = PreparedData.GetValue(null);
        try
        {
            PreparedData.SetValue(null, null);
            AssertUnavailable();
            Assert.That(PreparedData.GetValue(null), Is.Null);
        }
        finally
        {
            PreparedData.SetValue(null, previous);
        }
    }

    [Test]
    public void FailedStartupPublishesNothingAndExplicitRetryUsesRepairedInputs()
    {
        var previous = PreparedData.GetValue(null);
        var reload = typeof(GameConfig).GetProperty(nameof(GameConfig.ReloadConfigOnStart))!;
        var previousChecksum = GameConfig.GameDataChecksum;
        var previousReload = GameConfig.ReloadConfigOnStart;
        var path = ProjectSettings.GlobalizePath("user://config/gameplay/AbilityDefinitions.yml");
        var original = File.ReadAllText(path);
        var invalid = original.Replace("TargetingId: revivableAlly", "TargetingId: missing_targeting", StringComparison.Ordinal);
        Assert.That(invalid, Is.Not.EqualTo(original));
        try
        {
            reload.SetValue(null, false);
            PreparedData.SetValue(null, null);
            File.WriteAllText(path, invalid);
            var error = Assert.Throws<InvalidOperationException>(GameplayDefinitions.Initialize);
            Assert.That(error!.Message, Does.Contain("missing_targeting").And.Contain("TargetingId"));
            AssertUnavailable();
            Assert.That(PreparedData.GetValue(null), Is.Null);
            var repaired = original + "\n# repaired input\n";
            File.WriteAllText(path, repaired);
            AssertUnavailable();
            Assert.DoesNotThrow(GameplayDefinitions.Initialize);
            Assert.That(GameConfig.GameDataChecksum, Is.Not.Empty.And.Not.EqualTo(previousChecksum));
            Assert.That(GetInputs()["AbilityDefinitions.yml"], Is.EqualTo(repaired));
            var prepared = GameplayDefinitions.Current;
            var successfulChecksum = GameConfig.GameDataChecksum;
            File.WriteAllText(path, invalid);
            GameplayDefinitions.Initialize();
            GameConfig.EnsureCompatibilityData();
            Assert.That(GameplayDefinitions.Current, Is.SameAs(prepared));
            Assert.That(GameConfig.GameDataChecksum, Is.EqualTo(successfulChecksum));
            Assert.That(GetInputs()["AbilityDefinitions.yml"], Is.EqualTo(repaired));
        }
        finally
        {
            File.WriteAllText(path, original);
            reload.SetValue(null, previousReload);
            PreparedData.SetValue(null, previous);
        }
    }

    private static IReadOnlyDictionary<string, string> GetInputs()
    {
        var data = PreparedData.GetValue(null)!;
        return (IReadOnlyDictionary<string, string>)data.GetType().GetProperty("Inputs")!.GetValue(data)!;
    }

    private static void AssertUnavailable()
    {
        Assert.That(GameConfig.GameDataChecksum, Is.Empty);
        Assert.Throws<InvalidOperationException>(() => _ = GameplayDefinitions.Current);
        Assert.Throws<InvalidOperationException>(() => ItemRegistry.GetAll());
        Assert.Throws<InvalidOperationException>(() => ItemRegistry.GetAllCategories());
        Assert.Throws<InvalidOperationException>(() => ShipRegistry.GetAll());
        Assert.Throws<InvalidOperationException>(GameConfig.EnsureCompatibilityData);
    }
}
