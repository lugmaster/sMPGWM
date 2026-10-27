using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.MessageSystem;

public class MessageStoreTests
{
    private static MessageStore Create()
    {
        var store = new MessageStore();
        store.RegisterSender(new MessageSender("a", "Sender"));
        store.RegisterSender(new MessageSender("b", "Sender"));
        return store;
    }

    [Test]
    public void ReceiveReadAndDuplicate_PublishOnlyCommittedChanges()
    {
        using var store = Create();
        var notifications = 0;
        store.Changed += snapshot =>
        {
            notifications++;
            Assert.That(snapshot, Is.SameAs(store.Snapshot));
            Assert.That(snapshot.UnreadForSender("a") + snapshot.UnreadForSender("b"), Is.EqualTo(snapshot.UnreadCount));
        };
        Assert.That(store.Receive("one", "a", "Subject", "", MessageType.NonBlocking), Is.True);
        var original = store.Snapshot;
        Assert.That(original.UnreadCount, Is.EqualTo(1));
        Assert.That(store.MarkRead("one"), Is.True);
        Assert.That(store.MarkRead("one"), Is.False);
        Assert.That(store.MarkRead("missing"), Is.False);
        Assert.That(store.Receive("one", "b", "Replacement", "Body", MessageType.Blocking), Is.False);
        Assert.That(notifications, Is.EqualTo(2));
        Assert.That(store.Snapshot.Find("one")!.Subject, Is.EqualTo("Subject"));
        Assert.That(store.Snapshot.UnreadCount, Is.Zero);
        Assert.That(original.Find("one")!.IsRead, Is.False);
        Assert.Throws<NotSupportedException>(() => ((IList<PlayerMessage>)original.Messages).Clear());
    }

    [Test]
    public void Ordering_SeparatesSameNameSendersAndBreaksTimeTiesByArrival()
    {
        using var store = Create();
        var time = DateTimeOffset.UtcNow;
        store.Receive("b1", "b", "", "", MessageType.NonBlocking, time);
        store.Receive("a1", "a", "", "", MessageType.Blocking, time);
        store.Receive("a2", "a", "", "", MessageType.NonBlocking, time);
        Assert.That(store.Snapshot.Messages.Select(message => message.Id), Is.EqualTo(new[] { "a2", "a1", "b1" }));
        Assert.That(store.Snapshot.UnreadForSender("a"), Is.EqualTo(2));
        Assert.That(store.Snapshot.UnreadForSender("b"), Is.EqualTo(1));
    }

    [Test]
    public void SenderUnreadTotals_AreIndependentInEachSnapshot()
    {
        using var store = Create();
        store.Receive("a1", "a", "", "", MessageType.NonBlocking);
        store.Receive("a2", "a", "", "", MessageType.NonBlocking);
        store.Receive("b1", "b", "", "", MessageType.NonBlocking);
        var beforeRead = store.Snapshot;
        store.MarkRead("a1");
        var afterFirstRead = store.Snapshot;
        store.MarkRead("a2");

        Assert.That(beforeRead.UnreadForSender("a"), Is.EqualTo(2));
        Assert.That(afterFirstRead.UnreadForSender("a"), Is.EqualTo(1));
        Assert.That(store.Snapshot.UnreadForSender("a"), Is.Zero);
        Assert.That(store.Snapshot.UnreadForSender("b"), Is.EqualTo(1));
        Assert.That(store.Snapshot.UnreadForSender("unknown"), Is.Zero);
        Assert.That(store.Snapshot.UnreadCount, Is.EqualTo(1));
    }

    [Test]
    public void InvalidInputs_DoNotPublish()
    {
        using var store = Create();
        Assert.Throws<ArgumentException>(() => store.RegisterSender(new MessageSender("", "Name")));
        Assert.Throws<ArgumentException>(() => store.RegisterSender(new MessageSender("id", "")));
        Assert.Throws<ArgumentException>(() => store.Receive("", "a", "", "", MessageType.NonBlocking));
        Assert.Throws<ArgumentException>(() => store.Receive("id", "unknown", "", "", MessageType.NonBlocking));
        Assert.Throws<ArgumentNullException>(() => store.Receive("id", "a", null!, "", MessageType.NonBlocking));
        Assert.Throws<ArgumentNullException>(() => store.Receive("id", "a", "", null!, MessageType.NonBlocking));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Receive("id", "a", "", "", (MessageType)7));
        Assert.That(store.RegisterSender(new MessageSender("a", "Other Name")), Is.False);
        Assert.That(store.Snapshot.Messages, Is.Empty);
    }

    [Test]
    public void Acknowledgement_IsIndependentOfReadingAndProcessesOneArrivalAtATime()
    {
        using var store = Create();
        using var queue = new MessageAcknowledgements(store);
        store.Receive("first", "a", "", "", MessageType.Blocking);
        store.Receive("ordinary", "a", "", "", MessageType.NonBlocking);
        store.Receive("second", "b", "", "", MessageType.Blocking);
        store.MarkRead("first");
        store.MarkRead("second");
        Assert.That(queue.Current!.Id, Is.EqualTo("first"));
        Assert.That(store.Receive("first", "a", "", "", MessageType.Blocking), Is.False);
        queue.AcknowledgeCurrent();
        Assert.That(queue.Current!.Id, Is.EqualTo("second"));
        queue.AcknowledgeCurrent();
        Assert.That(queue.Current, Is.Null);
        store.MarkRead("ordinary");
        Assert.That(queue.Current, Is.Null);
    }

    [Test]
    public void ObserverFailure_DoesNotSkipOtherObserversAndReentrantWritesAreRejected()
    {
        var errors = new List<Exception>();
        using var store = new MessageStore(errors.Add);
        store.RegisterSender(new MessageSender("a", "A"));
        store.Changed += _ => throw new InvalidOperationException();
        var observed = false;
        store.Changed += _ => { observed = true; Assert.That(store.MarkRead("one"), Is.False); };
        store.Receive("one", "a", "", "", MessageType.NonBlocking);
        Assert.That(observed, Is.True);
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(store.Snapshot.UnreadCount, Is.EqualTo(1));
    }

    [Test]
    public void RequiredAcknowledgement_BlocksControlsIndependentlyOfWindowsAndSession()
    {
        var policy = new PlayerInputPolicy();
        policy.SetSessionActive(true);
        policy.SetHasPendingAcknowledgement(true);
        policy.SetHasBlockingWindow(true);
        policy.SetHasBlockingWindow(false);
        Assert.That(policy.CanControlPlayer, Is.False);
        policy.SetSessionActive(false);
        policy.SetHasPendingAcknowledgement(false);
        Assert.That(policy.CanControlPlayer, Is.False);
        policy.SetSessionActive(true);
        Assert.That(policy.CanControlPlayer, Is.True);
    }
}
