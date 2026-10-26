using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal static class InventoryProjection
{
    internal static IReadOnlyList<ItemCellBinding> Create(InventorySnapshot snapshot, ItemCatalog catalog, InventoryViewState state)
    {
        ItemCategoryId? category = state.SelectedTab > 0 && state.SelectedTab <= catalog.Categories.All.Count
            ? catalog.Categories.All[state.SelectedTab - 1].Id : null;
        var search = state.SearchText.Trim();
        var matches = new List<InventoryEntry>();
        foreach (var entry in snapshot.Occupied)
        {
            var definition = catalog.Get(entry.Entity.DefinitionId);
            if ((category == null || definition.CategoryId == category) &&
                definition.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)) matches.Add(entry);
        }
        matches.Sort((left, right) =>
        {
            var order = StringComparer.OrdinalIgnoreCase.Compare(catalog.Get(left.Entity.DefinitionId).DisplayName,
                catalog.Get(right.Entity.DefinitionId).DisplayName);
            if (order != 0) return state.SelectedSort == 1 ? -order : order;
            order = StringComparer.Ordinal.Compare(left.Entity.DefinitionId.Value, right.Entity.DefinitionId.Value);
            return order != 0 ? order : left.Entity.Id.Value.CompareTo(right.Entity.Id.Value);
        });
        var cells = new ItemCellBinding[matches.Count + snapshot.EmptyAddresses.Count];
        var index = 0;
        foreach (var entry in matches) cells[index++] = ItemCellBinding.Bag(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses) cells[index++] = ItemCellBinding.Bag(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }

    internal static IReadOnlyList<ItemCellBinding> Create(InventorySnapshot snapshot)
    {
        var cells = new ItemCellBinding[snapshot.Capacity];
        var index = 0;
        foreach (var entry in snapshot.Occupied) cells[index++] = ItemCellBinding.Bag(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses) cells[index++] = ItemCellBinding.Bag(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }
}
