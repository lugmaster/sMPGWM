using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ItemSystem;

public sealed class ItemCategoryDefinitions(ItemCategoryDefinition[] definitions)
    : DefinitionRegistry<ItemCategoryDefinition>(definitions, "item categories");
