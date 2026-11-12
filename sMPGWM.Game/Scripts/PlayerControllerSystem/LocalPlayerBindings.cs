using System;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.PlayerControllerSystem;

internal sealed class LocalPlayerBindings : IDisposable
{
    private readonly PlayerController _player;
    private readonly NetworkManager _network;
    private readonly UiCanvas _canvas;
    private readonly InventoryEquipmentTransferService _transfers;
    private readonly ItemHotbarController _hotbar;
    private readonly AbilityTargetingController _targeting;
    private readonly IGameFixture _fixture;
    private readonly InventoryController _emptyInventory;
    private readonly EquipmentController _emptyEquipment;
    private PlayerSession? _owner;
    private bool _disposed;
    private ShipController? _boundShip;
    private bool _admissionAvailable;

    public LocalPlayerBindings(PlayerController player, NetworkManager network, UiCanvas canvas,
        InventoryEquipmentTransferService transfers, ItemHotbarController hotbar,
        AbilityTargetingController targeting, IGameFixture fixture,
        InventoryController emptyInventory, EquipmentController emptyEquipment)
    {
        _player = player; _network = network; _canvas = canvas; _transfers = transfers;
        _hotbar = hotbar; _targeting = targeting; _fixture = fixture;
        _emptyInventory = emptyInventory; _emptyEquipment = emptyEquipment;
        player.ParticipantChanged += Bind;
        network.ItemBindingChanged += Refresh;
        hotbar.EquipmentAbilitiesAvailable = () => _network.ItemBaselineReady && _owner?.ControlledShip?.Runtime.AbilityAdmissionAvailable == true;
        Bind(player.Participant);
    }

    private void Bind(PlayerSession? owner)
    {
        if (_owner != null) _owner.ControlledShipChanged -= ShipChanged;
        if (!_network.World.AuthoritativeItems && _owner?.ControlledShip is { } previous)
            previous.Loadout.EvictReplica();
        _owner = owner;
        if (_owner != null) _owner.ControlledShipChanged += ShipChanged;
        _network.ResetItemBinding();
    }

    private void ShipChanged(ShipController? previous, ShipController? next)
    {
        _transfers.RevokeEditSession();
        _targeting.BindEquipment(null);
        if (!_network.World.AuthoritativeItems && previous != null) previous.Loadout.EvictReplica();
        _network.ResetItemBinding();
    }

    private void RuntimeChanged()
    {
        var available = _boundShip?.Runtime.AbilityAdmissionAvailable == true;
        if (available == _admissionAvailable) return;
        _admissionAvailable = available;
        _hotbar.InvalidateAvailability();
    }

    private void Refresh()
    {
        if (_boundShip != null) _boundShip.Runtime.Changed -= RuntimeChanged;
        _boundShip = _owner?.ControlledShip;
        _admissionAvailable = _boundShip?.Runtime.AbilityAdmissionAvailable == true;
        if (_boundShip != null) _boundShip.Runtime.Changed += RuntimeChanged;
        _transfers.RevokeEditSession();
        _hotbar.SetEditSession(0);
        _targeting.BindEquipment(null);
        _canvas.UnbindInventory();
        var inventory = _owner?.Inventory ?? _emptyInventory;
        var equipment = _owner?.ControlledShip?.Equipment ?? _emptyEquipment;
        _transfers.Rebind(inventory, equipment);
        _transfers.Ready = _owner != null && _network.ItemBaselineReady;
        _hotbar.Rebind(inventory, equipment);
        _canvas.BindInventory(inventory, equipment, _transfers);
        _canvas.BindHotbar(_hotbar, _fixture.Actions);
        _targeting.BindEquipment(_owner?.ControlledShip?.Equipment);
    }

    public void VerifyReady()
    {
        if (_disposed || _owner == null || _owner != _player.Participant ||
            _boundShip == null || _boundShip != _owner.ControlledShip ||
            !_network.ItemBaselineReady || !_transfers.Ready)
            throw new InvalidOperationException("Local inventory, equipment, and ship bindings are incomplete.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_boundShip != null) _boundShip.Runtime.Changed -= RuntimeChanged;
        _player.ParticipantChanged -= Bind;
        _network.ItemBindingChanged -= Refresh;
        if (_owner != null) _owner.ControlledShipChanged -= ShipChanged;
        _transfers.RevokeEditSession();
        _hotbar.EquipmentAbilitiesAvailable = null;
        _targeting.BindEquipment(null);
        _canvas.UnbindInventory();
    }
}
