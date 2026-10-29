using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.EquipmentSystem;

[TestFixture]
[Category("Godot")]
public class EquipmentTransferTests
{
    private InventoryController _inventoryImpl = null!;
    private EquipmentController _equipmentImpl = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private Guid _session;

    [SetUp]
    public void SetUp()
    {
        _inventoryImpl = new InventoryController();
        _equipmentImpl = new EquipmentController();
        _transfers = new InventoryEquipmentTransferService(_inventoryImpl, _equipmentImpl);
        _session = _transfers.OpenEditSession();
    }

    [TearDown]
    public void TearDown()
    {
        _transfers.Dispose();
        _equipmentImpl.Dispose();
        _inventoryImpl.Dispose();
    }

    private void Seed(params (int Address, string Definition, int Count)[] entries)
    {
        var slots = _inventoryImpl.Snapshot.Slots.ToArray();
        foreach (var entry in entries)
            slots[entry.Address] = ItemRegistry.CreateEntity(entry.Definition, entry.Count);
        _inventoryImpl.Install(_inventoryImpl.PrepareChange(slots));
    }

    private TransferRequest Request(ItemLocation source, ItemLocation destination)
    {
        ItemEntity? At(ItemLocation location) => location.InventoryAddress is { } address
            ? _inventoryImpl.Snapshot.Slots[address]
            : _equipmentImpl.Snapshot.Slots[_equipmentImpl.FindSlot(location.EquipmentSlotKey!)];
        return new TransferRequest(_session, source, At(source)!.Id, destination, At(destination)?.Id,
            _inventoryImpl.Revision, _equipmentImpl.Revision);
    }

    private TransferResult Move(ItemLocation source, ItemLocation destination) => _transfers.Transfer(Request(source, destination));

    [Test]
    public void AllCategorySlotCombinations_UseCategoryIdentity()
    {
        foreach (var category in ItemRegistry.GetAllCategories())
        foreach (var slot in EquipmentSlotDefinition.Defaults)
        {
            using var inventory = new InventoryController();
            using var equipment = new EquipmentController();
            using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
            var definition = ItemRegistry.GetAll().First(d => d.CategoryName == category.DisplayName);
            inventory.Add(definition.Id, 1);
            var before = inventory.Snapshot;
            var equipmentBefore = equipment.Snapshot;
            var entity = before.Slots[0]!;
            var request = new TransferRequest(transfers.OpenEditSession(), ItemLocation.Inventory(0), entity.Id,
                ItemLocation.Equipment(slot.Key), null, inventory.Revision, equipment.Revision);
            var accepted = category.Id == slot.AcceptedCategory;
            Assert.That(transfers.Transfer(request), Is.EqualTo(accepted ? TransferResult.Success : TransferResult.IncompatiblePlacement),
                $"{category.Id} -> {slot.Key}");
            if (accepted)
            {
                Assert.That(equipment.Snapshot.Slots[equipment.FindSlot(slot.Key)], Is.SameAs(entity));
                Assert.That(inventory.Snapshot.Slots[0], Is.Null);
            }
            else
            {
                Assert.That(inventory.Snapshot, Is.SameAs(before));
                Assert.That(equipment.Snapshot, Is.SameAs(equipmentBefore));
            }
        }
    }

    [Test]
    public void SlotDefinitionsAndSnapshots_AreOrderedAndReadOnly()
    {
        Assert.That(_equipmentImpl.Definitions.Select(slot => slot.Key), Is.EqualTo(new[]
        {
            "engine", "hull", "shield_generator", "reactor", "light_weapon_1", "light_weapon_2",
            "heavy_weapon_1", "heavy_weapon_2"
        }));
        Assert.That(_equipmentImpl.Snapshot.Slots, Has.All.Null);
        Assert.Throws<NotSupportedException>(() => ((IList<EquipmentSlotDefinition>)_equipmentImpl.Definitions).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ItemEntity?>)_equipmentImpl.Snapshot.Slots)[0] = null);
    }

    [Test]
    public void EquipmentSwap_ValidatesReversePlacementAndPreservesIdentity()
    {
        Seed((0, "mock_standard_light_weapon", 1), (1, "mock_standard_light_weapon", 1), (2, "mock_standard_heavy_weapon", 1), (3, "mock_standard_hull", 1));
        Move(ItemLocation.Inventory(0), ItemLocation.Equipment("light_weapon_1"));
        Move(ItemLocation.Inventory(1), ItemLocation.Equipment("light_weapon_2"));
        Move(ItemLocation.Inventory(2), ItemLocation.Equipment("heavy_weapon_1"));
        var before = _equipmentImpl.Snapshot;
        var inventoryBefore = _inventoryImpl.Snapshot;
        Assert.That(Move(ItemLocation.Equipment("light_weapon_1"), ItemLocation.Equipment("heavy_weapon_1")),
            Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(Move(ItemLocation.Equipment("light_weapon_1"), ItemLocation.Inventory(3)),
            Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(_equipmentImpl.Snapshot, Is.SameAs(before));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(Move(ItemLocation.Equipment("light_weapon_1"), ItemLocation.Equipment("light_weapon_2")),
            Is.EqualTo(TransferResult.Success));
        Assert.That(_equipmentImpl.Snapshot.Slots[4], Is.SameAs(before.Slots[5]));
        Assert.That(_equipmentImpl.Snapshot.Slots[5], Is.SameAs(before.Slots[4]));
        Assert.That(_equipmentImpl.Revision, Is.EqualTo(before.Revision + 1));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(Move(ItemLocation.Equipment("light_weapon_1"), ItemLocation.Inventory(0)), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventoryImpl.Snapshot.Slots[0], Is.SameAs(before.Slots[5]));
        Assert.That(_inventoryImpl.Capacity, Is.EqualTo(128));
    }

    [TestCase(7, 8, 5, 10)]
    [TestCase(2, 8, 0, 10)]
    [TestCase(7, 10, 7, 10)]
    public void Merges_ConserveCountsAndExpectedIdentities(int sourceCount, int destinationCount, int remaining, int total)
    {
        Seed((0, "mock_standard_consumable", sourceCount), (1, "mock_standard_consumable", destinationCount));
        var before = _inventoryImpl.Snapshot;
        var result = Move(ItemLocation.Inventory(0), ItemLocation.Inventory(1));
        Assert.That(result, Is.EqualTo(destinationCount == 10 ? TransferResult.NoChange : TransferResult.Success));
        Assert.That(_inventoryImpl.Snapshot.Slots[1]!.Count, Is.EqualTo(total));
        Assert.That(_inventoryImpl.Snapshot.Slots[1]!.Id, Is.EqualTo(before.Slots[1]!.Id));
        Assert.That(_inventoryImpl.Snapshot.Slots[0]?.Count ?? 0, Is.EqualTo(remaining));
        if (remaining > 0) Assert.That(_inventoryImpl.Snapshot.Slots[0]!.Id, Is.EqualTo(before.Slots[0]!.Id));
        else Assert.That(_inventoryImpl.Snapshot.Occupied.Any(entry => entry.Entity.Id == before.Slots[0]!.Id), Is.False);
        Assert.That(_inventoryImpl.Snapshot.Occupied.Sum(entry => (long)entry.Entity.Count), Is.EqualTo(sourceCount + destinationCount));
        Assert.That(_inventoryImpl.Revision, Is.EqualTo(before.Revision + (destinationCount == 10 ? 0 : 1)));
    }

    [Test]
    public void DifferentDefinitions_SwapAndEmptyDestinationsMoveWithoutGrowing()
    {
        Seed((0, "mock_standard_consumable", 7), (1, "mock_standard_resource", 8));
        var before = _inventoryImpl.Snapshot;
        Assert.That(Move(ItemLocation.Inventory(0), ItemLocation.Inventory(1)), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventoryImpl.Snapshot.Slots[0], Is.SameAs(before.Slots[1]));
        Assert.That(_inventoryImpl.Snapshot.Slots[1], Is.SameAs(before.Slots[0]));
        Assert.That(Move(ItemLocation.Inventory(1), ItemLocation.Inventory(127)), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventoryImpl.Snapshot.Slots[127], Is.SameAs(before.Slots[0]));
        var request = Request(ItemLocation.Inventory(127), ItemLocation.Inventory(1));
        Assert.That(_transfers.Transfer(request with { Destination = ItemLocation.Inventory(128) }), Is.EqualTo(TransferResult.InvalidLocation));
        Assert.That(_inventoryImpl.Capacity, Is.EqualTo(128));
    }

    [Test]
    public void ValidationAndNoOps_DoNotPublishOrChangeSnapshots()
    {
        Seed((0, "mock_standard_engine", 1));
        var inventoryBefore = _inventoryImpl.Snapshot;
        var equipmentBefore = _equipmentImpl.Snapshot;
        var notifications = 0;
        _inventoryImpl.Changed += _ => notifications++;
        _equipmentImpl.Changed += _ => notifications++;
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        Assert.That(_transfers.Validate(request), Is.EqualTo(TransferResult.Success));
        Assert.That(_transfers.Validate(request), Is.EqualTo(TransferResult.Success));
        Assert.That(Move(ItemLocation.Inventory(0), ItemLocation.Inventory(0)), Is.EqualTo(TransferResult.NoChange));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(_equipmentImpl.Snapshot, Is.SameAs(equipmentBefore));
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void StaleRequestsAndRevokedSessions_AreRejectedWithoutMutation()
    {
        Seed((0, "mock_standard_engine", 1));
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        Assert.That(_transfers.Transfer(request with { SourceEntity = ItemEntityId.Create() }), Is.EqualTo(TransferResult.StaleEntity));
        Assert.That(_transfers.Transfer(request with { DestinationEntity = ItemEntityId.Create() }), Is.EqualTo(TransferResult.StaleEntity));
        Assert.That(_transfers.Transfer(request with { Source = default }), Is.EqualTo(TransferResult.InvalidLocation));
        Assert.That(_transfers.Transfer(request with { Destination = ItemLocation.Equipment("missing") }), Is.EqualTo(TransferResult.InvalidLocation));
        Assert.That(_transfers.Transfer(request with { EquipmentRevision = -1 }), Is.EqualTo(TransferResult.StaleRevision));
        _transfers.RevokeEditSession();
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession));
        _session = _transfers.OpenEditSession();
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession));
        request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.Success));
        var inventoryAfter = _inventoryImpl.Snapshot;
        var equipmentAfter = _equipmentImpl.Snapshot;
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryAfter));
        Assert.That(_equipmentImpl.Snapshot, Is.SameAs(equipmentAfter));
    }

    [Test]
    public void Notifications_ObserveAtomicStateAndBlockAllReentrantMutations()
    {
        Seed((0, "mock_standard_engine", 1));
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        var notifications = 0;
        var inventoryRevision = _inventoryImpl.Revision;
        void Observe()
        {
            notifications++;
            Assert.That(_inventoryImpl.Snapshot.Slots[0], Is.Null);
            Assert.That(_equipmentImpl.Snapshot.Slots[0]!.Id, Is.EqualTo(request.SourceEntity));
            Assert.That(_inventoryImpl.Revision, Is.EqualTo(inventoryRevision + 1));
            Assert.That(_equipmentImpl.Revision, Is.EqualTo(1));
            Assert.That(_inventoryImpl.Add("mock_standard_engine", 1), Is.EqualTo(InventoryResult.PublicationInProgress));
            Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.PublicationInProgress));
            using var otherCoordinator = new InventoryEquipmentTransferService(_inventoryImpl, _equipmentImpl);
            Assert.That(otherCoordinator.Transfer(request), Is.EqualTo(TransferResult.PublicationInProgress));
        }
        var failures = new List<Exception>();
        Action<InventorySnapshot> inventoryObserver = _ => { try { Observe(); } catch (Exception exception) { failures.Add(exception); } };
        _inventoryImpl.Changed += inventoryObserver;
        _inventoryImpl.Changed += _ => throw new InvalidOperationException("observer failure");
        _equipmentImpl.Changed += _ => { try { Observe(); } catch (Exception exception) { failures.Add(exception); } };
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.Success));
        Assert.That(failures, Is.Empty);
        Assert.That(notifications, Is.EqualTo(2));
        _inventoryImpl.Changed -= inventoryObserver;
        Assert.That(_inventoryImpl.Add("mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
    }

    [Test]
    public void Replacement_SwapsBothOwnersAndStaleEmptyTargetCannotBeOverwritten()
    {
        Seed((0, "mock_standard_engine", 1), (1, "mock_standard_engine", 1));
        Move(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        var before = _equipmentImpl.Snapshot.Slots[0];
        var replacement = _inventoryImpl.Snapshot.Slots[1];
        var stale = Request(ItemLocation.Inventory(1), ItemLocation.Inventory(0));
        Assert.That(Move(ItemLocation.Inventory(1), ItemLocation.Equipment("engine")), Is.EqualTo(TransferResult.Success));
        Assert.That(_equipmentImpl.Snapshot.Slots[0], Is.SameAs(replacement));
        Assert.That(_inventoryImpl.Snapshot.Slots[1], Is.SameAs(before));
        _inventoryImpl.Add("mock_standard_hull", 1);
        var inventoryBefore = _inventoryImpl.Snapshot;
        Assert.That(_transfers.Transfer(stale), Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(_transfers.Transfer(stale with { InventoryRevision = _inventoryImpl.Revision, SourceEntity = before!.Id }),
            Is.EqualTo(TransferResult.StaleEntity));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
    }

    [Test]
    public void ObserverAndLoggingFailures_DoNotStopRemainingNotifications()
    {
        var errors = 0;
        using var inventory = new InventoryController( _ => { errors++; throw new Exception("logging"); });
        using var equipment = new EquipmentController( _ => { errors++; throw new Exception("logging"); });
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
        inventory.Add("mock_standard_engine", 1);
        var request = new TransferRequest(transfers.OpenEditSession(), ItemLocation.Inventory(0),
            inventory.Snapshot.Slots[0]!.Id, ItemLocation.Equipment("engine"), null, inventory.Revision, equipment.Revision);
        var notifications = 0;
        inventory.Changed += _ => throw new Exception("inventoryImpl");
        inventory.Changed += _ => notifications++;
        equipment.Changed += _ => throw new Exception("equipmentImpl");
        equipment.Changed += _ => notifications++;
        Assert.That(transfers.Transfer(request), Is.EqualTo(TransferResult.Success));
        Assert.That(errors, Is.EqualTo(2));
        Assert.That(notifications, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    [NonParallelizable]
    public void EquipmentObserverFailure_WritesFallbackAndContinuesNotifications(bool reporterThrows)
    {
        var observerFailure = new InvalidOperationException("equipment subscriber failed");
        var reporterFailure = new InvalidOperationException("equipment reporter failed");
        using var equipment = new EquipmentController(
            reporterThrows ? _ => throw reporterFailure : null);
        var notified = false;
        equipment.Changed += _ => throw observerFailure;
        equipment.Changed += snapshot => notified = ReferenceEquals(snapshot, equipment.Snapshot);
        var originalError = Console.Error;
        using var output = new System.IO.StringWriter();
        try
        {
            Console.SetError(output);
            Assert.DoesNotThrow(equipment.NotifyObservers);
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.That(notified, Is.True);
        Assert.That(output.ToString(), Does.Contain(observerFailure.ToString()));
        if (reporterThrows)
            Assert.That(output.ToString(), Does.Contain(reporterFailure.ToString()));
    }

    [Test]
    public void FullInventory_RejectsIllegalSwapWithoutGrowingOrAutoRouting()
    {
        _inventoryImpl.Add("mock_standard_engine", 128);
        Move(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        _inventoryImpl.Add("mock_standard_hull", 1);
        var inventoryBefore = _inventoryImpl.Snapshot;
        var equipmentBefore = _equipmentImpl.Snapshot;
        Assert.That(Move(ItemLocation.Equipment("engine"), ItemLocation.Inventory(0)), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(_equipmentImpl.Snapshot, Is.SameAs(equipmentBefore));
    }

    [TestCase("inventoryImpl")]
    [TestCase("equipmentImpl")]
    [TestCase("coordinator")]
    public void DisposedOwners_RejectTransfersAndNextGameIsIndependent(string owner)
    {
        Seed((0, "mock_standard_engine", 1));
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        if (owner == "inventoryImpl") _inventoryImpl.Dispose();
        if (owner == "equipmentImpl") _equipmentImpl.Dispose();
        if (owner == "coordinator") _transfers.Dispose();
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.Disposed));
        using var nextInventory = new InventoryController();
        using var nextEquipment = new EquipmentController();
        using var nextTransfers = new InventoryEquipmentTransferService(nextInventory, nextEquipment);
        nextTransfers.OpenEditSession();
        Assert.That(nextTransfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(nextInventory.Snapshot.Occupied, Is.Empty);
        Assert.That(nextEquipment.Snapshot.Slots, Has.All.Null);
    }
    [Test]
    public void HoverValidation_DoesNotAllocateOrPublishEvenForPartialMerges()
    {
        Seed((0, "mock_standard_consumable", 7), (1, "mock_standard_consumable", 8));
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Inventory(1));
        var before = _inventoryImpl.Snapshot;
        var notifications = 0;
        _inventoryImpl.Changed += _ => notifications++;
        for (var index = 0; index < 32; index++) _transfers.Validate(request);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var result = TransferResult.NoChange;
        for (var index = 0; index < 256; index++) result = _transfers.Validate(request);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Assert.That(result, Is.EqualTo(TransferResult.Success));
        Assert.That(allocated, Is.Zero);
        Assert.That(_inventoryImpl.Snapshot, Is.SameAs(before));
        Assert.That(notifications, Is.Zero);
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.Success));
        Assert.That(notifications, Is.EqualTo(1));
    }

}
