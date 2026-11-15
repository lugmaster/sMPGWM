using System;
using Godot;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.ShipSystem;

internal static class ShipDefinitionParser
{
    internal static ShipData Parse(JToken row, Func<string, bool> textureAvailable)
    {
        var result = DefinitionDeserializer.Deserialize<ShipDefinition>(row,
            ShipCatalogueLoader.Source.UserPath, ShipCatalogueLoader.Source.SchemaPath);
        if (!result.IsSuccess) throw new ArgumentException(result.Error!.ToDisplayString());
        return Create(result.Value!, textureAvailable);
    }

    internal static ShipData Create(ShipDefinition definition, Func<string, bool> textureAvailable)
    {
        var name = definition.Name;
        void Require(bool condition, string field)
        {
            if (!condition) throw new ArgumentException($"Ship '{name}': invalid {field}.");
        }
        float Number(double value, string field)
        {
            Require(double.IsFinite(value) && float.IsFinite((float)value), field);
            return (float)value;
        }
        Require(!string.IsNullOrWhiteSpace(name), "name (blank)");
        Require(!string.IsNullOrWhiteSpace(definition.Description), "description (blank)");
        Require(textureAvailable(definition.TexturePath), "texture_path (unavailable texture)");
        var points = new Vector2[definition.ColliderPoints.Count];
        for (var i = 0; i < points.Length; i++)
        {
            var pair = definition.ColliderPoints[i];
            Require(pair is { Length: 2 }, "collider_points");
            points[i] = new Vector2(Number(pair![0], "collider_points"), Number(pair[1], "collider_points"));
        }
        Require(IsUsablePolygon(points), "collider_points (duplicate, degenerate or intersecting polygon)");
        Require(definition.WeaponFixturePoints is { Length: > 0 }, "weapon_fixture_points (at least one coordinate pair required)");
        var weaponFixturePoints = new Vector2[definition.WeaponFixturePoints!.Length];
        for (var i = 0; i < weaponFixturePoints.Length; i++)
        {
            var pair = definition.WeaponFixturePoints[i];
            Require(pair is { Length: 2 }, "weapon_fixture_points");
            weaponFixturePoints[i] = new Vector2(Number(pair![0], "weapon_fixture_points"), Number(pair[1], "weapon_fixture_points"));
        }
        var s = definition.Stats;
        Number(s.ShieldCapacity, "ShieldCapacity");
        Number(s.Armor, "Armor");
        Number(s.RechargeTime, "RechargeTime");
        Number(s.DownTime, "DownTime");
        Number(s.EnergyProduction, "EnergyProduction");
        Number(s.EnergyStorage, "EnergyStorage");
        Require(definition.Mass >= 0 && definition.Mass <= float.MaxValue / 2, "mass");
        Require(double.IsFinite(definition.Health) && definition.Health > 0, "health");
        var stats = new ShipStats(Number(s.Speed, "Speed"), Number(s.AfterBurnerSpeed, "AfterBurnerSpeed"),
            Number(s.RotationSpeed, "RotationSpeed"), Number(s.Acceleration, "Acceleration"),
            Number(s.AfterBurnerAcceleration, "AfterBurnerAcceleration"), Number(definition.Mass, "mass"));
        return new ShipData(definition, points, weaponFixturePoints, stats);
    }

    internal static bool IsUsablePolygon(Vector2[] points)
    {
        if (points.Length < 3) return false;
        for (var i = 0; i < points.Length; i++)
            for (var j = i + 1; j < points.Length; j++)
                if (points[i] == points[j]) return false;
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
