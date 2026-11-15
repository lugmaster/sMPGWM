using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class WeaponSimulationTests
{
    [Test]
    public void PlayableDefinitionsRejectUnsupportedProfilesAndCadenceBeforeActivation()
    {
        var weapon = (WeaponItemDefinition)GameplayDefinitions.Current.Items.GetById("vulcan_mk1");
        Assert.Throws<ArgumentException>(() => ShipWeaponController.ValidateDefinition(weapon with { PresentationId = "missing" }));
        Assert.Throws<ArgumentException>(() => ShipWeaponController.ValidateDefinition(weapon with { AttackSpeed = 61 }));
        Assert.DoesNotThrow(() => ShipWeaponController.ValidateDefinition(weapon with { AttackSpeed = 60 }));
    }

    [TestCase(1, 11, 0)]
    [TestCase(2, 0, 3)]
    [TestCase(3, 11, 3)]
    [TestCase(0, 0, 0)]
    public void GroupsUseIndependentCadenceAndPaySupply(byte mask, int smgCount, int laserCount)
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            ship.GlobalPosition = new(10000, 10000);
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            var energy = ship.Runtime.Energy.Current;
            var health = ship.Runtime.Health.Current;
            var shield = ship.Runtime.Shield.Current;
            ship.SetInput(new(Vector2.Zero, 2, true, false, false, false, mask, ship.Equipment.Revision));
            for (var tick = 0; tick <= 40; tick++) weapons.Step(tick, _ => true);
            var smg = 0;
            var laser = 0;
            foreach (var shot in shots)
            {
                if (shot.Profile == 0) smg++; else laser++;
                Assert.That(shot.Hit, Is.False);
                Assert.That(shot.ShieldContact, Is.False);
            }
            Assert.That(smg, Is.EqualTo(smgCount));
            Assert.That(laser, Is.EqualTo(laserCount));
            Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(energy - laserCount * 4));
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(health));
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(shield));
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void BothMembershipFiresOnceAndMuzzlesUseAuthoritativeTransform()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            var equipment = ship.Equipment;
            var slot = equipment.FindSlot("weapon_2");
            var laser = equipment.Snapshot.Slots[slot]!;
            equipment.ApplyWeaponGroupDefinition(["weapon_2"], ["weapon_2"]);
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            ship.GlobalPosition = new(10000, 10000);
            ship.GlobalRotation = MathF.PI / 2;
            ship.SetInput(new(Vector2.Zero, -1, true, false, false, false, 3, equipment.Revision));
            weapons.Step(0, _ => true);
            weapons.Step(0, _ => true);
            for (var tick = 1; tick <= 20; tick++) weapons.Step(tick, _ => true);
            Assert.That(shots.Count, Is.EqualTo(2));
            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                Assert.That(shot.Item, Is.EqualTo(laser.Id.Value));
                Assert.That(shot.Sequence, Is.EqualTo(i + 1));
                Assert.That(shot.Muzzle, Is.EqualTo(i % ship.Runtime.WeaponFixturePoints.Count));
                var origin = ship.GlobalTransform * ship.Runtime.WeaponFixturePoints[shot.Muzzle];
                Assert.That(shot.Origin.DistanceTo(origin), Is.LessThan(.001f));
                Assert.That((shot.Endpoint - origin).Normalized().Dot(ship.GlobalTransform.X.Normalized()), Is.GreaterThan(.999f));
                Assert.That(shot.Endpoint.DistanceTo(origin), Is.EqualTo(((WeaponItemDefinition)laser.Definition).Range).Within(.01));
            }
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void StaleConfigurationUnreadyPeerAndMissingPowerCannotFire()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            Configure(weapons, _ => Assert.Fail("Rejected input fired."));
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 3, ship.Equipment.Revision - 1));
            weapons.Step(0, _ => true);
            ship.SetInput(ship.InputState with { WeaponRevision = ship.Equipment.Revision });
            weapons.Step(1, _ => false);
            ship.Runtime.StageComponentChange(ShipComponentKind.Power, false);
            ship.Runtime.CommitConfiguration(2);
            weapons.Step(2, _ => true);
            Assert.That(weapons.AcceptedCount, Is.Zero);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void NearestWorldBlockerWinsAndOriginInsideStillHits()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        var near = PhysicsServer2D.BodyCreate();
        var far = PhysicsServer2D.BodyCreate();
        var shape = PhysicsServer2D.CircleShapeCreate();
        try
        {
            var ship = world.Players[1].ControlledShip!;
            ship.GlobalPosition = new(10000, 10000);
            ship.ForceUpdateTransform();
            ship.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            var origin = ship.GlobalTransform * ship.Runtime.WeaponFixturePoints[0];
            PhysicsServer2D.ShapeSetData(shape, 10f);
            AddBlocker(near, shape, ship, origin + Vector2.Right * 100);
            AddBlocker(far, shape, ship, origin + Vector2.Right * 200);
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, ship.Equipment.Revision));
            weapons.Step(0, _ => true);
            Assert.That(shots.Count, Is.EqualTo(1));
            Assert.That(shots[0].Hit, Is.True);
            Assert.That(shots[0].ShieldContact, Is.False);
            Assert.That(shots[0].Endpoint.DistanceTo(origin + Vector2.Right * 90), Is.LessThan(.01));
            var nextOrigin = ship.GlobalTransform * ship.Runtime.WeaponFixturePoints[1 % ship.Runtime.WeaponFixturePoints.Count];
            PhysicsServer2D.BodySetState(near, PhysicsServer2D.BodyState.Transform, new Transform2D(0, nextOrigin));
            weapons.Step(20, _ => true);
            Assert.That(shots[1].Hit, Is.True);
            Assert.That(shots[1].Endpoint.DistanceTo(nextOrigin), Is.LessThan(.01));
            Assert.That(shots[1].Normal.Dot(Vector2.Left), Is.GreaterThan(.99));
        }
        finally
        {
            PhysicsServer2D.FreeRid(near); PhysicsServer2D.FreeRid(far); PhysicsServer2D.FreeRid(shape);
            world.Clear(); world.Free();
        }
    }

    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void ShieldContactRecordsDamageTimeAvailability(bool charged, bool powered)
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        var body = PhysicsServer2D.BodyCreate();
        var shape = PhysicsServer2D.CircleShapeCreate();
        try
        {
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var shooter = world.Players[1].ControlledShip!;
            var target = world.Players[2].ControlledShip!;
            target.Runtime.SetFaction(2);
            target.Runtime.SetShieldCurrent(charged ? target.Runtime.Shield.Capacity : 0);
            if (!powered)
            {
                target.Runtime.StageComponentChange(ShipComponentKind.Power, false);
                target.Runtime.CommitConfiguration(0);
            }
            shooter.GlobalPosition = new(10000, 10000);
            shooter.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
            var origin = shooter.GlobalTransform * shooter.Runtime.WeaponFixturePoints[0];
            PhysicsServer2D.ShapeSetData(shape, 10f);
            AddBlocker(body, shape, shooter, origin + Vector2.Right * 100);
            PhysicsServer2D.BodyAttachObjectInstanceId(body, target.GetInstanceId());
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            var shield = target.Runtime.Shield.Current;
            var health = target.Runtime.Health.Current;
            var downtime = target.Runtime.ShieldDowntimeDeadline;
            shooter.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, shooter.Equipment.Revision));
            weapons.Step(0, _ => true);
            Assert.That(shots.Count, Is.EqualTo(1));
            Assert.That(shots[0].Hit, Is.True);
            Assert.That(shots[0].ShieldContact, Is.EqualTo(charged && powered));
            Assert.That(target.Runtime.Shield.Current, Is.EqualTo(charged && powered ? shield - 9 : shield));
            Assert.That(target.Runtime.Health.Current, Is.EqualTo(charged && powered ? health : health - 9 * 100 / (100 + target.Runtime.Stats.Armor)).Within(1e-9));
            Assert.That(target.Runtime.ShieldDowntimeDeadline, Is.EqualTo(downtime));
        }
        finally
        {
            PhysicsServer2D.FreeRid(body); PhysicsServer2D.FreeRid(shape);
            world.Clear(); world.Free();
        }
    }

    private static void AddBlocker(Rid body, Rid shape, ShipController ship, Vector2 position)
    {
        PhysicsServer2D.BodySetMode(body, PhysicsServer2D.BodyMode.Static);
        PhysicsServer2D.BodyAddShape(body, shape);
        PhysicsServer2D.BodySetState(body, PhysicsServer2D.BodyState.Transform, new Transform2D(0, position));
        PhysicsServer2D.BodySetSpace(body, ship.GetWorld2D().Space);
    }

    [Test]
    public void LivingAllyIsExcludedButItsWreckBlocks()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        var shape = PhysicsServer2D.CircleShapeCreate();
        try
        {
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var shooter = world.Players[1].ControlledShip!;
            var ally = world.Players[2].ControlledShip!;
            shooter.GlobalPosition = new(10000, 10000);
            shooter.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
            var origin = shooter.GlobalTransform * shooter.Runtime.WeaponFixturePoints[0];
            PhysicsServer2D.ShapeSetData(shape, 50f);
            PhysicsServer2D.BodySetMode(ally.GetRid(), PhysicsServer2D.BodyMode.Static);
            PhysicsServer2D.BodyAddShape(ally.GetRid(), shape);
            PhysicsServer2D.BodySetState(ally.GetRid(), PhysicsServer2D.BodyState.Transform, new Transform2D(0, origin + Vector2.Right * 100));
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            shooter.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, shooter.Equipment.Revision));
            weapons.Step(0, _ => true);
            Assert.That(shots[0].Hit, Is.False);
            ally.Runtime.SetCondition(GameplayDefinitions.Current.Conditions.GetById("dead"), true);
            weapons.Step(20, _ => true);
            Assert.That(shots[1].Hit, Is.True);
        }
        finally { world.Clear(); world.Free(); PhysicsServer2D.FreeRid(shape); }
    }

    [Test]
    public void UnequippedWeaponRetainsCadenceThroughWorldRestorationAndReequip()
    {
        var source = CreateWorld();
        var restored = new GameWorld { AuthoritativeItems = true };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(restored);
        using var weapons = new WeaponSimulation(source);
        using var restoredWeapons = new WeaponSimulation(restored);
        try
        {
            var owner = source.Players[1];
            var ship = owner.ControlledShip!;
            ship.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
            Configure(weapons, _ => { });
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, ship.Equipment.Revision));
            weapons.Step(0, _ => true);
            var slot = ship.Equipment.FindSlot("weapon_2");
            var item = ship.Equipment.Snapshot.Slots[slot]!;
            var empty = -1;
            for (var i = 0; i < owner.Inventory.Capacity; i++)
                if (owner.Inventory.Snapshot.Slots[i] == null) { empty = i; break; }
            Assert.That(empty, Is.GreaterThanOrEqualTo(0));
            Assert.That(source.Items.TransferItem(item.Id, new(ship.Equipment.Container.Id, slot),
                new(owner.Inventory.Container.Id, empty), ship.Equipment.Revision, owner.Inventory.Revision), Is.EqualTo(TransferResult.Success));
            restored.RestoreState(source.CaptureState(0, 0));
            var restoredOwner = restored.Players[1];
            var restoredShip = restoredOwner.ControlledShip!;
            var restoredItem = restoredOwner.Inventory.Snapshot.Slots[empty]!;
            Assert.That(restoredItem.WeaponState!.Sequence, Is.EqualTo(1));
            Assert.That(restoredItem.WeaponState.NextEligibleTick, Is.EqualTo(20));
            Assert.That(restored.Items.TransferItem(restoredItem.Id, new(restoredOwner.Inventory.Container.Id, empty),
                new(restoredShip.Equipment.Container.Id, slot), restoredOwner.Inventory.Revision, restoredShip.Equipment.Revision), Is.EqualTo(TransferResult.Success));
            restoredShip.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
            var shots = new List<AcceptedShot>();
            Configure(restoredWeapons, shots.Add);
            restoredShip.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, restoredShip.Equipment.Revision));
            for (var tick = 1; tick <= 20; tick++) restoredWeapons.Step(tick, _ => true);
            Assert.That(shots.Count, Is.EqualTo(1));
            Assert.That(shots[0].Sequence, Is.EqualTo(2));
            Assert.That(shots[0].Tick, Is.EqualTo(20));
            Assert.That(shots[0].Item, Is.EqualTo(item.Id.Value));
        }
        finally { restored.Clear(); restored.Free(); source.Clear(); source.Free(); }
    }

    [Test]
    public void CapturedItemsKeepIndependentCadenceAfterSourceContinuesFiring()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            Configure(weapons, _ => { });
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 3, ship.Equipment.Revision));
            weapons.Step(0, _ => true);
            var captured = ship.Loadout.Store.Capture(ship.Equipment.Container);
            for (var tick = 1; tick <= 40; tick++) weapons.Step(tick, _ => true);
            var slot = ship.Equipment.FindSlot("weapon_1");
            Assert.That(captured.Slots[slot]!.WeaponState!.Sequence, Is.EqualTo(1));
            Assert.That(ship.Equipment.Snapshot.Slots[slot]!.WeaponState!.Sequence, Is.EqualTo(11));
        }
        finally { world.Clear(); world.Free(); }
    }

    private static GameWorld CreateWorld()
    {
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = DevelopmentLoadout.Equip };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        world.ApproveSpawn(1, PlayerStartupData.Create());
        Charge(world.Players[1].ControlledShip!);
        return world;
    }

    private static void Charge(ShipController ship)
    {
        var stats = ship.Runtime.EquipmentInputs;
        var battery = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("battery")]!.Id.Value;
        ship.Runtime.ApplyEquipment(stats with { EnergyProduction = 1200 + stats.EnergyConsumption }, battery);
        ship.Runtime.AdvanceResources(0);
        ship.Runtime.ApplyEquipment(stats, battery);
    }

    [Test]
    public void MagazineCompletesOn326BeforeFiringInFullPipeline()
    {
        var world = CreateWorld();
        using var abilities = new sMPGWM.Scripts.AbilitySystem.AbilityRuntime(GameplayDefinitions.Current);
        using var simulation = new GameplaySimulation(world, abilities);
        try
        {
            var ship = world.Players[1].ControlledShip!;
            ship.GlobalPosition = new(10000, 10000);
            var shots = new List<AcceptedShot>();
            Configure(simulation.Weapons, shots.Add);
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, ship.Equipment.Revision));
            for (var tick = 0; tick <= 326; tick++) simulation.Step(tick, _ => true, (_, _, _) => { });
            Assert.That(shots.Count, Is.EqualTo(61));
            Assert.That(shots[59].Tick, Is.EqualTo(236));
            Assert.That(shots[60].Tick, Is.EqualTo(326));
            var state = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!.WeaponState!;
            Assert.That(state.Rounds, Is.EqualTo(59));
            Assert.That(state.IsReloading, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void AcceptedHitsTradeWithoutCosmeticConsumer()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        var firstBody = PhysicsServer2D.BodyCreate();
        var secondBody = PhysicsServer2D.BodyCreate();
        var shape = PhysicsServer2D.CircleShapeCreate();
        try
        {
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var first = world.Players[1].ControlledShip!;
            var second = world.Players[2].ControlledShip!;
            Charge(second);
            second.Runtime.SetFaction(2);
            first.GlobalPosition = new(10000, 10000);
            second.GlobalPosition = new(10000, 20000);
            PhysicsServer2D.ShapeSetData(shape, 20f);
            foreach (var ship in new[] { first, second })
            {
                ship.Runtime.SetShieldCurrent(0);
                ship.Runtime.SetHealthCurrent(1);
                ship.Equipment.ApplyWeaponGroupDefinition(["weapon_2"], []);
                ship.SetInput(new(Vector2.Zero, 0, false, false, false, false, 1, ship.Equipment.Revision));
            }
            AddBlocker(firstBody, shape, first, first.GlobalTransform * first.Runtime.WeaponFixturePoints[0] + Vector2.Right * 100);
            PhysicsServer2D.BodyAttachObjectInstanceId(firstBody, second.GetInstanceId());
            AddBlocker(secondBody, shape, second, second.GlobalTransform * second.Runtime.WeaponFixturePoints[0] + Vector2.Right * 100);
            PhysicsServer2D.BodyAttachObjectInstanceId(secondBody, first.GetInstanceId());
            Configure(weapons, _ => { });
            weapons.Accepted = null;
            weapons.Step(0, _ => true);
            Assert.That(weapons.AcceptedCount, Is.EqualTo(2));
            Assert.That(first.Runtime.IsAlive, Is.False);
            Assert.That(second.Runtime.IsAlive, Is.False);
            Assert.That(first.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(second.Velocity, Is.EqualTo(Vector2.Zero));
        }
        finally
        {
            PhysicsServer2D.FreeRid(firstBody); PhysicsServer2D.FreeRid(secondBody); PhysicsServer2D.FreeRid(shape);
            world.Clear(); world.Free();
        }
    }

    [Test]
    public void LaterSameTickHitSeesShieldDepletedByEarlierSlot()
    {
        var world = CreateWorld();
        using var weapons = new WeaponSimulation(world);
        var body = PhysicsServer2D.BodyCreate();
        var shape = PhysicsServer2D.CircleShapeCreate();
        try
        {
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var shooter = world.Players[1].ControlledShip!;
            var target = world.Players[2].ControlledShip!;
            target.Runtime.SetFaction(2);
            target.Runtime.SetShieldCurrent(1.8);
            shooter.GlobalPosition = new(10000, 10000);
            PhysicsServer2D.ShapeSetData(shape, 50f);
            AddBlocker(body, shape, shooter, shooter.GlobalTransform * shooter.Runtime.WeaponFixturePoints[0] + Vector2.Right * 100);
            PhysicsServer2D.BodyAttachObjectInstanceId(body, target.GetInstanceId());
            var shots = new List<AcceptedShot>();
            Configure(weapons, shots.Add);
            var health = target.Runtime.Health.Current;
            shooter.SetInput(new(Vector2.Zero, 0, false, false, false, false, 3, shooter.Equipment.Revision));
            weapons.Step(0, _ => true);
            Assert.That(shots.Count, Is.EqualTo(2));
            Assert.That(shots[0].Profile, Is.Zero);
            Assert.That(shots[0].ShieldContact, Is.True);
            Assert.That(shots[1].ShieldContact, Is.False);
            Assert.That(target.Runtime.Shield.Current, Is.Zero);
            Assert.That(target.Runtime.Health.Current, Is.EqualTo(health - 9 * 100 / (100 + target.Runtime.Stats.Armor)).Within(1e-9));
        }
        finally
        {
            PhysicsServer2D.FreeRid(body); PhysicsServer2D.FreeRid(shape);
            world.Clear(); world.Free();
        }
    }

    private static void Configure(WeaponSimulation weapons, Action<AcceptedShot> accepted)
    {
        var session = Guid.NewGuid();
        weapons.Session = () => session;
        weapons.EntityIndex = _ => 0;
        weapons.Accepted = accepted;
    }
}
