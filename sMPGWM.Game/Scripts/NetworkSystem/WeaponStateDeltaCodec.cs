using System;
using System.Buffers.Binary;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public static class WeaponStateDeltaCodec
{
    public const int HeaderBytes = 52;
    public const int StateBytes = 54;
    public const int MaxBytes = HeaderBytes + StateBytes * WeaponStateCodec.MaxStates;

    public static byte[]? Encode(PlayerSession owner, ItemStore items, WeaponPublicationState published)
    {
        if (!published.MatchesBinding(owner)) throw new InvalidOperationException("Weapon delta requires a matching baseline.");
        var count = Visit(owner.Inventory.Container, items, published, 0, Span<byte>.Empty);
        if (owner.ControlledShip is { } ship)
            count += Visit(ship.Equipment.Container, items, published, ItemContainer.MaxCapacity, Span<byte>.Empty);
        if (count == 0) return null;
        var bytes = new byte[HeaderBytes + count * StateBytes];
        var remaining = bytes.AsSpan();
        BinaryPrimitives.WriteInt64LittleEndian(remaining, owner.ControlGeneration);
        (owner.ControlledShip is { } controlled ? Guid.ParseExact(controlled.EntityId, "N") : Guid.Empty).TryWriteBytes(remaining[8..]);
        BinaryPrimitives.WriteInt64LittleEndian(remaining[24..], owner.ControlledShip?.Loadout.Weapons.ConfigurationRevision ?? 0);
        BinaryPrimitives.WriteInt64LittleEndian(remaining[32..], owner.Inventory.Revision);
        BinaryPrimitives.WriteInt64LittleEndian(remaining[40..], owner.ControlledShip?.Equipment.Revision ?? 0);
        BinaryPrimitives.WriteInt32LittleEndian(remaining[48..], count);
        remaining = remaining[HeaderBytes..];
        var written = Visit(owner.Inventory.Container, items, published, 0, remaining);
        if (owner.ControlledShip is { } equipped)
            Visit(equipped.Equipment.Container, items, published, ItemContainer.MaxCapacity, remaining[(written * StateBytes)..]);
        return bytes;
    }

    private static int Visit(ItemContainer container, ItemStore items, WeaponPublicationState published, int offset, Span<byte> bytes)
    {
        var count = 0;
        var write = !bytes.IsEmpty;
        for (var i = 0; i < container.Slots.Count; i++)
        {
            if (container.Slots[i] is not { } id || !items.Weapons.TryGet(id, out var state) || !published.Changed(offset + i, state.Revision)) continue;
            count++;
            if (!write) continue;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, checked((ushort)(offset + i)));
            BinaryPrimitives.WriteUInt64LittleEndian(bytes[2..], id.Value);
            BinaryPrimitives.WriteInt64LittleEndian(bytes[10..], state.Revision);
            BinaryPrimitives.WriteInt32LittleEndian(bytes[18..], state.Rounds);
            BinaryPrimitives.WriteDoubleLittleEndian(bytes[22..], state.NextFireTick);
            BinaryPrimitives.WriteInt64LittleEndian(bytes[30..], state.ReloadStartTick);
            BinaryPrimitives.WriteInt64LittleEndian(bytes[38..], state.ReloadDeadline);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes[46..], state.ShotSequence);
            bytes = bytes[StateBytes..];
        }
        return count;
    }

    public static bool Apply(ReadOnlySpan<byte> bytes, PlayerSession owner, ItemStore items)
    {
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes[48..]);
        if (count < 1 || count > WeaponStateCodec.MaxStates || bytes.Length != HeaderBytes + count * StateBytes) return false;
        var generation = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        var configuration = BinaryPrimitives.ReadInt64LittleEndian(bytes[24..]);
        var inventory = BinaryPrimitives.ReadInt64LittleEndian(bytes[32..]);
        var equipment = BinaryPrimitives.ReadInt64LittleEndian(bytes[40..]);
        if (generation < 0 || configuration < 0 || inventory < 0 || equipment < 0) return false;
        if (generation < owner.ControlGeneration) return true;
        var ship = owner.ControlledShip;
        var entity = new Guid(bytes.Slice(8, 16));
        if (generation != owner.ControlGeneration || entity != (ship == null ? Guid.Empty : Guid.ParseExact(ship.EntityId, "N"))) return false;
        var currentConfiguration = ship?.Loadout.Weapons.ConfigurationRevision ?? 0;
        var currentEquipment = ship?.Equipment.Revision ?? 0;
        if (configuration < currentConfiguration || inventory < owner.Inventory.Revision || equipment < currentEquipment) return true;
        if (configuration != currentConfiguration || inventory != owner.Inventory.Revision || equipment != currentEquipment) return false;
        var remaining = bytes[HeaderBytes..];
        var previousSlot = -1;
        for (var i = 0; i < count; i++)
        {
            var slot = BinaryPrimitives.ReadUInt16LittleEndian(remaining);
            if (slot <= previousSlot || slot >= WeaponStateCodec.MaxStates) return false;
            previousSlot = slot;
            var container = slot < ItemContainer.MaxCapacity ? owner.Inventory.Container : ship?.Equipment.Container;
            var index = slot < ItemContainer.MaxCapacity ? slot : slot - ItemContainer.MaxCapacity;
            var id = BinaryPrimitives.ReadInt64LittleEndian(remaining[2..]);
            if (container == null || index >= container.Slots.Count || id <= 0 || container.Slots[index] is not { } item || item.Value != (ulong)id) return false;
            var state = Read(remaining, item);
            if (!items.Weapons.Validate(state, items) || !items.Weapons.TryGet(item, out var current) ||
                state.Revision == current.Revision && state != current.CaptureState()) return false;
            remaining = remaining[StateBytes..];
        }
        // Validate the entire batch before mutating any state.
        remaining = bytes[HeaderBytes..];
        for (var i = 0; i < count; i++)
        {
            var id = ItemEntityId.FromNetwork(BinaryPrimitives.ReadInt64LittleEndian(remaining[2..]));
            var state = Read(remaining, id);
            items.Weapons.TryGet(id, out var current);
            if (state.Revision > current.Revision) current.Install(state);
            remaining = remaining[StateBytes..];
        }
        return true;
    }

    private static WeaponStateSnapshot Read(ReadOnlySpan<byte> bytes, ItemEntityId item) => new(item,
        BinaryPrimitives.ReadInt64LittleEndian(bytes[10..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[18..]),
        BinaryPrimitives.ReadDoubleLittleEndian(bytes[22..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[30..]),
        BinaryPrimitives.ReadInt64LittleEndian(bytes[38..]), BinaryPrimitives.ReadUInt64LittleEndian(bytes[46..]));
}
