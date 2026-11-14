using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemRegistry
{
    public static ItemDefinition GetById(string id) => GameplayDefinitions.Current.Items.GetById(id);

    public static bool TryGetById(string id, out ItemDefinition definition) =>
        GameplayDefinitions.Current.Items.TryGetById(id, out definition!);

    public static IReadOnlyList<ItemDefinition> GetAll() => GameplayDefinitions.Current.Items.GetAll();
    public static ItemCategoryDefinition GetCategoryById(string id) => GameplayDefinitions.Current.ItemCategories.GetById(id);
    public static IReadOnlyList<ItemCategoryDefinition> GetAllCategories() => GameplayDefinitions.Current.ItemCategories.GetAll();
}
