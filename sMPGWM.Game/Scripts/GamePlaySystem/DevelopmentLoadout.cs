using System;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ShipSystem;
namespace sMPGWM.Scripts.GamePlaySystem;
public static class DevelopmentLoadout
{
    public static void Equip(ShipLoadout loadout, InventoryController inventory)
    {
        StartLoadout.Equip(loadout, inventory);
        if (inventory.Items.CreateItems(inventory.Container, "laser_mk1", 1) != InventoryResult.Success)
            throw new InvalidOperationException("Could not create development weapon.");
    }
}


