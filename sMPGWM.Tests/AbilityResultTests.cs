using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Tests;

public sealed class AbilityResultTests
{
    [Test]
    public void HistoryPreservesTypedPendingRateLimitedAndCompletedOutcomes()
    {
        var history = new ActivationHistory();
        Assert.That(history.Admit(1, 0, out var result), Is.True);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Pending));
        Assert.That(history.Admit(1, 1, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Pending));
        history.Complete(1, AbilityActivationResult.AbilitySourceChanged);
        Assert.That(history.Admit(1, 2, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.AbilitySourceChanged));
        Assert.That(history.Admit(2, 5, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.RateLimit));
        Assert.That(history.Admit(2, 6, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.RateLimit));
        Assert.That(history.Admit(33, 6, out result), Is.True);
        history.Complete(33, AbilityActivationResult.Busy);
        history.Complete(1, AbilityActivationResult.Accepted);
        Assert.That(history.Admit(33, 12, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Busy));
        Assert.That(history.Admit(1, 12, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.OldRequest));
        Assert.That(history.Admit(0, 12, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.OldRequest));
        foreach (var outcome in Enum.GetValues<AbilityActivationResult>())
            Assert.That(AbilityActivationResultText.Format(outcome), Does.Not.Contain("unknown result"));
        Assert.That(AbilityActivationResultText.Format(AbilityActivationResult.AbilitySourceChanged),
            Is.EqualTo("Rejected: ability source changed"));
    }
}

