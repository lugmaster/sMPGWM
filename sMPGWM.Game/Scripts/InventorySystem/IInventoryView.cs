

using System;

namespace sMPGWM.Scripts.InventorySystem;

public interface IInventoryView
{
    InventorySnapshot Snapshot { get; }
    event Action<InventorySnapshot>? Changed;
}
