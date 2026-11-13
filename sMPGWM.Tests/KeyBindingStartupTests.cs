using System.Diagnostics; // TEST-SUPPORT: KEYBIND-START-001 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup

namespace sMPGWM.Tests; // TEST-SUPPORT: KEYBIND-START-002 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup

[TestFixture] // TEST-SUPPORT: KEYBIND-START-003 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
public sealed class KeyBindingStartupTests // TEST-SUPPORT: KEYBIND-START-004 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
{
    [TestCase("valid", 0, "Successfully loaded config values.")] // TEST-SUPPORT: KEYBIND-START-005 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    [TestCase("missing", 1, "missing required configuration key")] // TEST-SUPPORT: KEYBIND-START-006 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    [TestCase("empty", 1, "[keybindings]")] // TEST-SUPPORT: KEYBIND-START-007 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    [TestCase("missing_action", 1, "Forwards")] // TEST-SUPPORT: KEYBIND-START-008 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    [TestCase("invalid_user", 1, "user://config/config.cfg")] // TEST-SUPPORT: KEYBIND-START-009 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    public async Task ConfigurationIsValidatedBeforeStartup(string scenario, int expectedExitCode, string diagnostic) // TEST-SUPPORT: KEYBIND-START-010 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    {
        var repository = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../..")); // TEST-SUPPORT: KEYBIND-START-011 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var game = Path.Combine(repository, "sMPGWM.Game"); // TEST-SUPPORT: KEYBIND-START-012 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name; // TEST-SUPPORT: KEYBIND-START-013 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var staging = Path.Combine(Path.GetTempPath(), "sMPGWM-config-startup-" + Guid.NewGuid().ToString("N")); // TEST-SUPPORT: KEYBIND-START-014 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        Directory.CreateDirectory(staging); // TEST-SUPPORT: KEYBIND-START-015 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        try // TEST-SUPPORT: KEYBIND-START-016 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        {
            CopyTree(Path.Combine(game, ".godot/mono/temp/bin", configuration), Path.Combine(staging, ".godot/mono/temp/bin", configuration)); // TEST-SUPPORT: KEYBIND-START-017 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            foreach (var script in new[] { "Scripts/GameFlowSystem/GameFlowManager.cs", "Scripts/GameConfigSystem/ConfigLoader.cs", "Scripts/UtilitySystem/AbstractSingleton.cs" }) // TEST-SUPPORT: KEYBIND-START-018 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                Copy(Path.Combine(game, script), Path.Combine(staging, script)); // TEST-SUPPORT: KEYBIND-START-019 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            var config = File.ReadAllText(Path.Combine(game, "config/config.cfg")).Replace("reload_config_on_start = true", "reload_config_on_start = false", StringComparison.Ordinal); // TEST-SUPPORT: KEYBIND-START-020 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            var section = config.IndexOf("[keybindings]", StringComparison.Ordinal); // TEST-SUPPORT: KEYBIND-START-021 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Assert.That(section, Is.GreaterThanOrEqualTo(0)); // TEST-SUPPORT: KEYBIND-START-022 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (scenario == "missing") config = config[..section]; // TEST-SUPPORT: KEYBIND-START-023 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (scenario == "empty") config = config[..section] + "[keybindings]\nbindings = {}\n"; // TEST-SUPPORT: KEYBIND-START-024 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (scenario == "missing_action") config = config.Replace("\"Forwards\"", "\"RemovedForwards\"", StringComparison.Ordinal); // TEST-SUPPORT: KEYBIND-START-025 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Directory.CreateDirectory(Path.Combine(staging, "config")); // TEST-SUPPORT: KEYBIND-START-026 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            File.WriteAllText(Path.Combine(staging, "config/config.cfg"), config); // TEST-SUPPORT: KEYBIND-START-027 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            File.WriteAllText(Path.Combine(staging, "project.godot"), string.Join("\n", new[] // TEST-SUPPORT: KEYBIND-START-028 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            {
                "config_version=5", // TEST-SUPPORT: KEYBIND-START-029 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "[application]", // TEST-SUPPORT: KEYBIND-START-030 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "config/name=\"sMPGWM configuration startup tests\"", // TEST-SUPPORT: KEYBIND-START-031 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "run/main_scene=\"res://startup.tscn\"", // TEST-SUPPORT: KEYBIND-START-032 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "[autoload]", // TEST-SUPPORT: KEYBIND-START-033 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "GameFlowManager=\"*res://Scripts/GameFlowSystem/GameFlowManager.cs\"", // TEST-SUPPORT: KEYBIND-START-034 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "ConfigLoader=\"*res://Scripts/GameConfigSystem/ConfigLoader.cs\"", // TEST-SUPPORT: KEYBIND-START-035 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "[dotnet]", // TEST-SUPPORT: KEYBIND-START-036 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "project/assembly_name=\"sMPGWM\"", // TEST-SUPPORT: KEYBIND-START-037 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "[rendering]", // TEST-SUPPORT: KEYBIND-START-038 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                "renderer/rendering_method=\"gl_compatibility\"" // TEST-SUPPORT: KEYBIND-START-039 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            }));
            File.WriteAllText(Path.Combine(staging, "startup.tscn"), "[gd_scene format=3]\n[node name=\"Startup\" type=\"Node\"]\n"); // TEST-SUPPORT: KEYBIND-START-040 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (scenario == "invalid_user") // TEST-SUPPORT: KEYBIND-START-041 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            {
                var firstRun = await RunGodot(staging); // TEST-SUPPORT: KEYBIND-START-042 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                Assert.That(firstRun.ExitCode, Is.Zero, firstRun.Output); // TEST-SUPPORT: KEYBIND-START-043 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                var userFiles = Directory.GetFiles(Path.Combine(staging, "userdata"), "config.cfg", SearchOption.AllDirectories); // TEST-SUPPORT: KEYBIND-START-044 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                Assert.That(userFiles, Has.Length.EqualTo(1)); // TEST-SUPPORT: KEYBIND-START-045 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                File.WriteAllText(userFiles[0], config[..section] + "[keybindings]\nbindings = {}\n"); // TEST-SUPPORT: KEYBIND-START-046 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            }
            var result = await RunGodot(staging); // TEST-SUPPORT: KEYBIND-START-047 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode), result.Output); // TEST-SUPPORT: KEYBIND-START-048 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Assert.That(result.Output, Does.Contain(diagnostic), result.Output); // TEST-SUPPORT: KEYBIND-START-049 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (expectedExitCode != 0) // TEST-SUPPORT: KEYBIND-START-050 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                Assert.That(result.Output, Does.Not.Contain("Successfully loaded config values."), result.Output); // TEST-SUPPORT: KEYBIND-START-051 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        }
        finally // TEST-SUPPORT: KEYBIND-START-052 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        {
            var resolved = Path.GetFullPath(staging); // TEST-SUPPORT: KEYBIND-START-053 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; // TEST-SUPPORT: KEYBIND-START-054 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)) // TEST-SUPPORT: KEYBIND-START-055 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
                throw new InvalidOperationException("Test cleanup target is outside the temporary directory."); // TEST-SUPPORT: KEYBIND-START-056 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Directory.Delete(resolved, true); // TEST-SUPPORT: KEYBIND-START-057 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        }
    }

    private static async Task<(int ExitCode, string Output)> RunGodot(string staging) // TEST-SUPPORT: KEYBIND-START-058 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    {
        var configured = Environment.GetEnvironmentVariable("GODOT_BIN"); // TEST-SUPPORT: KEYBIND-START-059 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // TEST-SUPPORT: KEYBIND-START-060 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var installed = Path.Combine(userProfile, "godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"); // TEST-SUPPORT: KEYBIND-START-061 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var executable = !string.IsNullOrWhiteSpace(configured) ? configured : File.Exists(installed) ? installed : "godot"; // TEST-SUPPORT: KEYBIND-START-062 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var start = new ProcessStartInfo(executable) // TEST-SUPPORT: KEYBIND-START-063 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, // TEST-SUPPORT: KEYBIND-START-064 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            CreateNoWindow = true, WorkingDirectory = staging // TEST-SUPPORT: KEYBIND-START-065 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        };
        foreach (var argument in new[] { "--headless", "--path", staging, "--quit-after", "1" }) // TEST-SUPPORT: KEYBIND-START-066 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            start.ArgumentList.Add(argument); // TEST-SUPPORT: KEYBIND-START-067 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        start.Environment["APPDATA"] = Path.Combine(staging, "userdata"); // TEST-SUPPORT: KEYBIND-START-068 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        start.Environment["XDG_DATA_HOME"] = Path.Combine(staging, "userdata"); // TEST-SUPPORT: KEYBIND-START-069 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        using var process = Process.Start(start)!; // TEST-SUPPORT: KEYBIND-START-070 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var stdout = process.StandardOutput.ReadToEndAsync(); // TEST-SUPPORT: KEYBIND-START-071 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        var stderr = process.StandardError.ReadToEndAsync(); // TEST-SUPPORT: KEYBIND-START-072 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)); // TEST-SUPPORT: KEYBIND-START-073 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        try // TEST-SUPPORT: KEYBIND-START-074 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        {
            await process.WaitForExitAsync(timeout.Token); // TEST-SUPPORT: KEYBIND-START-075 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        }
        catch (OperationCanceledException) // TEST-SUPPORT: KEYBIND-START-076 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        {
            process.Kill(true); // TEST-SUPPORT: KEYBIND-START-077 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            await process.WaitForExitAsync(); // TEST-SUPPORT: KEYBIND-START-078 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            throw new AssertionException("Headless configuration startup timed out."); // TEST-SUPPORT: KEYBIND-START-079 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        }
        return (process.ExitCode, await stdout + await stderr); // TEST-SUPPORT: KEYBIND-START-080 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    }

    private static void CopyTree(string source, string destination) // TEST-SUPPORT: KEYBIND-START-081 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) // TEST-SUPPORT: KEYBIND-START-082 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
            Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file))); // TEST-SUPPORT: KEYBIND-START-083 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    }

    private static void Copy(string source, string destination) // TEST-SUPPORT: KEYBIND-START-084 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!); // TEST-SUPPORT: KEYBIND-START-085 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
        File.Copy(source, destination, true); // TEST-SUPPORT: KEYBIND-START-086 | Tests: sMPGWM.Tests.KeyBindingStartupTests.ConfigurationIsValidatedBeforeStartup
    }
}

