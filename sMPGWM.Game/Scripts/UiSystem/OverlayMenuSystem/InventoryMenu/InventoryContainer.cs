

using Godot;
using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using ClickableIcon = sMPGWM.Scripts.UtilitySystem.UiComponents.ClickableIcon;
using ItemGrid = sMPGWM.Scripts.UtilitySystem.UiComponents.ItemGrid;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryContainer : VBoxContainer
{
    private InventoryViewState _viewState = null!;
    private LineEdit _search = null!;
    private TabBar _tabs = null!;
    private OptionButton _sort = null!;
    private MenuButton _weaponFilter = null!;
    private Label _emptyResult = null!;
    private ItemGrid _grid = null!;
    internal System.Collections.Generic.IReadOnlyList<ClickableIcon> Cells => _grid.Cells;
    internal event Action<ClickableIcon>? CellCreated;
    internal event Action? ViewChanging;
    internal event Action? ViewChanged;

    internal bool IsToolbarControl(Control control) =>
        control == _tabs || _tabs.IsAncestorOf(control) ||
        GetNode<Control>("FilterToolbar").IsAncestorOf(control);

    internal void Bind(InventoryViewState state)
    {
        Unbind();
        _viewState = state;
        _search = GetNode<LineEdit>("FilterToolbar/LineEdit");
        _sort = GetNode<OptionButton>("FilterToolbar/SortSelector");
        _tabs = GetNode<TabBar>("%CategoryTabs");
        _grid = GetNode<ItemGrid>("%ItemGrid");
        _grid.CellCreated += OnCellCreated;
        _tabs.ClearTabs();
        _tabs.AddTab("All");
        foreach (var category in ItemRegistry.GetAllCategories())
        {
            if (category.Id is "medium_weapon" or "heavy_weapon") continue;
            _tabs.AddTab("", ItemIconResolver.Category(category));
            var index = _tabs.TabCount - 1;
            _tabs.SetTabMetadata(index, category.Id);
            _tabs.SetTabTooltip(index, category.Id == "light_weapon" ? "Weapons" : category.DisplayName);
            _tabs.SetTabIconMaxWidth(index, 14);
        }
        state.SelectedTab = Math.Clamp(state.SelectedTab, 0, _tabs.TabCount - 1);
        _weaponFilter = GetNodeOrNull<MenuButton>("FilterToolbar/WeaponFilter");
        if (_weaponFilter == null)
        {
            _weaponFilter = new MenuButton { Name = "WeaponFilter", Text = "Filter", Flat = false };
            var popup = _weaponFilter.GetPopup();
            popup.AddSeparator("Weapon type", 100);
            popup.AddRadioCheckItem("All weapons", 0);
            popup.AddRadioCheckItem("Light weapons", 1);
            popup.AddRadioCheckItem("Medium weapons", 2);
            popup.AddRadioCheckItem("Heavy weapons", 3);
            popup.AddSeparator("Sort by name", 101);
            popup.AddRadioCheckItem("Ascending", 4);
            popup.AddRadioCheckItem("Descending", 5);
            GetNode<Control>("FilterToolbar").AddChild(_weaponFilter);
        }
        _weaponFilter.GetPopup().IdPressed += OnWeaponFilterChanged;
        _sort.Clear();
        _sort.AddItem("Name ascending");
        _sort.AddItem("Name descending");
        _sort.AddItem("Custom");
        _sort.AllowReselect = true;
        _emptyResult = GetNodeOrNull<Label>("EmptyResult");
        if (_emptyResult == null)
        {
            _emptyResult = new Label { Name = "EmptyResult", Text = "No matching items", MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_emptyResult);
            MoveChild(_emptyResult, GetNode<ItemGrid>("%ItemGrid").GetIndex());
        }
        _search.Text = state.SearchText;
        _tabs.CurrentTab = state.SelectedTab;
        RefreshFilterControls();
        _search.TextChanged += OnSearchChanged;
        _tabs.TabChanged += OnTabChanged;
        _sort.ItemSelected += OnSortChanged;
    }

    internal void Unbind()
    {
        if (_viewState == null) return;
        _grid.CellCreated -= OnCellCreated;
        _search.TextChanged -= OnSearchChanged;
        _tabs.TabChanged -= OnTabChanged;
        _sort.ItemSelected -= OnSortChanged;
        _weaponFilter.GetPopup().IdPressed -= OnWeaponFilterChanged;
        _viewState = null!;
    }

    internal void Render(InventorySnapshot snapshot, ItemVisuals visuals)
    {
        RefreshFilterControls();
        var cells = InventoryProjection.Create(snapshot, _viewState);
        _emptyResult.Visible = cells.Count == snapshot.EmptyAddresses.Count;
        _grid.Render(cells,
            (icon, binding) => visuals.Apply(icon, binding.Entity));
    }

    private void OnCellCreated(ClickableIcon cell) => CellCreated?.Invoke(cell);

    private void RefreshFilterControls()
    {
        var weapons = _viewState.SelectedCategoryId == "light_weapon";
        var searching = !string.IsNullOrWhiteSpace(_viewState.SearchText);
        _sort.Visible = !weapons;
        _sort.SetItemDisabled(2, _viewState.SelectedTab != 0);
        _sort.Disabled = searching;
        _sort.Select(_viewState.SelectedSort);
        _weaponFilter.Visible = weapons;
        var popup = _weaponFilter.GetPopup();
        for (var type = 0; type < 4; type++)
            popup.SetItemChecked(popup.GetItemIndex(type), _viewState.WeaponFilter == type);
        for (var sort = 0; sort < 2; sort++)
        {
            var index = popup.GetItemIndex(sort + 4);
            popup.SetItemChecked(index, _viewState.CategorySort == sort);
            popup.SetItemDisabled(index, searching);
        }
        _weaponFilter.TooltipText = popup.GetItemText(popup.GetItemIndex(_viewState.WeaponFilter)) +
            (_viewState.CategorySort == 1 ? "; name descending" : "; name ascending");
    }

    public override void _ExitTree() => Unbind();

    private void OnSearchChanged(string text) { ViewChanging?.Invoke(); _viewState.SearchText = text; ViewChanged?.Invoke(); }
    private void OnTabChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedTab = (int)index; ViewChanged?.Invoke(); }
    private void OnWeaponFilterChanged(long id)
    {
        if (id is < 0 or > 5) return;
        ViewChanging?.Invoke();
        if (id < 4) _viewState.WeaponFilter = (int)id;
        else _viewState.CategorySort = (int)id - 4;
        ViewChanged?.Invoke();
    }
    private void OnSortChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedSort = (int)index; _viewState.SortRequested = _viewState.SelectedTab == 0; ViewChanged?.Invoke(); }
}
