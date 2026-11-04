using sMPGWM.Scripts.Steam;

namespace sMPGWM.Tests.NetworkSystem;

public class PendingLobbyOperationTests
{
    [Test]
    public void SubmissionFailureAllowsRetryWithoutWaitingForCallback()
    {
        var operation = new PendingLobbyOperation();
        Action<long, ulong> callback = (_, _) => Assert.Fail("Failed submission cannot complete");
        Assert.Throws<InvalidOperationException>(() => operation.Submit(42, callback,
            () => throw new InvalidOperationException("Not submitted")));
        Assert.That(operation.Pending, Is.False);
        Assert.That(operation.Target, Is.Zero);
        var completions = 0;
        Assert.That(operation.Submit(43, (_, _) => completions++, () => { }), Is.True);
        operation.Complete(1, 43, _ => Assert.Fail());
        Assert.That(completions, Is.EqualTo(1));
    }

    [Test]
    public void CanceledOperationBlocksRetryUntilLateMembershipIsDrained()
    {
        var operation = new PendingLobbyOperation();
        var completions = 0;
        ulong left = 0;
        Action<long, ulong> callback = (_, _) => completions++;
        Assert.That(operation.Begin(42, callback), Is.True);
        operation.Cancel(callback);
        Assert.That(operation.Begin(42, callback), Is.False);
        operation.Complete(1, 99, id => left = id);
        Assert.That(operation.Pending, Is.True);
        operation.Complete(1, 42, id => left = id);
        Assert.That(left, Is.EqualTo(42));
        Assert.That(completions, Is.Zero);
        Assert.That(operation.Begin(42, callback), Is.True);
        operation.Complete(1, 42, id => left = id);
        operation.Complete(1, 42, id => left = id);
        Assert.That(completions, Is.EqualTo(1));
    }

    [Test]
    public void CanceledCreateFailureDoesNotLeaveMembershipAndCallbackCanReenter()
    {
        var operation = new PendingLobbyOperation();
        Action<long, ulong> callback = (_, _) => Assert.Fail("Canceled scene retained");
        operation.Begin(0, callback);
        operation.Cancel(callback);
        operation.Complete(2, 0, _ => Assert.Fail("Failed create has no membership"));
        Assert.That(operation.Begin(0, (_, _) =>
            Assert.That(operation.Begin(0, callback), Is.True)), Is.True);
        operation.Complete(1, 42, _ => Assert.Fail());
        Assert.That(operation.Pending, Is.True);
    }
}
