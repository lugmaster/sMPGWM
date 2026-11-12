using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

public class CompatibilityTests
{
    [Test] // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-001 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
    [NonParallelizable] // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-002 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
    public void CaptureRequiresExplicitDefinitionPreparation() // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-003 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
    {
        var checksum = typeof(sMPGWM.Scripts.GameConfigSystem.GameConfig).GetProperty("GameDataChecksum")!; // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-004 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
        var previous = checksum.GetValue(null); // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-005 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
        try
        {
            checksum.SetValue(null, string.Empty); // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-006 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
            Assert.Throws<InvalidOperationException>(() => Compatibility.Capture()); // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-007 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
            Assert.That(checksum.GetValue(null), Is.EqualTo(string.Empty)); // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-008 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
        }
        finally
        {
            checksum.SetValue(null, previous); // TEST-SUPPORT: JOIN-COMPATIBILITY-PREP-009 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.CaptureRequiresExplicitDefinitionPreparation
        }
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void BuildMismatchFollowsDevelopmentPolicy(bool development, bool expected)
    {
        var local = new Dictionary<string, string> { ["development"] = development ? "true" : "false", ["build"] = Guid.NewGuid().ToString("N") };
        var remote = new Dictionary<string, string>(local) { ["build"] = Guid.NewGuid().ToString("N") };
        Assert.That(Compatibility.Matches(local, key => remote[key]), Is.EqualTo(expected));
    }

    [TestCase("development")]
    [TestCase("config")]
    [TestCase("data")]
    [TestCase("version")]
    [TestCase("mode")]
    [TestCase("game_id")]
    [TestCase("protocol")] // TEST-SUPPORT: JOIN-COMPATIBILITY-001 | Tests: sMPGWM.Tests.NetworkSystem.CompatibilityTests.RequiredFieldMismatchRejects
    public void RequiredFieldMismatchRejects(string field)
    {
        var local = new Dictionary<string, string> { ["development"] = "true", [field] = "expected" };
        var remote = new Dictionary<string, string>(local) { [field] = "different" };
        Assert.That(Compatibility.Matches(local, key => remote[key]), Is.False);
    }

    [Test]
    public void HashIsOrderedAndNormalizesLineEndings()
    {
        var a = new Dictionary<string, string> { ["a"] = "hello\r\n", ["b"] = "world" };
        var b = new Dictionary<string, string> { ["b"] = "world", ["a"] = "hello\n" };
        Assert.That(ChecksumGenerator.Compute(a), Is.EqualTo(ChecksumGenerator.Compute(b)));
        b["a"] = "changed";
        Assert.That(ChecksumGenerator.Compute(a), Is.Not.EqualTo(ChecksumGenerator.Compute(b)));
    }

    [Test]
    public void MalformedAuthenticationIsRejected()
    {
        Assert.That(Compatibility.Accept(new byte[8193]), Is.False);
        Assert.That(Compatibility.Accept("invalid"u8.ToArray()), Is.False);
    }
}
