using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipAssetCatalogue : IDisposable
{
    private readonly Dictionary<string, ShipAssets> _ships = new(StringComparer.Ordinal);
    private readonly ShieldShapeCache _shapes = new();
    private PackedScene? _scene;
    private Shader? _shader;
    private bool _disposed;

    public Shader ShieldShader => !_disposed ? _shader! : throw new ObjectDisposedException(nameof(ShipAssetCatalogue));

    public ShipAssetCatalogue(IReadOnlyList<ShipData> definitions)
    {
        try
        {
            _scene = ResourceLoader.Load<PackedScene>("res://scenes/game/game_objects/ship.tscn")
                ?? throw new InvalidOperationException("Ship scene is unavailable.");
            _shader = ResourceLoader.Load<Shader>(ShieldVisual.ShaderPath)
                ?? throw new InvalidOperationException("Shield outline shader is unavailable.");
            foreach (var definition in definitions) _ships.Add(definition.Name, new ShipAssets(definition, _shapes));
        }
        catch { Dispose(); throw; }
    }

    public ShipAssets Get(ShipData definition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_ships.TryGetValue(definition.Name, out var assets) || !ReferenceEquals(assets.Definition, definition))
            throw new InvalidOperationException("Ship definition was not prepared for this session: " + definition.Name);
        return assets;
    }

    public ShipController CreateShip()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _scene!.Instantiate<ShipController>();
    }

    public void Dispose()
    {
        _disposed = true;
        _ships.Clear();
        _shapes.Dispose();
        _scene = null;
        _shader = null;
    }
}
