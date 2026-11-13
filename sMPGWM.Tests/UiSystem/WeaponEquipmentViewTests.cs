using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.UtilitySystem.Mocks;

namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: WEAPON-UI-GRID-001 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot

[TestFixture] // TEST-SUPPORT: WEAPON-UI-GRID-002 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
[Category("Godot")] // TEST-SUPPORT: WEAPON-UI-GRID-003 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
public sealed class WeaponEquipmentViewTests // TEST-SUPPORT: WEAPON-UI-GRID-004 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
{
    [Test] // TEST-SUPPORT: WEAPON-UI-GRID-005 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
    public void WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot() // TEST-SUPPORT: WEAPON-UI-GRID-006 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
    {
        using var equipment = new EquipmentController(); // TEST-SUPPORT: WEAPON-UI-GRID-007 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        var view = GD.Load<PackedScene>("res://scenes/ui/base/equipment_container.tscn").Instantiate<EquipmentContainer>(); // TEST-SUPPORT: WEAPON-UI-GRID-008 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: WEAPON-UI-GRID-009 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        root.AddChild(view); // TEST-SUPPORT: WEAPON-UI-GRID-010 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        try // TEST-SUPPORT: WEAPON-UI-GRID-011 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        {
            view.Render(equipment.Definitions, equipment.Snapshot, new ItemVisuals()); // TEST-SUPPORT: WEAPON-UI-GRID-012 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            var grid = view.GetNode<GridContainer>("Weapons/Slots"); // TEST-SUPPORT: WEAPON-UI-GRID-013 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            var counter = view.GetNode<Label>("Weapons/Capacity/Value"); // TEST-SUPPORT: WEAPON-UI-GRID-014 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(grid.Columns, Is.EqualTo(3)); // TEST-SUPPORT: WEAPON-UI-GRID-015 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(grid.GetChildCount(), Is.EqualTo(9)); // TEST-SUPPORT: WEAPON-UI-GRID-016 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(view.GetNode("Ship").GetChildCount(), Is.EqualTo(6)); // TEST-SUPPORT: WEAPON-UI-GRID-017 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(counter.Text, Is.EqualTo("0/0")); // TEST-SUPPORT: WEAPON-UI-GRID-018 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            ItemFixture.Place(equipment.Items, equipment.Container, equipment.FindSlot("weapon_fixture"), "mock_standard_weapon_fixture"); // TEST-SUPPORT: WEAPON-UI-GRID-019 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            ItemFixture.Place(equipment.Items, equipment.Container, equipment.FindSlot("weapon_1"), "mock_standard_heavy_weapon"); // TEST-SUPPORT: WEAPON-UI-GRID-020 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            view.Render(equipment.Definitions, equipment.Snapshot, new ItemVisuals()); // TEST-SUPPORT: WEAPON-UI-GRID-021 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(grid.GetChildCount(), Is.EqualTo(9)); // TEST-SUPPORT: WEAPON-UI-GRID-022 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(view.Cells[equipment.FindSlot("weapon_1")].Icon, Is.Not.Null); // TEST-SUPPORT: WEAPON-UI-GRID-023 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
            Assert.That(counter.Text, Is.EqualTo($"{equipment.Snapshot.MaxWeaponSlots - 3}/{equipment.Snapshot.MaxWeaponSlots}")); // TEST-SUPPORT: WEAPON-UI-GRID-024 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        }
        finally // TEST-SUPPORT: WEAPON-UI-GRID-025 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        {
            view.Free(); // TEST-SUPPORT: WEAPON-UI-GRID-026 | Tests: sMPGWM.Tests.UiSystem.WeaponEquipmentViewTests.WeaponsUseNineCellsAndCapacityCounterFromEquipmentSnapshot
        }
    }
}
