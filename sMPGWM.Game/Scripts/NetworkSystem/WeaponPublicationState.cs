using System;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public sealed class WeaponPublicationState
{
    private readonly long[] _revisions = new long[WeaponStateCodec.MaxStates];
    private long _generation = -1;
    private string _entity = "";
    private long _configuration;
    private long _inventory;
    private long _equipment;

    public bool MatchesBinding(PlayerSession owner) => _generation == owner.ControlGeneration &&
        _entity == (owner.ControlledShip?.EntityId ?? "") &&
        _configuration == (owner.ControlledShip?.Loadout.Weapons.ConfigurationRevision ?? 0) &&
        _inventory == owner.Inventory.Revision && _equipment == (owner.ControlledShip?.Equipment.Revision ?? 0);

    internal bool Changed(int slot, long revision) => _revisions[slot] != revision;

    public void Capture(PlayerSession owner, ItemStore items)
    {
        if (owner.ControlledShip?.Equipment.Container.Slots.Count > 64)
            throw new InvalidOperationException("Weapon equipment exceeds replication capacity.");
        if (!MatchesBinding(owner)) Array.Clear(_revisions);
        _generation = owner.ControlGeneration;
        _entity = owner.ControlledShip?.EntityId ?? "";
        _configuration = owner.ControlledShip?.Loadout.Weapons.ConfigurationRevision ?? 0;
        _inventory = owner.Inventory.Revision;
        _equipment = owner.ControlledShip?.Equipment.Revision ?? 0;
        Capture(owner.Inventory.Container, items, 0);
        if (owner.ControlledShip is { } ship) Capture(ship.Equipment.Container, items, ItemContainer.MaxCapacity);
    }

    private void Capture(ItemContainer container, ItemStore items, int offset)
    {
        for (var i = 0; i < container.Slots.Count; i++)
            if (container.Slots[i] is { } id && items.Weapons.TryGet(id, out var state))
                _revisions[offset + i] = state.Revision;
    }
}
