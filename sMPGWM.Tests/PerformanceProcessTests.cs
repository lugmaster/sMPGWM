using System.Diagnostics;
namespace sMPGWM.Tests;
public sealed class PerformanceProcessTests
{
    [Test, Explicit("Runs the full local performance matrix for about one hour.")]
    public async Task LocalPerformanceMatrix()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        var start = new ProcessStartInfo("python") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "tools/run_performance_matrix.py", "--output", Path.Combine(root, "docs/measurements/local-performance-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")) })
            start.ArgumentList.Add(argument);
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

