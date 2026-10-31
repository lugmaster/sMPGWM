using sMPGWM.Scripts.Network;
namespace sMPGWM.Tests.Network;
public class PeerHealthMonitorTests
{
    [Test]
    public void SamplesAreCorrelatedAndSmoothed()
    {
        var health = new PeerHealthMonitor(0);
        var first = health.Probe(0);
        Assert.That(health.Reply(first, "wrong", 100), Is.False);
        Assert.That(health.Reply(first, health.Token, 100), Is.True);
        Assert.That(health.Reply(first, health.Token, 101), Is.False);
        var second = health.Probe(2000);
        Assert.That(health.Reply(second, health.Token, 2180), Is.True);
        Assert.That(health.SmoothedRtt, Is.EqualTo(110));
        Assert.That(health.IsAvailable(12180), Is.False);
        Assert.That(health.Reply(second, health.Token, 12180), Is.False);
    }
    [Test]
    public void OlderReplyDoesNotOverwriteLatency()
    {
        var health = new PeerHealthMonitor(0);
        var first = health.Probe(0);
        var second = health.Probe(2000);
        Assert.That(health.Reply(second, health.Token, 2100), Is.True);
        Assert.That(health.Reply(first, health.Token, 2200), Is.True);
        Assert.That(health.LatestRtt, Is.EqualTo(100));
        Assert.That(health.Probe(2201), Is.Zero);
        Assert.That(new PeerHealthMonitor(0).IsExpired(10000), Is.True);
    }
}
