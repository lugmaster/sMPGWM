using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: SHOT-HISTORY-001 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
using sMPGWM.Scripts.WeaponSystem; // TEST-SUPPORT: SHOT-HISTORY-002 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict

namespace sMPGWM.Tests; // TEST-SUPPORT: SHOT-HISTORY-003 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict

public sealed class ShotHistoryTests // TEST-SUPPORT: SHOT-HISTORY-004 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
{ // TEST-SUPPORT: SHOT-HISTORY-005 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
    [Test] // TEST-SUPPORT: SHOT-HISTORY-006 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
    public void AcceptsReorderedShotsOnceAcrossWindowBoundaries() // TEST-SUPPORT: SHOT-HISTORY-007 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
    { // TEST-SUPPORT: SHOT-HISTORY-008 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        var window = new ShotSequenceWindow(); // TEST-SUPPORT: SHOT-HISTORY-009 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(0), Is.False); // TEST-SUPPORT: SHOT-HISTORY-010 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(100), Is.True); // TEST-SUPPORT: SHOT-HISTORY-011 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(100), Is.False); // TEST-SUPPORT: SHOT-HISTORY-012 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(37), Is.True); // TEST-SUPPORT: SHOT-HISTORY-013 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(37), Is.False); // TEST-SUPPORT: SHOT-HISTORY-014 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(36), Is.False); // TEST-SUPPORT: SHOT-HISTORY-015 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(101), Is.True); // TEST-SUPPORT: SHOT-HISTORY-016 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(100), Is.False); // TEST-SUPPORT: SHOT-HISTORY-017 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(99), Is.True); // TEST-SUPPORT: SHOT-HISTORY-018 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(165), Is.True); // TEST-SUPPORT: SHOT-HISTORY-019 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(101), Is.False); // TEST-SUPPORT: SHOT-HISTORY-020 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(164), Is.True); // TEST-SUPPORT: SHOT-HISTORY-021 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(ulong.MaxValue), Is.True); // TEST-SUPPORT: SHOT-HISTORY-022 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(ulong.MaxValue), Is.False); // TEST-SUPPORT: SHOT-HISTORY-023 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(ulong.MaxValue - 63), Is.True); // TEST-SUPPORT: SHOT-HISTORY-024 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
        Assert.That(window.TryAccept(ulong.MaxValue - 64), Is.False); // TEST-SUPPORT: SHOT-HISTORY-025 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries
    } // TEST-SUPPORT: SHOT-HISTORY-026 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries

    [Test] // TEST-SUPPORT: SHOT-HISTORY-027 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
    public void AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows() // TEST-SUPPORT: SHOT-HISTORY-028 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
    { // TEST-SUPPORT: SHOT-HISTORY-029 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        var history = new ShotHistory(1024); // TEST-SUPPORT: SHOT-HISTORY-030 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        var active = ItemEntityId.FromNetwork(1); // TEST-SUPPORT: SHOT-HISTORY-031 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        for (var id = 1; id <= 2048; id++) // TEST-SUPPORT: SHOT-HISTORY-032 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        { // TEST-SUPPORT: SHOT-HISTORY-033 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
            Assert.That(history.TryAccept(ItemEntityId.FromNetwork(id), 100, id), Is.True); // TEST-SUPPORT: SHOT-HISTORY-034 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
            Assert.That(history.TryAccept(active, (ulong)(100 + id), id + 1), Is.True); // TEST-SUPPORT: SHOT-HISTORY-035 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        } // TEST-SUPPORT: SHOT-HISTORY-036 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        Assert.That(history.TryAccept(active, 2148, 2049), Is.False); // TEST-SUPPORT: SHOT-HISTORY-037 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        var newest = ItemEntityId.FromNetwork(2048); // TEST-SUPPORT: SHOT-HISTORY-038 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        Assert.That(history.TryAccept(newest, 100, 2048), Is.False); // TEST-SUPPORT: SHOT-HISTORY-039 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        Assert.That(history.TryAccept(newest, 99, 2047), Is.True); // TEST-SUPPORT: SHOT-HISTORY-040 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        Assert.That(history.TryAccept(newest, 99, 2047), Is.False); // TEST-SUPPORT: SHOT-HISTORY-041 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
        Assert.That(history.TryAccept(newest, 36, 2046), Is.False); // TEST-SUPPORT: SHOT-HISTORY-042 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows
    } // TEST-SUPPORT: SHOT-HISTORY-043 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows

    [Test] // TEST-SUPPORT: SHOT-HISTORY-044 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
    public void ClearResetsHistoryAndInvalidShotsDoNotEvict() // TEST-SUPPORT: SHOT-HISTORY-045 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
    { // TEST-SUPPORT: SHOT-HISTORY-046 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        var history = new ShotHistory(2); // TEST-SUPPORT: SHOT-HISTORY-047 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        var first = ItemEntityId.FromNetwork(1); // TEST-SUPPORT: SHOT-HISTORY-048 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        var second = ItemEntityId.FromNetwork(2); // TEST-SUPPORT: SHOT-HISTORY-049 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        var third = ItemEntityId.FromNetwork(3); // TEST-SUPPORT: SHOT-HISTORY-050 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(first, 1, 100), Is.True); // TEST-SUPPORT: SHOT-HISTORY-051 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(second, 2, 200), Is.True); // TEST-SUPPORT: SHOT-HISTORY-052 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(second, 1, 50), Is.True); // TEST-SUPPORT: SHOT-HISTORY-053 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(default, 1, 300), Is.False); // TEST-SUPPORT: SHOT-HISTORY-054 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(third, 0, 300), Is.False); // TEST-SUPPORT: SHOT-HISTORY-055 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(first, 1, 100), Is.False); // TEST-SUPPORT: SHOT-HISTORY-056 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(third, 1, 300), Is.True); // TEST-SUPPORT: SHOT-HISTORY-057 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(second, 2, 200), Is.False); // TEST-SUPPORT: SHOT-HISTORY-058 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        history.Clear(); // TEST-SUPPORT: SHOT-HISTORY-059 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
        Assert.That(history.TryAccept(second, 2, 200), Is.True); // TEST-SUPPORT: SHOT-HISTORY-060 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
    } // TEST-SUPPORT: SHOT-HISTORY-061 | Tests: sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
} // TEST-SUPPORT: SHOT-HISTORY-062 | Tests: sMPGWM.Tests.ShotHistoryTests.AcceptsReorderedShotsOnceAcrossWindowBoundaries, sMPGWM.Tests.ShotHistoryTests.AcceptsNewWeaponsAfterCapacityAndRetainsRecentWindows, sMPGWM.Tests.ShotHistoryTests.ClearResetsHistoryAndInvalidShotsDoNotEvict
