using sMPGWM.Scripts.GameplayDataSystem;
namespace sMPGWM.Scripts.ItemSystem.Hotbar;

internal static class ItemHotbarPolicy
{
    public static bool IsConsumable(string category) => category == GameplayDefinitions.Current.ItemCategories.GetById("consumable").DisplayName;
    public static bool IsWeapon(string category) => category == GameplayDefinitions.Current.ItemCategories.GetById("light_weapon").DisplayName ||
        category == GameplayDefinitions.Current.ItemCategories.GetById("medium_weapon").DisplayName ||
        category == GameplayDefinitions.Current.ItemCategories.GetById("heavy_weapon").DisplayName;
}
