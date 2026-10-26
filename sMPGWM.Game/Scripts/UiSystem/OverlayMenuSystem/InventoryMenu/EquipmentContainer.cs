using Godot;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class EquipmentContainer : GridContainer
{
    private readonly List<IconWithLabel> _cells = new();

    public override void _Ready()
    {
        foreach (var preview in GetChildren())
        {
            RemoveChild(preview);
            preview.QueueFree();
        }
    }

    internal void Render(IReadOnlyList<EquipmentSlotDefinition> definitions, EquipmentSnapshot snapshot)
    {
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            if (index == _cells.Count)
            {
                var cell = HudProvider.CreateIconWithLabel();
                cell.LabelText = definition.DisplayName;
                _cells.Add(cell);
                AddChild(cell);
            }

            var icon = _cells[index].Cell;
            icon.Binding = new EquipmentCellBinding(definition.Key, snapshot.Slots[index], snapshot.Revision);
            icon.IconTexture = null;
            icon.TooltipText = definition.DisplayName;
        }
    }
}
