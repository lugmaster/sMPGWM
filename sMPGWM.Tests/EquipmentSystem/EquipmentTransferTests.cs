using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.EquipmentSystem;

[TestFixture]
public class EquipmentTransferTests
{
    private ItemCatalog _catalog = null!;
    private InventoryImpl _inventoryImpl = null!;
    private EquipmentImpl _equipmentImpl = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private Guid _session;

    [SetUp]
    public void SetUp()
    {
        var categories = ItemCategories.CreateDefault();
        var definitions = new List<ItemDefinition>();
        foreach (var category in categories.All)
            definitions.Add(new ItemDefinition(new ItemDefinitionId(category.Id.Value), category.DisplayName,
                category.Id, maximumStackSize: category.CanStack ? 10 : null));
        _catalog = new ItemCatalog(categories, definitions);
        _inventoryImpl = new InventoryImpl(_catalog);
        _equipmentImpl = new EquipmentImpl(_catalog);
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
            slots[entry.Address] = _catalog.CreateEntity(new ItemDefinitionId(entry.Definition), entry.Count);
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

    private static IEnumerable<TestCaseData> CompatibilityCases()
    {
        var equipment = new EquipmentImpl(new ItemCatalog(ItemCategories.CreateDefault(), Array.Empty<ItemDefinition>()));
        foreach (var category in ItemCategories.CreateDefault().All)
        foreach (var slot in equipment.Definitions)
            yield return new TestCaseData(category.Id.Value, slot.Key, category.Id == slot.AcceptedCategory);
        equipment.Dispose();
    }

    [TestCaseSource(nameof(CompatibilityCases))]
    public void AllCategorySlotCombinations_UseCategoryIdentity(string category, string slot, bool accepted)
    {
        Seed((0, category, 1));
        var inventoryBefore = _inventoryImpl.Snapshot;
        var equipmentBefore = _equipmentImpl.Snapshot;
        Assert.That(Move(ItemLocation.Inventory(0), ItemLocation.Equipment(slot)),
            Is.EqualTo(accepted ? TransferResult.Success : TransferResult.IncompatiblePlacement));
        if (accepted)
        {
            Assert.That(_equipmentImpl.Snapshot.Slots[_equipmentImpl.FindSlot(slot)], Is.SameAs(inventoryBefore.Slots[0]));
            Assert.That(_inventoryImpl.Snapshot.Slots[0], Is.Null);
        }
        else
        {
            Assert.That(_inventoryImpl.Snapshot, Is.SameAs(inventoryBefore));
            Assert.That(_equipmentImpl.Snapshot, Is.SameAs(equipmentBefore));
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
        Seed((0, "light_weapon", 1), (1, "light_weapon", 1), (2, "heavy_weapon", 1), (3, "hull", 1));
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
        Seed((0, "consumable", sourceCount), (1, "consumable", destinationCount));
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
        Seed((0, "consumable", 7), (1, "resource", 8));
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
        Seed((0, "engine", 1));
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
        Seed((0, "engine", 1));
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
        Seed((0, "engine", 1));
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
            Assert.That(_inventoryImpl.Add(new ItemDefinitionId("engine"), 1), Is.EqualTo(InventoryResult.PublicationInProgress));
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
        Assert.That(_inventoryImpl.Add(new ItemDefinitionId("engine"), 1), Is.EqualTo(InventoryResult.Success));
    }

    [Test]
    public void Replacement_SwapsBothOwnersAndStaleEmptyTargetCannotBeOverwritten()
    {
        Seed((0, "engine", 1), (1, "engine", 1));
        Move(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        var before = _equipmentImpl.Snapshot.Slots[0];
        var replacement = _inventoryImpl.Snapshot.Slots[1];
        var stale = Request(ItemLocation.Inventory(1), ItemLocation.Inventory(0));
        Assert.That(Move(ItemLocation.Inventory(1), ItemLocation.Equipment("engine")), Is.EqualTo(TransferResult.Success));
        Assert.That(_equipmentImpl.Snapshot.Slots[0], Is.SameAs(replacement));
        Assert.That(_inventoryImpl.Snapshot.Slots[1], Is.SameAs(before));
        _inventoryImpl.Add(new ItemDefinitionId("hull"), 1);
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
        using var inventory = new InventoryImpl(_catalog, _ => { errors++; throw new Exception("logging"); });
        using var equipment = new EquipmentImpl(_catalog, _ => { errors++; throw new Exception("logging"); });
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
        inventory.Add(new ItemDefinitionId("engine"), 1);
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

    [Test]
    public void FullInventory_RejectsIllegalSwapWithoutGrowingOrAutoRouting()
    {
        _inventoryImpl.Add(new ItemDefinitionId("engine"), 128);
        Move(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        _inventoryImpl.Add(new ItemDefinitionId("hull"), 1);
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
        Seed((0, "engine", 1));
        var request = Request(ItemLocation.Inventory(0), ItemLocation.Equipment("engine"));
        if (owner == "inventoryImpl") _inventoryImpl.Dispose();
        if (owner == "equipmentImpl") _equipmentImpl.Dispose();
        if (owner == "coordinator") _transfers.Dispose();
        Assert.That(_transfers.Transfer(request), Is.EqualTo(TransferResult.Disposed));
        using var nextInventory = new InventoryImpl(_catalog);
        using var nextEquipment = new EquipmentImpl(_catalog);
        using var nextTransfers = new InventoryEquipmentTransferService(nextInventory, nextEquipment);
        nextTransfers.OpenEditSession();
        Assert.That(nextTransfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(nextInventory.Snapshot.Occupied, Is.Empty);
        Assert.That(nextEquipment.Snapshot.Slots, Has.All.Null);
    }
}
