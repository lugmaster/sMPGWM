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
            File.Copy(Path.Combine(game, ".godot/extension_list.cfg"), Path.Combine(stage, ".godot/extension_list.cfg"));
            var project = File.ReadAllText(Path.Combine(game, "project.godot"))
                .Replace("uid://bwppb4jr80bw0", "res://scenes/start_scene.tscn")
                .Replace("uid://dnk44tr6vs3ar", "res://Scripts/GameFlowSystem/GameFlowManager.cs")
                .Replace("uid://cwgdwq3g3khns", "res://Scripts/Steam/SteamworksHelper.cs");
            project = project.Replace("[autoload]", "[autoload]\nNet14Driver=\"*res://net14_driver.gd\"");
            File.WriteAllText(Path.Combine(stage, "project.godot"), project);
            File.Copy(Path.Combine(root, "sMPGWM.Tests/Network/net14_driver.gd"), Path.Combine(stage, "net14_driver.gd"));
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            socket.Close();
            var config = File.ReadAllText(Path.Combine(stage, "config/config.cfg"))
                .Replace("port = 55555", $"port = {port}").Replace("connection_timeout = 60000", "connection_timeout = 1000");
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config);
            EngineProcess Start(string name, string role)
            {
                var process = new EngineProcess(stage, name, role);
                processes.Add(process);
                return process;
            }
            using var host = Start("host", "--enet-host");
            await host.WaitFor("Listening session=");
            var session = Regex.Match(host.Output, @"Listening session=(\w+)").Groups[1].Value;
            Assert.That(session, Has.Length.EqualTo(32), host.Output);
            foreach (var mode in new[] { "malformed", "silent" })
            {
                using var rejected = Start("auth-" + mode, "--no-network");
                await rejected.WaitFor("Steam initialized successfully");
                var sentBefore = Regex.Matches(host.Output, "NET00 auth_send peer=").Count;
                var failedBefore = Regex.Matches(host.Output, "NET00 auth_failed peer=").Count;
                rejected.Command($"auth|{mode}|{port}");
                await host.WaitForCount("NET00 auth_send peer=", sentBefore + 1);
                var deadline = Stopwatch.StartNew();
                if (mode == "malformed") await host.WaitFor("NET00 auth_rejected peer=");
                else await host.WaitForCount("NET00 auth_failed peer=", failedBefore + 1);
                var disconnectDeadline = Stopwatch.StartNew();
                var evidence = Path.Combine(stage, "auth-" + mode + ".command.auth-failed");
                while (!File.Exists(evidence) && disconnectDeadline.Elapsed.TotalSeconds < 3)
                    await Task.Delay(25);
                Assert.That(File.Exists(evidence), Is.True, rejected.Output);
                Assert.That(deadline.Elapsed.TotalSeconds, Is.LessThan(mode == "silent" ? 15 : 5));
                if (mode == "silent") Assert.That(deadline.Elapsed.TotalSeconds, Is.GreaterThan(8));
                Assert.That(host.Output, Does.Not.Contain("DiagnosticHello correlation="));
                Assert.That(host.Output, Does.Not.Contain("NET00 auth_complete peer="));
                Assert.That(host.Output, Does.Not.Contain("Network session stopped"));
                Assert.That(rejected.Output, Does.Not.Contain("MessageExchangeSucceeded"));
            }
            using (var occupied = Start("occupied", "--enet-host"))
                await occupied.WaitFor("CreateServer:");
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var client = Start("client" + attempt, "--enet-client");
                await client.WaitFor("MessageExchangeSucceeded");
                Assert.That(client.Output, Does.Contain("session=" + session));
                Assert.That(client.Output, Does.Contain("payload=ENet diagnostic hello"));
                Assert.That(client.Output, Does.Contain("NET00 auth_complete peer=1 result=Ok"));
                Assert.That(client.Output, Does.Not.Contain("ERROR:"));
                client.Command("duplicate");
                await Task.Delay(150);
                Assert.That(Regex.Matches(client.Output, "Diagnostic result=MessageExchangeSucceeded").Count, Is.EqualTo(1));
                client.Command("delayed-leave");
                await client.WaitFor("Network session stopped");
                await client.WaitFor("returning to start menu.");
                await Task.Delay(150);
                client.Command("restart");
                await client.WaitForCount("Diagnostic result=MessageExchangeSucceeded", 2);
                await Task.Delay(1600);
                Assert.That(Regex.Matches(client.Output, "returning to start menu.").Count, Is.EqualTo(1));
                Assert.That(client.Output, Does.Contain("NET00 auth_complete peer=1 result=Ok"));
                Assert.That(client.Output, Does.Not.Contain("ERROR:"));
                client.Command("quit");
                await client.WaitFor("completed Steam shutdown.");
                await client.WaitForExit();
                Assert.That(client.Output, Does.Contain("NET00 auth_complete peer=1 result=Ok"));
                Assert.That(client.Output, Does.Not.Contain("ERROR:"));
            }
            Assert.That(Regex.Matches(host.Output, "Host sender=1 DiagnosticReply").Count, Is.EqualTo(4), host.Output);
            Assert.That(host.Output, Does.Not.Contain("ERROR:"));
            host.Command("quit");
            await host.WaitForExit();
            Assert.That(host.Output, Does.Not.Contain("ERROR:"));
            using (var timeout = Start("timeout", "--enet-client"))
                await timeout.WaitFor("ENet connection failed or disconnected.");
            using (var cancel = Start("cancel-client", "--enet-client"))
            {
                await cancel.WaitFor("AbstractSingleton._Ready called for NetworkManager.");
                cancel.Command("cancel");
                await cancel.WaitFor("NET14 deliberate cancellation");
                await cancel.WaitFor("returning to start menu.");
                Assert.That(cancel.Output, Does.Not.Contain("MessageExchangeSucceeded"));
            }
            using (var retry = Start("retry-host", "--enet-host"))
            {
                await retry.WaitFor("Listening session=");
                retry.Command("suppress");
                await Task.Delay(100);
                using (var invalidReplies = Start("invalid-replies", "--enet-client"))
                {
                    await invalidReplies.WaitFor("DiagnosticHello correlation=");
                    var hello = Regex.Match(invalidReplies.Output, @"Client sender=(\d+) DiagnosticHello correlation=(\w+)");
                    var sender = hello.Groups[1].Value;
                    var correlation = hello.Groups[2].Value;
                    var hostSession = Regex.Match(retry.Output, @"Listening session=(\w+)").Groups[1].Value;
                    foreach (var reply in new[]
                    {
                        $"reply|{sender}|2|{correlation}|ENet diagnostic hello|{hostSession}",
                        $"reply|{sender}|1|{Guid.NewGuid():N}|ENet diagnostic hello|{hostSession}",
                        $"reply|{sender}|1|{correlation}|{new string('x', 257)}|{hostSession}",
                        $"reply|{sender}|1|{correlation}|ENet diagnostic hello|invalid"
                    })
                    {
                        retry.Command(reply);
                        await Task.Delay(100);
                        Assert.That(invalidReplies.Output, Does.Not.Contain("MessageExchangeSucceeded"));
                    }
                    retry.Command($"reply|{sender}|1|{correlation}|ENet diagnostic hello|{hostSession}");
                    await invalidReplies.WaitFor("MessageExchangeSucceeded");
                    invalidReplies.Command("quit");
                    await invalidReplies.WaitForExit();
                }
                using (var diagnosticTimeout = Start("diagnostic-timeout", "--enet-client"))
                {
                    await diagnosticTimeout.WaitFor("Diagnostic reply timeout.");
                    await diagnosticTimeout.WaitFor("returning to start menu.");
                    diagnosticTimeout.Command("send-error");
                    await diagnosticTimeout.WaitFor("Diagnostic send failed:");
                }
                retry.Command("quit");
                await retry.WaitForExit();
            }
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("max_players = 10", "max_players = 1"));
            using (var alone = Start("host-only", "--enet-host"))
            {
                await alone.WaitFor("Listening session=");
                using (var refused = Start("refused", "--enet-client"))
                {
                    await refused.WaitFor("returning to start menu.");
                    Assert.That(refused.Output, Does.Contain("Server disconnected.").Or.Contain("Native diagnostic authentication failed or timed out."));
                    Assert.That(refused.Output, Does.Not.Contain("MessageExchangeSucceeded"));
                }
                Assert.That(alone.Output, Does.Not.Contain("DiagnosticReply correlation="));
                alone.Command("quit");
                await alone.WaitForExit();
                Assert.That(alone.Output, Does.Not.Contain("ERROR:"));
            }
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("max_players = 10", "max_players = 2"));
            using (var bounded = Start("bounded-host", "--enet-host"))
            {
                await bounded.WaitFor("Listening session=");
                bounded.Command("suppress");
                await Task.Delay(100);
                using var pending = Start("pending-slot", "--enet-client");
                await pending.WaitFor("DiagnosticHello correlation=");
                using var overflow = Start("overflow", "--enet-client");
                await overflow.WaitFor("ENet connection failed or disconnected.");
                Assert.That(overflow.Output, Does.Not.Contain("MessageExchangeSucceeded"));
                bounded.Command("quit");
                await bounded.WaitForExit();
            }
            foreach (var capacity in new[] { 0, 11 })
            {
                File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("max_players = 10", $"max_players = {capacity}"));
                using var invalidCapacity = Start("capacity" + capacity, "--enet-host");
                await invalidCapacity.WaitFor("requires 1-10 player capacity");
            }
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace($"port = {port}", "port = 0"));
            using (var invalidPort = Start("invalid-port", "--enet-host"))
                await invalidPort.WaitFor("Invalid LAN endpoint.");
            File.WriteAllText(Path.Combine(stage, "config/config.cfg"), config.Replace("connection_timeout = 1000", "connection_timeout = 10000"));
            using (var steam = Start("steam-lifecycle", "--no-network"))
            {
                await steam.WaitFor("Steam initialized successfully");
                steam.Command("steam-cancel");
                await steam.WaitFor("Canceled Steam lobby membership drained.");
                await steam.WaitFor("returning to start menu.");
                await Task.Delay(200);
                for (var round = 1; round <= 2; round++)
                {
                    steam.Command("steam-host");
                    await steam.WaitForCount("Network Host: Listening", round);
                    Assert.That(steam.Output, Does.Contain("NET00 server_id=1"));
                    Assert.That(steam.Output, Does.Contain("NET00 phase=host"));
                    Assert.That(steam.Output, Does.Contain("ready; gameplay inactive."));
                    steam.Command("leave");
                    await steam.WaitForCount("returning to start menu.", round + 1);
                    await Task.Delay(200);
                }
                steam.Command("quit");
                await steam.WaitForExit();
                Assert.That(steam.Output, Does.Not.Contain("ERROR:"));
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
            start.Environment["NET00_CAPABILITIES"] = "1";
            start.Environment["NET14_COMMAND_FILE"] = _commandFile;
            start.Environment["APPDATA"] = Path.Combine(stage, "users", name);
            start.Environment["XDG_DATA_HOME"] = Path.Combine(stage, "users", name);
            _process = new Process { StartInfo = start };
            _process.OutputDataReceived += (_, args) => { lock (_output) _output.AppendLine(args.Data); };
            _process.ErrorDataReceived += (_, args) => { lock (_output) _output.AppendLine(args.Data); };
            _output.AppendLine($"Command: {executable} --headless --path {stage} -- {role}");
            _output.AppendLine("Inputs: " + Regex.Match(File.ReadAllText(Path.Combine(stage, "config/config.cfg")), @"(?m)^port = .*$").Value +
                "; connection_timeout=1000 ms; diagnostic_timeout=10000 ms; server=127.0.0.1 (invalid-endpoint case uses invalid-address).");
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
