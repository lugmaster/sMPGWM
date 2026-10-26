using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public sealed class IconAtlasProvider
{
    private readonly Texture2D[] _atlases;

    public IconAtlasProvider(params string[] atlasPaths)
    {
        _atlases = new Texture2D[atlasPaths.Length];
        for (var atlasIndex = 0; atlasIndex < atlasPaths.Length; atlasIndex++)
        {
            _atlases[atlasIndex] = ResourceLoader.Load<Texture2D>(atlasPaths[atlasIndex])
                ?? throw new InvalidOperationException($"Could not load icon atlas: {atlasPaths[atlasIndex]}");
        }
    }

    internal IconDefinition CreateIcon(int atlasIndex, int x, int y, int width, int height)
    {
        return new IconDefinition
        {
            IconTexture = new AtlasTexture
            {
                Atlas = _atlases[atlasIndex],
                Region = new Rect2(x, y, width, height),
                FilterClip = true
            }
        };
    }
}
