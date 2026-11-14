using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.EquipmentSystem;

[Category("Godot")]
public sealed class WeaponCapacityTests
{
    [TestCase("light_weapon", 1)]
    [TestCase("medium_weapon", 2)]
    [TestCase("heavy_weapon", 3)]
    public void WeaponCostsAreWeighted(string category, int cost)
    {
        Assert.That(WeaponSlotPolicy.GetCost(category), Is.EqualTo(cost));
    }

    [Test]
    public void WeaponsRequireFixtureAndRejectedMovesLeaveStateUnchanged()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var weapon = Add(inventory, "mock_standard_light_weapon");
        var before = inventory.Snapshot;
        Assert.That(Equip(inventory, equipment, weapon, "weapon_1"), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(inventory.Snapshot, Is.SameAs(before));
        Assert.That(equipment.Snapshot.MaxWeaponSlots, Is.Zero);
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.Zero);
        var fixture = Add(inventory, "mock_standard_weapon_fixture");
        Assert.That(Equip(inventory, equipment, fixture, "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, weapon, "weapon_1"), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.FreeWeaponSlots, Is.EqualTo(8));
    }

    [TestCase("light_weapon", 9)]
    [TestCase("heavy_weapon", 3)]
    public void EndgameFixtureAcceptsExactlyNineCapacity(string category, int count)
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Assert.That(Equip(inventory, equipment, Add(inventory, "mock_standard_weapon_fixture"), "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        for (var i = 1; i <= count; i++)
            Assert.That(Equip(inventory, equipment, Add(inventory, "mock_standard_" + category), "weapon_" + i), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(9));
        Assert.That(equipment.Snapshot.FreeWeaponSlots, Is.Zero);
        var extra = Add(inventory, "mock_standard_heavy_weapon");
        var before = equipment.Snapshot;
        Assert.That(Equip(inventory, equipment, extra, count == 9 ? "weapon_1" : "weapon_4"), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(equipment.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void MixedSwapsAndReorderingAccountForOutgoingWeapon()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var fixture = AddFixture(inventory, 6);
        Assert.That(Equip(inventory, equipment, fixture, "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        var light = Add(inventory, "mock_standard_light_weapon");
        var medium = Add(inventory, "mock_standard_medium_weapon");
        var heavy = Add(inventory, "mock_standard_heavy_weapon");
        Assert.That(Equip(inventory, equipment, light, "weapon_1"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, medium, "weapon_2"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, heavy, "weapon_3"), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.FreeWeaponSlots, Is.Zero);
        var replacement = Add(inventory, "mock_standard_light_weapon");
        Assert.That(Equip(inventory, equipment, replacement, "weapon_3"), Is.EqualTo(TransferResult.Success));
        Assert.That(inventory.Items.GetLocation(heavy.Id).ContainerId, Is.EqualTo(inventory.Container.Id));
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(4));
        Assert.That(Move(inventory.Items, equipment.Container, equipment.FindSlot("weapon_1"), equipment.Container, equipment.FindSlot("weapon_2")), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.Slots[equipment.FindSlot("weapon_1")]!.Id, Is.EqualTo(medium.Id));
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(4));
        Assert.That(Equip(inventory, equipment, heavy, "weapon_4"), Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(Equip(inventory, equipment, heavy, "weapon_2"), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(6));
    }

    [Test]
    public void RemovingFixtureReturnsWeaponsAtomicallyAndGrowsInventory()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var fixture = Add(inventory, "mock_standard_weapon_fixture");
        Assert.That(Equip(inventory, equipment, fixture, "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        var weapons = new[] { Add(inventory, "mock_standard_light_weapon"), Add(inventory, "mock_standard_medium_weapon"), Add(inventory, "mock_standard_heavy_weapon") };
        for (var i = 0; i < weapons.Length; i++)
            Assert.That(Equip(inventory, equipment, weapons[i], "weapon_" + (i + 1)), Is.EqualTo(TransferResult.Success));
        Assert.That(inventory.Items.CreateItems(inventory.Container, "mock_standard_engine", 127), Is.EqualTo(InventoryResult.Success));
        var inventoryBefore = inventory.Snapshot;
        var equipmentBefore = equipment.Snapshot;
        var source = new ItemLocation(equipment.Container.Id, equipment.FindSlot("weapon_fixture"));
        var destination = new ItemLocation(inventory.Container.Id, 127);
        Assert.That(inventory.Items.TransferItem(fixture.Id, source, destination, equipment.Revision, inventory.Revision, commit: false), Is.EqualTo(TransferResult.Success));
        Assert.That(inventory.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(equipment.Snapshot, Is.SameAs(equipmentBefore));
        var notifications = 0;
        void Observe()
        {
            notifications++;
            Assert.That(equipment.Snapshot.MaxWeaponSlots, Is.Zero);
            Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.Zero);
            Assert.That(inventory.Snapshot.Occupied, Has.Count.EqualTo(131));
            Assert.That(inventory.Capacity, Is.EqualTo(192));
            foreach (var weapon in weapons)
                Assert.That(inventory.Items.GetLocation(weapon.Id).ContainerId, Is.EqualTo(inventory.Container.Id));
        }
        inventory.Changed += _ => Observe();
        equipment.Changed += _ => Observe();
        Assert.That(inventory.Items.TransferItem(fixture.Id, source, destination, equipment.Revision, inventory.Revision), Is.EqualTo(TransferResult.Success));
        Assert.That(notifications, Is.EqualTo(2));
        Assert.That(inventory.Revision, Is.EqualTo(inventoryBefore.Revision + 1));
        Assert.That(equipment.Revision, Is.EqualTo(equipmentBefore.Revision + 1));
    }

    [Test]
    public void SmallerFixtureReturnsHighestOccupiedWeaponFirst()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Assert.That(Equip(inventory, equipment, Add(inventory, "mock_standard_weapon_fixture"), "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        var light = Add(inventory, "mock_standard_light_weapon");
        var medium = Add(inventory, "mock_standard_medium_weapon");
        var heavy = Add(inventory, "mock_standard_heavy_weapon");
        Assert.That(Equip(inventory, equipment, light, "weapon_1"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, medium, "weapon_5"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, heavy, "weapon_9"), Is.EqualTo(TransferResult.Success));
        Assert.That(Equip(inventory, equipment, AddFixture(inventory, 3), "weapon_fixture"), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.MaxWeaponSlots, Is.EqualTo(3));
        Assert.That(equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(3));
        Assert.That(equipment.Snapshot.Slots[equipment.FindSlot("weapon_1")]!.Id, Is.EqualTo(light.Id));
        Assert.That(equipment.Snapshot.Slots[equipment.FindSlot("weapon_5")]!.Id, Is.EqualTo(medium.Id));
        Assert.That(equipment.Snapshot.Slots[equipment.FindSlot("weapon_9")], Is.Null);
        Assert.That(inventory.Items.GetLocation(heavy.Id).ContainerId, Is.EqualTo(inventory.Container.Id));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidConfirmedAndRestoredLoadoutsAreRejected(bool includeFixture)
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var slots = new ItemEntity?[equipment.Definitions.Count];
        if (includeFixture)
            slots[equipment.FindSlot("weapon_fixture")] = new(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_weapon_fixture"), 1);
        for (var i = 1; i <= (includeFixture ? 4 : 1); i++)
            slots[equipment.FindSlot("weapon_" + i)] = new(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_heavy_weapon"), 1);
        var before = equipment.Snapshot;
        Assert.That(inventory.Items.Confirm(new[] { new ItemContainerConfirmation(equipment.Container, slots, 1) }), Is.False);
        Assert.That(equipment.Snapshot, Is.SameAs(before));
        var state = new ItemContainerState(equipment.Container.Id, 1, 0, slots);
        Assert.Throws<ArgumentException>(() => inventory.Items.Restore(new[] { new ItemContainerRestoration(equipment.Container, state) }));
        Assert.That(equipment.Snapshot, Is.SameAs(before));
        Assert.That(inventory.Snapshot.Occupied, Is.Empty);
    }

    private static ItemEntity Add(InventoryController inventory, string definition)
    {
        Assert.That(inventory.Items.CreateItems(inventory.Container, definition, 1), Is.EqualTo(InventoryResult.Success));
        for (var i = inventory.Container.Entities.Length - 1; i >= 0; i--)
            if (inventory.Container.Entities[i] is { } item && item.DefinitionId == definition) return item;
        throw new InvalidOperationException("Missing seeded item.");
    }

    private static ItemEntity AddFixture(InventoryController inventory, int capacity)
    {
        var definition = (WeaponFixtureItemDefinition)ItemRegistry.GetById("mock_standard_weapon_fixture") with
        {
            WeaponSlots = capacity, Mass = 1
        };
        var fixture = new ItemEntity(ItemEntityId.Create(), definition, 1);
        var slots = (ItemEntity?[])inventory.Container.Entities.Clone();
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) continue;
            slots[i] = fixture;
            Assert.That(inventory.Items.Confirm(new[] { new ItemContainerConfirmation(inventory.Container, slots, inventory.Revision + 1) }), Is.True);
            return fixture;
        }
        throw new InvalidOperationException("Missing fixture space.");
    }

    private static TransferResult Equip(InventoryController inventory, EquipmentController equipment, ItemEntity item, string key)
    {
        var source = inventory.Items.GetLocation(item.Id);
        return Move(inventory.Items, inventory.Container, source.SlotIndex, equipment.Container, equipment.FindSlot(key));
    }

    private static TransferResult Move(ItemStore store, ItemContainer source, int sourceIndex, ItemContainer destination, int destinationIndex)
    {
        return store.TransferItem(source.Entities[sourceIndex]!.Id, new(source.Id, sourceIndex), new(destination.Id, destinationIndex),
            source.Revision, destination.Revision, destination.Entities[destinationIndex]?.Id);
    }
}
