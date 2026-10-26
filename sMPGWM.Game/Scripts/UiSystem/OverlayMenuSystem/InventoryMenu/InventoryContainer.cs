using Godot;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryContainer : VBoxContainer
{
    private InventoryViewState _viewState = null!;
    private LineEdit _search = null!;
    private TabBar _tabs = null!;
    private OptionButton _sort = null!;

    internal void Bind(InventoryViewState state)
    {
        Unbind();
        _viewState = state;
        _search = GetNode<LineEdit>("FilterToolbar/LineEdit");
        _sort = GetNode<OptionButton>("FilterToolbar/SortSelector");
        _tabs = GetNode<TabBar>("%CategoryTabs");
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

    internal void Render(InventorySnapshot snapshot) =>
        GetNode<ItemGrid>("%ItemGrid").Render(InventoryProjection.Create(snapshot));

    public override void _ExitTree() => Unbind();

    private void OnSearchChanged(string text) => _viewState.SearchText = text;
    private void OnTabChanged(long index) => _viewState.SelectedTab = (int)index;
    private void OnSortChanged(long index) => _viewState.SelectedSort = (int)index;
}
