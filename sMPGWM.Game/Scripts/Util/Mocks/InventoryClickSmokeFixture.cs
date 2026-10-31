

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
        _equipment = new EquipmentController(_inventory.ItemRequestService);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        ItemFixture.Place(_inventory.ItemRequestService, _inventory.Container, 0, engine);
        _engine = _inventory.Snapshot.Slots[0]!.Id;
        // Keep the visible stack order deterministic: the UI sorts equal definitions by identity.
        var stacks = Enumerable.Range(0, 3).Select(_ =>
        {
            var staging = _inventory.ItemRequestService.CreateContainer(1);
            _inventory.ItemRequestService.CreateItems(staging, potion, 1);
            return staging;
        }).OrderBy(container => container.Slots[0]!.Value.Value).ToArray();
        var counts = new[] { 7, 8, 10 };
        for (var index = 0; index < stacks.Length; index++)
        {
            var staging = stacks[index];
            _inventory.ItemRequestService.CreateItems(staging, potion, counts[index] - 1);
            var id = staging.Slots[0]!.Value;
            _inventory.ItemRequestService.TransferItem(id, _inventory.ItemRequestService.GetLocation(id),
                new(_inventory.Container.Id, index + 1), staging.Revision, _inventory.Revision);
        }
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
    public void Fill() => _inventory.ItemRequestService.CreateItems(_inventory.Container, "mock_standard_engine", 124);

    public void Restore(GamePlayManager game)
    {
        _canvas.CloseAllMenus();
        game.BindInventoryPresentation(game.InventoryController, game.EquipmentController, game.ItemTransfers);
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
