using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

/// <summary>Immutable membership for occupied weapon bindings, in equipment-slot order.</summary>
public sealed class WeaponGroupConfiguration
{
    public static WeaponGroupConfiguration Empty { get; } = new(Array.Empty<WeaponGroupBinding>());
    public IReadOnlyList<WeaponGroupBinding> Bindings { get; }

    public WeaponGroupConfiguration(IReadOnlyList<WeaponGroupBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        if (bindings.Count > ItemContainer.MaxCapacity) throw new ArgumentException("Too many weapon bindings.", nameof(bindings));
        var copy = new WeaponGroupBinding[bindings.Count];
        var previous = -1;
        for (var i = 0; i < copy.Length; i++)
        {
            var binding = bindings[i];
            if (binding.Slot <= previous || binding.Slot >= ItemContainer.MaxCapacity || binding.Item == default)
                throw new ArgumentException("Weapon bindings must have identities and unique, ordered slots.", nameof(bindings));
            copy[i] = binding;
            previous = binding.Slot;
        }
        Bindings = Array.AsReadOnly(copy);
    }

    public bool TryGetBinding(int slot, ItemEntityId identity, out WeaponGroupBinding binding)
    {
        for (var i = 0; i < Bindings.Count; i++)
        {
            var candidate = Bindings[i];
            if (candidate.Slot != slot || candidate.Item != identity) continue;
            binding = candidate;
            return true;
        }
        binding = default;
        return false;
    }

    internal bool SameAs(WeaponGroupConfiguration other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (Bindings.Count != other.Bindings.Count) return false;
        for (var i = 0; i < Bindings.Count; i++)
            if (Bindings[i] != other.Bindings[i]) return false;
        return true;
    }
}
