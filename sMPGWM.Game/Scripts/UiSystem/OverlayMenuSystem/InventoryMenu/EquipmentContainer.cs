

using Godot;
using System;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using ClickableIcon = sMPGWM.Scripts.UtilitySystem.UiComponents.ClickableIcon;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class EquipmentContainer : TabContainer
{
    private readonly List<ClickableIcon> _cells = new();
    private GridContainer _shipSlots = null!;
    private ItemGrid _weaponSlots = null!;
    private ItemCellBinding[] _weaponBindings = Array.Empty<ItemCellBinding>();
    private Label _weaponCapacity = null!;
    internal IReadOnlyList<ClickableIcon> Cells => _cells;
    internal event Action<ClickableIcon>? CellCreated;
    internal event Action? ViewChanging;

    internal bool IsToolbarControl(Control control) =>
        control == GetTabBar() || GetTabBar().IsAncestorOf(control);

    public override void _Ready()
    {
        _shipSlots = GetNode<GridContainer>("Ship");
        _weaponSlots = GetNode<ItemGrid>("Weapons/Slots");
        var capacity = GetNode<Control>("Weapons/Capacity");
        capacity.TooltipText = "Free weapon slots / fixture capacity";
        capacity.GetNode<TextureRect>("Icon").Texture = ItemIconResolver.Category(ItemRegistry.GetCategoryById("weapon_fixture"));
        _weaponCapacity = capacity.GetNode<Label>("Value");
        SetTabIcon(0, ItemIconResolver.Category(ItemRegistry.GetCategoryById("hull")));
        SetTabIcon(1, ItemIconResolver.Category(ItemRegistry.GetCategoryById("light_weapon")));
        SetTabIcon(2, Icons.Travel.PersonPeopleThree.IconTexture);
        for (var index = 0; index < GetTabCount(); index++) SetTabIconMaxWidth(index, 14);
        TabChanged += OnTabChanged;
    }

    private void OnTabChanged(long index)
    {
        ViewChanging?.Invoke();
        foreach (var cell in _cells) cell.DismissTooltip();
    }

    internal void Render(IReadOnlyList<EquipmentSlotDefinition> definitions, EquipmentSnapshot snapshot, ItemVisuals visuals)
    {
        _weaponCapacity.Text = $"{snapshot.FreeWeaponSlots}/{snapshot.MaxWeaponSlots}";
        var weaponCount = 0;
        foreach (var definition in definitions)
            if (definition.IsWeaponSlot) weaponCount++;
        if (_weaponBindings.Length != weaponCount) _weaponBindings = new ItemCellBinding[weaponCount];
        var weaponIndex = 0;
        for (var index = 0; index < definitions.Count; index++)
            if (definitions[index].IsWeaponSlot)
                _weaponBindings[weaponIndex++] = ItemCellBinding.Equipment(definitions[index].Key, snapshot.Slots[index], snapshot.Revision);
        var emptyShown = false;
        _weaponSlots.Render(_weaponBindings, (icon, binding) =>
        {
            icon.Visible = snapshot.MaxWeaponSlots > 0 &&
                (binding.Entity != null || !emptyShown && snapshot.FreeWeaponSlots > 0);
            if (icon.Visible && binding.Entity == null) emptyShown = true;
        });
        _weaponSlots.Visible = snapshot.MaxWeaponSlots > 0;
        weaponIndex = 0;
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            if (index == _cells.Count)
            {
                if (definition.IsWeaponSlot) _cells.Add(_weaponSlots.Cells[weaponIndex]);
                else
                {
                    var cell = HudProvider.CreateClickableIconWithLabel();
                    cell.LabelText = definition.DisplayName;
                    _cells.Add(cell.Cell);
                    _shipSlots.AddChild(cell);
                }
                CellCreated?.Invoke(_cells[index]);
            }

            var icon = _cells[index];
            icon.IconSize = IconSize.Medium;
            if (!definition.IsWeaponSlot)
                icon.Binding = ItemCellBinding.Equipment(definition.Key, snapshot.Slots[index], snapshot.Revision);
            visuals.Apply(icon, snapshot.Slots[index], definition.DisplayName);
            if (definition.IsWeaponSlot) weaponIndex++;
        }
    }
}
