using Godot;
using sMPGWM.Scripts.VfxSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class CombatPresentationTests
{
    private static AcceptedShot Shot(Guid session, int profile = 0, bool hit = true, bool shield = true) =>
        new(session, 10, 1, 0, 1, 1, profile, 0, Vector2.Zero, new Vector2(100, 0), Vector2.Left, hit, shield);

    private static ShotPresentationController Create(Guid session)
    {
        var effects = new ShotPresentationController();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(effects);
        effects.Initialize();
        effects.Clear(0, session);
        return effects;
    }

    [TestCase(0, false, false)]
    [TestCase(0, true, false)]
    [TestCase(0, true, true)]
    [TestCase(1, false, false)]
    [TestCase(1, true, false)]
    [TestCase(1, true, true)]
    public void EveryContactHasOneProfileImpactAndIndependentShieldLayer(int profile, bool hit, bool shield)
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        try
        {
            var shot = Shot(session, profile, hit, shield);
            effects.Enqueue(shot);
            effects.Enqueue(shot);
            effects.Advance(10);
            Assert.That(effects.MuzzleMetrics(profile).Active, Is.EqualTo(1));
            Assert.That(effects.TracerMetrics(profile).Active, Is.EqualTo(1));
            Assert.That(effects.ImpactMetrics(profile).Active, Is.EqualTo(hit ? 1 : 0));
            Assert.That(effects.ShieldMetrics.Active, Is.EqualTo(shield ? 1 : 0));
            Assert.That(effects.ImpactMetrics(1 - profile).Active, Is.Zero);
            var activeImpact = FindVisible(effects, profile * 3 + 2);
            if (hit)
            {
                Assert.That(activeImpact!.GlobalPosition, Is.EqualTo(shot.Endpoint));
                Assert.That(Mathf.Abs(Mathf.AngleDifference(activeImpact.GlobalRotation, shot.Normal.Angle())), Is.LessThan(.00001));
                Assert.That(activeImpact.GetNode<AnimatedSprite2D>("Visual").SpriteFrames.GetFrameCount("impact"), Is.EqualTo(profile == 0 ? 9 : 17));
            }
            effects.Advance(40);
            Assert.That(effects.ActiveEffects, Is.Zero);
        }
        finally { effects.Free(); }
    }

    [Test]
    public void AuthoredAnimationsAndShieldAgeShareTimelineWithoutFadingSpriteArtwork()
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        try
        {
            var smg = new WeaponPresentationProfile("smg_standard");
            var laser = new WeaponPresentationProfile("laser_standard");
            Assert.That(smg.MuzzleDuration, Is.EqualTo(.05).Within(.00001));
            Assert.That(smg.ImpactDuration, Is.EqualTo(.225).Within(.00001));
            Assert.That(laser.MuzzleDuration, Is.EqualTo(.1).Within(.00001));
            Assert.That(laser.ImpactDuration, Is.EqualTo(.425).Within(.00001));
            effects.Enqueue(Shot(session, 1));
            effects.Advance(14.5);
            Assert.That(FindVisible(effects, 3)!.GetNode<AnimatedSprite2D>("Visual").Frame, Is.EqualTo(3));
            Assert.That(FindVisible(effects, 5)!.GetNode<AnimatedSprite2D>("Visual").Frame, Is.EqualTo(3));
            Assert.That(FindVisible(effects, 5)!.Modulate.A, Is.EqualTo(1));
            var arc = FindVisible(effects, 6)!;
            Assert.That(arc.Modulate.A, Is.EqualTo(.5f).Within(.00001));
            Assert.That(arc.ZIndex, Is.GreaterThan(FindVisible(effects, 5)!.ZIndex));
            effects.Advance(16);
            Assert.That(effects.MuzzleMetrics(1).Active, Is.Zero);
            effects.Advance(19);
            Assert.That(effects.ShieldMetrics.Active, Is.Zero);
            Assert.That(effects.ImpactMetrics(1).Active, Is.EqualTo(1));
            effects.Advance(35.5);
            Assert.That(effects.ActiveEffects, Is.Zero);
        }
        finally { effects.Free(); }
    }

    [Test]
    public void ImpactAndShieldExhaustionAreIndependentAndReusable()
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        try
        {
            var shot = Shot(session);
            for (var i = 1; i <= 320; i++) effects.Enqueue(shot with { Sequence = i });
            effects.Advance(10);
            effects.Enqueue(shot with { Profile = 1, Sequence = 321 });
            effects.Advance(10);
            Assert.That(effects.ShieldMetrics.Active, Is.EqualTo(320));
            Assert.That(effects.ShieldMetrics.Dropped, Is.EqualTo(1));
            Assert.That(effects.ImpactMetrics(1).Active, Is.EqualTo(1));
            effects.Clear(10, session);
            shot = shot with { Tick = 20, Profile = 1 };
            for (var i = 1; i <= 225; i++) effects.Enqueue(shot with { Sequence = i });
            effects.Advance(20);
            Assert.That(effects.ImpactMetrics(1).Active, Is.EqualTo(224));
            Assert.That(effects.ImpactMetrics(1).Dropped, Is.EqualTo(1));
            Assert.That(effects.ShieldMetrics.Active, Is.EqualTo(225));
            effects.Advance(60);
            Assert.That(effects.ActiveEffects, Is.Zero);
            effects.Enqueue(shot with { Tick = 61, Sequence = 226 });
            effects.Advance(61);
            Assert.That(FindVisible(effects, 6)!.Modulate.A, Is.EqualTo(1));
            Assert.That(FindVisible(effects, 5)!.GetNode<AnimatedSprite2D>("Visual").Frame, Is.Zero);
            Assert.That(effects.ActiveEffects, Is.EqualTo(4));
        }
        finally { effects.Free(); }
    }

    [Test]
    public void RecoverySessionAndRemoteTimelineRejectHistoricalReplayAndQueueOverflowIsBounded()
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        try
        {
            var shot = Shot(session, 1);
            effects.Enqueue(shot);
            effects.Advance(10, 9);
            Assert.That(effects.PendingCount, Is.EqualTo(1));
            Assert.That(effects.ActiveEffects, Is.Zero);
            effects.Advance(20, 20);
            Assert.That(effects.ActiveEffects, Is.EqualTo(1));
            effects.Clear(20, session);
            effects.Enqueue(shot);
            effects.Enqueue(shot with { Session = Guid.NewGuid(), Tick = 21 });
            Assert.That(effects.PendingCount, Is.Zero);
            for (var i = 1; i <= ShotPresentationController.QueueCapacity + 1; i++)
                effects.Enqueue(shot with { Tick = 21, Sequence = i });
            Assert.That(effects.PendingCount, Is.EqualTo(ShotPresentationController.QueueCapacity));
            Assert.That(effects.QueueDrops, Is.EqualTo(1));
            effects.Clear(21, session);
            Assert.That(effects.PendingCount, Is.Zero);
            Assert.That(effects.ActiveEffects, Is.Zero);
        }
        finally { effects.Free(); }
    }

    private static Node2D? FindVisible(ShotPresentationController effects, int pool)
    {
        foreach (var child in effects.GetChildren())
            if (child is Node2D node && node.Visible && node.Name.ToString().StartsWith("Effect" + pool + "_", StringComparison.Ordinal)) return node;
        return null;
    }

    [Test]
    public void MuzzleFollowsDisplayedFixtureWhileImpactRemainsInWorldSpace()
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            var spawn = world.ApproveSpawn(1, new("fighter"));
            var ship = world.Entities[spawn.EntityId];
            effects.Enqueue(Shot(session, 1), ship);
            effects.Advance(10);
            ship.Presentation.Present(new Vector2(200, 300), .7f);
            effects.Advance(11);
            var visual = ship.GetNode<Node2D>("ShipVisuals");
            Assert.That(FindVisible(effects, 3)!.GlobalPosition.IsEqualApprox(visual.GlobalTransform * ship.Runtime.WeaponFixturePoints[0]), Is.True);
            Assert.That(FindVisible(effects, 3)!.GlobalRotation, Is.EqualTo(.7f).Within(.00001));
            Assert.That(FindVisible(effects, 5)!.GlobalPosition, Is.EqualTo(new Vector2(100, 0)));
            Assert.That(FindVisible(effects, 6)!.GlobalPosition, Is.EqualTo(new Vector2(100, 0)));
            world.Free();
            effects.Advance(12);
            effects.Clear(12, session);
            Assert.That(effects.ActiveEffects, Is.Zero);
        }
        finally
        {
            effects.Free();
            if (GodotObject.IsInstanceValid(world)) world.Free();
        }
    }

    [TestCase(5, 4, 0)]
    [TestCase(10, 4, 0)]
    [TestCase(10, 20, 1)]
    [TestCase(10, 1, 0)]
    public void SynchronizedPlayerBurstsStayBounded(int players, int interval, int profile)
    {
        var session = Guid.NewGuid();
        var effects = Create(session);
        try
        {
            var template = Shot(session, profile);
            var timings = new double[120];
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var tick = 1; tick <= 120; tick++)
            {
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                if ((tick - 1) % interval == 0)
                    for (var item = 1; item <= players * 9; item++)
                        effects.Enqueue(template with { Tick = tick, Item = (ulong)item, Sequence = (tick - 1) / interval + 1 });
                effects.Advance(tick);
                timings[tick - 1] = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Array.Sort(timings);
            var muzzle = effects.MuzzleMetrics(profile);
            var tracer = effects.TracerMetrics(profile);
            var impact = effects.ImpactMetrics(profile);
            var shield = effects.ShieldMetrics;
            Assert.That(effects.QueueHighWater, Is.EqualTo(players * 9));
            Assert.That(muzzle.HighWater, Is.LessThanOrEqualTo(muzzle.Capacity));
            Assert.That(tracer.HighWater, Is.LessThanOrEqualTo(tracer.Capacity));
            Assert.That(impact.HighWater, Is.LessThanOrEqualTo(impact.Capacity));
            Assert.That(shield.HighWater, Is.LessThanOrEqualTo(shield.Capacity));
            Assert.That(effects.QueueDrops, Is.Zero);
            Assert.That(effects.IdentityDrops, Is.Zero);
            if (interval == 1) Assert.That(impact.Dropped, Is.GreaterThan(0));
            else
            {
                Assert.That(muzzle.Dropped, Is.Zero);
                Assert.That(tracer.Dropped, Is.Zero);
                Assert.That(impact.Dropped, Is.Zero);
                Assert.That(shield.Dropped, Is.Zero);
            }
            TestContext.Out.WriteLine($"Presentation {players}x9, {60d / interval:F1}/s: muzzle {muzzle}; tracer {tracer}; impact {impact}; shield {shield}; queue high {effects.QueueHighWater}; audio drops {effects.AudioDrops}; managed bytes {allocated}; update p95 {timings[113]:F3} ms, max {timings[119]:F3} ms.");
        }
        finally { effects.Free(); }
    }
}
