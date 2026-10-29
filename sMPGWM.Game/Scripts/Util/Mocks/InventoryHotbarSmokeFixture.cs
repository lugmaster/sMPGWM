

using System;
using System.Linq;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class InventoryHotbarSmokeFixture : RefCounted
{
    private InventoryController _inventory = null!;
    private EquipmentController _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemHotbarState _state = null!;
    private ItemHotbarAdapter _adapter = null!;
    private UiCanvas _canvas = null!;
    private int _activations;

    public void Show(GamePlayManager game)
    {
        var potion = "mock_standard_consumable";
        var weapon = "mock_standard_light_weapon";
        _inventory = new InventoryController();
        _equipment = new EquipmentController();
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _state = new ItemHotbarState(Enumerable.Range(0, 10).Select(index => new ActionHotbarBinding(index.ToString())));
        _adapter = new ItemHotbarAdapter(_state, _inventory, _equipment, _transfers);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = ItemRegistry.CreateEntity(potion, 7);
        slots[1] = ItemRegistry.CreateEntity(potion, 8);
        slots[2] = ItemRegistry.CreateEntity(weapon);
        _inventory.Install(_inventory.PrepareChange(slots));
        var equipment = _equipment.Snapshot.Slots.ToArray();
        equipment[4] = ItemRegistry.CreateEntity(weapon);
        _equipment.Install(_equipment.PrepareChange(equipment));
        _canvas = game.GetNode<UiCanvas>("UiCanvas");
        _canvas.CloseAllMenus();
        game.BindInventoryPresentation(_inventory, _equipment, _transfers);
        _canvas.BindHotbar(_state, _adapter, PlayerMock.Instance.GetHotBarSkills());
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
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = null;
        slots[1] = null;
        _inventory.Install(_inventory.PrepareChange(slots));
        _inventory.NotifyObservers();
        var equipment = _equipment.Snapshot.Slots.ToArray();
        equipment[4] = null;
        _equipment.Install(_equipment.PrepareChange(equipment));
        _equipment.NotifyObservers();
    }

    public void Restore(GamePlayManager game)
    {
        _canvas.CloseAllMenus();
        _canvas.OnRequestHotbarAction -= OnActivation;
        game.BindInventoryPresentation(game.InventoryController, game.EquipmentController, game.ItemTransfers);
        _canvas.BindHotbar(game.ItemHotbar, game.HotbarAdapter, PlayerMock.Instance.GetHotBarSkills());
        _state.Dispose();
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
