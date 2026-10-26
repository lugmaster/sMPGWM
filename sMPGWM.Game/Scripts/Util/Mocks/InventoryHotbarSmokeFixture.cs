#nullable enable

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
    private InventoryImpl _inventory = null!;
    private EquipmentImpl _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemHotbarState _state = null!;
    private ItemHotbarAdapter _adapter = null!;
    private UiCanvas _canvas = null!;
    private int _activations;

    public void Show(GamePlayManager game)
    {
        var potion = new ItemDefinitionId("hotbar_potion");
        var weapon = new ItemDefinitionId("hotbar_weapon");
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(potion, "Hotbar Potion", ItemCategories.Consumable, maximumStackSize: 10),
            new ItemDefinition(weapon, "Hotbar Weapon", ItemCategories.LightWeapon)
        });
        _inventory = new InventoryImpl(catalog);
        _equipment = new EquipmentImpl(catalog);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _state = new ItemHotbarState(Enumerable.Range(0, 10).Select(index => new ActionHotbarBinding(index.ToString())));
        _adapter = new ItemHotbarAdapter(_state, _inventory, _equipment, _transfers);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = catalog.CreateEntity(potion, 7);
        slots[1] = catalog.CreateEntity(potion, 8);
        slots[2] = catalog.CreateEntity(weapon);
        _inventory.Install(_inventory.PrepareChange(slots));
        var equipment = _equipment.Snapshot.Slots.ToArray();
        equipment[4] = catalog.CreateEntity(weapon);
        _equipment.Install(_equipment.PrepareChange(equipment));
        _canvas = game.GetNode<UiCanvas>("UiCanvas");
        _canvas.CloseAllMenus();
        _canvas.BindInventory(_inventory, _equipment, _transfers);
        _canvas.BindHotbar(_state, _adapter, catalog, PlayerMock.Instance.GetHotBarSkills());
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
        _canvas.BindInventory(game.InventoryImpl, game.EquipmentImpl, game.ItemTransfers);
        _canvas.BindHotbar(game.ItemHotbar, game.HotbarAdapter, game.Items, PlayerMock.Instance.GetHotBarSkills());
        _state.Dispose();
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }
}
