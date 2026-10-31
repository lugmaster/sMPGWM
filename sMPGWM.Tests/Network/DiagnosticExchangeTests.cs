using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

public class DiagnosticExchangeTests
{
    [Test]
    public void MatchingReplySucceedsOnce()
    {
        var exchange = new DiagnosticExchange();
        var session = Guid.NewGuid().ToString("N");
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, exchange.Payload, session), Is.True);
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, exchange.Payload, session), Is.False);
    }

    [Test]
    public void InvalidRepliesDoNotConsumeOutstandingRequest()
    {
        var exchange = new DiagnosticExchange();
        var session = Guid.NewGuid().ToString("N");
        Assert.That(exchange.Accept(2, 1, exchange.Correlation, exchange.Payload, session), Is.False);
        Assert.That(exchange.Accept(1, 2, exchange.Correlation, exchange.Payload, session), Is.False);
        Assert.That(exchange.Accept(1, 1, Guid.NewGuid().ToString("N"), exchange.Payload, session), Is.False);
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, "wrong", session), Is.False);
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, exchange.Payload, "invalid"), Is.False);
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, exchange.Payload, session), Is.True);
    }

    [Test]
    public void CancellationRejectsLateReply()
    {
        var exchange = new DiagnosticExchange();
        exchange.Cancel();
        Assert.That(exchange.Accept(1, 1, exchange.Correlation, exchange.Payload, Guid.NewGuid().ToString("N")), Is.False);
        Assert.That(new DiagnosticExchange().Correlation, Is.Not.EqualTo(exchange.Correlation));
    }

    [Test]
    public void PayloadBoundCountsUtf8Bytes()
    {
        var id = Guid.NewGuid().ToString("N");
        Assert.That(DiagnosticExchange.IsValid(1, id, new string('a', 256)), Is.True);
        Assert.That(DiagnosticExchange.IsValid(1, id, new string('a', 257)), Is.False);
        Assert.That(DiagnosticExchange.IsValid(1, id, new string('�', 86)), Is.False);
    }
}
