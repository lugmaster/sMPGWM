using System.Diagnostics;
using System.Xml.Linq;

namespace sMPGWM.Tests;

[TestFixture]
public class GodotIntegrationTests
{
    [Test]
    public async Task RegistryInventoryEquipmentHotbarAndPresentation_RunInsideGodot()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var game = Path.Combine(root, "sMPGWM.Game");
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var staging = Path.Combine(Path.GetTempPath(), "sMPGWM-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            // Copy only what these integration tests use. The actual game configuration and user data stay untouched.
            CopyTree(Path.Combine(game, ".godot/mono/temp/bin", configuration), Path.Combine(staging, ".godot/mono/temp/bin", configuration));
            CopyTree(Path.Combine(game, "assets/game_data"), Path.Combine(staging, "assets/game_data"));
            CopyTree(Path.Combine(game, "assets/ui/icons/atlases"), Path.Combine(staging, "assets/ui/icons/atlases"));
            foreach (var source in Directory.GetFiles(Path.Combine(game, ".godot/imported"), "icons_0.png-*"))
                Copy(source, Path.Combine(staging, ".godot/imported", Path.GetFileName(source)));
            foreach (var file in new[] { "Scripts/Util/AbstractSingleton.cs", "Scripts/Util/Mocks/HeadlessTestHost.cs", "Scripts/Util/UiComponents/IconDefinitions.cs", "Scripts/Network/MpManager.cs", "Scripts/Network/MpController.cs", "Scripts/InputSystem/InputController.cs" })
                Copy(Path.Combine(game, file), Path.Combine(staging, file));
            CopyTree(Path.Combine(game, "Scripts/ShipSystem"), Path.Combine(staging, "Scripts/ShipSystem"));
            foreach (var file in new[] { "Scripts/GamePlaySystem/PlayerController.cs", "Scripts/UiSystem/UiCanvas.cs",
                         "Scripts/UiSystem/HudSystem/PlayerHud.cs", "Scripts/UiSystem/HudSystem/Hotbar/HotbarControl.cs",
                         "Scripts/UiSystem/OverlayMenuSystem/UiWindowManager.cs" })
                Copy(Path.Combine(game, file), Path.Combine(staging, file));
            Copy(Path.Combine(game, "scenes/game/ship.tscn"), Path.Combine(staging, "scenes/game/ship.tscn"));
            // Session construction tests exercise the real canvas and runtime node composition.
            foreach (var directory in new[] { "Scripts/UiSystem", "Scripts/Util/UiComponents", "Scripts/Network", "Scripts/GameConfigSystem", "scenes/ui" })
                CopyTree(Path.Combine(game, directory), Path.Combine(staging, directory));
            Copy(Path.Combine(game, "config/config.cfg"), Path.Combine(staging, "config/config.cfg"));
            Copy(Path.Combine(game, "scenes/game/ui_canvas.tscn"), Path.Combine(staging, "scenes/game/ui_canvas.tscn"));
            CopyTree(Path.Combine(game, "assets/mocks"), Path.Combine(staging, "assets/mocks"));
            foreach (var source in Directory.GetFiles(Path.Combine(game, ".godot/imported"), "radar_mock.png-*"))
                Copy(source, Path.Combine(staging, ".godot/imported", Path.GetFileName(source)));
            foreach (var source in Directory.GetFiles(Path.Combine(game, ".godot/imported"), "ship_fighter_reference.png-*"))
                Copy(source, Path.Combine(staging, ".godot/imported", Path.GetFileName(source)));
            File.WriteAllText(Path.Combine(staging, "project.godot"), """
                config_version=5
                [application]
                config/name="sMPGWM integration tests"
                run/main_scene="res://tests.tscn"
                [autoload]
                ConfigLoader="*res://Scripts/GameConfigSystem/ConfigLoader.cs"
                IconDefinitions="*res://Scripts/Util/UiComponents/IconDefinitions.cs"
                [dotnet]
                project/assembly_name="sMPGWM"
                [rendering]
                renderer/rendering_method="gl_compatibility"
                """);
            File.WriteAllText(Path.Combine(staging, "tests.tscn"), """
                [gd_scene load_steps=2 format=3]
                [ext_resource type="Script" path="res://Scripts/Util/Mocks/HeadlessTestHost.cs" id="1"]
                [node name="Tests" type="Node"]
                script = ExtResource("1")
                """);
            var report = Path.Combine(staging, "results.xml");
            var start = new ProcessStartInfo(FindGodot())
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                CreateNoWindow = true, WorkingDirectory = staging
            };
            foreach (var argument in new[] { "--headless", "--path", staging, "--", typeof(GodotTestRunner).Assembly.Location, report })
                start.ArgumentList.Add(argument);
            start.Environment["APPDATA"] = Path.Combine(staging, "userdata");
            start.Environment["XDG_DATA_HOME"] = Path.Combine(staging, "userdata");
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                await process.WaitForExitAsync();
                throw new AssertionException("Headless Godot timed out.");
            }
            var output = await stdout + await stderr;
            Assert.That(File.Exists(report), Is.True, output);
            var results = XDocument.Load(report);
            var destination = Path.Combine(TestContext.CurrentContext.WorkDirectory, "godot-results.xml");
            File.Copy(report, destination, true);
            TestContext.AddTestAttachment(destination, "Individual headless Godot test results");
            var failures = results.Descendants("test-case").Where(n => (string?)n.Attribute("result") != "Passed");
            Assert.That(process.ExitCode, Is.Zero, string.Join("\n", failures.Select(n => n.ToString())) + "\n" + output);
            TestContext.Progress.WriteLine($"Godot: {results.Root!.Attribute("passed")} passed; {results.Root.Attribute("skipped")} skipped.");
        }
        finally
        {
            var resolved = Path.GetFullPath(staging);
            var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup target is outside the temporary directory.");
            Directory.Delete(resolved, true);
        }
    }

    private static string FindGodot()
    {
        var configured = Environment.GetEnvironmentVariable("GODOT_BIN");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var local = Path.Combine(home, "godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe");
        return File.Exists(local) ? local : "godot";
    }

    private static void Copy(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, true);
    }

    private static void CopyTree(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
    }
}
