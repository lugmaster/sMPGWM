using System;
using System.Collections.Generic;
using System.IO;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public static class WeaponStateCodec
{
    public const int MaxStates = ItemContainer.MaxCapacity + 64;
    public const int MaxBytes = 300000;
    public static byte[] Encode(PlayerSession owner, ItemStore items)
    {
        var states = new List<WeaponStateSnapshot>();
        Add(owner.Inventory.Container, items, states);
        if (owner.ControlledShip is { } ship) Add(ship.Equipment.Container, items, states);
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        w.Write(owner.ControlGeneration);
        w.Write(owner.ControlledShip != null);
        if (owner.ControlledShip is { } controlled)
        {
            w.Write(Guid.ParseExact(controlled.EntityId, "N").ToByteArray());
            var config = controlled.Loadout.Weapons.CaptureConfiguration();
            w.Write(config.Revision); w.Write(config.Groups.Length);
            for (var i = 0; i < config.Groups.Length; i++) { w.Write(config.Groups[i]); w.Write(config.Items[i].Value); }
        }
        w.Write(states.Count);
        foreach (var s in states)
        {
            w.Write(s.ItemId.Value); w.Write(s.Revision); w.Write(s.Rounds); w.Write(s.NextFireTick);
            w.Write(s.ReloadStartTick); w.Write(s.ReloadDeadline); w.Write(s.ShotSequence);
        }
        return stream.ToArray();
    }
    private static void Add(ItemContainer container, ItemStore items, List<WeaponStateSnapshot> states)
    {
        foreach (var id in container.Slots)
            if (id is { } item && items.Weapons.TryGet(item, out var state)) states.Add(state.CaptureState());
    }
    public static bool Apply(byte[] bytes, PlayerSession owner, ItemStore items) => Apply(bytes, owner, items, out _);
    public static bool Apply(byte[] bytes, PlayerSession owner, ItemStore items, out bool applied)
    {
        applied = false;
        if (bytes.Length > MaxBytes || bytes.Length < 13) return false;
        try
        {
            using var r = new BinaryReader(new MemoryStream(bytes));
            var generation = r.ReadInt64(); var hasShip = r.ReadByte();
            if (generation < owner.ControlGeneration) return true;
            if (generation != owner.ControlGeneration || hasShip > 1 || (hasShip != 0) != (owner.ControlledShip != null)) return false;
            WeaponConfigurationSnapshot? config = null;
            if (hasShip != 0)
            {
                var entity = new Guid(r.ReadBytes(16)).ToString("N"); var revision = r.ReadInt64(); var slots = r.ReadInt32();
                if (entity != owner.ControlledShip!.EntityId || slots < 0 || slots > 64) return false;
                var groups = new byte[slots]; var ids = new ItemEntityId[slots];
                for (var i = 0; i < slots; i++) { groups[i] = r.ReadByte(); var id = r.ReadInt64(); if (id < 0) return false; ids[i] = id == 0 ? default : ItemEntityId.FromNetwork(id); }
                config = new(revision, groups, ids);
                if (revision < owner.ControlledShip.Loadout.Weapons.ConfigurationRevision) return true;
                if (!owner.ControlledShip.Loadout.Weapons.ValidateConfiguration(config)) return false;
            }
            var count = r.ReadInt32(); if (count < 0 || count > MaxStates) return false;
            var expected = new HashSet<ItemEntityId>();
            Collect(owner.Inventory.Container, items, expected);
            if (owner.ControlledShip is { } ship) Collect(ship.Equipment.Container, items, expected);
            if (count != expected.Count) return false;
            var states = new WeaponStateSnapshot[count];
            for (var i = 0; i < count; i++)
            {
                var id = ItemEntityId.FromNetwork(r.ReadInt64());
                states[i] = new(id, r.ReadInt64(), r.ReadInt32(), r.ReadDouble(), r.ReadInt64(), r.ReadInt64(), r.ReadUInt64());
                if (!expected.Remove(id) || !items.Weapons.Validate(states[i], items)) return false;
            }
            if (r.BaseStream.Position != r.BaseStream.Length) return false;
            if (!items.Weapons.Confirm(states, items)) return false;
            applied = config == null || owner.ControlledShip!.Loadout.Weapons.ConfirmConfiguration(config);
            return applied;
        }
        catch (Exception e) when (e is IOException or ArgumentException) { return false; }
    }
    private static void Collect(ItemContainer container, ItemStore items, HashSet<ItemEntityId> ids)
    {
        foreach (var id in container.Slots)
            if (id is { } item && items.GetItem(item).Definition.Weapon != null) ids.Add(item);
    }
}
