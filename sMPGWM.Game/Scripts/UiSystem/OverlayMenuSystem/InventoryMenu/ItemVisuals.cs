#nullable enable

using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed class ItemVisuals(ItemCatalog catalog)
{
    private readonly Dictionary<string, Texture2D?> _textures = new();
    private Texture2D? _placeholder;

    internal void Apply(ClickableIcon icon, ItemEntity? entity, string emptyTooltip = "")
    {
        icon.SetDisplay(null, new IconDisplayData());
        if (entity == null)
        {
            icon.SetDisplay(null, new IconDisplayData(emptyTooltip));
            return;
        }

        var definition = catalog.Get(entity.DefinitionId);
        var texture = Resolve(definition.IconAssetPath);
        icon.SetDisplay(texture ?? Placeholder(), ItemDisplayFormatter.Create(catalog, entity));
    }

    private Texture2D? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (_textures.TryGetValue(path, out var texture)) return texture;
        texture = ResourceLoader.Exists(path, "Texture2D") ? ResourceLoader.Load<Texture2D>(path) : null;
        _textures.Add(path, texture);
        return texture;
    }

    private Texture2D Placeholder()
    {
        if (_placeholder != null) return _placeholder;
        var image = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
        image.Fill(new Color(0.55f, 0.57f, 0.6f));
        image.FillRect(new Rect2I(4, 4, 16, 16), new Color(0.28f, 0.3f, 0.33f));
        _placeholder = ImageTexture.CreateFromImage(image);
        return _placeholder;
    }
}
