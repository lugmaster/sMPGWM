using Godot;
using System.Collections.Generic;
using System;
using sMPGWM.Scripts.UiSystem.HudSystem;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class ItemGrid: ScrollContainer
{
    private GridContainer _slotGrid = null!;
    private readonly List<ClickableIcon> _cells = new();

    public override void _Ready()
    {
        _slotGrid = GetNode<GridContainer>("%SlotGrid");
        foreach (var preview in _slotGrid.GetChildren())
        {
            _slotGrid.RemoveChild(preview);
            preview.QueueFree();
        }
    }

    internal void Render<TBinding>(IReadOnlyList<TBinding> bindings, Action<ClickableIcon, TBinding> render)
    {
        while (_cells.Count > bindings.Count)
        {
            var last = _cells[^1];
            _cells.RemoveAt(_cells.Count - 1);
            _slotGrid.RemoveChild(last);
            last.QueueFree();
        }

        for (var index = 0; index < bindings.Count; index++)
        {
            if (index == _cells.Count)
            {
                var cell = HudProvider.CreateClickableIcon();
                cell.IconSize = IconSize.Medium;
                _cells.Add(cell);
                _slotGrid.AddChild(cell);
            }

            _cells[index].Binding = bindings[index];
            render(_cells[index], bindings[index]);
        }
    }
}
