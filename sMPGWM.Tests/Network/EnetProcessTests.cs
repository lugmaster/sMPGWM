using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace sMPGWM.Tests.Network;

public class EnetProcessTests
{
    [Test]
    public async Task ProductionScene_ExchangeRejoinOccupiedPortAndTimeout()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var stage = Path.Combine(Path.GetTempPath(), "sMPGWM-enet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        var processes = new List<EngineProcess>();
        try
        {
            var game = Path.Combine(root, "sMPGWM.Game");
            var configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
            foreach (var directory in new[] { "addons", "Scripts", "scenes", "assets", "config", ".godot/imported", $".godot/mono/temp/bin/{configuration}" })
                CopyTree(Path.Combine(game, directory), Path.Combine(stage, directory));
            File.Copy(Path.Combine(game, ".godot/uid_cache.bin"), Path.Combine(stage, ".godot/uid_cache.bin"));
            // ENet lifecycle tests do not need Steam or its native extension.
            File.WriteAllText(Path.Combine(stage, ".godot/extension_list.cfg"), "");
            var project = File.ReadAllText(Path.Combine(game, "project.godot"))
                .Replace("uid://bwppb4jr80bw0", "res://scenes/start_scene.tscn")
                .Replace("uid://dnk44tr6vs3ar", "res://Scripts/GameFlowSystem/GameFlowManager.cs")
                .Replace("uid://cwgdwq3g3khns", "res://Scripts/Steam/SteamworksHelper.cs");
            project = Regex.Replace(project, @"(?m)^SteamworksHelper=.*\r?\n", "");
            project = project.Replace("[autoload]", "[autoload]\nNet14Driver=\"*res://net14_driver.gd\"");
            File.WriteAllText(Path.Combine(stage, "project.godot"), project);
            File.Copy(Path.Combine(root, "sMPGWM.Tests/Network/net14_driver.gd"), Path.Combine(stage, "net14_driver.gd"));
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            socket.Close();
            var config = File.ReadAllText(Path.Combine(stage, "config/config.cfg"))
                .Replace("port = 55555", $"port = {port}").Replace("connection_timeout = 60000", "connection_timeout = 3000");
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config);
            EngineProcess Start(string name, string role)
            {
                var process = new EngineProcess(stage, name, role);
                processes.Add(process);
                return process;
            }
            using (var host = Start("host", "--enet-host"))
            {
                await host.WaitFor("Game flow: InGame");
                host.Command("check-ingame");
                await host.WaitFor("NET InGame enables gameplay");
                foreach (var mode in new[] { "malformed", "silent" })
                {
                    using var rejected = Start("auth-" + mode, "--no-network");
                    await rejected.WaitFor("NET driver ready");
                    var failures = Regex.Matches(host.Output, "NET00 auth_failed peer=").Count;
                    rejected.Command($"auth|{mode}|{port}");
                    if (mode == "malformed") await host.WaitFor("NET00 auth_rejected peer=");
                    else await host.WaitForCount("NET00 auth_failed peer=", failures + 1);
                    Assert.That(host.Output, Does.Not.Contain("Game flow: Disconnecting"));
                }
                using (var occupied = Start("occupied", "--enet-host"))
                {
                    await occupied.WaitFor("ENet startup failed:");
                    await occupied.WaitFor("Game flow: StartMenu");
                }
                using var client = Start("client", "--enet-client");
                await client.WaitFor("Game flow: InGame");
                client.Command("check-ingame");
                await client.WaitFor("NET InGame enables gameplay");
                Assert.That(client.Output.IndexOf("host round trip complete", StringComparison.Ordinal),
                    Is.LessThan(client.Output.IndexOf("Game flow: InGame", StringComparison.Ordinal)));
                client.Command("duplicate");
                await Task.Delay(100);
                Assert.That(Regex.Matches(client.Output, "Game flow: InGame").Count, Is.EqualTo(1));
                client.Command("delayed-leave");
                await client.WaitFor("NET Disconnecting disables gameplay");
                await client.WaitFor("Game flow: StartMenu");
                client.Command("restart");
                await client.WaitForCount("Game flow: InGame", 2);
                await Task.Delay(1600);
                Assert.That(Regex.Matches(client.Output, "Game flow: StartMenu").Count, Is.EqualTo(1));
                host.Command("leave");
                await client.WaitForCount("Game flow: Disconnecting", 2);
                await client.WaitForCount("Game flow: StartMenu", 2);
                Assert.That(client.Output, Does.Not.Contain("SCRIPT ERROR:"));
            }
            using (var absent = Start("absent", "--enet-client"))
            {
                await absent.WaitFor("Game flow: Loading");
                absent.Command("check-loading");
                await absent.WaitFor("NET loading keeps gameplay inactive");
                await absent.WaitFor("Game flow: StartMenu");
                Assert.That(absent.Output, Does.Not.Contain("Game flow: InGame"));
            }
            using (var fakeHost = Start("delayed-host", "--no-network"))
            {
                await fakeHost.WaitFor("NET driver ready");
                fakeHost.Command($"fake-host|{port}");
                await fakeHost.WaitFor("NET fake host ready");
                using (var client = Start("delayed-client", "--enet-client"))
                {
                    await fakeHost.WaitFor("NET hello received");
                    client.Command("check-loading");
                    await client.WaitFor("NET loading keeps gameplay inactive");
                    fakeHost.Command("wrong-reply");
                    await Task.Delay(100);
                    Assert.That(client.Output, Does.Not.Contain("Game flow: InGame"));
                    fakeHost.Command("reply");
                    await client.WaitFor("Game flow: InGame");
                    fakeHost.Command("reply");
                    await Task.Delay(100);
                    Assert.That(Regex.Matches(client.Output, "Game flow: InGame").Count, Is.EqualTo(1));
                    client.Command("leave");
                    await client.WaitFor("Game flow: StartMenu");
                }
                using (var timeout = Start("reply-timeout", "--enet-client"))
                {
                    await timeout.WaitFor("Host reply timed out.");
                    await timeout.WaitFor("Game flow: StartMenu");
                    Assert.That(timeout.Output, Does.Not.Contain("Game flow: InGame"));
                }
                using (var canceled = Start("canceled", "--enet-client"))
                {
                    await canceled.WaitFor("waiting for host reply");
                    canceled.Command("leave");
                    await canceled.WaitFor("Game flow: StartMenu");
                    canceled.Command("late-ready");
                    await canceled.WaitFor("NET late completion ignored");
                    Assert.That(canceled.Output, Does.Not.Contain("Game flow: InGame"));
                }
            }
            foreach (var capacity in new[] { 0, 11 })
            {
                File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("max_players = 10", $"max_players = {capacity}"));
                using var invalid = Start("capacity" + capacity, "--enet-host");
                await invalid.WaitFor("requires 1-10 player capacity");
            }
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("127.0.0.1", "invalid-address"));
            using (var invalid = Start("invalid-endpoint", "--enet-client"))
                await invalid.WaitFor("Invalid LAN endpoint.");
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
                var log = Path.Combine(TestContext.CurrentContext.WorkDirectory, "enet-" + process.Name + ".log");
                File.WriteAllText(log, process.Output);
                TestContext.AddTestAttachment(log);
            }
            var resolved = Path.GetFullPath(stage);
            if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsafe staging cleanup path.");
            Directory.Delete(resolved, true);
        }
    }

    private static void CopyTree(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private sealed class EngineProcess : IDisposable
    {
        private readonly Process _process;
        private readonly string _commandFile;
        private readonly StringBuilder _output = new();
        public string Name { get; }
        public string Output { get { lock (_output) return _output.ToString(); } }

        public EngineProcess(string stage, string name, string role)
        {
            Name = name;
            _commandFile = Path.Combine(stage, name + ".command");
            var executable = Environment.GetEnvironmentVariable("GODOT_BIN") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe");
            var start = new ProcessStartInfo(executable)
            {
                WorkingDirectory = stage, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var arg in new[] { "--headless", "--path", stage, "--", role }) start.ArgumentList.Add(arg);
            start.Environment["NET14_COMMAND_FILE"] = _commandFile;
            start.Environment["APPDATA"] = Path.Combine(stage, "users", name);
            start.Environment["XDG_DATA_HOME"] = Path.Combine(stage, "users", name);
            _process = new Process { StartInfo = start };
            _process.OutputDataReceived += (_, args) => { lock (_output) _output.AppendLine(args.Data); };
            _process.ErrorDataReceived += (_, args) => { lock (_output) _output.AppendLine(args.Data); };
            _output.AppendLine($"Command: {executable} --headless --path {stage} -- {role}");
            _output.AppendLine("Inputs: " + Regex.Match(File.ReadAllText(Path.Combine(stage, "config/config.cfg")), @"(?m)^port = .*$").Value +
                "; connection_timeout=3000 ms; server=127.0.0.1 (invalid-endpoint case uses invalid-address).");
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Command(string command) => File.WriteAllText(_commandFile, command);

        public async Task WaitForExit()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _process.WaitForExitAsync(timeout.Token);
            Assert.That(_process.ExitCode, Is.Zero, Output);
        }

        public Task WaitFor(string marker) => WaitForCount(marker, 1);

        public async Task WaitForCount(string marker, int count)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(20))
            {
                if (Regex.Matches(Output, Regex.Escape(marker)).Count >= count) return;
                if (_process.HasExited) break;
                await Task.Delay(25);
            }
            Assert.Fail($"Missing {marker} in {Name}:\n{Output}");
        }

        public void Dispose()
        {
            if (!_process.HasExited) _process.Kill(true);
            _process.WaitForExit();
        }
    }
}
