using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: RESTORE-ITEM-001 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
using sMPGWM.Scripts.UtilitySystem; // TEST-SUPPORT: RESTORE-ITEM-002 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem

namespace sMPGWM.Tests.ItemSystem; // TEST-SUPPORT: RESTORE-ITEM-004 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem

[Category("Godot")] // TEST-SUPPORT: RESTORE-ITEM-006 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
public sealed class ItemRestorationTests // TEST-SUPPORT: RESTORE-ITEM-007 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
{ // TEST-SUPPORT: RESTORE-ITEM-008 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds, sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication, sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    [Test] // TEST-SUPPORT: RESTORE-ITEM-009 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
    public void RestorePreservesIdentityRevisionLocationsAndReservesFutureIds() // TEST-SUPPORT: RESTORE-ITEM-010 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
    { // TEST-SUPPORT: RESTORE-ITEM-011 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var definition = ItemRegistry.GetById("mock_standard_engine"); // TEST-SUPPORT: RESTORE-ITEM-012 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var highest = checked((long)ItemEntityId.Create().Value + 1000); // TEST-SUPPORT: RESTORE-ITEM-013 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var firstId = new ItemContainerId((ulong)highest - 2); // TEST-SUPPORT: RESTORE-ITEM-014 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var secondId = new ItemContainerId((ulong)highest - 1); // TEST-SUPPORT: RESTORE-ITEM-015 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var itemId = ItemEntityId.FromNetwork(highest); // TEST-SUPPORT: RESTORE-ITEM-016 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var store = new ItemStore(); // TEST-SUPPORT: RESTORE-ITEM-017 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var first = store.CreateContainer(2); // TEST-SUPPORT: RESTORE-ITEM-018 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var second = store.CreateContainer(2); // TEST-SUPPORT: RESTORE-ITEM-019 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var published = 0; // TEST-SUPPORT: RESTORE-ITEM-020 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Action observer = () => // TEST-SUPPORT: RESTORE-ITEM-021 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        { // TEST-SUPPORT: RESTORE-ITEM-022 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            Assert.That(first.Revision, Is.EqualTo(17)); // TEST-SUPPORT: RESTORE-ITEM-023 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            Assert.That(second.Revision, Is.EqualTo(9)); // TEST-SUPPORT: RESTORE-ITEM-024 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            Assert.That(store.GetLocation(itemId), Is.EqualTo(new ItemLocation(firstId, 1))); // TEST-SUPPORT: RESTORE-ITEM-025 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            published++; // TEST-SUPPORT: RESTORE-ITEM-026 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        }; // TEST-SUPPORT: RESTORE-ITEM-027 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        first.Changed += observer; // TEST-SUPPORT: RESTORE-ITEM-028 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        store.Restore(new[] // TEST-SUPPORT: RESTORE-ITEM-029 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        { // TEST-SUPPORT: RESTORE-ITEM-030 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            new ItemContainerRestoration(first, new(firstId, 17, 0, new ItemEntity?[] { null, new(itemId, definition, 1) })), // TEST-SUPPORT: RESTORE-ITEM-031 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
            new ItemContainerRestoration(second, new(secondId, 9, 0, new ItemEntity?[2])) // TEST-SUPPORT: RESTORE-ITEM-032 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        }); // TEST-SUPPORT: RESTORE-ITEM-033 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(first.Id, Is.EqualTo(firstId)); // TEST-SUPPORT: RESTORE-ITEM-034 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(second.Id, Is.EqualTo(secondId)); // TEST-SUPPORT: RESTORE-ITEM-035 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(first.Slots[1], Is.EqualTo(itemId)); // TEST-SUPPORT: RESTORE-ITEM-036 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(published, Is.EqualTo(1)); // TEST-SUPPORT: RESTORE-ITEM-037 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(ItemEntityId.Create().Value, Is.GreaterThan((ulong)highest)); // TEST-SUPPORT: RESTORE-ITEM-038 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        var snapshot = store.Capture(first); // TEST-SUPPORT: RESTORE-ITEM-039 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(snapshot.Revision, Is.EqualTo(17)); // TEST-SUPPORT: RESTORE-ITEM-040 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(snapshot.Slots[1]!.Id, Is.EqualTo(itemId)); // TEST-SUPPORT: RESTORE-ITEM-041 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(snapshot.Slots[1]!.Count, Is.EqualTo(1)); // TEST-SUPPORT: RESTORE-ITEM-042 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        first.Changed -= observer; // TEST-SUPPORT: RESTORE-ITEM-043 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(store.TransferItem(itemId, new(firstId, 1), new(secondId, 0), 17, 9), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: RESTORE-ITEM-044 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
        Assert.That(snapshot.Slots[1]!.Id, Is.EqualTo(itemId)); // TEST-SUPPORT: RESTORE-ITEM-045 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds
    } // TEST-SUPPORT: RESTORE-ITEM-046 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestorePreservesIdentityRevisionLocationsAndReservesFutureIds

    [Test] // TEST-SUPPORT: RESTORE-ITEM-048 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
    public void RestoreCanSwapTransientContainerIdsBeforePublication() // TEST-SUPPORT: RESTORE-ITEM-049 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
    { // TEST-SUPPORT: RESTORE-ITEM-050 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        var store = new ItemStore(); // TEST-SUPPORT: RESTORE-ITEM-051 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        var first = store.CreateContainer(1); // TEST-SUPPORT: RESTORE-ITEM-052 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        var second = store.CreateContainer(1); // TEST-SUPPORT: RESTORE-ITEM-053 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        var firstId = first.Id; // TEST-SUPPORT: RESTORE-ITEM-054 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        var secondId = second.Id; // TEST-SUPPORT: RESTORE-ITEM-055 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        store.Restore(new[] // TEST-SUPPORT: RESTORE-ITEM-056 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        { // TEST-SUPPORT: RESTORE-ITEM-057 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
            new ItemContainerRestoration(first, new(secondId, 0, 0, new ItemEntity?[1])), // TEST-SUPPORT: RESTORE-ITEM-058 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
            new ItemContainerRestoration(second, new(firstId, 0, 0, new ItemEntity?[1])) // TEST-SUPPORT: RESTORE-ITEM-059 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        }); // TEST-SUPPORT: RESTORE-ITEM-060 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        Assert.That(first.Id, Is.EqualTo(secondId)); // TEST-SUPPORT: RESTORE-ITEM-061 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        Assert.That(second.Id, Is.EqualTo(firstId)); // TEST-SUPPORT: RESTORE-ITEM-062 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        Assert.That(store.CreateItems(first, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: RESTORE-ITEM-063 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
        Assert.That(store.GetLocation(first.Slots[0]!.Value), Is.EqualTo(new ItemLocation(secondId, 0))); // TEST-SUPPORT: RESTORE-ITEM-064 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication
    } // TEST-SUPPORT: RESTORE-ITEM-065 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RestoreCanSwapTransientContainerIdsBeforePublication

    [TestCase("duplicate")] // TEST-SUPPORT: RESTORE-ITEM-067 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    [TestCase("placement")] // TEST-SUPPORT: RESTORE-ITEM-068 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    [TestCase("prepare")] // TEST-SUPPORT: RESTORE-ITEM-069 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    public void InvalidRestorePreservesEveryTargetAndRetainedItem(string reason) // TEST-SUPPORT: RESTORE-ITEM-070 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    { // TEST-SUPPORT: RESTORE-ITEM-071 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var definition = ItemRegistry.GetById("mock_standard_engine"); // TEST-SUPPORT: RESTORE-ITEM-072 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var store = new ItemStore(); // TEST-SUPPORT: RESTORE-ITEM-073 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var retained = store.CreateContainer(1); // TEST-SUPPORT: RESTORE-ITEM-074 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(store.CreateItems(retained, definition.Id, 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: RESTORE-ITEM-075 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var retainedItem = retained.Slots[0]!.Value; // TEST-SUPPORT: RESTORE-ITEM-076 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var first = store.CreateContainer(1); // TEST-SUPPORT: RESTORE-ITEM-077 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var second = store.CreateContainer(1, accepts: (_, _, _) => reason != "placement"); // TEST-SUPPORT: RESTORE-ITEM-078 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var firstId = first.Id; // TEST-SUPPORT: RESTORE-ITEM-079 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var secondId = second.Id; // TEST-SUPPORT: RESTORE-ITEM-080 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var firstSlots = first.Slots; // TEST-SUPPORT: RESTORE-ITEM-081 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var secondSlots = second.Slots; // TEST-SUPPORT: RESTORE-ITEM-082 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var item = new ItemEntity(ItemEntityId.Create(), definition, 1); // TEST-SUPPORT: RESTORE-ITEM-083 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var other = reason == "duplicate" ? item : new ItemEntity(ItemEntityId.Create(), definition, 1); // TEST-SUPPORT: RESTORE-ITEM-084 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var installs = 0; // TEST-SUPPORT: RESTORE-ITEM-085 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var publications = 0; // TEST-SUPPORT: RESTORE-ITEM-086 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        first.PrepareChange = _ => () => installs++; // TEST-SUPPORT: RESTORE-ITEM-087 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        if (reason == "prepare") second.PrepareChange = _ => throw new InvalidOperationException("Rejected preparation"); // TEST-SUPPORT: RESTORE-ITEM-088 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        first.Changed += () => publications++; // TEST-SUPPORT: RESTORE-ITEM-089 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        second.Changed += () => publications++; // TEST-SUPPORT: RESTORE-ITEM-090 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        var entries = new[] // TEST-SUPPORT: RESTORE-ITEM-091 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        { // TEST-SUPPORT: RESTORE-ITEM-092 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
            new ItemContainerRestoration(first, new(new ItemContainerId(RuntimeIds.Next()), 4, 0, new[] { item })), // TEST-SUPPORT: RESTORE-ITEM-093 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
            new ItemContainerRestoration(second, new(new ItemContainerId(RuntimeIds.Next()), 5, 0, new[] { other })) // TEST-SUPPORT: RESTORE-ITEM-094 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        }; // TEST-SUPPORT: RESTORE-ITEM-095 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        if (reason == "prepare") Assert.Throws<InvalidOperationException>(() => store.Restore(entries)); // TEST-SUPPORT: RESTORE-ITEM-096 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        else Assert.Throws<ArgumentException>(() => store.Restore(entries)); // TEST-SUPPORT: RESTORE-ITEM-096B | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(first.Id, Is.EqualTo(firstId)); // TEST-SUPPORT: RESTORE-ITEM-097 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(second.Id, Is.EqualTo(secondId)); // TEST-SUPPORT: RESTORE-ITEM-098 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(first.Slots, Is.SameAs(firstSlots)); // TEST-SUPPORT: RESTORE-ITEM-099 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(second.Slots, Is.SameAs(secondSlots)); // TEST-SUPPORT: RESTORE-ITEM-100 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(first.Revision, Is.Zero); // TEST-SUPPORT: RESTORE-ITEM-101 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(second.Revision, Is.Zero); // TEST-SUPPORT: RESTORE-ITEM-102 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(installs, Is.Zero); // TEST-SUPPORT: RESTORE-ITEM-103 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(publications, Is.Zero); // TEST-SUPPORT: RESTORE-ITEM-104 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
        Assert.That(store.GetLocation(retainedItem), Is.EqualTo(new ItemLocation(retained.Id, 0))); // TEST-SUPPORT: RESTORE-ITEM-105 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem
    } // TEST-SUPPORT: RESTORE-ITEM-106 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.InvalidRestorePreservesEveryTargetAndRetainedItem

    [Test] // TEST-SUPPORT: RESTORE-ITEM-VIEW-108 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
    public void RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem() // TEST-SUPPORT: RESTORE-ITEM-VIEW-109 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
    { // TEST-SUPPORT: RESTORE-ITEM-VIEW-110 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var store = new ItemStore(); // TEST-SUPPORT: RESTORE-ITEM-VIEW-111 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        using var inventory = new sMPGWM.Scripts.InventorySystem.InventoryController(store); // TEST-SUPPORT: RESTORE-ITEM-VIEW-112 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        using var equipment = new sMPGWM.Scripts.EquipmentSystem.EquipmentController(store); // TEST-SUPPORT: RESTORE-ITEM-VIEW-113 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var oldInventory = inventory.Snapshot; // TEST-SUPPORT: RESTORE-ITEM-VIEW-114 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var oldEquipment = equipment.Snapshot; // TEST-SUPPORT: RESTORE-ITEM-VIEW-115 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var definition = ItemRegistry.GetById("mock_standard_engine"); // TEST-SUPPORT: RESTORE-ITEM-VIEW-116 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var inventoryItem = new ItemEntity(ItemEntityId.Create(), definition, 1); // TEST-SUPPORT: RESTORE-ITEM-VIEW-117 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var equippedItem = new ItemEntity(ItemEntityId.Create(), definition, 1); // TEST-SUPPORT: RESTORE-ITEM-VIEW-118 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var inventorySlots = new ItemEntity?[inventory.Capacity]; // TEST-SUPPORT: RESTORE-ITEM-VIEW-119 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var equipmentSlots = new ItemEntity?[equipment.Definitions.Count]; // TEST-SUPPORT: RESTORE-ITEM-VIEW-120 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        inventorySlots[0] = inventoryItem; // TEST-SUPPORT: RESTORE-ITEM-VIEW-121 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var slot = equipment.FindSlot("engine"); // TEST-SUPPORT: RESTORE-ITEM-VIEW-122 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        equipmentSlots[slot] = equippedItem; // TEST-SUPPORT: RESTORE-ITEM-VIEW-123 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        var observations = 0; // TEST-SUPPORT: RESTORE-ITEM-VIEW-124 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        inventory.Changed += _ => // TEST-SUPPORT: RESTORE-ITEM-VIEW-125 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        { // TEST-SUPPORT: RESTORE-ITEM-VIEW-126 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
            Assert.That(equipment.Snapshot.Slots[slot]!.Id, Is.EqualTo(equippedItem.Id)); // TEST-SUPPORT: RESTORE-ITEM-VIEW-127 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
            observations++; // TEST-SUPPORT: RESTORE-ITEM-VIEW-128 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        }; // TEST-SUPPORT: RESTORE-ITEM-VIEW-129 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        store.Restore(new[] // TEST-SUPPORT: RESTORE-ITEM-VIEW-130 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        { // TEST-SUPPORT: RESTORE-ITEM-VIEW-131 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
            new ItemContainerRestoration(inventory.Container, new(inventory.Container.Id, 0, sMPGWM.Scripts.InventorySystem.InventoryController.GrowthChunk, inventorySlots)), // TEST-SUPPORT: RESTORE-ITEM-VIEW-132 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
            new ItemContainerRestoration(equipment.Container, new(equipment.Container.Id, 0, 0, equipmentSlots)) // TEST-SUPPORT: RESTORE-ITEM-VIEW-133 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        }); // TEST-SUPPORT: RESTORE-ITEM-VIEW-134 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(observations, Is.EqualTo(1)); // TEST-SUPPORT: RESTORE-ITEM-VIEW-135 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(inventoryItem.Id)); // TEST-SUPPORT: RESTORE-ITEM-VIEW-136 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(inventory.Snapshot, Is.Not.SameAs(oldInventory)); // TEST-SUPPORT: RESTORE-ITEM-VIEW-137 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(equipment.Snapshot, Is.Not.SameAs(oldEquipment)); // TEST-SUPPORT: RESTORE-ITEM-VIEW-138 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(oldInventory.Slots[0], Is.Null); // TEST-SUPPORT: RESTORE-ITEM-VIEW-139 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
        Assert.That(oldEquipment.Slots[slot], Is.Null); // TEST-SUPPORT: RESTORE-ITEM-VIEW-140 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
    } // TEST-SUPPORT: RESTORE-ITEM-VIEW-141 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
} // TEST-SUPPORT: RESTORE-ITEM-VIEW-142 | Tests: sMPGWM.Tests.ItemSystem.ItemRestorationTests.RevisionZeroRestoreRefreshesViewsBeforeAnyObserverReadsThem
