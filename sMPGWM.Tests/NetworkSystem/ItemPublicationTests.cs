using System.Reflection;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class ItemPublicationTests
{
    [Test]
    public void UnchangedTenPlayerPublicationDoesNotAllocate()
    {
        var world = new GameWorld();
        var network = new NetworkManager { World = world };
        try
        {
            var joins = new HostJoinProtocol(10000);
            typeof(NetworkManager).GetField("_joins", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(network, joins);
            var published = Field<Dictionary<int, (long, long, long)>>(network, "_publishedItemRevisions");
            var groups = Field<Dictionary<(int Recipient, int Owner), (long, long)>>(network, "_groupPublished");
            for (var peer = 1; peer <= 10; peer++)
            {
                world.ApproveSpawn(peer, PlayerStartupData.Create());
                if (peer == 1) continue;
                joins.Begin(peer, 0);
                joins.Advance(peer, PeerJoinPhase.Connected, PeerJoinPhase.Verified, 0);
                joins.Advance(peer, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, 0);
                joins.Advance(peer, PeerJoinPhase.Spawning, PeerJoinPhase.Ready, 0);
            }
            foreach (var player in world.Participants)
            {
                published[player.ControllingPeerId] = (player.ControlGeneration, player.Inventory.Revision, player.ControlledShip!.Equipment.Revision);
                for (var recipient = 2; recipient <= 10; recipient++)
                    groups[(recipient, player.ControllingPeerId)] = (player.ControlGeneration, player.ControlledShip.Equipment.Revision);
            }
            var publish = typeof(NetworkManager).GetMethod("PublishItemChanges", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action>(network);
            for (var i = 0; i < 100; i++) publish();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) publish();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
        finally { network.Free(); world.Clear(); world.Free(); }
    }

    [Test]
    public void DepartureKeepsUnaffectedEquipmentVersionsAndRequestOutcomes()
    {
        var network = new NetworkManager();
        try
        {
            var groups = Field<Dictionary<(int Recipient, int Owner), (long, long)>>(network, "_groupPublished");
            groups[(2, 3)] = (4, 5);
            groups[(3, 2)] = (6, 7);
            groups[(3, 1)] = (8, 9);
            var outcomes = Field<Dictionary<int, (long, TransferResult)>>(network, "_groupOutcomes");
            outcomes[2] = (10, TransferResult.Success);
            outcomes[3] = (11, TransferResult.NoChange);
            var remove = typeof(NetworkManager).GetMethod("RemovePlayerItems", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<int>>(network);
            remove(2);
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[(3, 1)], Is.EqualTo((8L, 9L)));
            Assert.That(outcomes.Count, Is.EqualTo(1));
            Assert.That(outcomes[3], Is.EqualTo((11L, TransferResult.NoChange)));
        }
        finally { network.Free(); }
    }

    [TestCase(5)]
    [TestCase(10)]
    public void EncodingOncePerOwnerAvoidsRecipientScaledSnapshotAllocation(int players)
    {
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = DevelopmentLoadout.Equip };
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var snapshot = world.Players[1].ControlledShip!.Equipment.Snapshot;
            for (var i = 0; i < 100; i++) Encode();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
                for (var recipient = 1; recipient < players; recipient++) Encode();
            var repeated = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++) Encode();
            var shared = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.Out.WriteLine($"Equipment codec bytes/100 owner updates, {players} players: per-recipient control={repeated}; once per owner={shared}. Excludes RPC marshalling and native allocations.");
            Assert.That(shared, Is.GreaterThan(0));
            Assert.That(repeated, Is.GreaterThan(shared * (players - 2)));

            void Encode()
            {
                using var slots = ItemSnapshotCodec.EncodeSlots(snapshot.Slots);
                using var groups = WeaponGroupSnapshotCodec.Encode(snapshot.WeaponGroups);
            }
        }
        finally { world.Clear(); world.Free(); }
    }

    private static T Field<T>(NetworkManager network, string name) =>
        (T)typeof(NetworkManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(network)!;
}
