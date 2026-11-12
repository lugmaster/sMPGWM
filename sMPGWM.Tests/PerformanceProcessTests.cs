using System.Diagnostics; // TEST-SUPPORT: PERF-TEST-1 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
namespace sMPGWM.Tests; // TEST-SUPPORT: PERF-TEST-2 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
public sealed class PerformanceProcessTests // TEST-SUPPORT: PERF-TEST-3 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
{ // TEST-SUPPORT: PERF-TEST-4 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    [Test, Explicit("Runs the full local performance matrix for about one hour.")] // TEST-SUPPORT: PERF-TEST-5 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    public async Task LocalPerformanceMatrix() // TEST-SUPPORT: PERF-TEST-6 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    { // TEST-SUPPORT: PERF-TEST-7 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../..")); // TEST-SUPPORT: PERF-TEST-8 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        var start = new ProcessStartInfo("python") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true }; // TEST-SUPPORT: PERF-TEST-9 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        foreach (var argument in new[] { "tools/run_performance_matrix.py", "--output", Path.Combine(root, "docs/measurements/local-performance-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")) }) // TEST-SUPPORT: PERF-TEST-10 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            start.ArgumentList.Add(argument); // TEST-SUPPORT: PERF-TEST-11 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        using var process = Process.Start(start)!; // TEST-SUPPORT: PERF-TEST-12 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        using var timeout = new CancellationTokenSource(TimeSpan.FromHours(2)); // TEST-SUPPORT: PERF-TEST-13 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        try { await process.WaitForExitAsync(timeout.Token); } // TEST-SUPPORT: PERF-TEST-14 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        catch (OperationCanceledException) // TEST-SUPPORT: PERF-TEST-15 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        { // TEST-SUPPORT: PERF-TEST-16 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            process.Kill(true); // TEST-SUPPORT: PERF-TEST-17 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            await process.WaitForExitAsync(); // TEST-SUPPORT: PERF-TEST-18 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            throw new AssertionException("Local performance matrix timed out."); // TEST-SUPPORT: PERF-TEST-19 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        } // TEST-SUPPORT: PERF-TEST-20 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        Assert.That(process.ExitCode, Is.Zero); // TEST-SUPPORT: PERF-TEST-21 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    } // TEST-SUPPORT: PERF-TEST-22 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
} // TEST-SUPPORT: PERF-TEST-23 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

