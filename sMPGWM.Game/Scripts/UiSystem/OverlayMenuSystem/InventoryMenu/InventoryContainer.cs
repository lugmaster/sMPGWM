using Godot;
using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryContainer : VBoxContainer
{
    private InventoryViewState _viewState = null!;
    private LineEdit _search = null!;
    private TabBar _tabs = null!;
    private OptionButton _sort = null!;
    private ItemCatalog _catalog = null!;
    private Label _emptyResult = null!;
    internal event Action? ViewChanging;
    internal event Action? ViewChanged;

    internal bool IsToolbarControl(Control control) =>
        control == _tabs || _tabs.IsAncestorOf(control) ||
        GetNode<Control>("FilterToolbar").IsAncestorOf(control);

    internal void Bind(InventoryViewState state, ItemCatalog catalog)
    {
        Unbind();
        _viewState = state;
        _catalog = catalog;
        _search = GetNode<LineEdit>("FilterToolbar/LineEdit");
        _sort = GetNode<OptionButton>("FilterToolbar/SortSelector");
        _tabs = GetNode<TabBar>("%CategoryTabs");
        _tabs.ClearTabs();
        foreach (var tab in CategoryTab.Create(catalog.Categories)) _tabs.AddTab(tab.Title);
        _sort.Clear();
        _sort.AddItem("Name ascending");
        _sort.AddItem("Name descending");
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
        _search.TextChanged -= OnSearchChanged;
        _tabs.TabChanged -= OnTabChanged;
        _sort.ItemSelected -= OnSortChanged;
        _viewState = null!;
    }

    internal void Render(InventorySnapshot snapshot, ItemVisuals visuals)
    {
        var cells = InventoryProjection.Create(snapshot, _catalog, _viewState);
        _emptyResult.Visible = cells.Count == snapshot.EmptyAddresses.Count;
        GetNode<ItemGrid>("%ItemGrid").Render(cells,
            (icon, binding) => visuals.Apply(icon, binding.Entity));
    }

    public override void _ExitTree() => Unbind();

    private void OnSearchChanged(string text) { ViewChanging?.Invoke(); _viewState.SearchText = text; ViewChanged?.Invoke(); }
    private void OnTabChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedTab = (int)index; ViewChanged?.Invoke(); }
    private void OnSortChanged(long index) { ViewChanging?.Invoke(); _viewState.SelectedSort = (int)index; ViewChanged?.Invoke(); }
}
