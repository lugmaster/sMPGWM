using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal static class InventoryProjection
{
    internal static IReadOnlyList<ItemCellBinding> Create(InventorySnapshot snapshot, InventoryViewState state)
    {
        var category = state.SelectedCategoryId;
        var search = state.SearchText.Trim();
        var matches = new List<InventoryEntry>();
        foreach (var entry in snapshot.Occupied)
        {
            var definition = entry.Entity.Definition;
            if ((category == null || (category == "light_weapon"
                    ? (definition.CategoryId == "light_weapon" && state.WeaponFilter is 0 or 1) ||
                      (definition.CategoryId == "medium_weapon" && state.WeaponFilter is 0 or 2) ||
                      (definition.CategoryId == "heavy_weapon" && state.WeaponFilter is 0 or 3)
                    : definition.CategoryId == category)) &&
                definition.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)) matches.Add(entry);
        }
        state.Reconcile(snapshot);
        if (state.SelectedTab == 0)
        {
            var byId = new Dictionary<ItemEntityId, InventoryEntry>();
            foreach (var entry in matches) byId.Add(entry.Entity.Id, entry);
            var result = new List<ItemCellBinding>();
            var empty = 0;
            for (var slot = 0; slot < state.Layout.Count; slot++)
            {
                var id = state.Layout[slot];
                if (id.HasValue && byId.TryGetValue(id.Value, out var entry))
                    result.Add(ItemCellBinding.Bag(entry.Address, entry.Entity, snapshot.Revision) with { VisualSlot = slot });
                else if (!id.HasValue)
                    result.Add(ItemCellBinding.Bag(snapshot.EmptyAddresses[empty++], null, snapshot.Revision) with { VisualSlot = slot });
            }
            return result.AsReadOnly();
        }
        matches.Sort((left, right) =>
        {
            if (category == "light_weapon")
            {
                var categoryOrder = WeaponOrder(left.Entity.Definition.CategoryId).CompareTo(WeaponOrder(right.Entity.Definition.CategoryId));
                if (categoryOrder != 0) return categoryOrder;
            }
            return Compare(left, right, state.CategorySort);
        });
        var cells = new ItemCellBinding[matches.Count + snapshot.EmptyAddresses.Count];
        var index = 0;
        foreach (var entry in matches) cells[index++] = ItemCellBinding.Bag(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses) cells[index++] = ItemCellBinding.Bag(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }

    internal static int Compare(InventoryEntry left, InventoryEntry right, int sort)
    {
        var order = StringComparer.OrdinalIgnoreCase.Compare(left.Entity.Definition.DisplayName, right.Entity.Definition.DisplayName);
        if (order != 0) return sort == 1 ? -order : order;
        order = StringComparer.Ordinal.Compare(left.Entity.DefinitionId, right.Entity.DefinitionId);
        return order != 0 ? order : left.Entity.Id.Value.CompareTo(right.Entity.Id.Value);
    }

    private static int WeaponOrder(string categoryId) => categoryId switch
    {
        "light_weapon" => 1,
        "medium_weapon" => 2,
        "heavy_weapon" => 3,
        _ => 0
    };

    internal static IReadOnlyList<ItemCellBinding> Create(InventorySnapshot snapshot)
    {
        var cells = new ItemCellBinding[snapshot.Capacity];
        var index = 0;
        foreach (var entry in snapshot.Occupied) cells[index++] = ItemCellBinding.Bag(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses) cells[index++] = ItemCellBinding.Bag(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }
}
