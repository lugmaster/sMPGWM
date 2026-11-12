using sMPGWM.Scripts.GameplayDataSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.ShipSystem;

public static class ShipRegistry
{
    private sealed class ShipDefinitions(IEnumerable<ShipData> definitions)
        : DefinitionRegistry<ShipData>(definitions, "ships");

    private static Lazy<ShipDefinitions> Data = new(() => new ShipDefinitions(Load().Values));
    internal static void ResetPreparation() => Data = new(() => new ShipDefinitions(Load().Values));
    public static ShipData GetById(string id) => Data.Value.GetById(id);
    public static bool TryGetById(string id, out ShipData data) => Data.Value.TryGetById(id, out data!);
    public static IReadOnlyList<ShipData> GetAll() => Data.Value.GetAll();

    private static IReadOnlyDictionary<string, ShipData> Load()
    {
        var yaml = FileLoader.LoadYaml(FilePaths.UserShipDataYaml,
            FilePaths.ShipDataYaml, FilePaths.ShipDataYamlSchema);
        var result = ShipDefinitionLoader.Load(yaml,
            FilePaths.UserShipDataYaml, FilePaths.ShipDataYamlSchema);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error!.ToDisplayString());
        }
        try
        {
            return Build(result.Value!, path => ResourceLoader.Load<Texture2D>(path) is { } texture &&
                texture.GetWidth() > 0 && texture.GetHeight() > 0);
        }
        catch (ArgumentException e)
        {
            throw new InvalidOperationException(new DataLoadError("Invalid ship definition", e.Message,
                FilePaths.UserShipDataYaml, FilePaths.ShipDataYamlSchema, e).ToDisplayString(), e);
        }
    }

    internal static IReadOnlyDictionary<string, ShipData> Build(ShipDefinitionsDocument document,
        Func<string, bool> textureAvailable)
    {
        var result = new Dictionary<string, ShipData>(StringComparer.Ordinal);
        foreach (var definition in document.Ships)
        {
            var name = definition.Name;
            void Require(bool condition, string field)
            {
                if (!condition) throw new ArgumentException($"Ship '{name}': invalid {field}.");
            }
            float Number(double value, string field, bool positive = false)
            {
                Require(double.IsFinite(value) && float.IsFinite((float)value) &&
                    (!positive || (float)value > 0), field);
                return (float)value;
            }
            Require(!string.IsNullOrWhiteSpace(name) && !result.ContainsKey(name), "name (blank or duplicate)");
            Require(textureAvailable(definition.TexturePath), "texture_path (unavailable texture)");
            var points = definition.ColliderPoints.Select(pair =>
            {
                Require(pair.Length == 2 && pair.All(v => double.IsFinite(v) && float.IsFinite((float)v)), "collider_points");
                return new Vector2((float)pair[0], (float)pair[1]);
            }).ToArray();
            Require(IsUsablePolygon(points), "collider_points (duplicate, degenerate or intersecting polygon)");
            var s = definition.Stats;
            Number(s.ShieldCapacity, "ShieldCapacity");
            Number(s.Armor, "Armor");
            Number(s.RechargeTime, "RechargeTime");
            Number(s.DownTime, "DownTime");
            Number(s.EnergyProduction, "EnergyProduction");
            Number(s.EnergyStorage, "EnergyStorage");
            Require(definition.Mass >= 0, "mass");
            Require(double.IsFinite(definition.Health) && definition.Health > 0, "health");
            var stats = new ShipStats(Number(s.Speed, "Speed"), Number(s.AfterBurnerSpeed, "AfterBurnerSpeed"),
                Number(s.RotationSpeed, "RotationSpeed"), Number(s.Acceleration, "Acceleration"),
                Number(s.AfterBurnerAcceleration, "AfterBurnerAcceleration"), Number(definition.Mass, "mass"));
            Require(definition.WeaponSlots.Light >= 0 && definition.WeaponSlots.Heavy >= 0, "weapon_slots");
            result.Add(name, new ShipData(definition, points, stats));
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, ShipData>(result);
    }

    internal static bool IsUsablePolygon(Vector2[] points)
    {
        if (points.Length < 3 || points.Distinct().Count() != points.Length) return false;
        static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
            ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);
        static bool On(Vector2 a, Vector2 b, Vector2 c) =>
            c.X >= Math.Min(a.X, b.X) && c.X <= Math.Max(a.X, b.X) &&
            c.Y >= Math.Min(a.Y, b.Y) && c.Y <= Math.Max(a.Y, b.Y);
        double area = 0;
        for (var i = 0; i < points.Length; i++)
        {
            var a = points[i]; var b = points[(i + 1) % points.Length];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
            var previous = points[(i + points.Length - 1) % points.Length];
            if (Cross(previous, a, b) == 0 && (On(previous, a, b) || On(a, b, previous))) return false;
            for (var j = i + 1; j < points.Length; j++)
            {
                if (j == i + 1 || i == 0 && j == points.Length - 1) continue;
                var c = points[j]; var d = points[(j + 1) % points.Length];
                var abC = Cross(a, b, c); var abD = Cross(a, b, d);
                var cdA = Cross(c, d, a); var cdB = Cross(c, d, b);
                if (abC == 0 && On(a, b, c) || abD == 0 && On(a, b, d) ||
                    cdA == 0 && On(c, d, a) || cdB == 0 && On(c, d, b) ||
                    Math.Sign(abC) != Math.Sign(abD) && Math.Sign(cdA) != Math.Sign(cdB)) return false;
            }
        }
        return area != 0 && double.IsFinite(area);
    }
}
