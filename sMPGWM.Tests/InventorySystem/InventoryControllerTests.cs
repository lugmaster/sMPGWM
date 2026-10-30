using sMPGWM.Scripts.Util.Mocks;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.InventorySystem;

[TestFixture]
[Category("Godot")]
public class InventoryControllerTests
{
    private static readonly string Engine = "mock_standard_engine";
    private static readonly string Repair = "mock_standard_consumable";
    private static readonly string Resource = "mock_standard_resource";

    [Test]
    public void NewInventory_Has128GenuineEmptyAddresses()
    {
        using var inventory = new InventoryController();
        Assert.That(inventory.Capacity, Is.EqualTo(128));
        Assert.That(inventory.Revision, Is.Zero);
        Assert.That(inventory.Snapshot.Occupied, Is.Empty);
        Assert.That(inventory.Snapshot.EmptyAddresses, Is.EqualTo(Enumerable.Range(0, 128)));
    }

    [TestCase(1, 128)]
    [TestCase(128, 128)]
    [TestCase(129, 192)]
    [TestCase(192, 192)]
    [TestCase(193, 256)]
    [TestCase(257, 320)]
    public void BulkAdd_GrowsInMinimum64CellChunks(int quantity, int capacity)
    {
        using var inventory = new InventoryController();
        var notifications = 0;
        InventorySnapshot? observed = null;
        inventory.Changed += snapshot =>
        {
            notifications++;
            observed = snapshot;
        };

        Assert.That(inventory.Items.CreateItems(inventory.Container, Engine, quantity), Is.EqualTo(InventoryResult.Success));
        Assert.That(inventory.Capacity, Is.EqualTo(capacity));
        Assert.That(inventory.Snapshot.Occupied.Select(entry => entry.Address),
            Is.EqualTo(Enumerable.Range(0, quantity)));
        Assert.That(inventory.Snapshot.Occupied.Select(entry => entry.Entity.Id).Distinct().Count(), Is.EqualTo(quantity));
        Assert.That(inventory.Revision, Is.EqualTo(1));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(observed, Is.SameAs(inventory.Snapshot));
        Assert.That(observed!.Occupied.Count, Is.EqualTo(quantity));
    }

    [Test]
    public void Add_FillsPartialStacksBeforeHolesInStorageOrderAndPreservesIds()
    {
        using var inventory = new InventoryController();
        ItemFixture.Place(inventory.Items, inventory.Container, 3, Repair, 7);
        ItemFixture.Place(inventory.Items, inventory.Container, 7, Repair, 5);
        ItemFixture.Place(inventory.Items, inventory.Container, 9, Engine);
        var before = inventory.Snapshot;

        Assert.That(inventory.Items.CreateItems(inventory.Container, Repair, 6), Is.EqualTo(InventoryResult.Success));
        Assert.That(inventory.Snapshot.Slots[3]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Slots[7]!.Count, Is.EqualTo(8));
        Assert.That(inventory.Snapshot.Slots[0], Is.Null);
        Assert.That(inventory.Snapshot.Slots[3]!.Id, Is.EqualTo(before.Slots[3]!.Id));
        Assert.That(inventory.Snapshot.Slots[7]!.Id, Is.EqualTo(before.Slots[7]!.Id));
        Assert.That(inventory.Snapshot.Slots[9], Is.SameAs(before.Slots[9]));
        Assert.That(before.Slots[3]!.Count, Is.EqualTo(7));

        inventory.Items.CreateItems(inventory.Container, Repair, 15);
        Assert.That(inventory.Snapshot.Slots[0]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Slots[1]!.Count, Is.EqualTo(3));
        Assert.That(inventory.Snapshot.Slots[7]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count), Is.EqualTo(34));
    }

    [Test]
    public void FullStacks_AllocateNextStackAndRetainExistingIdentity()
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, Repair, 1280);
        var before = inventory.Snapshot;
        inventory.Items.CreateItems(inventory.Container, Repair, 1);
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots.Take(128), Is.EqualTo(before.Slots));
        Assert.That(inventory.Snapshot.Slots[128]!.Count, Is.EqualTo(1));
        Assert.That(inventory.Snapshot.Slots[128]!.Id, Is.Not.EqualTo(before.Slots[0]!.Id));
    }

    [Test]
    public void BulkAddition_CrossesStackBoundariesWithoutLosingQuantity()
    {
        using var inventory = new InventoryController();
        var limit = ItemRegistry.GetById(Resource).StackLimit;
        var quantity = (long)limit * 2 + 3;
        inventory.Items.CreateItems(inventory.Container, Resource, quantity);
        Assert.That(inventory.Snapshot.Occupied.Select(entry => entry.Entity.Count),
            Is.EqualTo(new[] { limit, limit, 3 }));
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count), Is.EqualTo(quantity));
        inventory.Items.CreateItems(inventory.Container, Resource, limit);
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count),
            Is.EqualTo(quantity + limit));
    }

    [TestCase(-1, InventoryResult.NegativeQuantity)]
    [TestCase(0, InventoryResult.NoChange)]
    [TestCase(long.MaxValue, InventoryResult.CapacityExceeded)]
    [TestCase((long)int.MaxValue, InventoryResult.CapacityExceeded)]
    public void InvalidOrNoopAdd_PreservesExactSnapshotAndPublishesNothing(long quantity, InventoryResult expected)
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, Repair, 7);
        var before = inventory.Snapshot;
        var notifications = 0;
        inventory.Changed += _ => notifications++;
        Assert.That(inventory.Items.CreateItems(inventory.Container, Engine, quantity), Is.EqualTo(expected));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        Assert.That(notifications, Is.Zero);
        Assert.That(inventory.Items.CreateItems(inventory.Container, Resource, long.MaxValue), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(inventory.Items.CreateItems(inventory.Container, Repair, long.MaxValue), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity()
    {
        using var inventory = new InventoryController();
        var before = inventory.Snapshot;
        Assert.Throws<ArgumentNullException>(() => inventory.Items.CreateItems(inventory.Container, null!, 1));
        Assert.That(inventory.Items.CreateItems(inventory.Container, "missing", 0), Is.EqualTo(InventoryResult.UnknownDefinition));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void TransferOut_LeavesHolesAndNeverShrinks()
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, Engine, 129);
        var retained = inventory.Snapshot.Slots[128];
        ItemFixture.MoveAway(inventory.Items, inventory.Container, 0);
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots[128], Is.SameAs(retained));
        Assert.That(inventory.Snapshot.EmptyAddresses[0], Is.Zero);
        inventory.Items.CreateItems(inventory.Container, Engine, 1);
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots[0], Is.Not.Null);
        Assert.That(inventory.Snapshot.Slots[128], Is.SameAs(retained));
    }

    [Test]
    public void Snapshots_DoNotExposeWritableStorage()
    {
        using var inventory = new InventoryController();
        ItemFixture.Place(inventory.Items, inventory.Container, 2, Engine);
        var before = inventory.Snapshot;
        var slots = before.Slots.ToArray();
        slots[2] = null;
        Assert.That(inventory.Snapshot.Slots[2], Is.Not.Null);
        Assert.Throws<NotSupportedException>(() => ((IList<ItemEntity?>)inventory.Snapshot.Slots)[2] = null);
        Assert.Throws<NotSupportedException>(() => ((IList<InventoryEntry>)inventory.Snapshot.Occupied).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)inventory.Snapshot.EmptyAddresses).Clear());
        ItemFixture.MoveAway(inventory.Items, inventory.Container, 2);
        Assert.That(inventory.Snapshot.Slots[2], Is.Null);
        Assert.That(before.Slots[2], Is.Not.Null);
    }

    [Test]
    public void Notifications_RejectReentrantMutation()
    {
        using var inventory = new InventoryController();
        var reentrant = InventoryResult.Success;
        InventorySnapshot? observed = null;
        inventory.Changed += snapshot =>
        {
            reentrant = inventory.Items.CreateItems(inventory.Container, Engine, 1);
            observed = snapshot;
        };
        Assert.That(inventory.Items.CreateItems(inventory.Container, Engine, 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(observed, Is.SameAs(inventory.Snapshot));
        Assert.That(reentrant, Is.EqualTo(InventoryResult.PublicationInProgress));
        Assert.That(inventory.Revision, Is.EqualTo(1));
    }

    [TestCase(typeof(InvalidOperationException))]
    [TestCase(typeof(OverflowException))]
    [TestCase(typeof(OutOfMemoryException))]
    public void SubscriberFailure_PropagatesOriginalExceptionAndStopsNotifications(Type exceptionType)
    {
        using var inventory = new InventoryController();
        var failure = (Exception)Activator.CreateInstance(exceptionType)!;
        Action<InventorySnapshot> broken = _ => throw failure;
        var laterCalled = false;
        inventory.Changed += broken;
        inventory.Changed += _ => laterCalled = true;

        var thrown = Assert.Throws(exceptionType, () => inventory.Items.CreateItems(inventory.Container, Engine, 1));
        Assert.That(thrown, Is.SameAs(failure));
        Assert.That(laterCalled, Is.False);
        Assert.That(inventory.Revision, Is.EqualTo(1));
        var item = inventory.Snapshot.Slots[0]!;
        Assert.That(inventory.Items.GetLocation(item.Id), Is.EqualTo(new ItemLocation(inventory.Container.Id, 0)));
        Assert.That(inventory.Items.Publishing, Is.False);
        inventory.Changed -= broken;
        Assert.That(inventory.Items.CreateItems(inventory.Container, Engine, 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(laterCalled, Is.True);
    }

    [Test]
    public void Disposal_RevokesMutationsAndNewGameHasIndependentEmptyStorage()
    {
        var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, Engine, 129);
        var before = inventory.Snapshot;
        inventory.Dispose();
        inventory.Dispose();
        Assert.That(inventory.Items.CreateItems(inventory.Container, Engine, 1), Is.EqualTo(InventoryResult.Disposed));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        using var nextGame = new InventoryController();
        Assert.That(nextGame.Capacity, Is.EqualTo(128));
        Assert.That(nextGame.Snapshot.Occupied, Is.Empty);
        Assert.That(nextGame.Revision, Is.Zero);
    }
}
