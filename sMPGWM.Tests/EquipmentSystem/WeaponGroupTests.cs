using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.EquipmentSystem;

[Category("Godot")]
public sealed class WeaponGroupTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void MembershipRestoresBeforePublicationWithoutChangingItems(bool primary, bool secondary)
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var item = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var slot = equipment.FindSlot("weapon_1");
        var before = equipment.Snapshot;
        Assert.That(before.WeaponGroups.Bindings, Is.EqualTo(new[] { new WeaponGroupBinding(slot, item.Id, true, false) }));
        Assert.That(equipment.SetWeaponGroups(slot, item.Id, equipment.Revision, primary, secondary),
            Is.EqualTo(primary && !secondary ? TransferResult.NoChange : TransferResult.Success));
        Assert.That(equipment.Snapshot.Slots[slot], Is.SameAs(item));
        Assert.That(inventory.Items.GetItem(item.Id), Is.SameAs(item));
        Assert.That(before.WeaponGroups.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, item.Id, true, false)));
        var accepted = equipment.Snapshot.WeaponGroups;
        Assert.That(accepted.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, item.Id, primary, secondary)));
        var state = inventory.Items.Capture(equipment.Container);
        var slots = CopySlots(state);
        using var restored = new EquipmentController();
        var publications = 0;
        restored.Changed += snapshot =>
        {
            publications++;
            Assert.That(snapshot.WeaponGroups.Bindings, Is.EqualTo(accepted.Bindings));
            Assert.That(snapshot.Revision, Is.EqualTo(state.Revision));
        };
        var install = restored.PrepareWeaponGroupConfirmation(slots, state.Revision, accepted);
        restored.Items.Restore(new[] { new ItemContainerRestoration(restored.Container, state) }, install);
        Assert.That(publications, Is.EqualTo(1));
        Assert.That(restored.Snapshot.WeaponGroups.Bindings, Is.EqualTo(accepted.Bindings));
        Assert.That(restored.Revision, Is.EqualTo(state.Revision));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ManualAndFixtureRemovalClearBindingsWithoutChangingItems(bool removeFixture)
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var fixture = Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var item = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var slot = equipment.FindSlot("weapon_1");
        Assert.That(equipment.SetWeaponGroups(slot, item.Id, equipment.Revision, true, true), Is.EqualTo(TransferResult.Success));
        var before = equipment.Snapshot;
        var source = removeFixture ? fixture : item;
        var sourceSlot = removeFixture ? equipment.FindSlot("weapon_fixture") : slot;
        Assert.That(inventory.Items.TransferItem(source.Id, new(equipment.Container.Id, sourceSlot),
            new(inventory.Container.Id, 0), equipment.Revision, inventory.Revision), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.Slots[slot], Is.Null);
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings, Is.Empty);
        Assert.That(inventory.Items.GetItem(item.Id), Is.SameAs(item));
        Assert.That(before.WeaponGroups.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, item.Id, true, true)));
        Assert.That(equipment.SetWeaponGroups(slot, item.Id, before.Revision, true, true), Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(equipment.SetWeaponGroups(slot, item.Id, equipment.Revision, true, true), Is.EqualTo(TransferResult.StaleEntity));
    }

    [Test]
    public void ReplacementAndReequippingUseDefaultsAndRejectOldIdentity()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var first = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var slot = equipment.FindSlot("weapon_1");
        Assert.That(equipment.SetWeaponGroups(slot, first.Id, equipment.Revision, false, true), Is.EqualTo(TransferResult.Success));
        var revision = equipment.Revision;
        var replacement = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, replacement.Id, true, false)));
        Assert.That(inventory.Items.GetItem(first.Id), Is.SameAs(first));
        Assert.That(equipment.SetWeaponGroups(slot, first.Id, revision, true, true), Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(equipment.SetWeaponGroups(slot, first.Id, equipment.Revision, true, true), Is.EqualTo(TransferResult.StaleEntity));
        var location = inventory.Items.GetLocation(first.Id);
        Assert.That(inventory.Items.TransferItem(first.Id, location, new(equipment.Container.Id, slot),
            inventory.Revision, equipment.Revision, replacement.Id), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, first.Id, true, false)));
        Assert.That(equipment.Snapshot.Slots[slot], Is.SameAs(first));
    }

    [Test]
    public void IdenticalWeaponsAreIndependentAndEmptyGroupsAreCompleteConfiguration()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var first = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var second = Equip(inventory, equipment, "vulcan_mk1", "weapon_2");
        var firstSlot = equipment.FindSlot("weapon_1");
        var secondSlot = equipment.FindSlot("weapon_2");
        Assert.That(equipment.SetWeaponGroups(firstSlot, first.Id, equipment.Revision, false, true), Is.EqualTo(TransferResult.Success));
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(new[]
        {
            new WeaponGroupBinding(firstSlot, first.Id, false, true),
            new WeaponGroupBinding(secondSlot, second.Id, true, false)
        }));
        equipment.ApplyWeaponGroupDefinition([], []);
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(new[]
        {
            new WeaponGroupBinding(firstSlot, first.Id, false, false),
            new WeaponGroupBinding(secondSlot, second.Id, false, false)
        }));
        var revision = equipment.Revision;
        equipment.ApplyWeaponGroupDefinition([], []);
        Assert.That(equipment.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void InvalidOrConflictingConfigurationCannotChangeEquipment()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var item = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var slot = equipment.FindSlot("weapon_1");
        var before = equipment.Snapshot;
        var slots = CopySlots(inventory.Items.Capture(equipment.Container));
        var invalid = new[]
        {
            WeaponGroupConfiguration.Empty,
            new WeaponGroupConfiguration(new[] { new WeaponGroupBinding(slot, ItemEntityId.Create(), true, false) }),
            new WeaponGroupConfiguration(new[] { new WeaponGroupBinding(slot + 1, item.Id, true, false) })
        };
        foreach (var groups in invalid)
            Assert.Throws<ArgumentException>(() => equipment.PrepareWeaponGroupConfirmation(slots, equipment.Revision + 1, groups));
        Assert.Throws<ArgumentException>(() => new WeaponGroupConfiguration(new[]
        {
            new WeaponGroupBinding(slot, item.Id, true, false),
            new WeaponGroupBinding(slot, item.Id, false, true)
        }));
        var conflicting = new WeaponGroupConfiguration(new[] { new WeaponGroupBinding(slot, item.Id, false, true) });
        Assert.Throws<ArgumentException>(() => equipment.PrepareWeaponGroupConfirmation(slots, equipment.Revision, conflicting));
        Assert.That(equipment.Snapshot, Is.SameAs(before));
        Assert.That(equipment.Snapshot.Slots[slot], Is.SameAs(item));
    }

    [Test]
    public void FailedLoadoutPreparationPreservesEquipmentAndMembershipWithoutPublication()
    {
        using var inventory = new InventoryController();
        using var equipment = new EquipmentController(inventory.Items);
        var fixture = Equip(inventory, equipment, "mock_standard_weapon_fixture", "weapon_fixture");
        var item = Equip(inventory, equipment, "vulcan_mk1", "weapon_1");
        var slot = equipment.FindSlot("weapon_1");
        Assert.That(equipment.SetWeaponGroups(slot, item.Id, equipment.Revision, false, true), Is.EqualTo(TransferResult.Success));
        var before = equipment.Snapshot;
        var inventoryBefore = inventory.Snapshot;
        var publications = 0;
        equipment.Changed += _ => publications++;
        equipment.PrepareLoadoutChange = _ => throw new InvalidOperationException("Rejected loadout preparation");
        try
        {
            var fixtureSlot = equipment.FindSlot("weapon_fixture");
            Assert.That(inventory.Items.TransferItem(fixture.Id, new(equipment.Container.Id, fixtureSlot),
                new(inventory.Container.Id, 0), equipment.Revision, inventory.Revision), Is.EqualTo(TransferResult.IncompatiblePlacement));
        }
        finally
        {
            equipment.PrepareLoadoutChange = null;
        }
        Assert.That(publications, Is.Zero);
        Assert.That(equipment.Snapshot, Is.SameAs(before));
        Assert.That(inventory.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(equipment.Snapshot.WeaponGroups, Is.SameAs(before.WeaponGroups));
        Assert.That(equipment.Snapshot.WeaponGroups.Bindings[0], Is.EqualTo(new WeaponGroupBinding(slot, item.Id, false, true)));
        Assert.That(equipment.Snapshot.Slots[slot], Is.SameAs(item));
        Assert.That(inventory.Items.GetLocation(item.Id), Is.EqualTo(new ItemLocation(equipment.Container.Id, slot)));
    }

    private static ItemEntity?[] CopySlots(ItemContainerState state)
    {
        var slots = new ItemEntity?[state.Slots.Count];
        for (var i = 0; i < slots.Length; i++) slots[i] = state.Slots[i];
        return slots;
    }

    private static ItemEntity Equip(InventoryController inventory, EquipmentController equipment, string definition, string key)
    {
        Assert.That(inventory.Items.CreateItems(inventory.Container, definition, 1), Is.EqualTo(InventoryResult.Success));
        var slot = equipment.FindSlot(key);
        for (var i = 0; i < inventory.Container.Entities.Length; i++)
        {
            if (inventory.Container.Entities[i] is not { } item || item.DefinitionId != definition) continue;
            Assert.That(inventory.Items.TransferItem(item.Id, new(inventory.Container.Id, i),
                new(equipment.Container.Id, slot), inventory.Revision, equipment.Revision, equipment.Snapshot.Slots[slot]?.Id), Is.EqualTo(TransferResult.Success));
            return item;
        }
        throw new AssertionException("Missing item");
    }
}
