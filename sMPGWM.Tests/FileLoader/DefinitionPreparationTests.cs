using System.Reflection;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using File = System.IO.File;

namespace sMPGWM.Tests.FileLoader;

[Category("Godot")]
[NonParallelizable]
public class DefinitionPreparationTests
{
    [Test]
    public void FailedPreparationClearsPartialCachesAndRetriesRepairedInputs()
    {
        GameConfig.EnsureCompatibilityData();
        var checksum = typeof(GameConfig).GetProperty(nameof(GameConfig.GameDataChecksum))!;
        var reload = typeof(GameConfig).GetProperty(nameof(GameConfig.ReloadConfigOnStart))!;
        var previousChecksum = GameConfig.GameDataChecksum;
        var previousReload = GameConfig.ReloadConfigOnStart;
        var captured = new Dictionary<string, string>(sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs);
        var registries = new[] { typeof(ItemRegistry), typeof(ShipRegistry), typeof(GameplayDefinitions) };
        var fields = new FieldInfo[registries.Length];
        var previousData = new object[registries.Length];
        for (var i = 0; i < registries.Length; i++)
        {
            fields[i] = registries[i].GetField("Data", BindingFlags.NonPublic | BindingFlags.Static)!;
            previousData[i] = fields[i].GetValue(null)!;
        }
        var path = ProjectSettings.GlobalizePath("user://config/gameplay/AbilityDefinitions.yml");
        var original = File.ReadAllText(path);
        var invalid = original.Replace("TargetingId: revivableAlly", "TargetingId: missing_targeting", StringComparison.Ordinal);
        Assert.That(invalid, Is.Not.EqualTo(original));
        try
        {
            reload.SetValue(null, false);
            checksum.SetValue(null, string.Empty);
            ItemRegistry.ResetPreparation();
            ShipRegistry.ResetPreparation();
            GameplayDefinitions.ResetPreparation();
            sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs.Clear();
            File.WriteAllText(path, invalid);
            var error = Assert.Throws<InvalidOperationException>(GameConfig.EnsureCompatibilityData);
            Assert.That(error!.Message, Does.Contain("missing_targeting").And.Contain("TargetingId"));
            Assert.That(GameConfig.GameDataChecksum, Is.Empty);
            Assert.That(sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs, Is.Empty);
            foreach (var field in fields)
            {
                var cache = field.GetValue(null)!;
                Assert.That(cache.GetType().GetProperty("IsValueCreated")!.GetValue(cache), Is.False);
            }
            var repaired = original + "\n# repaired input\n";
            File.WriteAllText(path, repaired);
            Assert.DoesNotThrow(GameConfig.EnsureCompatibilityData);
            Assert.That(GameConfig.GameDataChecksum, Is.Not.Empty.And.Not.EqualTo(previousChecksum));
            Assert.That(sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs["AbilityDefinitions.yml"], Is.EqualTo(repaired));
            var prepared = GameplayDefinitions.Current;
            var successfulChecksum = GameConfig.GameDataChecksum;
            GameConfig.EnsureCompatibilityData();
            Assert.That(GameplayDefinitions.Current, Is.SameAs(prepared));
            Assert.That(GameConfig.GameDataChecksum, Is.EqualTo(successfulChecksum));
        }
        finally
        {
            File.WriteAllText(path, original);
            reload.SetValue(null, previousReload);
            checksum.SetValue(null, previousChecksum);
            for (var i = 0; i < fields.Length; i++) fields[i].SetValue(null, previousData[i]);
            sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs.Clear();
            foreach (var pair in captured) sMPGWM.Scripts.FileLoadingSystem.FileLoader.CapturedInputs.Add(pair.Key, pair.Value);
        }
    }
}
