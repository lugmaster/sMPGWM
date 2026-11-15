using System.Diagnostics;
namespace sMPGWM.Tests;
public sealed class PerformanceProcessTests
{
    [Test, Explicit("Runs the full local performance matrix for about one hour.")]
    public Task LocalPerformanceMatrix() => RunMatrix("local-performance", []);

    [Test, Explicit("Runs 5/10-player shield-impact combat with nine weapons at 60 shots/sec configured cadence; about 22 minutes.")]
    public Task CombatPerformanceMatrix() => RunMatrix("combat-performance",
        ["--scenario", "combat", "--weapons", "9", "--weapon-rate", "60", "--seconds", "210", "--repeats", "3"]);

    private static async Task RunMatrix(string label, string[] arguments)
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var start = new ProcessStartInfo("python") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "tools/run_performance_matrix.py", "--output", Path.Combine(root, "docs/measurements/" + label + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")) })
            start.ArgumentList.Add(argument);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromHours(2));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            await process.WaitForExitAsync();
            throw new AssertionException("Local performance matrix timed out.");
        }
        Assert.That(process.ExitCode, Is.Zero);
    }
}

