#nullable enable

using sMPGWM.Scripts.Util.UiComponents;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.EquipmentSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "InventoryMenu";

    private InventoryImpl? _inventory;
    private EquipmentImpl? _equipment;
    private InventoryViewState _viewState = null!;
    private InventoryContainer _inventoryView = null!;
    private EquipmentContainer _equipmentView = null!;
    private ItemVisuals _visuals = null!;

    internal void Bind(InventoryImpl inventoryImpl, EquipmentImpl equipmentImpl,
        InventoryViewState state)
    {
        Unbind();
        _inventory = inventoryImpl;
        _equipment = equipmentImpl;
        _visuals = new ItemVisuals(inventoryImpl.Catalog);
        _viewState = state;
        if (IsNodeReady()) BindViews();
    }

    protected override void OnReady()
    {
        base.OnReady();
        _inventoryView = GetNode<InventoryContainer>("%InventoryContainer");
        _equipmentView = GetNode<EquipmentContainer>("%EquipmentContainer");
        if (_inventory != null) BindViews();
    }

    private void BindViews()
    {
        _inventoryView.Bind(_viewState);
        _inventory!.Changed += RefreshInventory;
        _equipment!.Changed += RefreshEquipment;
        RefreshInventory(_inventory.Snapshot);
        RefreshEquipment(_equipment.Snapshot);
    }

    internal void Unbind()
    {
        DismissTooltips();
        if (_inventory != null) _inventory.Changed -= RefreshInventory;
        if (_equipment != null) _equipment.Changed -= RefreshEquipment;
        _inventoryView?.Unbind();
        _inventory = null;
        _equipment = null;
    }

    internal void DismissTooltips()
    {
        if (!IsNodeReady()) return;
        foreach (var icon in FindChildren("*", "Button", true, false))
            if (icon is ClickableIcon cell) cell.DismissTooltip();
    }

    public override void _ExitTree()
    {
        Unbind();
        base._ExitTree();
    }

    private void RefreshInventory(InventorySnapshot snapshot) => _inventoryView.Render(snapshot, _visuals);
    private void RefreshEquipment(EquipmentSnapshot snapshot) => _equipmentView.Render(_equipment!.Definitions, snapshot, _visuals);
}
