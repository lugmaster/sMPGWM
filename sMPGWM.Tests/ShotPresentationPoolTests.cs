using Godot; // TEST-SUPPORT: SHOT-POOL-001 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: SHOT-POOL-002 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
using sMPGWM.Scripts.VfxSystem; // TEST-SUPPORT: SHOT-POOL-003 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
using sMPGWM.Scripts.WeaponSystem; // TEST-SUPPORT: SHOT-POOL-004 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
using sMPGWM.Scripts.WorldSystem; // TEST-SUPPORT: SHOT-POOL-005 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear

namespace sMPGWM.Tests; // TEST-SUPPORT: SHOT-POOL-006 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear

[Category("Godot")] // TEST-SUPPORT: SHOT-POOL-007 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
public sealed class ShotPresentationPoolTests // TEST-SUPPORT: SHOT-POOL-008 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
{ // TEST-SUPPORT: SHOT-POOL-009 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
    [Test] // TEST-SUPPORT: SHOT-POOL-010 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
    public void SaturatedPoolsRecoverAfterExpirationAndRepeatedClear() // TEST-SUPPORT: SHOT-POOL-011 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
    { // TEST-SUPPORT: SHOT-POOL-012 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        var world = new GameWorld(); // TEST-SUPPORT: SHOT-POOL-013 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        var effects = new ShotPresentationController(); // TEST-SUPPORT: SHOT-POOL-014 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(world); // TEST-SUPPORT: SHOT-POOL-015 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        world.AddChild(effects); // TEST-SUPPORT: SHOT-POOL-016 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        try // TEST-SUPPORT: SHOT-POOL-017 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        { // TEST-SUPPORT: SHOT-POOL-018 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Initialize(world); // TEST-SUPPORT: SHOT-POOL-019 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: SHOT-POOL-020 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            world.SpawnEntity(new(entity, "fighter", Vector2.Zero, 0, Vector2.Zero)); // TEST-SUPPORT: SHOT-POOL-021 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            ulong sequence = 0; // TEST-SUPPORT: SHOT-POOL-022 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Fill(10); // TEST-SUPPORT: SHOT-POOL-023 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(1024)); // TEST-SUPPORT: SHOT-POOL-024 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.HighWater, Is.EqualTo(1024)); // TEST-SUPPORT: SHOT-POOL-025 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.DroppedEffects, Is.EqualTo(512)); // TEST-SUPPORT: SHOT-POOL-026 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Advance(14); // TEST-SUPPORT: SHOT-POOL-027 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(768)); // TEST-SUPPORT: SHOT-POOL-028 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Advance(16); // TEST-SUPPORT: SHOT-POOL-029 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(256)); // TEST-SUPPORT: SHOT-POOL-030 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Advance(20); // TEST-SUPPORT: SHOT-POOL-031 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(256)); // TEST-SUPPORT: SHOT-POOL-032 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Advance(40); // TEST-SUPPORT: SHOT-POOL-033 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.Zero); // TEST-SUPPORT: SHOT-POOL-034 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Fill(41); // TEST-SUPPORT: SHOT-POOL-035 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(1024)); // TEST-SUPPORT: SHOT-POOL-036 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.DroppedEffects, Is.EqualTo(1024)); // TEST-SUPPORT: SHOT-POOL-037 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Clear(41); // TEST-SUPPORT: SHOT-POOL-038 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Clear(41); // TEST-SUPPORT: SHOT-POOL-039 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.Zero); // TEST-SUPPORT: SHOT-POOL-040 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Fill(42); // TEST-SUPPORT: SHOT-POOL-041 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.ActiveEffects, Is.EqualTo(1024)); // TEST-SUPPORT: SHOT-POOL-042 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            Assert.That(effects.DroppedEffects, Is.EqualTo(1536)); // TEST-SUPPORT: SHOT-POOL-043 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear

            void Fill(long tick) // TEST-SUPPORT: SHOT-POOL-044 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            { // TEST-SUPPORT: SHOT-POOL-045 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                for (var i = 0; i < 256; i++) // TEST-SUPPORT: SHOT-POOL-046 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                { // TEST-SUPPORT: SHOT-POOL-047 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                    sequence++; // TEST-SUPPORT: SHOT-POOL-048 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                    effects.Enqueue(new(tick, ItemEntityId.FromNetwork(1), sequence, entity, 1, "smg_standard", 0, Vector2.Zero, Vector2.Right * 100, Vector2.Left, ShotResultFlags.Hull | ShotResultFlags.Shield)); // TEST-SUPPORT: SHOT-POOL-049 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                    effects.Enqueue(new(tick, ItemEntityId.FromNetwork(2), sequence, entity, 1, "laser_standard", 0, Vector2.Zero, Vector2.Right * 100, Vector2.Left, ShotResultFlags.Hull | ShotResultFlags.Shield)); // TEST-SUPPORT: SHOT-POOL-050 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                } // TEST-SUPPORT: SHOT-POOL-051 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
                effects.Advance(tick); // TEST-SUPPORT: SHOT-POOL-052 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            } // TEST-SUPPORT: SHOT-POOL-053 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        } // TEST-SUPPORT: SHOT-POOL-054 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        finally // TEST-SUPPORT: SHOT-POOL-055 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        { // TEST-SUPPORT: SHOT-POOL-056 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            effects.Clear(long.MaxValue); // TEST-SUPPORT: SHOT-POOL-057 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            world.Clear(); // TEST-SUPPORT: SHOT-POOL-058 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
            world.Free(); // TEST-SUPPORT: SHOT-POOL-059 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
        } // TEST-SUPPORT: SHOT-POOL-060 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
    } // TEST-SUPPORT: SHOT-POOL-061 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear
} // TEST-SUPPORT: SHOT-POOL-062 | Tests: sMPGWM.Tests.ShotPresentationPoolTests.SaturatedPoolsRecoverAfterExpirationAndRepeatedClear

