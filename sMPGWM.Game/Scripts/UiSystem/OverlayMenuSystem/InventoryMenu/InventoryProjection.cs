using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal static class InventoryProjection
{
    internal static IReadOnlyList<InventoryCellBinding> Create(InventorySnapshot snapshot, ItemCatalog catalog,
        InventoryViewState state)
    {
        var tabs = CategoryTab.Create(catalog.Categories);
        var category = tabs[state.SelectedTab].CategoryId;
        var search = state.SearchText.Trim();
        var matches = snapshot.Occupied.Where(entry =>
        {
            var definition = catalog.Get(entry.Entity.DefinitionId);
            return (category == null || definition.CategoryId == category) &&
                definition.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase);
        });
        var sorted = state.SelectedSort == 1
            ? matches.OrderByDescending(entry => catalog.Get(entry.Entity.DefinitionId).DisplayName, StringComparer.OrdinalIgnoreCase)
            : matches.OrderBy(entry => catalog.Get(entry.Entity.DefinitionId).DisplayName, StringComparer.OrdinalIgnoreCase);
        return Array.AsReadOnly(sorted.ThenBy(entry => entry.Entity.DefinitionId.Value, StringComparer.Ordinal)
            .ThenBy(entry => entry.Entity.Id.Value)
            .Select(entry => new InventoryCellBinding(entry.Address, entry.Entity, snapshot.Revision))
            .Concat(snapshot.EmptyAddresses.Select(address => new InventoryCellBinding(address, null, snapshot.Revision)))
            .ToArray());
    }

    internal static IReadOnlyList<InventoryCellBinding> Create(InventorySnapshot snapshot)
    {
        var cells = new InventoryCellBinding[snapshot.Capacity];
        var index = 0;
        foreach (var entry in snapshot.Occupied)
            cells[index++] = new InventoryCellBinding(entry.Address, entry.Entity, snapshot.Revision);
        foreach (var address in snapshot.EmptyAddresses)
            cells[index++] = new InventoryCellBinding(address, null, snapshot.Revision);
        return Array.AsReadOnly(cells);
    }
}
