

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
            _tabs.AddTab(category.DisplayName, ItemIconResolver.Category(category));
            var index = _tabs.TabCount - 1;
            _tabs.SetTabMetadata(index, category.Id);
            _tabs.SetTabIconMaxWidth(index, 14);
        }
        state.SelectedTab = Math.Clamp(state.SelectedTab, 0, _tabs.TabCount - 1);
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
        _sort.Select(state.SelectedSort);
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
        _viewState = null!;
    }

    internal void Render(InventorySnapshot snapshot, ItemVisuals visuals)
    {
        var cells = InventoryProjection.Create(snapshot, _viewState);
        _sort.SetItemDisabled(2, _viewState.SelectedTab != 0);
        _sort.Disabled = !string.IsNullOrWhiteSpace(_viewState.SearchText);
        _sort.Select(_viewState.SelectedSort);
        _emptyResult.Visible = cells.Count == snapshot.EmptyAddresses.Count;
        _grid.Render(cells,
            (icon, binding) => visuals.Apply(icon, binding.Entity));
    }

    private void OnCellCreated(ClickableIcon cell) => CellCreated?.Invoke(cell);

    public override void _ExitTree() => Unbind();

    private void OnSearchChanged(string text) { ViewChanging?.Invoke(); _viewState.SearchText = text; ViewChanged?.Invoke(); }
    private void OnTabChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedTab = (int)index; ViewChanged?.Invoke(); }
    private void OnSortChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedSort = (int)index; _viewState.SortRequested = _viewState.SelectedTab == 0; ViewChanged?.Invoke(); }
}
