using sMPGWM.Scripts.Steam;

namespace sMPGWM.Tests.NetworkSystem;

public class LobbyDiscoveryTests
{
    [Test]
    public void DiscoveryDoesNotRequireHostIdentityOrJoinCompatibility()
    {
        var discovery = new LobbyDiscovery("game", "CoOP", "0.2");
        var metadata = new Dictionary<string, string>
        {
            ["game_id"] = "game", ["mode"] = "CoOP", ["version"] = "0.2"
        };

        // Indexing throws if discovery tries to read a name, owner, session or checksum.
        Assert.That(discovery.Matches(key => metadata[key]), Is.True);
    }

    [TestCase("game_id")]
    [TestCase("mode")]
    [TestCase("version")]
    public void DiscoveryRejectsDifferentSearchCriteria(string field)
    {
        var discovery = new LobbyDiscovery("game", "CoOP", "0.2");
        var metadata = new Dictionary<string, string>(discovery.Filters) { [field] = "different" };
        Assert.That(discovery.Matches(key => metadata[key]), Is.False);
    }

    [Test]
    public void CanceledSearchCannotPopulateANewerBrowser()
    {
        var search = new PendingLobbySearch();
        var replies = new List<long>();
        Action<long[]> oldBrowser = ids => replies.AddRange(ids);
        Action<long[]> newBrowser = ids => replies.AddRange(ids);
        Assert.That(search.Submit(oldBrowser, () => { }), Is.True);
        search.Cancel(oldBrowser);
        Assert.That(search.Submit(newBrowser, () => Assert.Fail("Must drain old search first")), Is.False);
        search.Complete([10]);
        Assert.That(replies, Is.Empty);
        Assert.That(search.Submit(newBrowser, () => { }), Is.True);
        search.Complete([20]);
        search.Complete([30]);
        Assert.That(replies, Is.EqualTo(new long[] { 20 }));
    }

    [Test]
    public void SearchSubmissionFailureAllowsRetry()
    {
        var search = new PendingLobbySearch();
        Assert.Throws<InvalidOperationException>(() => search.Submit(_ => { },
            () => throw new InvalidOperationException()));
        Assert.That(search.Pending, Is.False);
        Assert.That(search.Submit(_ => { }, () => { }), Is.True);
    }
}
