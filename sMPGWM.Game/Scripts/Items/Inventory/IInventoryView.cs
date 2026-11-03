

using System;

namespace sMPGWM.Scripts.Items.Inventory;

public interface IInventoryView
{
    InventorySnapshot Snapshot { get; }
    event Action<InventorySnapshot>? Changed;
}
