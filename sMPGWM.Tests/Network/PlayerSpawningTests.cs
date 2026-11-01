using Godot;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

[Category("Godot")]
public class PlayerSpawningTests
{
    [Test]
    public void StartupMessageSurvivesVariantRoundTrip()
    {
        var original = PlayerStartupData.Create();
        using var payload = original.ToVariant();
        Assert.That(payload.VariantType, Is.EqualTo(Variant.Type.String));
        Assert.That(PlayerStartupData.TryFromVariant(payload, out var received), Is.True);
        Assert.That(received, Is.EqualTo(original));
    }

    [TestCase("hello")]
    [TestCase("here i am -1")]
    [TestCase("here i am 99999999999999999")]
    public void InvalidPayloadIsRejected(string message)
    {
        Assert.That(PlayerStartupData.TryFromVariant(message, out _), Is.False);
        Assert.That(PlayerStartupData.TryFromVariant(42, out _), Is.False);
    }

    [Test]
    public void DuplicateSpawnsAndRepeatedCleanupAreSafe()
    {
        var manager = new MpManager();
        try
        {
            manager.Spawn(1, new PlayerStartupData("here i am 1"));
            manager.Spawn(2, new PlayerStartupData("here i am 2"));
            var player = manager.MpControllers[2];
            
            Assert.That(player.ControllingPeerId, Is.EqualTo(2));
            manager.Spawn(2, new PlayerStartupData("here i am 3"));
            Assert.That(manager.MpControllers.Count, Is.EqualTo(2));
            Assert.That(manager.MpControllers[2], Is.SameAs(player));
            Assert.That(manager.StartupData[2].Message, Is.EqualTo("here i am 2"));
            manager.Despawn(2);
            manager.Despawn(2);
            Assert.That(manager.MpControllers.ContainsKey(2), Is.False);
            Assert.That(manager.StartupData.ContainsKey(2), Is.False);
            manager.Clear();
            manager.Clear();
            Assert.That(manager.GetChildCount(), Is.Zero);
            Assert.That(manager.MpControllers, Is.Empty);
        }
        finally { manager.Free(); }
    }
}
