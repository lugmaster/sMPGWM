using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventoryHotbarSmokeFixture : RefCounted
{
    private InventoryController _inventory = null!;
    private EquipmentController _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemHotbarState _state = null!;
    private ItemHotbarController _controller = null!;
    private UiCanvas _canvas = null!;
    private int _activations;

    public void Show(GamePlayManager game)
    {
        var potion = "mock_standard_consumable";
        var weapon = "mock_standard_light_weapon";
        _inventory = new InventoryController();
        _equipment = new EquipmentController(_inventory.ItemRequestService);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _state = new ItemHotbarState(Enumerable.Range(0, 10).Select(index => new ActionHotbarBinding(index.ToString())));
        _controller = new ItemHotbarController(_state, _inventory, _equipment, _transfers);
        ItemFixture.Place(_inventory.ItemRequestService, _inventory.Container, 0, potion, 7);
        ItemFixture.Place(_inventory.ItemRequestService, _inventory.Container, 1, potion, 8);
        ItemFixture.Place(_inventory.ItemRequestService, _inventory.Container, 2, weapon);
        ItemFixture.Place(_inventory.ItemRequestService, _equipment.Container, 4, weapon);
        _canvas = game.GetNode<UiCanvas>("UiCanvas");
        _canvas.CloseAllMenus();
        game.BindInventoryPresentation(_inventory, _equipment, _transfers);
        _canvas.BindHotbar(_controller, PlayerMock.Instance.GetHotBarSkills());
        _canvas.OnRequestHotbarAction += OnActivation;
        _canvas.OpenMenu(UiWindowId.InventoryMenu);
    }

    private void OnActivation(int position) => _activations++;
    public void Observe(UiCanvas canvas)
    {
        _canvas = canvas;
        _canvas.OnRequestHotbarAction += OnActivation;
    }
    public void StopObserving() => _canvas.OnRequestHotbarAction -= OnActivation;
    public int Activations() => _activations;
    public long Revision() => _state.Revision;
    public long InventoryRevision() => _inventory.Revision;
    public string BindingAt(int position) => _state.Entries[position] switch
    {
        ConsumableHotbarBinding => "consumable",
        WeaponHotbarBinding weapon => weapon.SlotKey,
        ActionHotbarBinding action => action.ActionKey,
        _ => "empty"
    };

    public void EmptyLinkedSources()
    {
        ItemFixture.MoveAway(_inventory.ItemRequestService, _inventory.Container, 0, 1);
        ItemFixture.MoveAway(_inventory.ItemRequestService, _equipment.Container, 4);
    }

    public void Restore(GamePlayManager game)
    {
        _canvas.CloseAllMenus();
        _canvas.OnRequestHotbarAction -= OnActivation;
        game.BindInventoryPresentation(game.InventoryController, game.EquipmentController, game.ItemTransfers);
        _canvas.BindHotbar(game.HotbarController, PlayerMock.Instance.GetHotBarSkills());
        _state.Dispose();
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
