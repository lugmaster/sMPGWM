using Godot; // TEST-SUPPORT: SHIELD-IDLE-RENDER-001 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
using sMPGWM.Scripts.VfxSystem; // TEST-SUPPORT: SHIELD-IDLE-RENDER-002 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
using sMPGWM.Scripts.WorldSystem; // TEST-SUPPORT: SHIELD-IDLE-RENDER-003 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled

namespace sMPGWM.Tests; // TEST-SUPPORT: SHIELD-IDLE-RENDER-005 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled

[Category("Godot")] // TEST-SUPPORT: SHIELD-IDLE-RENDER-007 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
public sealed class ShieldIdleRenderingTests // TEST-SUPPORT: SHIELD-IDLE-RENDER-008 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
{ // TEST-SUPPORT: SHIELD-IDLE-RENDER-009 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
    [Test] // TEST-SUPPORT: SHIELD-IDLE-RENDER-010 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
    public void ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled() // TEST-SUPPORT: SHIELD-IDLE-RENDER-011 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
    { // TEST-SUPPORT: SHIELD-IDLE-RENDER-012 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        var tree = (SceneTree)Engine.GetMainLoop(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-013 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        var previousDebug = tree.DebugCollisionsHint; // TEST-SUPPORT: SHIELD-IDLE-RENDER-014 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        var viewport = new SubViewport { Size = new(320, 180), World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always }; // TEST-SUPPORT: SHIELD-IDLE-RENDER-015 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        tree.Root.GetNode("Tests").AddChild(viewport); // TEST-SUPPORT: SHIELD-IDLE-RENDER-016 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        var world = new GameWorld { AuthoritativeItems = true }; // TEST-SUPPORT: SHIELD-IDLE-RENDER-017 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        viewport.AddChild(world); // TEST-SUPPORT: SHIELD-IDLE-RENDER-018 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        try // TEST-SUPPORT: SHIELD-IDLE-RENDER-019 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        { // TEST-SUPPORT: SHIELD-IDLE-RENDER-020 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            tree.DebugCollisionsHint = true; // TEST-SUPPORT: SHIELD-IDLE-RENDER-021 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var ship = world.SpawnEntity(new(Guid.NewGuid().ToString("N"), "fighter", new(160, 90), 0, Vector2.Zero)); // TEST-SUPPORT: SHIELD-IDLE-RENDER-022 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            ship.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 1, 0, 100, 1000, 0, 100, 0, 0, false, false, true, true, false), 0); // TEST-SUPPORT: SHIELD-IDLE-RENDER-023 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            ship.Runtime.SetShieldCurrent(100); // TEST-SUPPORT: SHIELD-IDLE-RENDER-024 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            ship.SynchronizeShieldCollider(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-025 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var collider = ship.GetNode<CollisionShape2D>("ShieldCollider"); // TEST-SUPPORT: SHIELD-IDLE-RENDER-026 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var overlay = ship.GetNode<ShieldColliderDebugView>("ShieldColliderDebugView"); // TEST-SUPPORT: SHIELD-IDLE-RENDER-027 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var glow = ship.GetNode<ShieldVisual>("ShipVisuals/ShieldVisual"); // TEST-SUPPORT: SHIELD-IDLE-RENDER-028 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(ship.ShieldColliderActive, Is.True); // TEST-SUPPORT: SHIELD-IDLE-RENDER-029 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(collider.Disabled, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-030 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(collider.Visible, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-031 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(overlay.Enabled, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-032 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(overlay.Visible, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-033 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(glow.Visible, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-034 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            using var ray = PhysicsRayQueryParameters2D.Create(ship.Position + new Vector2(80, 20), ship.Position + new Vector2(0, 20), 2); // TEST-SUPPORT: SHIELD-IDLE-RENDER-035 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(ship.GetWorld2D().DirectSpaceState.IntersectRay(ray).Count, Is.GreaterThan(0)); // TEST-SUPPORT: SHIELD-IDLE-RENDER-036 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            if (DisplayServer.GetName() == "headless") return; // TEST-SUPPORT: SHIELD-IDLE-RENDER-037 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var sprite = ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D"); // TEST-SUPPORT: SHIELD-IDLE-RENDER-038 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            ship.ForceUpdateTransform(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-039 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            sprite.ForceUpdateTransform(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-040 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            collider.ForceUpdateTransform(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-041 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.CanvasItemAddTextureRect(sprite.GetCanvasItem(), new(-sprite.Texture.GetSize() / 2, sprite.Texture.GetSize()), sprite.Texture.GetRid()); // TEST-SUPPORT: SHIELD-IDLE-RENDER-042 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            // Native draw geometry is queued explicitly because this suite runs before the first scene frame.
            collider.Shape.Draw(collider.GetCanvasItem(), new(0, 1, 1, .4f)); // TEST-SUPPORT: SHIELD-IDLE-RENDER-044 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.ViewportSetActive(viewport.GetViewportRid(), true); // TEST-SUPPORT: SHIELD-IDLE-RENDER-045 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.ForceDraw(false); // TEST-SUPPORT: SHIELD-IDLE-RENDER-046 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.ForceSync(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-047 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            using var hidden = viewport.GetTexture().GetImage(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-048 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(hidden.IsEmpty(), Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-049 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            collider.Visible = true; // TEST-SUPPORT: SHIELD-IDLE-RENDER-050 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.ForceDraw(false); // TEST-SUPPORT: SHIELD-IDLE-RENDER-051 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            RenderingServer.ForceSync(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-052 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            using var visible = viewport.GetTexture().GetImage(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-053 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            var changedPixels = 0; // TEST-SUPPORT: SHIELD-IDLE-RENDER-054 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            for (var y = 55; y < 125; y++) // TEST-SUPPORT: SHIELD-IDLE-RENDER-055 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
                for (var x = 120; x < 200; x++) // TEST-SUPPORT: SHIELD-IDLE-RENDER-056 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
                    if (hidden.GetPixel(x, y) != visible.GetPixel(x, y)) changedPixels++; // TEST-SUPPORT: SHIELD-IDLE-RENDER-057 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(changedPixels, Is.GreaterThan(500), "The positive control must render the native collider while the default hides the same geometry."); // TEST-SUPPORT: SHIELD-IDLE-RENDER-058 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            collider.Visible = false; // TEST-SUPPORT: SHIELD-IDLE-RENDER-059 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(collider.Disabled, Is.False); // TEST-SUPPORT: SHIELD-IDLE-RENDER-060 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            Assert.That(ship.GetWorld2D().DirectSpaceState.IntersectRay(ray).Count, Is.GreaterThan(0)); // TEST-SUPPORT: SHIELD-IDLE-RENDER-061 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        } // TEST-SUPPORT: SHIELD-IDLE-RENDER-062 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        finally // TEST-SUPPORT: SHIELD-IDLE-RENDER-063 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        { // TEST-SUPPORT: SHIELD-IDLE-RENDER-064 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            tree.DebugCollisionsHint = previousDebug; // TEST-SUPPORT: SHIELD-IDLE-RENDER-065 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            world.Clear(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-066 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
            viewport.Free(); // TEST-SUPPORT: SHIELD-IDLE-RENDER-067 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
        } // TEST-SUPPORT: SHIELD-IDLE-RENDER-068 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
    } // TEST-SUPPORT: SHIELD-IDLE-RENDER-069 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled
} // TEST-SUPPORT: SHIELD-IDLE-RENDER-070 | Tests: sMPGWM.Tests.ShieldIdleRenderingTests.ActiveShieldStaysInvisibleWithNativeCollisionDebugEnabled

