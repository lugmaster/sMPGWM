using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public sealed class CursorTextureProvider : IDisposable
{
    public ImageTexture Red { get; }
    public ImageTexture Green { get; }
    public ImageTexture Blue { get; }

    public CursorTextureProvider(string path)
    {
        var texture = ResourceLoader.Load<Texture2D>(path)
            ?? throw new InvalidOperationException($"Could not load cursor: {path}");
        using var source = texture.GetImage();
        if (source.IsCompressed() && source.Decompress() != Error.Ok)
            throw new InvalidOperationException($"Could not decompress cursor: {path}");
        Red = Tint(source, Colors.Red);
        Green = Tint(source, Colors.Green);
        Blue = Tint(source, Colors.Blue);
    }

    private static ImageTexture Tint(Image source, Color color)
    {
        using var image = (Image)source.Duplicate();
        image.Convert(Image.Format.Rgba8);
        for (var y = 0; y < image.GetHeight(); y++)
            for (var x = 0; x < image.GetWidth(); x++)
                image.SetPixel(x, y, image.GetPixel(x, y) * color);
        return ImageTexture.CreateFromImage(image);
    }

    public void Dispose()
    {
        Red.Dispose();
        Green.Dispose();
        Blue.Dispose();
    }
}
