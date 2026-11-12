using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.ItemSystem.Hotbar;

public sealed class ItemHotbarSnapshot
{
    public long Revision { get; }
    public IReadOnlyList<HotbarBinding?> Entries { get; }

    public ItemHotbarSnapshot(long revision, IReadOnlyList<HotbarBinding?> entries)
    {
        if (revision < 0 || revision == long.MaxValue || entries.Count != ItemHotbarState.PositionCount)
            throw new ArgumentException("Invalid restored hotbar revision or capacity.");
        var copy = new HotbarBinding?[entries.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            var valid = entries[i] switch
            {
                null => true,
                ConsumableHotbarBinding binding => !string.IsNullOrWhiteSpace(binding.DefinitionId),
                WeaponHotbarBinding binding => !string.IsNullOrWhiteSpace(binding.SlotKey),
                ActionHotbarBinding binding => !string.IsNullOrWhiteSpace(binding.ActionKey),
                EquipmentAbilityHotbarBinding binding => !string.IsNullOrWhiteSpace(binding.SlotKey) &&
                    !string.IsNullOrWhiteSpace(binding.AbilityId),
                _ => false
            };
            if (!valid) throw new ArgumentException($"Invalid restored hotbar binding at position {i}.");
            copy[i] = entries[i];
        }
        Revision = revision;
        Entries = Array.AsReadOnly(copy);
    }
}
