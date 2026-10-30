using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.EquipmentSystem;
using ItemLocation = sMPGWM.Scripts.ItemSystem.ItemLocation;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
[Category("Godot")]
public class ItemSystemTests
{
    private const string Engine = "mock_standard_engine";
    private const string Consumable = "mock_standard_consumable";

    [Test]
    public void ContainerSubscriberFailure_PropagatesAndResetsPublicationGuard()
    {
        var items = new ItemRequestService();
        var container = items.CreateContainer(2);
        var failure = new InvalidOperationException("Broken container subscriber");
        var laterCalled = false;
        container.Changed += () => throw failure;
        container.Changed += () => laterCalled = true;

        Assert.That(Assert.Throws<InvalidOperationException>(() => items.CreateItems(container, Engine, 1)), Is.SameAs(failure));
        Assert.That(laterCalled, Is.False);
        Assert.That(items.Publishing, Is.False);
        var id = container.Slots[0]!.Value;
        Assert.That(items.GetLocation(id), Is.EqualTo(new ItemLocation(container.Id, 0)));
    }

    [Test]
    public void InventoryAndChest_MoveTheSameInstanceAndUpdateItsLocation()
    {
        var items = new ItemRequestService();
        using var inventory = new InventoryController(items);
        var chest = items.CreateContainer(2);
        items.CreateItems(inventory.Container, Engine, 1);
        var item = inventory.Snapshot.Slots[0]!;
        var source = items.GetLocation(item.Id);
        var destination = new ItemLocation(chest.Id, 1);

        Assert.That(items.TransferItem(item.Id, source, destination, inventory.Revision, chest.Revision), Is.EqualTo(TransferResult.Success));
        Assert.That(inventory.Snapshot.Slots[0], Is.Null);
        Assert.That(chest.Slots[1], Is.EqualTo(item.Id));
        Assert.That(items.GetItem(item.Id), Is.SameAs(item));
        Assert.That(items.GetLocation(item.Id), Is.EqualTo(destination));
    }

    [Test]
    public void FixedCapacityCreationFailure_LeavesExistingStateUntouched()
    {
        var items = new ItemRequestService();
        var chest = items.CreateContainer(1);
        items.CreateItems(chest, Consumable, 7);
        var id = chest.Slots[0]!.Value;
        var item = items.GetItem(id);
        var revision = chest.Revision;
        Assert.That(items.CreateItems(chest, Consumable, 4), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(chest.Revision, Is.EqualTo(revision));
        Assert.That(items.GetItem(id), Is.SameAs(item));
        Assert.That(items.GetLocation(id), Is.EqualTo(new ItemLocation(chest.Id, 0)));
    }

    [Test]
    public void Swap_ValidatesBothDestinationsBeforeCommitting()
    {
        var items = new ItemRequestService();
        var first = items.CreateContainer(1, accepts: (_, definition, _) => definition.Id == Engine);
        var second = items.CreateContainer(1);
        items.CreateItems(first, Engine, 1);
        items.CreateItems(second, Consumable, 1);
        var id = first.Slots[0]!.Value;
        var other = second.Slots[0]!.Value;
        Assert.That(items.TransferItem(id, items.GetLocation(id), new(second.Id, 0),
            first.Revision, second.Revision, other), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(first.Slots[0], Is.EqualTo(id));
        Assert.That(second.Slots[0], Is.EqualTo(other));
        Assert.That(items.GetLocation(id), Is.EqualTo(new ItemLocation(first.Id, 0)));
    }

    [Test]
    public void Observers_SeeBothCommittedLocationsAndCannotMutateAnotherContainer()
    {
        var items = new ItemRequestService();
        using var first = new InventoryController(items);
        using var second = new InventoryController(items);
        items.CreateItems(first.Container, Engine, 1);
        var id = first.Snapshot.Slots[0]!.Id;
        var destination = new ItemLocation(second.Container.Id, 0);
        var observed = false;
        first.Changed += _ =>
        {
            Assert.That(second.Snapshot.Slots[0]!.Id, Is.EqualTo(id));
            Assert.That(items.GetLocation(id), Is.EqualTo(destination));
            Assert.That(items.CreateItems(second.Container, Engine, 1), Is.EqualTo(InventoryResult.PublicationInProgress));
            observed = true;
        };
        Assert.That(items.TransferItem(id, items.GetLocation(id), destination, first.Revision, second.Revision),
            Is.EqualTo(TransferResult.Success));
        Assert.That(observed, Is.True);
    }

    [Test]
    public void SecondRequestFromOldSource_CannotMoveAnAlreadyTransferredItem()
    {
        var items = new ItemRequestService();
        var source = items.CreateContainer(1);
        var first = items.CreateContainer(1);
        var second = items.CreateContainer(1);
        items.CreateItems(source, Engine, 1);
        var id = source.Slots[0]!.Value;
        var oldLocation = items.GetLocation(id);
        var oldRevision = source.Revision;
        items.TransferItem(id, oldLocation, new(first.Id, 0), oldRevision, first.Revision);
        Assert.That(items.TransferItem(id, oldLocation, new(second.Id, 0), oldRevision, second.Revision),
            Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(items.TransferItem(id, oldLocation, new(second.Id, 0), source.Revision, second.Revision),
            Is.EqualTo(TransferResult.StaleEntity));
        Assert.That(second.Slots[0], Is.Null);
        Assert.That(items.GetLocation(id), Is.EqualTo(new ItemLocation(first.Id, 0)));
    }
}
