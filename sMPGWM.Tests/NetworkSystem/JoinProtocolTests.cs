using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

internal class JoinProtocolTests
{

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
        Assert.That(client.Phase, Is.EqualTo(ClientJoinPhase.AwaitingCompletion));
        Assert.That(client.ApplySnapshot(25), Is.False);
        Assert.That(client.Complete(30), Is.True);
        Assert.That(client.Complete(35), Is.False);
        Assert.That(client.Expired(10000), Is.False);
    }

    [TestCase(ClientJoinPhase.AwaitingHello)]
    [TestCase(ClientJoinPhase.AwaitingSnapshot)]
    [TestCase(ClientJoinPhase.AwaitingCompletion)]
    public void ClientCannotAdvanceAfterDeadline(ClientJoinPhase phase)
    {
        var client = new ClientJoinProtocol(100);
        client.Begin("token", 0);
        if (phase != ClientJoinPhase.AwaitingHello) client.AcceptHello("token", 0);
        if (phase == ClientJoinPhase.AwaitingCompletion) client.ApplySnapshot(0);
        Assert.That(client.Expired(99), Is.False);
        Assert.That(client.Expired(100), Is.True);
        Assert.That(client.AcceptHello("token", 100), Is.False);
        Assert.That(client.ApplySnapshot(100), Is.False);
        Assert.That(client.Complete(100), Is.False);
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
