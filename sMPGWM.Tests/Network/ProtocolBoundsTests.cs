using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

public class ProtocolBoundsTests
{
    [Test]
    public void EnvelopeRejectsStaleIdentityAndOversizedPayload()
    {
        var expected = new AdmissionEnvelope(DiagnosticExchange.ProtocolVersion,
            Guid.NewGuid().ToString("N"), 1, 1, Guid.NewGuid().ToString("N"));
        Assert.That(expected.Matches(expected, 4096), Is.True);
        Assert.That(expected.Matches(expected, 4097), Is.False);
        Assert.That(expected.Matches(expected, -1), Is.False);
        Assert.That((expected with { Token = Guid.NewGuid().ToString("N") }).Matches(expected, 0), Is.False);
        Assert.That((expected with { Session = Guid.NewGuid().ToString("N") }).Matches(expected, 0), Is.False);
        Assert.That((expected with { WorldEpoch = 2 }).Matches(expected, 0), Is.False);
        Assert.That((expected with { ContentRevision = 2 }).Matches(expected, 0), Is.False);
        Assert.That((expected with { Protocol = -1 }).Matches(expected, 0), Is.False);
    }

    [Test]
    public void FloodCannotExtendRateWindow()
    {
        var limit = new ProtocolRateLimit();
        for (var i = 0; i < 16; i++) Assert.That(limit.Accept(0), Is.True);
        Assert.That(limit.Accept(0), Is.False);
        Assert.That(limit.Accept(999), Is.False);
        Assert.That(limit.Accept(1000), Is.True);
    }
}
