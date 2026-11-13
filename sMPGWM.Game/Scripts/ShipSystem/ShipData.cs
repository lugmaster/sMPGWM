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
        Stats = stats;
        StatModifiers = definition.Stats with { };
        Mass = definition.Mass;
        Health = definition.Health;
    }
}
