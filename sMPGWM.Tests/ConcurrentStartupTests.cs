using System.Diagnostics;

namespace sMPGWM.Tests;

public sealed class ConcurrentStartupTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task ThreeInstancesShareUserFilesDuringFreshAndRepeatedStartup(bool reload)
    {
        var repository = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var game = Path.Combine(repository, "sMPGWM.Game");
        var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var staging = Path.Combine(Path.GetTempPath(), "sMPGWM-concurrent-startup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var directory in new[] { ".godot/mono/temp/bin/" + configuration, "Scripts", "assets/game_data",
                         "assets/ui/cursors", "assets/ui/icons/atlases", "assets/mocks", ".godot/imported" })
                CopyTree(Path.Combine(game, directory), Path.Combine(staging, directory));
            Directory.CreateDirectory(Path.Combine(staging, "config"));
            var config = File.ReadAllText(Path.Combine(game, "config/config.cfg"));
            config = System.Text.RegularExpressions.Regex.Replace(config, @"reload_config_on_start\s*=\s*(true|false)",
                "reload_config_on_start = " + (reload ? "true" : "false"));
            File.WriteAllText(Path.Combine(staging, "config/config.cfg"), config);
            File.WriteAllText(Path.Combine(staging, "project.godot"), """
                config_version=5
                [application]
                config/name="sMPGWM concurrent startup tests"
                run/main_scene="res://startup.tscn"
                [autoload]
                StartupGate="*res://startup_gate.gd"
                GameFlowManager="*res://Scripts/GameFlowSystem/GameFlowManager.cs"
                ConfigLoader="*res://Scripts/GameConfigSystem/ConfigLoader.cs"
                IconDefinitions="*res://Scripts/UtilitySystem/UiComponents/IconDefinitions.cs"
                DefinitionStartup="*res://Scripts/GameplayDataSystem/DefinitionStartup.cs"
                [dotnet]
                project/assembly_name="sMPGWM"
                [rendering]
                renderer/rendering_method="gl_compatibility"
                """);
            File.WriteAllText(Path.Combine(staging, "startup.tscn"), "[gd_scene format=3]\n[node name=\"Startup\" type=\"Node\"]\n");
            File.WriteAllText(Path.Combine(staging, "startup_gate.gd"), """
                extends Node
                func _enter_tree() -> void:
                    var gate := "res://gates/" + OS.get_cmdline_user_args()[0]
                    var ready := FileAccess.open(gate + "/" + str(OS.get_process_id()), FileAccess.WRITE)
                    ready.close()
                    var deadline := Time.get_ticks_msec() + 30000
                    while not FileAccess.file_exists(gate + "/go"):
                        if Time.get_ticks_msec() >= deadline:
                            get_tree().quit(1)
                            return
                        OS.delay_msec(5)
                """);

            // The first round creates missing files; subsequent rounds reuse the same user directory.
            for (var round = 0; round < 3; round++)
                await StartThree(staging, round);

            var userFiles = Directory.GetFiles(Path.Combine(staging, "userdata"), "*.yml", SearchOption.AllDirectories);
            Assert.That(userFiles, Has.Length.EqualTo(22));
            foreach (var file in userFiles)
            {
                var relative = Path.GetRelativePath(Path.Combine(staging, "userdata"), file);
                var source = relative.Contains(Path.DirectorySeparatorChar + "items" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    ? Path.Combine(game, "assets/game_data/items", Path.GetFileName(file))
                    : Path.Combine(game, "assets/game_data", Path.GetFileName(file));
                Assert.That(File.ReadAllText(file), Is.EqualTo(File.ReadAllText(source)), relative);
            }
            var userConfigs = Directory.GetFiles(Path.Combine(staging, "userdata"), "config.cfg", SearchOption.AllDirectories);
            Assert.That(userConfigs, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(userConfigs[0]), Is.EqualTo(config));
            Assert.That(Directory.GetFiles(Path.Combine(staging, "userdata"), "*.tmp", SearchOption.AllDirectories), Is.Empty);
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

    private static async Task StartThree(string staging, int round)
    {
        var gate = Path.Combine(staging, "gates", round.ToString());
        Directory.CreateDirectory(gate);
        var configured = Environment.GetEnvironmentVariable("GODOT_BIN");
        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe");
        var executable = !string.IsNullOrWhiteSpace(configured) ? configured : File.Exists(installed) ? installed : "godot";
        var processes = new Process?[3];
        var outputs = new Task<string>[3];
        var errors = new Task<string>[3];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            for (var index = 0; index < processes.Length; index++)
            {
                var start = new ProcessStartInfo(executable)
                {
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                    CreateNoWindow = true, WorkingDirectory = staging
                };
                foreach (var argument in new[] { "--headless", "--path", staging, "--quit-after", "1", "--", round.ToString() })
                    start.ArgumentList.Add(argument);
                start.Environment["APPDATA"] = Path.Combine(staging, "userdata");
                start.Environment["XDG_DATA_HOME"] = Path.Combine(staging, "userdata");
                var process = Process.Start(start)!;
                processes[index] = process;
                outputs[index] = process.StandardOutput.ReadToEndAsync();
                errors[index] = process.StandardError.ReadToEndAsync();
            }
            while (Directory.GetFiles(gate).Length < processes.Length)
                await Task.Delay(10, timeout.Token);
            File.WriteAllText(Path.Combine(gate, "go"), "");
            for (var index = 0; index < processes.Length; index++)
            {
                await processes[index]!.WaitForExitAsync(timeout.Token);
                var output = await outputs[index] + await errors[index];
                Assert.That(processes[index]!.ExitCode, Is.Zero, $"Round {round}, instance {index}:\n{output}");
                Assert.That(output, Does.Contain("Compatibility development="), output);
                Assert.That(output, Does.Not.Contain("Failed to prepare definitions"), output);
            }
        }
        finally
        {
            foreach (var process in processes)
            {
                if (process == null) continue;
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
                process.Dispose();
            }
        }
    }

    private static void CopyTree(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
