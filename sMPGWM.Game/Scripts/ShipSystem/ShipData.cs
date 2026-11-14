using sMPGWM.Scripts.GameplayDataSystem;
using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public sealed record ShipData : GameplayDefinition
{
    public string Name { get; }
    public string TexturePath { get; }
    public IReadOnlyList<Vector2> ColliderPoints { get; }
    public ShieldGeometry? ShieldGeometry { get; }
    public IReadOnlyList<Vector2> MuzzleOffsets { get; }
    public ShipStats Stats { get; }
    public ShipStatModifiersDefinition StatModifiers { get; }
    public double Mass { get; }
    public double Health { get; }

    internal ShipData(ShipDefinition definition, Vector2[] points, ShipStats stats)
        : base(definition.Name, definition.Name, "", null)
    {
        Name = definition.Name;
        TexturePath = definition.TexturePath;
        ColliderPoints = Array.AsReadOnly((Vector2[])points.Clone());
        if (definition.ShieldGeometry is { } shield)
        {
            var circle = shield.Shape == "circle";
            ShieldGeometry = new(circle ? ShieldGeometryShape.Circle : ShieldGeometryShape.Ellipse,
                new((float)shield.Center[0], (float)shield.Center[1]),
                circle ? new((float)shield.Radius!.Value, (float)shield.Radius.Value) :
                    new((float)shield.Radii![0], (float)shield.Radii[1]),
                circle ? 0 : (float)shield.RotationDegrees!.Value);
        }
        var muzzles = new Vector2[definition.MuzzleOffsets.Count];
        for (var i = 0; i < muzzles.Length; i++)
            muzzles[i] = new((float)definition.MuzzleOffsets[i][0], (float)definition.MuzzleOffsets[i][1]);
        MuzzleOffsets = Array.AsReadOnly(muzzles);
        Stats = stats;
        StatModifiers = definition.Stats with { };
        Mass = definition.Mass;
        Health = definition.Health;
    }
}
