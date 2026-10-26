#nullable enable

using Godot;
using System;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class EquipmentContainer : GridContainer
{
    private readonly List<ClickableIcon> _cells = new();
    internal IReadOnlyList<ClickableIcon> Cells => _cells;
    internal event Action<ClickableIcon>? CellCreated;

    public override void _Ready()
    {
        foreach (var preview in GetChildren())
        {
            RemoveChild(preview);
            preview.QueueFree();
        }
    }

    internal void Render(IReadOnlyList<EquipmentSlotDefinition> definitions, EquipmentSnapshot snapshot, ItemVisuals visuals)
    {
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            if (index == _cells.Count)
            {
                var cell = HudProvider.CreateIconWithLabel();
                cell.LabelText = definition.DisplayName;
                _cells.Add(cell.Cell);
                AddChild(cell);
                CellCreated?.Invoke(cell.Cell);
            }

            var icon = _cells[index];
            icon.IconSize = IconSize.Medium;
            icon.Binding = ItemCellBinding.Equipment(definition.Key, snapshot.Slots[index], snapshot.Revision);
            visuals.Apply(icon, snapshot.Slots[index], definition.DisplayName);
        }
    }
}
