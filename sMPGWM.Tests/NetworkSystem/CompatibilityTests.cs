using System.Reflection;
using System.Text.Json;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

public class CompatibilityTests
{
    [Test]
    [NonParallelizable]
    public void CaptureRequiresExplicitDefinitionPreparation()
    {
        var preparedData = typeof(GameplayDefinitions).GetField("Data", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = preparedData.GetValue(null);
        try
        {
            preparedData.SetValue(null, null);
            Assert.Throws<InvalidOperationException>(() => Compatibility.Capture());
            Assert.That(GameConfig.GameDataChecksum, Is.Empty);
            Assert.That(preparedData.GetValue(null), Is.Null);
        }
        finally
        {
            preparedData.SetValue(null, previous);
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
    [TestCase("protocol")]
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
        Assert.That(Compatibility.Accept("{}"u8.ToArray()), Is.False);
        Assert.That(Compatibility.Accept("[]"u8.ToArray()), Is.False);
        Assert.That(Compatibility.Accept("[1,2,3,4,5,6,7,8,9]"u8.ToArray()), Is.False);
        Assert.That(Compatibility.Accept("[null,null,null,null,null,null,null,null,null]"u8.ToArray()), Is.False);
    }

    [Test, Category("Godot")]
    public void AuthenticationUsesPositionalScalarsAndRejectsOldProtocol()
    {
        GameConfig.EnsureCompatibilityData();
        var payload = Compatibility.Payload();
        using var document = JsonDocument.Parse(payload);
        Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(document.RootElement.GetArrayLength(), Is.EqualTo(9));
        foreach (var value in document.RootElement.EnumerateArray())
            Assert.That(value.ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(Compatibility.Accept(payload), Is.True);
        var fields = JsonSerializer.Deserialize<string[]>(payload)!;
        fields[1] = "14";
        Assert.That(Compatibility.Accept(JsonSerializer.SerializeToUtf8Bytes(fields)), Is.False);
        Assert.That(Compatibility.Accept(JsonSerializer.SerializeToUtf8Bytes(Compatibility.Capture())), Is.False);
        Assert.That(Compatibility.Accept(JsonSerializer.SerializeToUtf8Bytes(fields[..8])), Is.False);
    }
}
