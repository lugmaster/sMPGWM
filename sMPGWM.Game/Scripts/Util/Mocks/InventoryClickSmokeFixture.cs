

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
    private InventoryController _inventory = null!;
    private EquipmentController _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private UiCanvas _canvas = null!;
    private ItemEntityId _engine;

    public void Show(GamePlayManager game)
    {
        var engine = "mock_standard_engine";
        var potion = "mock_standard_consumable";
        _inventory = new InventoryController();
        _equipment = new EquipmentController();
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = ItemRegistry.CreateEntity(engine);
        _engine = slots[0]!.Id;
        var stacks = Enumerable.Range(0, 3).Select(_ => ItemRegistry.CreateEntity(potion)).OrderBy(entity => entity.Id.Value).ToArray();
        slots[1] = new ItemEntity(stacks[0].Id, ItemRegistry.GetById(potion), 7);
        slots[2] = new ItemEntity(stacks[1].Id, ItemRegistry.GetById(potion), 8);
        slots[3] = new ItemEntity(stacks[2].Id, ItemRegistry.GetById(potion), 10);
        _inventory.Install(_inventory.PrepareChange(slots));
        _canvas = game.GetNode<UiCanvas>("UiCanvas");
        _canvas.CloseAllMenus();
        game.BindInventoryPresentation(_inventory, _equipment, _transfers);
        _canvas.OpenMenu(UiWindowId.InventoryMenu);
    }

    public bool Carrying() => _canvas.GetNode<HFlowContainer>("Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow")
        .GetChildren().OfType<InventoryMenu>().Any(menu => menu.IsCarrying);

    public bool EngineEquipped() => _equipment.Snapshot.Slots[0]?.Id == _engine;
    public int CountAt(int address) => _inventory.Snapshot.Slots[address]?.Count ?? 0;
    public long Revision() => _inventory.Revision;
    public void Fill() => _inventory.Add("mock_standard_engine", 124);

    public void Restore(GamePlayManager game)
    {
        _canvas.CloseAllMenus();
        game.BindInventoryPresentation(game.InventoryController, game.EquipmentController, game.ItemTransfers);
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
