using System.Diagnostics;

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-PROCESS-TEST-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge

public sealed class AbilityGameplayProcessTests // TEST-SUPPORT: ABILITY-PROCESS-TEST-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
{
    [Test, Explicit("Runs ten headless Godot processes with impaired ENet transport.")] // TEST-SUPPORT: ABILITY-PROCESS-TEST-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    public async Task TenPlayersWithTwoAuthoredBehavioursConverge() // TEST-SUPPORT: ABILITY-PROCESS-TEST-004 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../..")); // TEST-SUPPORT: ABILITY-PROCESS-TEST-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        var start = new ProcessStartInfo("python") // TEST-SUPPORT: ABILITY-PROCESS-TEST-006 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, // TEST-SUPPORT: ABILITY-PROCESS-TEST-007 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            RedirectStandardOutput = true, RedirectStandardError = true // TEST-SUPPORT: ABILITY-PROCESS-TEST-008 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        };
        foreach (var argument in new[] { "tools/run_shield_slice.py", "--players", "10", "--impaired", "--variants" }) // TEST-SUPPORT: ABILITY-PROCESS-TEST-009 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            start.ArgumentList.Add(argument); // TEST-SUPPORT: ABILITY-PROCESS-TEST-010 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        using var process = Process.Start(start)!; // TEST-SUPPORT: ABILITY-PROCESS-TEST-011 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        var output = process.StandardOutput.ReadToEndAsync(); // TEST-SUPPORT: ABILITY-PROCESS-TEST-012 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        var errors = process.StandardError.ReadToEndAsync(); // TEST-SUPPORT: ABILITY-PROCESS-TEST-013 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4)); // TEST-SUPPORT: ABILITY-PROCESS-TEST-014 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        try { await process.WaitForExitAsync(timeout.Token); } // TEST-SUPPORT: ABILITY-PROCESS-TEST-015 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        catch (OperationCanceledException) // TEST-SUPPORT: ABILITY-PROCESS-TEST-016 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        {
            process.Kill(true); // TEST-SUPPORT: ABILITY-PROCESS-TEST-017 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            await process.WaitForExitAsync(); // TEST-SUPPORT: ABILITY-PROCESS-TEST-018 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            throw new AssertionException("Ability network verification timed out."); // TEST-SUPPORT: ABILITY-PROCESS-TEST-019 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        }
        Assert.That(process.ExitCode, Is.Zero, await output + await errors); // TEST-SUPPORT: ABILITY-PROCESS-TEST-020 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    }
}
