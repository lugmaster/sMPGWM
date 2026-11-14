using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
[Category("Godot")]
public sealed class WeaponEquipmentViewTests
{
    [TestCase(1)]
    [TestCase(9)]
    public void AvailableCapacityShowsOneEmptyCellAndGrowsToThreeRows(int capacity)
    {
        using var equipment = new EquipmentController();
        var view = GD.Load<PackedScene>("res://scenes/ui/base/equipment_container.tscn").Instantiate<EquipmentContainer>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(view);
        try
        {
            var slots = new ItemEntity?[equipment.Definitions.Count];
            view.Render(equipment.Definitions, new EquipmentSnapshot(0, slots), new ItemVisuals());
            var grid = view.GetNode<GridContainer>("Weapons/Slots/SlotGrid");
            var scroller = view.GetNode<ScrollContainer>("Weapons/Slots");
            var counter = view.GetNode<Label>("Weapons/Capacity/Value");
            Assert.That(grid.Columns, Is.EqualTo(3));
            Assert.That(grid.GetChildCount(), Is.EqualTo(9));
            Assert.That(VisibleCount(grid), Is.Zero);
            Assert.That(view.GetNode("Ship").GetChildCount(), Is.EqualTo(6));
            Assert.That(counter.Text, Is.EqualTo("0/0"));
            Assert.That(grid.GetCombinedMinimumSize().Y, Is.Zero);
            Assert.That(view.GetNode<Control>("Weapons/Capacity").SizeFlagsHorizontal, Is.EqualTo(Control.SizeFlags.ShrinkCenter));
            var fixture = (WeaponFixtureItemDefinition)ItemRegistry.GetById("mock_standard_weapon_fixture") with { WeaponSlots = capacity };
            slots[equipment.FindSlot("weapon_fixture")] = new ItemEntity(ItemEntityId.Create(), fixture, 1);
            view.Render(equipment.Definitions, new EquipmentSnapshot(1, slots), new ItemVisuals());
            Assert.That(VisibleCount(grid), Is.EqualTo(1));
            Assert.That(counter.Text, Is.EqualTo($"{capacity}/{capacity}"));
            Assert.That(scroller.VerticalScrollMode, Is.EqualTo(ScrollContainer.ScrollMode.Disabled));
            var firstRowHeight = grid.GetCombinedMinimumSize().Y;
            Assert.That(firstRowHeight, Is.GreaterThan(0));
            for (var index = 1; index <= capacity; index++)
            {
                var key = $"weapon_{index}";
                slots[equipment.FindSlot(key)] = new ItemEntity(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_light_weapon"), 1);
                view.Render(equipment.Definitions, new EquipmentSnapshot(index + 1, slots), new ItemVisuals());
                Assert.That(VisibleCount(grid), Is.EqualTo(index < capacity ? index + 1 : index));
                Assert.That(counter.Text, Is.EqualTo($"{capacity - index}/{capacity}"));
                Assert.That(view.Cells[equipment.FindSlot(key)].Visible, Is.True);
            }
            Assert.That(grid.GetChildCount(), Is.EqualTo(9));
            if (capacity == 9) Assert.That(grid.GetCombinedMinimumSize().Y, Is.GreaterThan(firstRowHeight * 2));
            else Assert.That(grid.GetCombinedMinimumSize().Y, Is.EqualTo(firstRowHeight));
        }
        finally
        {
            view.Free();
        }
    }

    [Test]
    public void WeightedCapacityAndHolesPreserveSlotBindings()
    {
        using var equipment = new EquipmentController();
        var view = GD.Load<PackedScene>("res://scenes/ui/base/equipment_container.tscn").Instantiate<EquipmentContainer>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(view);
        try
        {
            var slots = new ItemEntity?[equipment.Definitions.Count];
            slots[equipment.FindSlot("weapon_fixture")] = new ItemEntity(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_weapon_fixture"), 1);
            foreach (var key in new[] { "weapon_1", "weapon_2", "weapon_9" })
                slots[equipment.FindSlot(key)] = new ItemEntity(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_heavy_weapon"), 1);
            view.Render(equipment.Definitions, new EquipmentSnapshot(1, slots), new ItemVisuals());
            var grid = view.GetNode<GridContainer>("Weapons/Slots/SlotGrid");
            var counter = view.GetNode<Label>("Weapons/Capacity/Value");
            Assert.That(VisibleCount(grid), Is.EqualTo(3));
            Assert.That(counter.Text, Is.EqualTo("0/9"));
            Assert.That(view.Cells[equipment.FindSlot("weapon_3")].Visible, Is.False);
            var lastWeaponCell = view.Cells[equipment.FindSlot("weapon_9")];
            var lastWeapon = slots[equipment.FindSlot("weapon_9")];
            slots[equipment.FindSlot("weapon_1")] = null;
            view.Render(equipment.Definitions, new EquipmentSnapshot(2, slots), new ItemVisuals());
            Assert.That(VisibleCount(grid), Is.EqualTo(3));
            Assert.That(counter.Text, Is.EqualTo("3/9"));
            Assert.That(view.Cells[equipment.FindSlot("weapon_9")], Is.SameAs(lastWeaponCell));
            Assert.That(lastWeaponCell.Visible, Is.True);
            var occupiedBinding = (ItemCellBinding)lastWeaponCell.Binding!;
            Assert.That(occupiedBinding.SlotKey, Is.EqualTo("weapon_9"));
            Assert.That(occupiedBinding.Entity, Is.SameAs(lastWeapon));
            Assert.That(occupiedBinding.Revision, Is.EqualTo(2));
            var emptyCell = view.Cells[equipment.FindSlot("weapon_1")];
            Assert.That(emptyCell.Visible, Is.True);
            Assert.That(((ItemCellBinding)emptyCell.Binding!).SlotKey, Is.EqualTo("weapon_1"));
            Assert.That(((ItemCellBinding)emptyCell.Binding!).Entity, Is.Null);
            Assert.That(view.Cells[equipment.FindSlot("weapon_3")].Visible, Is.False);
            Array.Clear(slots);
            view.Render(equipment.Definitions, new EquipmentSnapshot(3, slots), new ItemVisuals());
            Assert.That(VisibleCount(grid), Is.Zero);
            Assert.That(counter.Text, Is.EqualTo("0/0"));
            Assert.That(grid.GetCombinedMinimumSize().Y, Is.Zero);
        }
        finally
        {
            view.Free();
        }
    }

    private static int VisibleCount(Node grid)
    {
        var count = 0;
        foreach (var child in grid.GetChildren())
            if (((Control)child).Visible) count++;
        return count;
    }
}
