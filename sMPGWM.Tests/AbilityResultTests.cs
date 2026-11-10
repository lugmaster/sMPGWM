using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: ABILITY-RESULT-001 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: ABILITY-RESULT-002 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
using sMPGWM.Scripts.UiSystem; // TEST-SUPPORT: ABILITY-RESULT-003 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-RESULT-004 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes

public sealed class AbilityResultTests // TEST-SUPPORT: ABILITY-RESULT-005 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
{
    [Test] // TEST-SUPPORT: ABILITY-RESULT-006 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
    public void HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes() // TEST-SUPPORT: ABILITY-RESULT-007 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
    {
        var history = new ActivationHistory(); // TEST-SUPPORT: ABILITY-RESULT-008 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(1, 0, out var result), Is.True); // TEST-SUPPORT: ABILITY-RESULT-009 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Pending)); // TEST-SUPPORT: ABILITY-RESULT-010 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(1, 1, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-011 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Pending)); // TEST-SUPPORT: ABILITY-RESULT-012 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        history.Complete(1, AbilityActivationResult.AbilitySourceChanged); // TEST-SUPPORT: ABILITY-RESULT-013 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(1, 2, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-014 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.AbilitySourceChanged)); // TEST-SUPPORT: ABILITY-RESULT-015 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(2, 5, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-016 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.RateLimit)); // TEST-SUPPORT: ABILITY-RESULT-017 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(2, 6, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-018 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.RateLimit)); // TEST-SUPPORT: ABILITY-RESULT-019 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(33, 6, out result), Is.True); // TEST-SUPPORT: ABILITY-RESULT-020 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        history.Complete(33, AbilityActivationResult.Busy); // TEST-SUPPORT: ABILITY-RESULT-021 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        history.Complete(1, AbilityActivationResult.Accepted); // TEST-SUPPORT: ABILITY-RESULT-022 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(33, 12, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-023 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Busy)); // TEST-SUPPORT: ABILITY-RESULT-024 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(1, 12, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-025 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.OldRequest)); // TEST-SUPPORT: ABILITY-RESULT-026 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(history.Admit(0, 12, out result), Is.False); // TEST-SUPPORT: ABILITY-RESULT-027 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(result, Is.EqualTo(AbilityActivationResult.OldRequest)); // TEST-SUPPORT: ABILITY-RESULT-028 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        foreach (var outcome in Enum.GetValues<AbilityActivationResult>()) // TEST-SUPPORT: ABILITY-RESULT-029 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
            Assert.That(AbilityActivationResultText.Format(outcome), Does.Not.Contain("unknown result")); // TEST-SUPPORT: ABILITY-RESULT-030 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
        Assert.That(AbilityActivationResultText.Format(AbilityActivationResult.AbilitySourceChanged), // TEST-SUPPORT: ABILITY-RESULT-031 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
            Is.EqualTo("Rejected: ability source changed")); // TEST-SUPPORT: ABILITY-RESULT-032 | Tests: sMPGWM.Tests.AbilityResultTests.HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes
    }
}

