#nullable enable

using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventoryClickSmokeFixture : RefCounted
{
    private InventoryImpl _inventory = null!;
    private EquipmentImpl _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private UiCanvas _canvas = null!;
    private ItemEntityId _engine;

    public void Show(GamePlayManager game)
    {
        var engine = new ItemDefinitionId("click_engine");
        var potion = new ItemDefinitionId("click_potion");
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(engine, "Click Engine", ItemCategories.Engine),
            new ItemDefinition(potion, "Click Potion", ItemCategories.Consumable, maximumStackSize: 10)
        });
        _inventory = new InventoryImpl(catalog);
        _equipment = new EquipmentImpl(catalog);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = catalog.CreateEntity(engine);
        _engine = slots[0]!.Id;
        var stacks = Enumerable.Range(0, 3).Select(_ => catalog.CreateEntity(potion)).OrderBy(entity => entity.Id.Value).ToArray();
        slots[1] = new ItemEntity(stacks[0].Id, potion, 7);
        slots[2] = new ItemEntity(stacks[1].Id, potion, 8);
        slots[3] = new ItemEntity(stacks[2].Id, potion, 10);
        _inventory.Install(_inventory.PrepareChange(slots));
        _canvas = game.GetNode<UiCanvas>("UiCanvas");
        _canvas.CloseAllMenus();
        _canvas.BindInventory(_inventory, _equipment, _transfers);
        _canvas.OpenMenu(UiWindowId.InventoryMenu);
    }

    public bool Carrying() => _canvas.GetNode<HFlowContainer>("Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow")
        .GetChildren().OfType<InventoryMenu>().Any(menu => menu.IsCarrying);

    public bool EngineEquipped() => _equipment.Snapshot.Slots[0]?.Id == _engine;
    public int CountAt(int address) => _inventory.Snapshot.Slots[address]?.Count ?? 0;
    public long Revision() => _inventory.Revision;
    public void Fill() => _inventory.Add(new ItemDefinitionId("click_engine"), 124);

    public void Restore(GamePlayManager game)
    {
        _canvas.CloseAllMenus();
        _canvas.BindInventory(game.InventoryImpl, game.EquipmentImpl, game.ItemTransfers);
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
