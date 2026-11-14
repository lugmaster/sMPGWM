using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Tests.ItemSystem;

[Category("Godot")]
public sealed class ItemRestorationTests
{
    [Test]
    public void RestorePreservesIdentityRevisionLocationsAndReservesFutureIds()
    {
        var definition = ItemRegistry.GetById("mock_standard_engine");
        var highest = checked((long)ItemEntityId.Create().Value + 1000);
        var firstId = new ItemContainerId((ulong)highest - 2);
        var secondId = new ItemContainerId((ulong)highest - 1);
        var itemId = ItemEntityId.FromNetwork(highest);
        var store = new ItemStore();
        var first = store.CreateContainer(2);
        var second = store.CreateContainer(2);
        var published = 0;
        Action observer = () =>
        {
            Assert.That(first.Revision, Is.EqualTo(17));
            Assert.That(second.Revision, Is.EqualTo(9));
            Assert.That(store.GetLocation(itemId), Is.EqualTo(new ItemLocation(firstId, 1)));
            published++;
        };
        first.Changed += observer;
        store.Restore(new[]
        {
            new ItemContainerRestoration(first, new(firstId, 17, 0, new ItemEntity?[] { null, new(itemId, definition, 1) })),
            new ItemContainerRestoration(second, new(secondId, 9, 0, new ItemEntity?[2]))
        });
        Assert.That(first.Id, Is.EqualTo(firstId));
        Assert.That(second.Id, Is.EqualTo(secondId));
        Assert.That(first.Slots[1], Is.EqualTo(itemId));
        Assert.That(published, Is.EqualTo(1));
        Assert.That(ItemEntityId.Create().Value, Is.GreaterThan((ulong)highest));
        var snapshot = store.Capture(first);
        Assert.That(snapshot.Revision, Is.EqualTo(17));
        Assert.That(snapshot.Slots[1]!.Id, Is.EqualTo(itemId));
        Assert.That(snapshot.Slots[1]!.Count, Is.EqualTo(1));
        first.Changed -= observer;
        Assert.That(store.TransferItem(itemId, new(firstId, 1), new(secondId, 0), 17, 9), Is.EqualTo(TransferResult.Success));
        Assert.That(snapshot.Slots[1]!.Id, Is.EqualTo(itemId));
    }

    [Test]
    public void RestoreCanSwapTransientContainerIdsBeforePublication()
    {
        var store = new ItemStore();
        var first = store.CreateContainer(1);
        var second = store.CreateContainer(1);
        var firstId = first.Id;
        var secondId = second.Id;
        store.Restore(new[]
        {
            new ItemContainerRestoration(first, new(secondId, 0, 0, new ItemEntity?[1])),
            new ItemContainerRestoration(second, new(firstId, 0, 0, new ItemEntity?[1]))
        });
        Assert.That(first.Id, Is.EqualTo(secondId));
        Assert.That(second.Id, Is.EqualTo(firstId));
        Assert.That(store.CreateItems(first, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(store.GetLocation(first.Slots[0]!.Value), Is.EqualTo(new ItemLocation(secondId, 0)));
    }

    [TestCase("duplicate")]
    [TestCase("placement")]
    [TestCase("prepare")]
    public void InvalidRestorePreservesEveryTargetAndRetainedItem(string reason)
    {
        var definition = ItemRegistry.GetById("mock_standard_engine");
        var store = new ItemStore();
        var retained = store.CreateContainer(1);
        Assert.That(store.CreateItems(retained, definition.Id, 1), Is.EqualTo(InventoryResult.Success));
        var retainedItem = retained.Slots[0]!.Value;
        var first = store.CreateContainer(1);
        var second = store.CreateContainer(1, accepts: (_, _, _) => reason != "placement");
        var firstId = first.Id;
        var secondId = second.Id;
        var firstSlots = first.Slots;
        var secondSlots = second.Slots;
        var item = new ItemEntity(ItemEntityId.Create(), definition, 1);
        var other = reason == "duplicate" ? item : new ItemEntity(ItemEntityId.Create(), definition, 1);
        var installs = 0;
        var publications = 0;
        first.PrepareChange = _ => () => installs++;
        if (reason == "prepare") second.PrepareChange = _ => throw new InvalidOperationException("Rejected preparation");
        first.Changed += () => publications++;
        second.Changed += () => publications++;
        var entries = new[]
        {
            new ItemContainerRestoration(first, new(new ItemContainerId(RuntimeIds.Next()), 4, 0, new[] { item })),
            new ItemContainerRestoration(second, new(new ItemContainerId(RuntimeIds.Next()), 5, 0, new[] { other }))
        };
        if (reason == "prepare") Assert.Throws<InvalidOperationException>(() => store.Restore(entries));
        else Assert.Throws<ArgumentException>(() => store.Restore(entries));
        Assert.That(first.Id, Is.EqualTo(firstId));
        Assert.That(second.Id, Is.EqualTo(secondId));
        Assert.That(first.Slots, Is.SameAs(firstSlots));
        Assert.That(second.Slots, Is.SameAs(secondSlots));
        Assert.That(first.Revision, Is.Zero);
        Assert.That(second.Revision, Is.Zero);
        Assert.That(installs, Is.Zero);
        Assert.That(publications, Is.Zero);
        Assert.That(store.GetLocation(retainedItem), Is.EqualTo(new ItemLocation(retained.Id, 0)));
    }

    [Test]
    public void RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem()
    {
        var store = new ItemStore();
        using var inventory = new sMPGWM.Scripts.InventorySystem.InventoryController(store);
        using var equipment = new sMPGWM.Scripts.EquipmentSystem.EquipmentController(store);
        var oldInventory = inventory.Snapshot;
        var oldEquipment = equipment.Snapshot;
        var definition = ItemRegistry.GetById("mock_standard_engine");
        var inventoryItem = new ItemEntity(ItemEntityId.Create(), definition, 1);
        var equippedItem = new ItemEntity(ItemEntityId.Create(), definition, 1);
        var inventorySlots = new ItemEntity?[inventory.Capacity];
        var equipmentSlots = new ItemEntity?[equipment.Definitions.Count];
        inventorySlots[0] = inventoryItem;
        var slot = equipment.FindSlot("engine");
        equipmentSlots[slot] = equippedItem;
        var observations = 0;
        inventory.Changed += _ =>
        {
            Assert.That(equipment.Snapshot.Slots[slot]!.Id, Is.EqualTo(equippedItem.Id));
            observations++;
        };
        store.Restore(new[]
        {
            new ItemContainerRestoration(inventory.Container, new(inventory.Container.Id, 0, sMPGWM.Scripts.InventorySystem.InventoryController.GrowthChunk, inventorySlots)),
            new ItemContainerRestoration(equipment.Container, new(equipment.Container.Id, 0, 0, equipmentSlots))
        });
        Assert.That(observations, Is.EqualTo(1));
        Assert.That(inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(inventoryItem.Id));
        Assert.That(inventory.Snapshot, Is.Not.SameAs(oldInventory));
        Assert.That(equipment.Snapshot, Is.Not.SameAs(oldEquipment));
        Assert.That(oldInventory.Slots[0], Is.Null);
        Assert.That(oldEquipment.Slots[slot], Is.Null);
    }
}
