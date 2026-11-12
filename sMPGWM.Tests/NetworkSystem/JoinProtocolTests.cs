using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

internal class JoinProtocolTests
{
    [Test] // TEST-SUPPORT: JOIN-ITEM-DELAY-001 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
    public void MissingItemsCannotCompleteOrExtendSnapshotDeadline() // TEST-SUPPORT: JOIN-ITEM-DELAY-002 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
    {
        var client = new ClientJoinProtocol(100); // TEST-SUPPORT: JOIN-ITEM-DELAY-003 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.ApplyItems(0), Is.False); // TEST-SUPPORT: JOIN-ITEM-DELAY-004 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        client.Begin("token", 0); // TEST-SUPPORT: JOIN-ITEM-DELAY-005 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        client.AcceptHello("token", 10); // TEST-SUPPORT: JOIN-ITEM-DELAY-006 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.ApplyItems(15), Is.False); // TEST-SUPPORT: JOIN-ITEM-DELAY-007 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        client.ApplySnapshot(90); // TEST-SUPPORT: JOIN-ITEM-DELAY-008 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.HasWorld, Is.True); // TEST-SUPPORT: JOIN-ITEM-DELAY-009 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.Complete(100), Is.False); // TEST-SUPPORT: JOIN-ITEM-DELAY-010 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.ApplySnapshot(105), Is.False); // TEST-SUPPORT: JOIN-ITEM-DELAY-011 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.ApplyItems(110), Is.False); // TEST-SUPPORT: JOIN-ITEM-DELAY-012 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
        Assert.That(client.Expired(110), Is.True); // TEST-SUPPORT: JOIN-ITEM-DELAY-013 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MissingItemsCannotCompleteOrExtendSnapshotDeadline
    }

    [TestCase(false)] // TEST-SUPPORT: JOIN-ITEM-STOP-001 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
    [TestCase(true)] // TEST-SUPPORT: JOIN-ITEM-STOP-002 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
    public void CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks(bool itemsApplied) // TEST-SUPPORT: JOIN-ITEM-STOP-003 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
    {
        var client = new ClientJoinProtocol(100); // TEST-SUPPORT: JOIN-ITEM-STOP-004 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        client.Begin("token", 0); // TEST-SUPPORT: JOIN-ITEM-STOP-005 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        client.AcceptHello("token", 1); // TEST-SUPPORT: JOIN-ITEM-STOP-006 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        client.ApplySnapshot(2); // TEST-SUPPORT: JOIN-ITEM-STOP-007 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        if (itemsApplied) client.ApplyItems(3); // TEST-SUPPORT: JOIN-ITEM-STOP-008 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        client.Stop(); // TEST-SUPPORT: JOIN-ITEM-STOP-009 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        Assert.That(client.ApplyItems(4), Is.False); // TEST-SUPPORT: JOIN-ITEM-STOP-010 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        Assert.That(client.Complete(5), Is.False); // TEST-SUPPORT: JOIN-ITEM-STOP-011 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
        Assert.That(client.HasWorld, Is.False); // TEST-SUPPORT: JOIN-ITEM-STOP-012 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.CancellationWhileWaitingForItemsOrCompletionRejectsLateCallbacks
    }

    [Test] // TEST-SUPPORT: JOIN-SNAPSHOT-001 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
    public void MembershipSnapshotIsReusedUntilMembershipChanges() // TEST-SUPPORT: JOIN-SNAPSHOT-002 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
    {
        var host = new HostJoinProtocol(100); // TEST-SUPPORT: JOIN-SNAPSHOT-003 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        var empty = host.Snapshot(); // TEST-SUPPORT: JOIN-SNAPSHOT-004 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(empty, Is.Empty); // TEST-SUPPORT: JOIN-SNAPSHOT-005 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Begin(2, 0), Is.True); // TEST-SUPPORT: JOIN-SNAPSHOT-006 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        var first = host.Snapshot(); // TEST-SUPPORT: JOIN-SNAPSHOT-007 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(first.Length, Is.EqualTo(1)); // TEST-SUPPORT: JOIN-SNAPSHOT-008 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Snapshot(), Is.SameAs(first)); // TEST-SUPPORT: JOIN-SNAPSHOT-009 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Begin(2, 1), Is.False); // TEST-SUPPORT: JOIN-SNAPSHOT-010 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Remove(3, out _), Is.False); // TEST-SUPPORT: JOIN-SNAPSHOT-011 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Snapshot(), Is.SameAs(first)); // TEST-SUPPORT: JOIN-SNAPSHOT-012 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 1), Is.True); // TEST-SUPPORT: JOIN-SNAPSHOT-013 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(first[0].Value.Phase, Is.EqualTo(PeerJoinPhase.Verified)); // TEST-SUPPORT: JOIN-SNAPSHOT-014 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Snapshot(), Is.SameAs(first)); // TEST-SUPPORT: JOIN-SNAPSHOT-015 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Begin(3, 1), Is.True); // TEST-SUPPORT: JOIN-SNAPSHOT-016 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        var both = host.Snapshot(); // TEST-SUPPORT: JOIN-SNAPSHOT-017 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(both.Length, Is.EqualTo(2)); // TEST-SUPPORT: JOIN-SNAPSHOT-018 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(first.Length, Is.EqualTo(1)); // TEST-SUPPORT: JOIN-SNAPSHOT-019 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Reject(2, 2), Is.True); // TEST-SUPPORT: JOIN-SNAPSHOT-020 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Snapshot(), Is.SameAs(both)); // TEST-SUPPORT: JOIN-SNAPSHOT-021 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Remove(2, out _), Is.True); // TEST-SUPPORT: JOIN-SNAPSHOT-022 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        var remaining = host.Snapshot(); // TEST-SUPPORT: JOIN-SNAPSHOT-023 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(remaining.Length, Is.EqualTo(1)); // TEST-SUPPORT: JOIN-SNAPSHOT-024 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(remaining[0].Key, Is.EqualTo(3)); // TEST-SUPPORT: JOIN-SNAPSHOT-025 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(both.Length, Is.EqualTo(2)); // TEST-SUPPORT: JOIN-SNAPSHOT-026 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        host.Clear(); // TEST-SUPPORT: JOIN-SNAPSHOT-027 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(host.Snapshot(), Is.Empty); // TEST-SUPPORT: JOIN-SNAPSHOT-028 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
        Assert.That(remaining[0].Key, Is.EqualTo(3)); // TEST-SUPPORT: JOIN-SNAPSHOT-029 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.MembershipSnapshotIsReusedUntilMembershipChanges
    }

    [Test]
    public void HostRejectsOutOfOrderAndDuplicateMessagesWithoutExtendingDeadline()
    {
        var host = new HostJoinProtocol(100);
        Assert.That(host.Begin(2, 0), Is.True);
        Assert.That(host.Begin(2, 50), Is.False);
        Assert.That(host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Ready, 50), Is.False);
        Assert.That(host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 60), Is.True);
        Assert.That(host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 150), Is.False);
        Assert.That(host.Advance(2, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, 160), Is.False);
        Assert.That(host.Snapshot().Single().Value.Expired(160), Is.True);
    }

    [TestCase(PeerJoinPhase.Connected)]
    [TestCase(PeerJoinPhase.Verified)]
    [TestCase(PeerJoinPhase.Spawning)]
    public void HostTimesOutEveryPendingPhase(PeerJoinPhase phase)
    {
        var host = new HostJoinProtocol(100);
        host.Begin(2, 0);
        if (phase != PeerJoinPhase.Connected)
            host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 0);
        if (phase == PeerJoinPhase.Spawning)
            host.Advance(2, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, 0);
        Assert.That(host.Snapshot().Single().Value.Expired(99), Is.False);
        Assert.That(host.Snapshot().Single().Value.Expired(100), Is.True);
    }

    [Test]
    public void RejectionAfterSpawnAllocationIsTerminalUntilRemovalAndDoesNotAnnounceMembership()
    {
        var host = new HostJoinProtocol(100);
        host.Begin(2, 0);
        host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 1);
        host.Advance(2, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, 2);
        Assert.That(host.Reject(2, 3), Is.True);
        Assert.That(host.Reject(2, 100), Is.False);
        Assert.That(host.Advance(2, PeerJoinPhase.Spawning, PeerJoinPhase.Ready, 4), Is.False);
        var rejected = host.Snapshot().Single().Value;
        Assert.That(rejected.ReceivesWorld, Is.False);
        Assert.That(rejected.Expired(502), Is.False);
        Assert.That(rejected.Expired(503), Is.True);
        Assert.That(host.Remove(2, out var wasReady), Is.True);
        Assert.That(wasReady, Is.False);
        Assert.That(host.Remove(2, out _), Is.False);
        Assert.That(host.Begin(2, 600), Is.True);
        Assert.That(host.Is(2, PeerJoinPhase.Connected), Is.True);
    }

    [Test]
    public void ReadyPeerHasNoJoinDeadlineAndDepartureIsPublishedOnlyOnce()
    {
        var host = new HostJoinProtocol(100);
        host.Begin(2, 0);
        host.Advance(2, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 1);
        host.Advance(2, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, 2);
        Assert.That(host.Advance(2, PeerJoinPhase.Spawning, PeerJoinPhase.Ready, 3), Is.True);
        Assert.That(host.Snapshot().Single().Value.Expired(10000), Is.False);
        host.Reject(2, 10000);
        Assert.That(host.Remove(2, out var wasReady), Is.True);
        Assert.That(wasReady, Is.True);
        Assert.That(host.Remove(2, out var again), Is.False);
        Assert.That(again, Is.False);
        host.Clear();
        Assert.That(host.Begin(2, 11000), Is.True);
    }

    [Test]
    public void ClientRequiresMatchingHelloAndAppliedSnapshotBeforeCompletion()
    {
        var client = new ClientJoinProtocol(100);
        Assert.That(client.Complete(0), Is.False);
        Assert.That(client.Begin("token", 0), Is.True);
        Assert.That(client.Begin("replacement", 5), Is.False);
        Assert.That(client.ApplySnapshot(5), Is.False);
        Assert.That(client.AcceptHello("wrong", 5), Is.False);
        Assert.That(client.AcceptHello("token", 10), Is.True);
        Assert.That(client.AcceptHello("token", 15), Is.False);
        Assert.That(client.Complete(15), Is.False);
        Assert.That(client.ApplySnapshot(20), Is.True);
        Assert.That(client.HasWorld, Is.True);
        Assert.That(client.Phase, Is.EqualTo(ClientJoinPhase.AwaitingItems)); // TEST-SUPPORT: JOIN-ITEM-GATE-001 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientRequiresMatchingHelloAndAppliedSnapshotBeforeCompletion
        Assert.That(client.Complete(25), Is.False); // TEST-SUPPORT: JOIN-ITEM-GATE-002 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientRequiresMatchingHelloAndAppliedSnapshotBeforeCompletion
        Assert.That(client.ApplyItems(26), Is.True); // TEST-SUPPORT: JOIN-ITEM-GATE-003 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientRequiresMatchingHelloAndAppliedSnapshotBeforeCompletion
        Assert.That(client.ApplyItems(27), Is.False); // TEST-SUPPORT: JOIN-ITEM-GATE-004 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientRequiresMatchingHelloAndAppliedSnapshotBeforeCompletion
        Assert.That(client.ApplySnapshot(25), Is.False);
        Assert.That(client.Complete(30), Is.True);
        Assert.That(client.Complete(35), Is.False);
        Assert.That(client.Expired(10000), Is.False);
    }

    [TestCase(ClientJoinPhase.AwaitingHello)]
    [TestCase(ClientJoinPhase.AwaitingSnapshot)]
    [TestCase(ClientJoinPhase.AwaitingItems)] // TEST-SUPPORT: JOIN-ITEM-GATE-005 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientCannotAdvanceAfterDeadline
    [TestCase(ClientJoinPhase.AwaitingCompletion)]
    public void ClientCannotAdvanceAfterDeadline(ClientJoinPhase phase)
    {
        var client = new ClientJoinProtocol(100);
        client.Begin("token", 0);
        if (phase != ClientJoinPhase.AwaitingHello) client.AcceptHello("token", 0);
        if (phase is ClientJoinPhase.AwaitingItems or ClientJoinPhase.AwaitingCompletion) client.ApplySnapshot(0); // TEST-SUPPORT: JOIN-ITEM-GATE-006 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientCannotAdvanceAfterDeadline
        if (phase == ClientJoinPhase.AwaitingCompletion) client.ApplyItems(0); // TEST-SUPPORT: JOIN-ITEM-GATE-007 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientCannotAdvanceAfterDeadline
        Assert.That(client.Expired(99), Is.False);
        Assert.That(client.Expired(100), Is.True);
        Assert.That(client.AcceptHello("token", 100), Is.False);
        Assert.That(client.ApplySnapshot(100), Is.False);
        Assert.That(client.Complete(100), Is.False);
        Assert.That(client.ApplyItems(100), Is.False); // TEST-SUPPORT: JOIN-ITEM-GATE-008 | Tests: sMPGWM.Tests.NetworkSystem.JoinProtocolTests.ClientCannotAdvanceAfterDeadline
    }

    [Test]
    public void DisconnectDuringSnapshotApplicationCannotPublishReady()
    {
        var client = new ClientJoinProtocol(100);
        client.Begin("token", 0);
        client.AcceptHello("token", 1);
        client.Stop();
        Assert.That(client.ApplySnapshot(2), Is.False);
        Assert.That(client.Complete(3), Is.False);
        Assert.That(client.HasWorld, Is.False);
        Assert.That(client.Expired(10000), Is.False);
        Assert.That(new ClientJoinProtocol(100).Begin("fresh", 10000), Is.True);
    }
}
