using Godot;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.Items;

internal static class ItemIconResolver
{
    private static Texture2D? _placeholder;

    internal static Texture2D Item(ItemDefinition definition) => definition.Icon.IconTexture;
    internal static Texture2D Category(ItemCategoryDefinition category) => category.Icon.IconTexture;

    // TODO Precompute one static place holder
    internal static Texture2D Placeholder()
    {
        if (_placeholder != null) return _placeholder;
        var image = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
        image.Fill(new Color(0.55f, 0.57f, 0.6f));
        image.FillRect(new Rect2I(4, 4, 16, 16), new Color(0.28f, 0.3f, 0.33f));
        return _placeholder = ImageTexture.CreateFromImage(image);
    }
}
