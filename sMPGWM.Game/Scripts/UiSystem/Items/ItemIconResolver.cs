#nullable enable

using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.Items;

internal static class ItemIconResolver
{
    private static Texture2D? _placeholder;

    internal static Texture2D Item(ItemCatalog catalog, ItemDefinition definition) =>
        Load(definition.IconAssetPath) ?? Category(catalog.Categories.Get(definition.CategoryId)) ?? Placeholder();

    internal static Texture2D? Category(ItemCategory category) => Load(category.IconAssetPath) ??
        (category.IconKey == null ? null : Icons.Get(category.IconKey).IconTexture);

    private static Texture2D? Load(string? path) => !string.IsNullOrWhiteSpace(path) && ResourceLoader.Exists(path, "Texture2D")
        ? ResourceLoader.Load<Texture2D>(path) : null;

    internal static Texture2D Placeholder()
    {
        if (_placeholder != null) return _placeholder;
        var image = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
        image.Fill(new Color(0.55f, 0.57f, 0.6f));
        image.FillRect(new Rect2I(4, 4, 16, 16), new Color(0.28f, 0.3f, 0.33f));
        return _placeholder = ImageTexture.CreateFromImage(image);
    }
}
