using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class WeaponDeathMovementTests
{
    [Test]
    public void DeathStopsVelocityAndDiscardsHistoricalMovementPresentation()
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            ship.Velocity = new Vector2(100, 30);
            ship.SetInput(new(Vector2.Right, 1, true, true, false, false, 3, ship.Equipment.Revision));
            var alive = MovementStateAdapter.Capture(1, owner, 0);
            var presentation = new RemoteMovementPresentation();
            presentation.Apply(1, ship, 0, alive, true);
            var position = ship.GlobalPosition;
            ship.Runtime.ApplyDamage(1000000);
            Assert.That(ship.Runtime.IsAlive, Is.False);
            Assert.That(ship.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(ship.InputState.FireMask, Is.Zero);
            Assert.That(ship.InputState.Direction, Is.EqualTo(Vector2.Zero));
            Assert.That(ship.MovementConfiguration.Enabled, Is.False);
            presentation.Apply(1, ship, 1, alive with { Position = position + Vector2.One * 100 }, false);
            presentation.Render(1, 10, world.Players);
            ship.Simulate(1d / 60);
            Assert.That(ship.GlobalPosition, Is.EqualTo(position));
            Assert.That(ship.Presentation.Position, Is.EqualTo(position));
            Assert.That(ship.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(ship.PresentedMovementFlags, Is.Zero);
        }
        finally { world.Clear(); world.Free(); }
    }
}
