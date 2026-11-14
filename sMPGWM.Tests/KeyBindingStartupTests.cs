using System.Diagnostics;

namespace sMPGWM.Tests;

[TestFixture]
public sealed class KeyBindingStartupTests
{
    [TestCase("valid", 0, "Successfully loaded config values.")]
    [TestCase("missing", 1, "missing required configuration key")]
    [TestCase("empty", 1, "[keybindings]")]
    [TestCase("missing_action", 1, "Forwards")]
    [TestCase("invalid_user", 1, "user://config/config.cfg")]
    public async Task ConfigurationIsValidatedBeforeStartup(string scenario, int expectedExitCode, string diagnostic)
    {
        var repository = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var game = Path.Combine(repository, "sMPGWM.Game");
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var staging = Path.Combine(Path.GetTempPath(), "sMPGWM-config-startup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            CopyTree(Path.Combine(game, ".godot/mono/temp/bin", configuration), Path.Combine(staging, ".godot/mono/temp/bin", configuration));
            foreach (var script in new[] { "Scripts/GameFlowSystem/GameFlowManager.cs", "Scripts/GameConfigSystem/ConfigLoader.cs", "Scripts/UtilitySystem/AbstractSingleton.cs" })
                Copy(Path.Combine(game, script), Path.Combine(staging, script));
            var config = File.ReadAllText(Path.Combine(game, "config/config.cfg")).Replace("reload_config_on_start = true", "reload_config_on_start = false", StringComparison.Ordinal);
            var section = config.IndexOf("[keybindings]", StringComparison.Ordinal);
            Assert.That(section, Is.GreaterThanOrEqualTo(0));
            if (scenario == "missing") config = config[..section];
            if (scenario == "empty") config = config[..section] + "[keybindings]\nbindings = {}\n";
            if (scenario == "missing_action") config = config.Replace("\"Forwards\"", "\"RemovedForwards\"", StringComparison.Ordinal);
            Directory.CreateDirectory(Path.Combine(staging, "config"));
            File.WriteAllText(Path.Combine(staging, "config/config.cfg"), config);
            File.WriteAllText(Path.Combine(staging, "project.godot"), string.Join("\n", new[]
            {
                "config_version=5",
                "[application]",
                "config/name=\"sMPGWM configuration startup tests\"",
                "run/main_scene=\"res://startup.tscn\"",
                "[autoload]",
                "GameFlowManager=\"*res://Scripts/GameFlowSystem/GameFlowManager.cs\"",
                "ConfigLoader=\"*res://Scripts/GameConfigSystem/ConfigLoader.cs\"",
                "[dotnet]",
                "project/assembly_name=\"sMPGWM\"",
                "[rendering]",
                "renderer/rendering_method=\"gl_compatibility\""
            }));
            File.WriteAllText(Path.Combine(staging, "startup.tscn"), "[gd_scene format=3]\n[node name=\"Startup\" type=\"Node\"]\n");
            if (scenario == "invalid_user")
            {
                var firstRun = await RunGodot(staging);
                Assert.That(firstRun.ExitCode, Is.Zero, firstRun.Output);
                var userFiles = Directory.GetFiles(Path.Combine(staging, "userdata"), "config.cfg", SearchOption.AllDirectories);
                Assert.That(userFiles, Has.Length.EqualTo(1));
                File.WriteAllText(userFiles[0], config[..section] + "[keybindings]\nbindings = {}\n");
            }
            var result = await RunGodot(staging);
            Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode), result.Output);
            Assert.That(result.Output, Does.Contain(diagnostic), result.Output);
            if (expectedExitCode != 0)
                Assert.That(result.Output, Does.Not.Contain("Successfully loaded config values."), result.Output);
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

    private static async Task<(int ExitCode, string Output)> RunGodot(string staging)
    {
        var configured = Environment.GetEnvironmentVariable("GODOT_BIN");
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var installed = Path.Combine(userProfile, "godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe");
        var executable = !string.IsNullOrWhiteSpace(configured) ? configured : File.Exists(installed) ? installed : "godot";
        var start = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            CreateNoWindow = true, WorkingDirectory = staging
        };
        foreach (var argument in new[] { "--headless", "--path", staging, "--quit-after", "1" })
            start.ArgumentList.Add(argument);
        start.Environment["APPDATA"] = Path.Combine(staging, "userdata");
        start.Environment["XDG_DATA_HOME"] = Path.Combine(staging, "userdata");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            await process.WaitForExitAsync();
            throw new AssertionException("Headless configuration startup timed out.");
        }
        return (process.ExitCode, await stdout + await stderr);
    }

    private static void CopyTree(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
    }

    private static void Copy(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination, true);
    }
}

