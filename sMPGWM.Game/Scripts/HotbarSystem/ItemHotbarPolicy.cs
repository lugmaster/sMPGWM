using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.HotbarSystem;

internal static class ItemHotbarPolicy
{
    public static bool IsConsumable(string category) => category == ItemRegistry.GetCategoryById("consumable").DisplayName;
    public static bool IsWeapon(string category) => category == ItemRegistry.GetCategoryById("light_weapon").DisplayName || category == ItemRegistry.GetCategoryById("heavy_weapon").DisplayName;
}
