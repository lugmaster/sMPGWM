using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Tests;

public sealed class ReloadHistoryTests
{
    [Test]
    public void PendingAndCompletedDuplicatesPrecedeRateLimit()
    {
        var history = new ReloadHistory();
        Assert.That(history.Admit(1, 0, out var result), Is.True);
        Assert.That(result, Is.EqualTo(ReloadResult.Pending));
        Assert.That(history.Admit(1, 0, out result), Is.False);
        Assert.That(result, Is.EqualTo(ReloadResult.Pending));
        history.Complete(1, ReloadResult.Applied);
        Assert.That(history.Admit(1, 1, out result), Is.False);
        Assert.That(result, Is.EqualTo(ReloadResult.Applied));
        Assert.That(history.Admit(2, 1, out result), Is.False);
        Assert.That(result, Is.EqualTo(ReloadResult.RateLimit));
        Assert.That(history.Admit(2, 6, out result), Is.False);
        Assert.That(result, Is.EqualTo(ReloadResult.RateLimit));
        Assert.That(history.Admit(3, 6, out result), Is.True);
    }

    [Test]
    public void EvictedAndInvalidRequestsCannotReplay()
    {
        var history = new ReloadHistory();
        for (var request = 1; request <= 33; request++)
        {
            Assert.That(history.Admit(request, request * 6, out _), Is.True);
            history.Complete(request, ReloadResult.Applied);
        }
        foreach (var request in new[] { -1L, 0, 1, long.MaxValue })
        {
            Assert.That(history.Admit(request, 1000, out var result), Is.False);
            Assert.That(result, Is.EqualTo(ReloadResult.OldRequest));
        }
    }
}
