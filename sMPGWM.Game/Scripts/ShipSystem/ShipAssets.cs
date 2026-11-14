using System;
using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipAssets
{
    public ShipData Definition { get; }
    public Texture2D Texture { get; }
    internal Vector2[] HullPoints { get; }
    public Shape2D? ShieldShape { get; }

    internal ShipAssets(ShipData definition, ShieldShapeCache shapes)
    {
        Definition = definition;
        Texture = ResourceLoader.Load<Texture2D>(definition.TexturePath)
            ?? throw new InvalidOperationException("Texture unavailable: " + definition.TexturePath);
        HullPoints = new Vector2[definition.ColliderPoints.Count];
        for (var i = 0; i < HullPoints.Length; i++) HullPoints[i] = definition.ColliderPoints[i];
        if (definition.ShieldGeometry is { } geometry) ShieldShape = shapes.GetShape(geometry);
    }
}
