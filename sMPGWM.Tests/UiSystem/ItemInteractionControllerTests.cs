using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class ItemInteractionControllerTests
{
    private InventoryImpl _inventory = null!;
    private EquipmentImpl _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemInteractionController _controller = null!;
    private ItemCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(new ItemDefinitionId("engine"), "Engine", ItemCategories.Engine),
            new ItemDefinition(new ItemDefinitionId("potion"), "Potion", ItemCategories.Consumable, maximumStackSize: 10)
        });
        _inventory = new InventoryImpl(_catalog);
        _equipment = new EquipmentImpl(_catalog);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _controller = new ItemInteractionController(_transfers);
        _controller.SetSession(_transfers.OpenEditSession());
    }

    [TearDown]
    public void TearDown()
    {
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }

    private void Seed(params (string Definition, int Count)[] entries)
    {
        var slots = _inventory.Snapshot.Slots.ToArray();
        for (var index = 0; index < entries.Length; index++)
            slots[index] = _catalog.CreateEntity(new ItemDefinitionId(entries[index].Definition), entries[index].Count);
        _inventory.Install(_inventory.PrepareChange(slots));
    }

    private ItemPointerBinding Bag(int address) => new(ItemLocation.Inventory(address),
        _inventory.Snapshot.Slots[address], _inventory.Revision);
    private ItemPointerBinding Slot(string key) => new(ItemLocation.Equipment(key),
        _equipment.Snapshot.Slots[_equipment.FindSlot(key)], _equipment.Revision);
    private void Click(ItemPointerBinding target, long mapping = 0) =>
        _controller.Click(target, _inventory.Revision, _equipment.Revision, mapping);

    [Test]
    public void PickupValidateAndEquip_PreserveSourceUntilCommit()
    {
        Seed(("engine", 1));
        var source = Bag(0);
        var before = _inventory.Snapshot;
        Click(source);
        for (var index = 0; index < 3; index++)
            Assert.That(_controller.Validate(Slot("engine")), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Click(Slot("engine"));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_equipment.Snapshot.Slots[0], Is.SameAs(source.Entity));
        Assert.That(_inventory.Snapshot.Slots[0], Is.Null);
        Click(Slot("engine"));
        Click(Bag(10));
        Assert.That(_inventory.Snapshot.Slots[10], Is.SameAs(source.Entity));
    }

    [Test]
    public void EmptyPickupAndSourceClickAndExplicitCancel_DoNotMutate()
    {
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
        Seed(("engine", 1));
        var before = _inventory.Snapshot;
        Click(Bag(0));
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
        Click(Bag(0));
        _controller.Cancel();
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void InvalidTargetRetainsCarrying_AndNextValidTargetWorks()
    {
        Seed(("engine", 1));
        var before = _inventory.Snapshot;
        Click(Bag(0));
        Click(Slot("hull"));
        Assert.That(_controller.Feedback, Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(_controller.IsCarrying, Is.True);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Click(Slot("engine"));
        Assert.That(_controller.IsCarrying, Is.False);
    }

    [TestCase(8, 5, false)]
    [TestCase(2, 0, false)]
    [TestCase(10, 7, true)]
    public void MergeEndsCarryingExceptFullStack(int destinationCount, int remaining, bool carrying)
    {
        Seed(("potion", 7), ("potion", destinationCount));
        var source = Bag(0).Entity!;
        var destination = Bag(1).Entity!;
        Click(Bag(0));
        Click(Bag(1));
        Assert.That(_controller.IsCarrying, Is.EqualTo(carrying));
        Assert.That(_inventory.Snapshot.Slots[0]?.Count ?? 0, Is.EqualTo(remaining));
        Assert.That(_inventory.Snapshot.Slots[1]!.Id, Is.EqualTo(destination.Id));
        if (remaining > 0) Assert.That(_inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(source.Id));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ChangedRevisionOrMapping_CancelsWithoutPickingUpTarget(bool revision)
    {
        Seed(("engine", 1), ("engine", 1));
        Click(Bag(0));
        if (revision) _inventory.Add(new ItemDefinitionId("engine"), 1);
        var before = _inventory.Snapshot;
        Click(Bag(1), revision ? 0 : 1);
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void RevokedSessionAndStaleDisplayedTarget_CancelWithoutMutation()
    {
        Seed(("engine", 1));
        Click(Bag(0));
        _transfers.RevokeEditSession();
        Click(Bag(2));
        Assert.That(_controller.IsCarrying, Is.False);
        _controller.SetSession(_transfers.OpenEditSession());
        Click(Bag(0));
        var stale = Bag(2) with { Revision = -1 };
        Click(stale);
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot.Slots[0], Is.Not.Null);
    }

    [Test]
    public void ReopenSessionAndEquipmentRefresh_ClearPendingSource()
    {
        Seed(("engine", 1));
        Click(Bag(0));
        _controller.SetSession(_transfers.OpenEditSession());
        Assert.That(_controller.IsCarrying, Is.False);
        Click(Bag(0));
        _controller.Refresh(_inventory.Revision, _equipment.Revision + 1, 0);
        Assert.That(_controller.IsCarrying, Is.False);
        _controller.SetSession(Guid.Empty);
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
    }
}
