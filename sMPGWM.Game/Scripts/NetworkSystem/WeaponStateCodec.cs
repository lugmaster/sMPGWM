using System;
using System.Buffers.Binary;
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
    private const int HeaderBytes = 13;
    private const int ConfigurationBytes = 28;
    private const int BindingBytes = 9;
    private const int StateBytes = 52;

    public static byte[] Encode(PlayerSession owner, ItemStore items)
    {
        var ship = owner.ControlledShip;
        var count = Count(owner.Inventory.Container, items);
        if (ship != null) count += Count(ship.Equipment.Container, items);
        var configurationBytes = ship == null ? 0 : ConfigurationBytes + BindingBytes * ship.Loadout.Weapons.Bindings.Count;
        var bytes = new byte[HeaderBytes + configurationBytes + StateBytes * count];
        var remaining = bytes.AsSpan();
        BinaryPrimitives.WriteInt64LittleEndian(remaining, owner.ControlGeneration);
        remaining[8] = ship == null ? (byte)0 : (byte)1;
        remaining = remaining[9..];
        if (ship != null)
        {
            var weapons = ship.Loadout.Weapons;
            Guid.ParseExact(ship.EntityId, "N").TryWriteBytes(remaining);
            BinaryPrimitives.WriteInt64LittleEndian(remaining[16..], weapons.ConfigurationRevision);
            BinaryPrimitives.WriteInt32LittleEndian(remaining[24..], weapons.Bindings.Count);
            remaining = remaining[ConfigurationBytes..];
            for (var i = 0; i < weapons.Bindings.Count; i++)
            {
                remaining[0] = weapons.GetGroup(i);
                BinaryPrimitives.WriteUInt64LittleEndian(remaining[1..], weapons.Bindings[i]?.ItemId.Value ?? 0);
                remaining = remaining[BindingBytes..];
            }
        }
        BinaryPrimitives.WriteInt32LittleEndian(remaining, count);
        remaining = remaining[4..];
        Write(owner.Inventory.Container, items, ref remaining);
        if (ship != null) Write(ship.Equipment.Container, items, ref remaining);
        return bytes;
    }

    private static int Count(ItemContainer container, ItemStore items)
    {
        var count = 0;
        for (var i = 0; i < container.Slots.Count; i++)
            if (container.Slots[i] is { } item && items.Weapons.TryGet(item, out _)) count++;
        return count;
    }

    private static void Write(ItemContainer container, ItemStore items, ref Span<byte> remaining)
    {
        for (var i = 0; i < container.Slots.Count; i++)
        {
            if (container.Slots[i] is not { } item || !items.Weapons.TryGet(item, out var state)) continue;
            BinaryPrimitives.WriteUInt64LittleEndian(remaining, item.Value);
            BinaryPrimitives.WriteInt64LittleEndian(remaining[8..], state.Revision);
            BinaryPrimitives.WriteInt32LittleEndian(remaining[16..], state.Rounds);
            BinaryPrimitives.WriteDoubleLittleEndian(remaining[20..], state.NextFireTick);
            BinaryPrimitives.WriteInt64LittleEndian(remaining[28..], state.ReloadStartTick);
            BinaryPrimitives.WriteInt64LittleEndian(remaining[36..], state.ReloadDeadline);
            BinaryPrimitives.WriteUInt64LittleEndian(remaining[44..], state.ShotSequence);
            remaining = remaining[StateBytes..];
        }
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
