using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal static class InventoryProjection
{
    internal static IReadOnlyList<InventoryCellBinding> Create(InventorySnapshot snapshot)
    {
        var cells = new InventoryCellBinding[snapshot.Capacity];
        var index = 0;
        foreach (var entry in snapshot.Occupied)
            cells[index++] = new InventoryCellBinding(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses)
            cells[index++] = new InventoryCellBinding(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }
}
