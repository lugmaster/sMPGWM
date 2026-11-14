using sMPGWM.Scripts.GameplayDataSystem;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;
namespace sMPGWM.Tests;
internal sealed class ShipItemTestContext : IDisposable
{
    private readonly ShipController _ship;
    public PlayerSession Player { get; }
    public ItemStore Store => Player.Inventory.Items;
    public InventoryController Inventory => Player.Inventory;
    public EquipmentController Equipment => _ship.Equipment;
    public ShipItemTestContext()
    {
        var store = new ItemStore();
        _ship = ShipFactory.Create(GameplayDefinitions.Current.Ships.GetById("fighter"), "11111111111111111111111111111111", Vector2.Zero, 0, Vector2.Zero, store, false);
        Player = new(store) { ControllingPeerId = 1 };
        Player.AssignShip(_ship);
    }
    public TransferResult Transfer(TransferRequest request) => Player.Transfer(request);
    public bool Provides(string slot, long source, string ability)
    {
        var index = Equipment.FindSlot(slot);
        if (index < 0 || source <= 0 || Equipment.Snapshot.Slots[index] is not { } item || item.Id.Value != (ulong)source) return false;
        foreach (var id in item.Definition.AbilityIds) if (id == ability) return true;
        return false;
    }
    public void Dispose()
    {
        Player.Dispose();
        _ship.Loadout.Dispose();
        _ship.Runtime.Dispose();
        _ship.Free();
    }
}
