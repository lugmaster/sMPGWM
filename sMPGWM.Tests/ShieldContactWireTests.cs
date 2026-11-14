using Godot; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-001 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-002 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-003 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
using sMPGWM.Scripts.WeaponSystem; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-004 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations

namespace sMPGWM.Tests; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-005 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations

public sealed class ShieldContactWireTests // TEST-SUPPORT: SHIELD-CONTACT-WIRE-006 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
{
    [Test] // TEST-SUPPORT: SHIELD-CONTACT-WIRE-007 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
    public void ContactIdentityAndMalformedCombinations() // TEST-SUPPORT: SHIELD-CONTACT-WIRE-008 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
    {
        var session = Guid.NewGuid(); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-009 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        var target = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-010 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        var shot = new AcceptedShot(12, ItemEntityId.FromNetwork(1), 1, Guid.NewGuid().ToString("N"), 0, "smg_standard", 0, Vector2.Zero, Vector2.Right, Vector2.Left, ShotResultFlags.ShieldContact | ShotResultFlags.Hull, target); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-011 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        foreach (var flags in new[] { ShotResultFlags.ShieldContact, ShotResultFlags.ShieldContact | ShotResultFlags.Shield, ShotResultFlags.ShieldContact | ShotResultFlags.Hull, ShotResultFlags.ShieldContact | ShotResultFlags.Wreck, ShotResultFlags.Hull, ShotResultFlags.Wreck }) // TEST-SUPPORT: SHIELD-CONTACT-WIRE-012 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        {
            var expected = shot with { Flags = flags }; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-013 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            Assert.That(ShotEventCodec.Decode(ShotEventCodec.Encode(session, new[] { expected }), out _, out var decoded), Is.True); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-014 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            Assert.That(decoded[0], Is.EqualTo(expected)); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-015 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        }
        foreach (var flags in new[] { ShotResultFlags.Miss, ShotResultFlags.World }) // TEST-SUPPORT: SHIELD-CONTACT-WIRE-016 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        {
            var expected = shot with { Flags = flags, TargetEntityId = "" }; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-017 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            Assert.That(ShotEventCodec.Decode(ShotEventCodec.Encode(session, new[] { expected }), out _, out var decoded), Is.True); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-018 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            Assert.That(decoded[0].TargetEntityId, Is.Empty); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-019 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        }
        var bytes = ShotEventCodec.Encode(session, new[] { shot }); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-020 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        foreach (var flags in new byte[] { 32, 255, (byte)ShotResultFlags.World, (byte)ShotResultFlags.Shield, (byte)(ShotResultFlags.Wreck | ShotResultFlags.Hull) }) // TEST-SUPPORT: SHIELD-CONTACT-WIRE-021 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        {
            var invalid = (byte[])bytes.Clone(); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-022 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            invalid[ShotEventCodec.HeaderBytes + 74] = flags; // TEST-SUPPORT: SHIELD-CONTACT-WIRE-023 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
            Assert.That(ShotEventCodec.Decode(invalid, out _, out _), Is.False); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-024 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        }
        Array.Clear(bytes, ShotEventCodec.HeaderBytes + 75, 16); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-025 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        Assert.That(ShotEventCodec.Decode(bytes, out _, out _), Is.False); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-026 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
        Assert.That(ShotEventCodec.MaxEvents, Is.EqualTo(13)); // TEST-SUPPORT: SHIELD-CONTACT-WIRE-027 | Tests: sMPGWM.Tests.ShieldContactWireTests.ContactIdentityAndMalformedCombinations
    }
}
