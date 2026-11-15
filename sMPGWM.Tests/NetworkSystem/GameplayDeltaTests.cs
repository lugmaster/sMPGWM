using Godot;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class GameplayDeltaTests
{
    [Test]
    public void ResourcePublicationReusesStorageWithinMeasuredAllocationBudget()
    {
        using var host = new ShipRuntime(100);
        host.InitializeResources();
        using var publisher = new GameplayDeltaState();
        var first = publisher.Encode(1, host);
        for (var i = 0; i < 100; i++)
        {
            host.SetHealthCurrent(90 + i % 2);
            publisher.Encode(1, host);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) host.SetHealthCurrent(90 + i % 2);
        var runtimeAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
        using var wrapperProbe = new Godot.Collections.Array();
        using var nested = new Godot.Collections.Array();
        wrapperProbe.Resize(3);
        for (var i = 0; i < 100; i++)
        {
            wrapperProbe[0] = nested; wrapperProbe[1] = nested; wrapperProbe[2] = nested;
        }
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            wrapperProbe[0] = nested; wrapperProbe[1] = nested; wrapperProbe[2] = nested;
        }
        var nestedArrayAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        Godot.Collections.Array? last = null;
        for (var i = 0; i < 1000; i++)
        {
            host.SetHealthCurrent(90 + i % 2);
            last = publisher.Encode(1, host);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"Resource publication bytes/1000: {allocated}; runtime only: {runtimeAllocated}; three nested-array assignments: {nestedArrayAllocated}.");
        // Cached array Variants eliminate the warmed resource encoder wrapper allocations.
        Assert.That(allocated, Is.Zero);
        Assert.That(last, Is.SameAs(first));
        Assert.That(last![5].AsGodotArray()[9].AsDouble(), Is.EqualTo(91));
    }

    [Test]
    public void PreparedConfigurationAndEffectsMatchFullSnapshotSchema()
    {
        var definitions = GameplayTestData.Load();
        using var host = new ShipRuntime(100);
        host.ApplyEquipment(new ResolvedShipStats(10, 20, 1, 1, 1, 10, 0, 100,
            1000, 1000, 120, 0, 100, true, true, true, true, true), 1);
        host.InitializeResources();
        host.SetFaction(7);
        host.SetCondition(definitions.Conditions.GetById("dead"), true);
        using var publisher = new GameplayDeltaState();
        using var full = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host);
        var delta = publisher.Encode(1, host)!;
        using var rebuilt = full.Duplicate();
        var configuration = delta[3].AsGodotArray();
        var effects = delta[4].AsGodotArray();
        var resources = delta[5].AsGodotArray();
        for (var i = 0; i < configuration.Count; i++) rebuilt[GameplayDeltaState.ConfigurationFields[i]] = configuration[i];
        for (var i = 0; i < effects.Count; i++) rebuilt[GameplayDeltaState.EffectFields[i]] = effects[i];
        for (var i = 0; i < resources.Count; i++) rebuilt[GameplayDeltaState.ResourceFields[i]] = resources[i];
        using var fullRows = new Godot.Collections.Array { full };
        using var rebuiltRows = new Godot.Collections.Array { rebuilt };
        Assert.That(GameplaySnapshotCodec.TryDecode(fullRows, definitions, out var expected), Is.True);
        Assert.That(GameplaySnapshotCodec.TryDecode(rebuiltRows, definitions, out var actual), Is.True);
        Assert.That(actual[0].Stats, Is.EqualTo(expected[0].Stats));
        Assert.That(actual[0].Conditions, Is.EqualTo(expected[0].Conditions));
        Assert.That(actual[0].FactionId, Is.EqualTo(expected[0].FactionId));
        Assert.That(actual[0].Components, Is.EqualTo(expected[0].Components));
        Assert.That(actual[0].HealthCurrent, Is.EqualTo(expected[0].HealthCurrent));
    }

    [Test]
    public void ResourceChangesDoNotResendConfigurationOrEffectsAndRoundTrip()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var publisher = new GameplayDeltaState();
        using var receiver = new GameplayDeltaReceiver();
        using var host = new ShipRuntime(100);
        host.ApplyEquipment(new ResolvedShipStats(10, 20, 1, 1, 1, 10, 0, 100,
            1000, 1000, 120, 0, 100, true, true, true, true, true), 1);
        host.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var ship = world.Players[1].ControlledShip!;
            string? Resolve(int index) => index == 1 ? ship.EntityId : null;
            var first = publisher.Encode(1, host)!;
            Assert.That(first[3].AsGodotArray().Count, Is.EqualTo(6));
            Assert.That(first[4].AsGodotArray().Count, Is.EqualTo(2));
            Assert.That(receiver.Apply(new() { first }, world, definitions, Resolve), Is.True);
            host.AdvanceResources(1);
            var delta = publisher.Encode(1, host)!;
            Assert.That(delta[3].AsGodotArray(), Is.Empty);
            Assert.That(delta[4].AsGodotArray(), Is.Empty);
            Assert.That(receiver.Apply(new() { delta }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(host.Energy.Current));
            Assert.That(publisher.Encode(1, host), Is.Null);

            host.SetFaction(7);
            var config = publisher.Encode(1, host)!;
            Assert.That(config[3].AsGodotArray().Count, Is.EqualTo(6));
            Assert.That(config[4].AsGodotArray(), Is.Empty);
            Assert.That(receiver.Apply(new() { config }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.FactionId, Is.EqualTo(7));

            host.SetCondition(definitions.Conditions.GetById("dead"), true);
            var death = publisher.Encode(1, host)!;
            Assert.That(death[4].AsGodotArray().Count, Is.EqualTo(2));
            Assert.That(receiver.Apply(new() { death }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.IsAlive, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void RejectedConfigurationAndEffectBatchCanRetryWithoutLeakingStagedState()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var receiver = new GameplayDeltaReceiver();
        using var firstPublisher = new GameplayDeltaState();
        using var secondPublisher = new GameplayDeltaState();
        using var firstHost = new ShipRuntime(100);
        using var secondHost = new ShipRuntime(100);
        firstHost.InitializeResources(); secondHost.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var firstShip = world.Players[1].ControlledShip!;
            var secondShip = world.Players[2].ControlledShip!;
            string? Resolve(int index) => index == 1 ? firstShip.EntityId : index == 2 ? secondShip.EntityId : null;
            using var initial = new Godot.Collections.Array { firstPublisher.Encode(1, firstHost)!, secondPublisher.Encode(2, secondHost)! };
            Assert.That(receiver.Apply(initial, world, definitions, Resolve), Is.True);
            var firstFaction = firstShip.Runtime.FactionId;
            var secondFaction = secondShip.Runtime.FactionId;
            firstHost.SetFaction(7);
            firstHost.SetCondition(definitions.Conditions.GetById("jammed"), true);
            secondHost.SetFaction(8);
            secondHost.SetCondition(definitions.Conditions.GetById("silenced"), true);
            using var first = firstPublisher.Encode(1, firstHost)!.Duplicate(true);
            using var second = secondPublisher.Encode(2, secondHost)!.Duplicate(true);
            using var invalid = second.Duplicate(true);
            using (var effects = invalid[4].AsGodotArray())
            using (var conditions = effects[1].AsGodotArray()) conditions.Add("missing");
            using var rejected = new Godot.Collections.Array { first, invalid };
            Assert.That(receiver.Apply(rejected, world, definitions, Resolve), Is.False);
            Assert.That(firstShip.Runtime.FactionId, Is.EqualTo(firstFaction));
            Assert.That(secondShip.Runtime.FactionId, Is.EqualTo(secondFaction));
            Assert.That(firstShip.Runtime.Conditions, Is.Empty);
            Assert.That(secondShip.Runtime.Conditions, Is.Empty);
            using var valid = new Godot.Collections.Array { second, first };
            Assert.That(receiver.Apply(valid, world, definitions, Resolve), Is.True);
            Assert.That(firstShip.Runtime.FactionId, Is.EqualTo(7));
            Assert.That(secondShip.Runtime.FactionId, Is.EqualTo(8));
            Assert.That(firstShip.Runtime.Conditions, Is.EquivalentTo(new[] { "jammed" }));
            Assert.That(secondShip.Runtime.Conditions, Is.EquivalentTo(new[] { "silenced" }));

            firstHost.SetHealthCurrent(90); secondHost.SetHealthCurrent(80);
            using var resources = new Godot.Collections.Array { firstPublisher.Encode(1, firstHost)!, secondPublisher.Encode(2, secondHost)! };
            Assert.That(receiver.Apply(resources, world, definitions, Resolve), Is.True);
            firstHost.SetFaction(9); secondHost.SetFaction(10);
            using var configuration = new Godot.Collections.Array { firstPublisher.Encode(1, firstHost)!, secondPublisher.Encode(2, secondHost)! };
            Assert.That(receiver.Apply(configuration, world, definitions, Resolve), Is.True);
            Assert.That(firstShip.Runtime.Conditions, Is.EquivalentTo(new[] { "jammed" }));
            Assert.That(secondShip.Runtime.Conditions, Is.EquivalentTo(new[] { "silenced" }));
            Assert.That(firstShip.Runtime.Health.Current, Is.EqualTo(90));
            Assert.That(secondShip.Runtime.Health.Current, Is.EqualTo(80));
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void WarmedConfigurationAndEffectReceptionHasBoundedAllocation()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var receiver = new GameplayDeltaReceiver();
        using var publisher = new GameplayDeltaState();
        using var host = new ShipRuntime(100);
        host.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var ship = world.Players[1].ControlledShip!;
            string? Resolve(int index) => index == 1 ? ship.EntityId : null;
            Func<int, string?> resolve = Resolve;
            using var rows = new Godot.Collections.Array { publisher.Encode(1, host)! };
            Assert.That(receiver.Apply(rows, world, definitions, resolve), Is.True);
            var condition = definitions.Conditions.GetById("jammed");
            long allocated = 0;
            var valid = true;
            for (var i = 0; i < 1100; i++)
            {
                host.SetFaction(1 + i % 2);
                host.SetCondition(condition, i % 2 == 0);
                publisher.Encode(1, host);
                var before = GC.GetAllocatedBytesForCurrentThread();
                valid &= receiver.Apply(rows, world, definitions, resolve);
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (i >= 100) allocated += bytes;
            }
            TestContext.Out.WriteLine($"Configuration/effect reception bytes/1000: {allocated}.");
            Assert.That(valid, Is.True);
            Assert.That(allocated, Is.LessThanOrEqualTo(3_600_000));
            Assert.That(ship.Runtime.FactionId, Is.EqualTo(host.FactionId));
            Assert.That(ship.Runtime.Conditions, Is.EqualTo(host.Conditions));
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void InvalidResourceBatchDoesNotPartiallyApplyAndSeedSupportsLateJoin()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var publisher = new GameplayDeltaState();
        using var receiver = new GameplayDeltaReceiver();
        using var host = new ShipRuntime(100);
        host.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var ship = world.Players[1].ControlledShip!;
            string? Resolve(int index) => index == 1 ? ship.EntityId : null;
            Assert.That(receiver.Apply(new() { publisher.Encode(1, host)! }, world, definitions, Resolve), Is.True);
            receiver.Seed(ship.EntityId, ship.Runtime);
            host.SetHealthCurrent(90);
            var row = publisher.Encode(1, host)!;
            var revision = ship.Runtime.Revision;
            using var duplicate = row.Duplicate(true);
            Assert.That(receiver.Apply(new() { row, duplicate }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            var values = duplicate[5].AsGodotArray(); values[9] = double.NaN;
            Assert.That(receiver.Apply(new() { duplicate }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            Assert.That(receiver.Apply(new() { row }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(90));
            host.AdvanceResources(10);
            host.SetHealthCurrent(80);
            var next = publisher.Encode(1, host)!;
            using var missingConfiguration = next.Duplicate(true);
            missingConfiguration[1] = host.Revision;
            Assert.That(receiver.Apply(new() { missingConfiguration }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(90));
            Assert.That(receiver.Apply(new() { next }, world, definitions, Resolve), Is.True);
            host.SetHealthCurrent(70);
            var later = publisher.Encode(1, host)!;
            using var backwards = later.Duplicate(true);
            backwards[5].AsGodotArray()[4] = 9L;
            Assert.That(receiver.Apply(new() { backwards }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(80));
            Assert.That(receiver.Apply(new() { later }, world, definitions, Resolve), Is.True);
        }
        finally { world.Clear(); world.Free(); }
    }
}
