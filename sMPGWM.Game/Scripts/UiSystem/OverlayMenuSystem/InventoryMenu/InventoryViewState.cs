using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed class InventoryViewState
{
    internal string SearchText { get; set; } = "";
    internal int SelectedTab { get; set; }
    internal int WeaponFilter { get; set; }
    internal string? SelectedCategoryId
    {
        get
        {
            var index = 1;
            foreach (var category in ItemRegistry.GetAllCategories())
            {
                if (category.Id == "heavy_weapon") continue;
                if (index++ == SelectedTab) return category.Id;
            }
            return null;
        }
    }
    internal int AllSort { get; set; }
    internal int CategorySort { get; set; }
    internal int SelectedSort { get => SelectedTab == 0 ? AllSort : CategorySort; set { if (SelectedTab == 0) AllSort = value; else CategorySort = value; } }
    internal bool SortRequested { get; set; }
    internal List<ItemEntityId?> Layout { get; } = new();
    internal bool CanArrange => SelectedTab == 0 && string.IsNullOrWhiteSpace(SearchText);

    internal void Reconcile(InventorySnapshot snapshot)
    {
        var initial = Layout.Count == 0;
        var ids = snapshot.Occupied.Select(e => e.Entity.Id).ToHashSet();
        for (var i = 0; i < Layout.Count; i++)
            if (Layout[i] is { } id && !ids.Contains(id)) Layout[i] = null;
        while (Layout.Count < snapshot.Capacity) Layout.Add(null);
        var known = Layout.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        foreach (var entry in snapshot.Occupied)
            if (known.Add(entry.Entity.Id)) Layout[Layout.IndexOf(null)] = entry.Entity.Id;
        if ((initial || SortRequested) && AllSort != 2)
        {
            var entries = snapshot.Occupied.ToList();
            entries.Sort((a, b) => InventoryProjection.Compare(a, b, AllSort));
            Layout.Clear();
            Layout.AddRange(entries.Select(e => (ItemEntityId?)e.Entity.Id));
            while (Layout.Count < snapshot.Capacity) Layout.Add(null);
        }
        SortRequested = false;
    }

    internal void Place(ItemEntityId source, int destination)
    {
        var origin = Layout.IndexOf(source);
        if (origin < 0 || origin == destination) return;
        (Layout[origin], Layout[destination]) = (Layout[destination], Layout[origin]);
        AllSort = 2;
    }
}
