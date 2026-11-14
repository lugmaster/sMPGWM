using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShieldPhysicsGeometry
{
    public const float MaximumOutwardError = 0.5f;
    public const int MaximumSegments = 4096;
    private const double RoundingAllowance = 0.000001;
    private static readonly Dictionary<ShieldGeometry, Shape2D> Shapes = new();

    public static Shape2D GetShape(ShieldGeometry geometry)
    {
        if (Shapes.TryGetValue(geometry, out var cached)) return cached;
        var radius = Math.Max(geometry.Radii.X, geometry.Radii.Y);
        if (!geometry.Center.IsFinite() || !geometry.Radii.IsFinite() ||
            geometry.Radii.X <= 0 || geometry.Radii.Y <= 0 || !float.IsFinite(geometry.RotationDegrees))
            throw new ArgumentException("Shield geometry must have finite, positive dimensions.", nameof(geometry));
        Shape2D shape;
        if (geometry.Shape == ShieldGeometryShape.Circle)
        {
            if (geometry.Radii.X != geometry.Radii.Y)
                throw new ArgumentException("A circular shield requires equal radii.", nameof(geometry));
            shape = new CircleShape2D { Radius = radius };
        }
        else if (geometry.Shape == ShieldGeometryShape.Ellipse)
        {
            var segments = 32;
            // Scaling a circumscribed unit polygon preserves containment. The largest axis bounds error.
            while (radius * ((1 + RoundingAllowance) / Math.Cos(Math.PI / segments) - 1) > MaximumOutwardError)
            {
                if (segments == MaximumSegments)
                    throw new ArgumentException("Shield exceeds the supported polygon approximation tolerance.", nameof(geometry));
                segments *= 2;
            }
            var points = new Vector2[segments];
            var scale = (1 + RoundingAllowance) / Math.Cos(Math.PI / segments);
            var minor = Math.Min(geometry.Radii.X, geometry.Radii.Y);
            if ((MathF.BitIncrement(minor) - minor) / minor > RoundingAllowance / 4)
                throw new ArgumentException("Shield dimensions exceed supported float precision.", nameof(geometry));
            for (var i = 0; i < segments; i++)
            {
                var angle = (2 * i + 1) * Math.PI / segments;
                points[i] = new Vector2((float)(geometry.Radii.X * scale * Math.Cos(angle)),
                    (float)(geometry.Radii.Y * scale * Math.Sin(angle)));
                if (!points[i].IsFinite())
                    throw new ArgumentException("Shield polygon vertices must be finite.", nameof(geometry));
            }
            shape = new ConvexPolygonShape2D { Points = points };
        }
        else throw new ArgumentException("Unknown shield geometry shape.", nameof(geometry));
        Shapes.Add(geometry, shape);
        return shape;
    }
}
