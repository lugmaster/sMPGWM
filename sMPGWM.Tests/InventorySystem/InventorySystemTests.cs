using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using Inventory = sMPGWM.Scripts.InventorySystem.InventorySystem;

namespace sMPGWM.Tests.InventorySystem;

[TestFixture]
public class InventorySystemTests
{
    private static readonly ItemDefinitionId Engine = new("engine");
    private static readonly ItemDefinitionId Repair = new("repair");
    private static readonly ItemDefinitionId Resource = new("resource");
    private static readonly ItemCatalog Catalog = new(ItemCategories.CreateDefault(), new[]
    {
        new ItemDefinition(Engine, "Engine", ItemCategories.Engine),
        new ItemDefinition(Repair, "Repair", ItemCategories.Consumable, maximumStackSize: 10),
        new ItemDefinition(Resource, "Resource", ItemCategories.Resource)
    });

    [Test]
    public void NewInventory_Has128GenuineEmptyAddresses()
    {
        using var inventory = new Inventory(Catalog);
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
        using var inventory = new Inventory(Catalog);
        var notifications = 0;
        InventorySnapshot? observed = null;
        inventory.Changed += snapshot =>
        {
            notifications++;
            observed = snapshot;
        };

        Assert.That(inventory.Add(Engine, quantity), Is.EqualTo(InventoryResult.Success));
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
        using var inventory = new Inventory(Catalog);
        var slots = inventory.Snapshot.Slots.ToArray();
        slots[3] = Catalog.CreateEntity(Repair, 7);
        slots[7] = Catalog.CreateEntity(Repair, 5);
        slots[9] = Catalog.CreateEntity(Engine);
        inventory.Install(inventory.PrepareChange(slots));
        var before = inventory.Snapshot;

        Assert.That(inventory.Add(Repair, 6), Is.EqualTo(InventoryResult.Success));
        Assert.That(inventory.Snapshot.Slots[3]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Slots[7]!.Count, Is.EqualTo(8));
        Assert.That(inventory.Snapshot.Slots[0], Is.Null);
        Assert.That(inventory.Snapshot.Slots[3]!.Id, Is.EqualTo(before.Slots[3]!.Id));
        Assert.That(inventory.Snapshot.Slots[7]!.Id, Is.EqualTo(before.Slots[7]!.Id));
        Assert.That(inventory.Snapshot.Slots[9], Is.SameAs(before.Slots[9]));
        Assert.That(before.Slots[3]!.Count, Is.EqualTo(7));

        inventory.Add(Repair, 15);
        Assert.That(inventory.Snapshot.Slots[0]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Slots[1]!.Count, Is.EqualTo(3));
        Assert.That(inventory.Snapshot.Slots[7]!.Count, Is.EqualTo(10));
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count), Is.EqualTo(34));
    }

    [Test]
    public void FullStacks_AllocateNextStackAndRetainExistingIdentity()
    {
        using var inventory = new Inventory(Catalog);
        inventory.Add(Repair, 1280);
        var before = inventory.Snapshot;
        inventory.Add(Repair, 1);
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots.Take(128), Is.EqualTo(before.Slots));
        Assert.That(inventory.Snapshot.Slots[128]!.Count, Is.EqualTo(1));
        Assert.That(inventory.Snapshot.Slots[128]!.Id, Is.Not.EqualTo(before.Slots[0]!.Id));
    }

    [Test]
    public void LargeStackQuantities_UseLongArithmetic()
    {
        using var inventory = new Inventory(Catalog);
        var quantity = (long)int.MaxValue * 2 + 3;
        inventory.Add(Resource, quantity);
        Assert.That(inventory.Snapshot.Occupied.Select(entry => entry.Entity.Count),
            Is.EqualTo(new[] { int.MaxValue, int.MaxValue, 3 }));
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count), Is.EqualTo(quantity));
        inventory.Add(Resource, int.MaxValue);
        Assert.That(inventory.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count),
            Is.EqualTo(quantity + int.MaxValue));
    }

    [TestCase(-1, InventoryResult.NegativeQuantity)]
    [TestCase(0, InventoryResult.NoChange)]
    [TestCase(long.MaxValue, InventoryResult.CapacityExceeded)]
    [TestCase((long)int.MaxValue, InventoryResult.CapacityExceeded)]
    public void InvalidOrNoopAdd_PreservesExactSnapshotAndPublishesNothing(long quantity, InventoryResult expected)
    {
        using var inventory = new Inventory(Catalog);
        inventory.Add(Repair, 7);
        var before = inventory.Snapshot;
        var notifications = 0;
        inventory.Changed += _ => notifications++;
        Assert.That(inventory.Add(Engine, quantity), Is.EqualTo(expected));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        Assert.That(notifications, Is.Zero);
        Assert.That(inventory.Add(Resource, long.MaxValue), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(inventory.Add(Repair, long.MaxValue), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity()
    {
        using var inventory = new Inventory(Catalog);
        var before = inventory.Snapshot;
        Assert.That(inventory.Add(default, 1), Is.EqualTo(InventoryResult.UnknownDefinition));
        Assert.That(inventory.Add(new ItemDefinitionId("missing"), 0), Is.EqualTo(InventoryResult.UnknownDefinition));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void RemovalThroughInternalPlan_LeavesHolesAndNeverShrinks()
    {
        using var inventory = new Inventory(Catalog);
        inventory.Add(Engine, 129);
        var slots = inventory.Snapshot.Slots.ToArray();
        var retained = slots[128];
        slots[0] = null;
        inventory.Install(inventory.PrepareChange(slots));
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots[128], Is.SameAs(retained));
        Assert.That(inventory.Snapshot.EmptyAddresses[0], Is.Zero);
        inventory.Add(Engine, 1);
        Assert.That(inventory.Capacity, Is.EqualTo(192));
        Assert.That(inventory.Snapshot.Slots[0], Is.Not.Null);
        Assert.That(inventory.Snapshot.Slots[128], Is.SameAs(retained));
    }

    [Test]
    public void SnapshotsAndPreparedPlans_DoNotExposeWritableStorage()
    {
        using var inventory = new Inventory(Catalog);
        var slots = inventory.Snapshot.Slots.ToArray();
        slots[2] = Catalog.CreateEntity(Engine);
        var plan = inventory.PrepareChange(slots);
        slots[2] = null;
        inventory.Install(plan);
        Assert.That(inventory.Snapshot.Slots[2], Is.Not.Null);
        Assert.Throws<NotSupportedException>(() => ((IList<ItemEntity?>)inventory.Snapshot.Slots)[2] = null);
        Assert.Throws<NotSupportedException>(() => ((IList<InventoryEntry>)inventory.Snapshot.Occupied).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)inventory.Snapshot.EmptyAddresses).Clear());
    }

    [Test]
    public void InternalPlans_RejectDuplicatesStalePlansOtherOwnersAndNoops()
    {
        using var inventory = new Inventory(Catalog);
        using var other = new Inventory(Catalog);
        var slots = inventory.Snapshot.Slots.ToArray();
        var noop = inventory.PrepareChange(slots);
        Assert.That(inventory.Install(noop), Is.EqualTo(InventoryResult.NoChange));
        slots[0] = Catalog.CreateEntity(Engine);
        var plan = inventory.PrepareChange(slots);
        Assert.That(other.Install(plan), Is.EqualTo(InventoryResult.StaleRevision));
        Assert.That(inventory.Install(plan), Is.EqualTo(InventoryResult.Success));
        Assert.That(inventory.Install(plan), Is.EqualTo(InventoryResult.StaleRevision));
        Assert.That(inventory.Install(noop), Is.EqualTo(InventoryResult.StaleRevision));
        slots[1] = slots[0];
        Assert.Throws<ArgumentException>(() => inventory.PrepareChange(slots));
        Assert.Throws<ArgumentException>(() => inventory.PrepareChange(new ItemEntity?[64]));
    }

    [Test]
    public void Notifications_IsolateFailuresAndRejectReentrantMutation()
    {
        var errors = new List<Exception>();
        using var inventory = new Inventory(Catalog, exception =>
        {
            errors.Add(exception);
            throw new InvalidOperationException("Logging also failed");
        });
        var reentrant = InventoryResult.Success;
        var observed = new List<InventorySnapshot>();
        inventory.Changed += _ => throw new InvalidOperationException("Observer failed");
        inventory.Changed += snapshot =>
        {
            reentrant = inventory.Add(Engine, 1);
            observed.Add(snapshot);
        };
        Assert.That(inventory.Add(Engine, 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(observed.Single(), Is.SameAs(inventory.Snapshot));
        Assert.That(reentrant, Is.EqualTo(InventoryResult.PublicationInProgress));
        Assert.That(inventory.Revision, Is.EqualTo(1));
        Assert.That(inventory.Add(Engine, 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(inventory.Revision, Is.EqualTo(2));
    }

    [Test]
    public void Disposal_RevokesMutationsAndNewGameHasIndependentEmptyStorage()
    {
        var inventory = new Inventory(Catalog);
        inventory.Add(Engine, 129);
        var before = inventory.Snapshot;
        inventory.Dispose();
        inventory.Dispose();
        Assert.That(inventory.Add(Engine, 1), Is.EqualTo(InventoryResult.Disposed));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        using var nextGame = new Inventory(Catalog);
        Assert.That(nextGame.Capacity, Is.EqualTo(128));
        Assert.That(nextGame.Snapshot.Occupied, Is.Empty);
        Assert.That(nextGame.Revision, Is.Zero);
    }
}
