using System.Diagnostics;

namespace sMPGWM.Tests;

public sealed class AbilityGameplayProcessTests
{
    [Test, Explicit("Runs ten headless Godot processes with impaired ENet transport.")]
    public async Task TenPlayersWithTwoAuthoredBehavioursConverge()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var start = new ProcessStartInfo("python")
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "tools/run_shield_slice.py", "--players", "10", "--impaired", "--variants" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            await process.WaitForExitAsync();
            throw new AssertionException("Ability network verification timed out.");
        }
        Assert.That(process.ExitCode, Is.Zero, await output + await errors);
    }
}
