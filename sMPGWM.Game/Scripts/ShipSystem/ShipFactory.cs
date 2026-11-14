using System;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.ShipSystem;
public static class ShipFactory
{
    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity)
        => Create(data, entityId, position, rotation, velocity, new ItemStore(), false);

    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity,
        ItemStore store, bool authoritative)
        => Create(data, entityId, position, rotation, velocity, store, authoritative, null);

    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity,
        ItemStore store, bool authoritative, ShipAssetCatalogue? assets)
    {
        ShipController? ship = null;
        ShipLoadout? loadout = null;
        try
        {
            var prepared = assets?.Get(data);
            ship = assets?.CreateShip() ?? GamePlayProvider.CreateShip();
            ship.EntityId = entityId; ship.Data = data;
            ship.ConfigureLoadout(store, authoritative);
            loadout = ship.Loadout;
            ship.Position = position; ship.Rotation = rotation; ship.Velocity = velocity;
            var collider = prepared?.HullPoints;
            if (collider == null)
            {
                collider = new Vector2[data.ColliderPoints.Count];
                for (var i = 0; i < collider.Length; i++) collider[i] = data.ColliderPoints[i];
            }
            ship.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon = collider;
            ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture = prepared?.Texture ?? ResourceLoader.Load<Texture2D>(data.TexturePath)
                ?? throw new InvalidOperationException("Texture unavailable: " + data.TexturePath);
            ship.PrepareShieldCollider(prepared?.ShieldShape);
            if (data.ShieldGeometry is { } geometry)
            {
                var visual = new sMPGWM.Scripts.VfxSystem.ShieldVisual { Name = "ShieldVisual" };
                if (assets != null) visual.Initialize(geometry, assets.ShieldShader);
                else visual.Initialize(geometry);
                ship.GetNode<Node2D>("ShipVisuals").AddChild(visual);
                var debug = new sMPGWM.Scripts.VfxSystem.ShieldColliderDebugView { Name = "ShieldColliderDebugView" }; // TEST-SUPPORT: SHIELD-FACTORY-DEBUG-001 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
                debug.Initialize(ship, geometry); // TEST-SUPPORT: SHIELD-FACTORY-DEBUG-002 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
                ship.AddChild(debug); // TEST-SUPPORT: SHIELD-FACTORY-DEBUG-003 | Tests: sMPGWM.Tests.ShieldVisualTests.DebugOverlayTracksEffectiveShape
            }
            ship.SetLocalControl(false); ship.CaptureState();
            return ship;
        }
        catch (Exception e)
        {
            var failure = e;
            try
            {
                try { loadout?.Dispose(); }
                finally
                {
                    if (ship != null)
                    {
                        try { ship.Runtime?.Dispose(); }
                        finally { ship.Free(); }
                    }
                }
            }
            catch (Exception cleanupError)
            {
                failure = new AggregateException("Ship construction and cleanup failed.", e, cleanupError);
            }
            throw new InvalidOperationException($"Constructing ship '{data.Name}' entity '{entityId}' failed: {e.Message}", failure);
        }
    }
}
