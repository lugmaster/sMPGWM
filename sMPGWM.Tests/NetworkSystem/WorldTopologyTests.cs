using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.World;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public class WorldTopologyTests
{
    private static ShipSpawnRecord Record(int peer) => new(peer, Guid.NewGuid().ToString("N"), "fighter",
        new Vector2(peer * 300, 100), 0, Vector2.Zero);

    [Test]
    public void UncontrolledShipBelongsToWorldAndSurvivesControlChanges()
    {
        var world = new GameWorld();
        try
        {
            var record = Record(1);
            var ship = world.SpawnEntity(EntitySpawnRecord.From(record));
            Assert.That(world.Players, Is.Empty);
            Assert.That(ship.GetParent(), Is.SameAs(world));
            Assert.Throws<InvalidOperationException>(() => world.SpawnEntity(EntitySpawnRecord.From(record)));
            var player = new sMPGWM.Scripts.World.PlayerSession { ControllingPeerId = 1 };
            player.AssignShip(ship);
            player.AssignShip(null);
            Assert.That(ship.GetParent(), Is.SameAs(world));
            Assert.That(ship.IsQueuedForDeletion(), Is.False);
            Assert.That(world.CaptureTopology().Entities.Single().EntityId, Is.EqualTo(record.EntityId));
            world.Clear();
            Assert.That(ship.IsQueuedForDeletion(), Is.True);
        }
        finally { world.Free(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AtomicTransferPreservesEntityRegardlessOfParticipantOrder(bool reverse)
    {
        var world = new GameWorld();
        try
        {
            var first = Record(1); var second = Record(2);
            world.Spawn(first); world.Spawn(second);
            var transferred = world.Players[1].ControlledShip!;
            var released = world.Players[2].ControlledShip!;
            var participants = new[] { new ParticipantBinding(1, 2, ""), new ParticipantBinding(2, 2, first.EntityId) };
            if (reverse) Array.Reverse(participants);
            var topology = new WorldTopology(participants, new[] { EntitySpawnRecord.From(first), EntitySpawnRecord.From(second) });
            world.ApplyTopology(topology);
            Assert.That(world.Players[1].ControlledShip, Is.Null);
            Assert.That(world.Players[2].ControlledShip, Is.SameAs(transferred));
            Assert.That(transferred.IsQueuedForDeletion(), Is.False);
            Assert.That(released.IsQueuedForDeletion(), Is.False);
            Assert.That(world.Entities[second.EntityId], Is.SameAs(released));
            Assert.That(world.ApplyTopology(topology), Is.Empty, "Duplicate topology must be idempotent.");
            world.Despawn(1);
            Assert.That(transferred.IsQueuedForDeletion(), Is.False);
            world.DestroyEntity(second.EntityId);
            Assert.That(released.IsQueuedForDeletion(), Is.True);
        }
        finally { world.Free(); }
    }

    [Test]
    public void TransferAndDepartureInSameTransactionDoesNotDestroyTransferredShip()
    {
        var world = new GameWorld();
        try
        {
            var first = Record(1); var second = Record(2);
            world.Spawn(first); world.Spawn(second);
            var transferred = world.Entities[first.EntityId];
            world.ApplyTopology(new WorldTopology(new[] { new ParticipantBinding(2, 2, first.EntityId) }, new[] { EntitySpawnRecord.From(first), EntitySpawnRecord.From(second) }));
            Assert.That(world.Players.ContainsKey(1), Is.False);
            Assert.That(world.Players[2].ControlledShip, Is.SameAs(transferred));
            Assert.That(transferred.IsQueuedForDeletion(), Is.False);
        }
        finally { world.Free(); }
    }

    [Test]
    public void LateJoinKeepsShiplessHostAndUncontrolledEntities()
    {
        var source = new GameWorld(); var joined = new GameWorld();
        try
        {
            var first = Record(1); source.Spawn(first); source.Spawn(Record(2));
            source.Players[1].AssignShip(null);
            var topology = source.CaptureTopology();
            using var payload = topology.ToVariant();
            Assert.That(WorldTopology.TryRead(payload, out var parsed), Is.True);
            joined.ApplyTopology(parsed);
            Assert.That(joined.Players.Keys, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(joined.Players[1].ControlledShip, Is.Null);
            Assert.That(joined.Players[1].ControlGeneration, Is.EqualTo(2));
            Assert.That(joined.Entities.ContainsKey(first.EntityId), Is.True);
            Assert.That(joined.CaptureTopology().Identity, Is.EqualTo(topology.Identity));
        }
        finally { source.Free(); joined.Free(); }
    }

    [Test]
    public void SwapPreservesBothBodiesAndStaleTopologyCannotMutateThem()
    {
        var world = new GameWorld();
        try
        {
            var first = Record(1); var second = Record(2);
            world.Spawn(first); world.Spawn(second);
            var initial = world.CaptureTopology();
            var a = world.Entities[first.EntityId]; var b = world.Entities[second.EntityId];
            var swap = new WorldTopology(new[] { new ParticipantBinding(1, 3, second.EntityId),
                new ParticipantBinding(2, 3, first.EntityId) }, initial.Entities);
            world.ApplyTopology(swap);
            Assert.That(world.Players[1].ControlledShip, Is.SameAs(b));
            Assert.That(world.Players[2].ControlledShip, Is.SameAs(a));
            Assert.Throws<InvalidOperationException>(() => world.ApplyTopology(initial));
            Assert.That(world.Players[1].ControlledShip, Is.SameAs(b));
            Assert.That(world.Players[2].ControlledShip, Is.SameAs(a));
        }
        finally { world.Free(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EveryRemoteProxyIsRestoredBeforeLocalReplay(bool reverse)
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            for (int peer = 1; peer <= 3; peer++) world.Spawn(Record(peer));
            var owner = world.Players[1]; owner.ControlledShip!.SetLocalControl(true);
            var prediction = new LocalMovementPrediction();
            var baseline = MovementStateAdapter.Capture(1, owner, 0);
            prediction.ApplyBaseline(0, baseline, owner.ControlledShip);
            for (int i = 0; i < 4; i++) prediction.Step(1, owner);
            var samples = world.Players.Select(p => MovementStateAdapter.Capture(p.Key, p.Value, 0) with
                { Position = p.Value.ControlledShip!.GlobalPosition + new Vector2(10, 0), AppliedFlags = 4 }).ToArray();
            if (reverse) Array.Reverse(samples);
            var presentation = new RemoteMovementPresentation();
            int replays = 0;
            MovementSnapshotApplication.Apply(samples, world.Players,
                (remote, sample) => presentation.Apply(sample.Peer, remote.ControlledShip!, 2, sample, false),
                (local, sample) =>
                {
                    foreach (var proxy in samples.Where(s => s.Peer != 1))
                        Assert.That(world.Players[proxy.Peer].ControlledShip!.GlobalPosition, Is.EqualTo(proxy.Position));
                    Assert.That(prediction.Reconcile(1, local, 2, sample), Is.True);
                    replays++;
                });
            Assert.That(replays, Is.EqualTo(1));
            Assert.That(owner.ControlledShip.GlobalPosition.X, Is.EqualTo(baseline.Position.X + 10).Within(.01));
            Assert.That(world.Players[2].ControlledShip!.AppliedMovementFlags, Is.EqualTo(4));
        }
        finally { world.Free(); }
    }
}
