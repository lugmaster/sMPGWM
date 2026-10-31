using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

public class NetworkSessionTests
{
    [Test]
    public void LateReadyAfterCancellationCannotReactivateSession()
    {
        var session = new NetworkSession();
        Assert.That(session.TryStart(), Is.True);
        Assert.That(session.TryStop("Canceled"), Is.True);
        Assert.That(session.TryReady(), Is.False);
        session.CompleteStop();
        Assert.That(session.TryReady(), Is.False);
        Assert.That(session.TryStart(), Is.False);
        Assert.That(session.TryStop("Late disconnect"), Is.False);
        Assert.That(session.FailureReason, Is.EqualTo("Canceled"));
    }

    [Test]
    public void DuplicateCallbacksDoNotPublishDuplicateReadinessOrStop()
    {
        var session = new NetworkSession();
        var states = new List<SessionState>();
        session.Changed += states.Add;
        Assert.That(session.TryReady(), Is.False);
        session.TryStart();
        session.TryStart();
        session.TryReady();
        session.TryReady();
        session.TryStop();
        session.TryStop();
        session.CompleteStop();
        session.CompleteStop();
        Assert.That(states, Is.EqualTo(new[] {
            SessionState.Starting, SessionState.Ready, SessionState.Stopping, SessionState.Stopped }));
    }
}
