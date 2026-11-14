using Godot;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
[Category("Godot")]
public sealed class InventoryWeaponFilterTests
{
    [TestCase(0)]
    [TestCase(1)]
    public void WeaponDropdownCombinesTypeAndSortAndRetainsSelection(int sort)
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, "mock_standard_medium_weapon", 1);
        inventory.Items.CreateItems(inventory.Container, "mock_advanced_medium_weapon", 1);
        inventory.Items.CreateItems(inventory.Container, "vulcan_mk1", 1);
        inventory.Items.CreateItems(inventory.Container, "mock_standard_heavy_weapon", 1);
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SelectedTab = 1 };
        while (state.SelectedCategoryId != "light_weapon") state.SelectedTab++;
        var weaponTab = state.SelectedTab;
        var view = GD.Load<PackedScene>("res://scenes/ui/base/inventory_container.tscn").Instantiate<InventoryContainer>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(view);
        try
        {
            view.Bind(state);
            view.Render(before, new ItemVisuals());
            var filter = view.GetNode<MenuButton>("FilterToolbar/WeaponFilter");
            var popup = filter.GetPopup();
            Assert.That(filter.Text, Is.EqualTo("Filter"));
            Assert.That(filter.Visible, Is.True);
            Assert.That(view.GetNode<OptionButton>("FilterToolbar/SortSelector").Visible, Is.False);
            Assert.That(VisibleDropdownCount(view), Is.EqualTo(1));
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 2L);
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 4L + sort);
            view.Render(before, new ItemVisuals());
            Assert.That(state.WeaponFilter, Is.EqualTo(2));
            Assert.That(state.CategorySort, Is.EqualTo(sort));
            for (var id = 0; id < 4; id++)
                Assert.That(popup.IsItemChecked(popup.GetItemIndex(id)), Is.EqualTo(id == 2));
            Assert.That(popup.IsItemChecked(popup.GetItemIndex(4 + sort)), Is.True);
            Assert.That(popup.IsItemChecked(popup.GetItemIndex(5 - sort)), Is.False);
            Assert.That(view.Cells.Count, Is.EqualTo(before.EmptyAddresses.Count + 2));
            var first = (ItemCellBinding)view.Cells[0].Binding!;
            var second = (ItemCellBinding)view.Cells[1].Binding!;
            Assert.That(first.Entity!.DefinitionId, Is.EqualTo(sort == 0 ? "mock_advanced_medium_weapon" : "mock_standard_medium_weapon"));
            Assert.That(second.Entity!.Definition.CategoryId, Is.EqualTo("medium_weapon"));
            var tabs = view.GetNode<TabBar>("%CategoryTabs");
            tabs.CurrentTab = 0;
            view.Render(before, new ItemVisuals());
            Assert.That(filter.Visible, Is.False);
            Assert.That(view.GetNode<OptionButton>("FilterToolbar/SortSelector").Visible, Is.True);
            tabs.CurrentTab = weaponTab;
            view.Render(before, new ItemVisuals());
            view.Bind(state);
            view.Render(before, new ItemVisuals());
            Assert.That(state.WeaponFilter, Is.EqualTo(2));
            Assert.That(state.CategorySort, Is.EqualTo(sort));
            Assert.That(filter.GetPopup().IsItemChecked(popup.GetItemIndex(2)), Is.True);
            Assert.That(filter.GetPopup().IsItemChecked(popup.GetItemIndex(4 + sort)), Is.True);
            Assert.That(VisibleDropdownCount(view), Is.EqualTo(1));
            var notifications = 0;
            view.ViewChanged += () => notifications++;
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(inventory.Snapshot, Is.SameAs(before));
        }
        finally
        {
            view.Free();
        }
    }

    [Test]
    public void SearchKeepsWeaponTypesAvailableAndOtherTabsKeepNormalSort()
    {
        using var inventory = new InventoryController();
        inventory.Items.CreateItems(inventory.Container, "mock_standard_heavy_weapon", 1);
        inventory.Items.CreateItems(inventory.Container, "mock_advanced_heavy_weapon", 1);
        inventory.Items.CreateItems(inventory.Container, "vulcan_mk1", 1);
        var before = inventory.Snapshot;
        var state = new InventoryViewState { SelectedTab = 1 };
        while (state.SelectedCategoryId != "light_weapon") state.SelectedTab++;
        var view = GD.Load<PackedScene>("res://scenes/ui/base/inventory_container.tscn").Instantiate<InventoryContainer>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(view);
        try
        {
            view.Bind(state);
            view.Render(before, new ItemVisuals());
            var filter = view.GetNode<MenuButton>("FilterToolbar/WeaponFilter");
            var popup = filter.GetPopup();
            var search = view.GetNode<LineEdit>("FilterToolbar/LineEdit");
            search.Text = "Standard";
            search.EmitSignal(LineEdit.SignalName.TextChanged, search.Text);
            view.Render(before, new ItemVisuals());
            Assert.That(state.SearchText, Is.EqualTo("Standard"));
            Assert.That(filter.Disabled, Is.False);
            for (var id = 0; id < 4; id++)
                Assert.That(popup.IsItemDisabled(popup.GetItemIndex(id)), Is.False);
            Assert.That(popup.IsItemDisabled(popup.GetItemIndex(4)), Is.True);
            Assert.That(popup.IsItemDisabled(popup.GetItemIndex(5)), Is.True);
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 3L);
            view.Render(before, new ItemVisuals());
            Assert.That(state.WeaponFilter, Is.EqualTo(3));
            Assert.That(view.Cells.Count, Is.EqualTo(before.EmptyAddresses.Count + 1));
            Assert.That(((ItemCellBinding)view.Cells[0].Binding!).Entity!.DefinitionId, Is.EqualTo("mock_standard_heavy_weapon"));
            search.Text = "";
            search.EmitSignal(LineEdit.SignalName.TextChanged, search.Text);
            view.Render(before, new ItemVisuals());
            Assert.That(popup.IsItemDisabled(popup.GetItemIndex(4)), Is.False);
            Assert.That(popup.IsItemDisabled(popup.GetItemIndex(5)), Is.False);
            Assert.That(view.Cells.Count, Is.EqualTo(before.EmptyAddresses.Count + 2));
            view.GetNode<TabBar>("%CategoryTabs").CurrentTab = 1;
            view.Render(before, new ItemVisuals());
            var sort = view.GetNode<OptionButton>("FilterToolbar/SortSelector");
            Assert.That(filter.Visible, Is.False);
            Assert.That(sort.Visible, Is.True);
            Assert.That(sort.Disabled, Is.False);
            Assert.That(VisibleDropdownCount(view), Is.EqualTo(1));
            sort.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            Assert.That(state.CategorySort, Is.EqualTo(1));
            Assert.That(inventory.Snapshot, Is.SameAs(before));
        }
        finally
        {
            view.Free();
        }
    }

    private static int VisibleDropdownCount(InventoryContainer view)
    {
        var count = 0;
        foreach (var child in view.GetNode("FilterToolbar").GetChildren())
            if (child is Control { Visible: true } && child is OptionButton or MenuButton) count++;
        return count;
    }
}
