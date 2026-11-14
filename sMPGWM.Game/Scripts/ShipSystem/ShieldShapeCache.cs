using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShieldShapeCache : IDisposable
{
    private readonly Dictionary<ShieldGeometry, Shape2D> _shapes = new();
    private bool _disposed;

    public Shape2D GetShape(ShieldGeometry geometry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_shapes.TryGetValue(geometry, out var shape)) return shape;
        shape = ShieldPhysicsGeometry.GetShape(geometry);
        _shapes.Add(geometry, shape);
        return shape;
    }

    public void Dispose()
    {
        _disposed = true;
        // Active nodes retain native references until their deferred destruction completes.
        _shapes.Clear();
    }
}
