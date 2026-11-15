# Weapons | Firing Visuals

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 4 of 7  
Dependencies: Stories 1, 2, and 3.

## Outcome

Holding a group trigger fires all eligible weapons assigned to that group in a local host session, using their own cadence. Show muzzle flashes, streaks/tracers, and the weapon's impact animation on every hit. An active shield independently adds its procedural hit flash. Damage is excluded.

## Scope

- Implement fixed-forward automatic hitscan, nearest valid collision, shooter/ally exclusions, range and spread, per-item sequence, and alternating authored muzzles (epic ?6.1 and ?7).
- Both triggers can be held together; each weapon fires at most once per physics tick. Grouped weapons start together when eligible but retain their individual rates rather than sharing one cadence.
- First shot is immediate; Vulcan fires every 4 ticks and laser every 20 at 60 Hz. Preserve fractional cadence, cooldown across equipment changes, and no idle catch-up bursts.
- Require existing power availability but temporarily omit ammunition/reload and per-shot energy admission. This intentional intermediate behavior is replaced by story 6.
- Use LMB for Fire Group 1, F for Fire Group 2; preserve RMB steering. Merge configurable bindings without overwriting user settings. UI/focus blocking releases both triggers.
- Prepare the two exact profiles and existing audio from epic section 4.3. Use bounded reusable pools and lifecycle cleanup. Every hit requests one profile-selected weapon impact, regardless of shield state; an active-shield hit additionally requests one generic shield arc. Misses produce neither impact nor arc. Tracers never govern collision timing.
- Integrate only into the authoritative simulation, never prediction replay. Keep multiplayer firing disabled until story 5 supplies its authority/replication path.

## Presentation contract

The weapon owns its firing presentation through `PresentationId`; the target's shield owns whether a shield response occurs. These are independent visual layers at the same contact, not alternative impact types. This decision replaces the earlier restriction against separate shield feedback.

For `laser_mk1`, `PresentationId: laser_standard` selects the existing `laser_standard` assets for muzzle, tracer, impact, and firing audio. In particular, **every laser hit** plays `res://scenes/vfx/impacts/metal_impact_laser_standard/metal_impact_laser_standard.tscn`, backed by `res://assets/vfx/impacts/metal_impact_laser_standard/metal_impact_laser_standard.png`. The `metal_impact` filename does not restrict it to hull or metal contacts. Vulcan continues to use `smg_standard`; it does not inherit the laser animation.

| Contact | Weapon impact | Shield response |
| --- | --- | --- |
| Miss | None | None |
| World blocker or wreck | One impact from the firing weapon's profile | None |
| Ship without available shield protection | One impact from the firing weapon's profile | None |
| Ship with active shield protection | One impact from the firing weapon's profile | One shared procedural arc |
| Shield break or overflow, once story 7 adds damage | One impact from the firing weapon's profile | One arc for that shield hit; no duplicate hull impact or additional break effect |

Muzzle flashes, tracers, and firing audio remain associated with accepted shots, including misses. Request the weapon impact once per collision, not once per shield/hull result flag. Cosmetic capacity limits may drop individual effects, but shield state must never suppress, replace, or select the weapon's animation.

### Ownership and hit-time state

- Weapon presentation consumes the prepared profile and generic hit/miss geometry. It does not read target shield resources or branch on shield availability. Do not add a shield-effect reference to weapon YAML or `WeaponPresentationProfile`.
- Target-side shield logic determines the shield-contact fact using the existing `ShipRuntime.ShieldProtectionAvailable` rule: generator/component present, positive charge, powered systems available, and downtime elapsed. Merely having a shield generator or stored charge is insufficient.
- The shield presentation consumer receives the confirmed shield contact's position, normal, and timing. It uses the same arc for every weapon and does not inspect weapon ID, archetype, damage source, or presentation profile. Keep rendering/pool ownership in presentation; the resource system need not instantiate Godot visual nodes.
- In story 4, capture shield availability at authoritative collision resolution and emit a cosmetic shield contact without changing shield/health resources or starting downtime. Story 5 carries this hit-time fact with the host-confirmed event; clients must not infer it from their current shield snapshot when the event is displayed.
- In story 7, capture the target's protection decision when each hit is resolved in stable damage order, before that hit mutates shield resources. Shield feedback is an active-shield contact response, not a test for a positive damage amount. Preserve the response on the hit that depletes protection, even though the target is no longer protected afterward. Later hits on the depleted target produce only their weapon impact.
- One presentation dispatcher may request both independent pools after accepting an event once. A shared event envelope does not require the weapon renderer to know shield state or the shield renderer to know weapon identity. Deduplication must cover both visual layers together.

### Shield arc and spatial behavior

Preserve the original geometry and initial timing from commit `299f5c5dbbaa8230308cff782760d22d6ce8da21`:

```csharp
public partial class ShieldFlash : Node2D
{
    public override void _Draw() => DrawArc(Vector2.Zero, 9, -1.2f, 1.2f, 12, new Color(.25f, .7f, 1f), 2, true);
}
```

Place it at the recorded contact point, rotate it by the impact normal's angle, and fade its alpha linearly from 1 to 0 over 150 ms. This is a small directional impact crescent, not a ship-sized shield outline. Preserve its world-space position during its lifetime, as in the reference; following the target or fitting its hull is outside this story. Render the arc above the weapon impact so both layers remain readable.

The shield effect may be packaged as `scenes/vfx/impacts/shield_impact_standard/shield_impact_standard.tscn` with a `ShieldFlash` root for authoring, but it remains a single shield-owned effect outside the weapon profiles. No new raster shield asset is required. Retain fixed drawing geometry and animate node modulation; no per-hit timer/tween or per-frame geometry changes are needed for this baseline.

Weapon muzzle flashes follow the displayed firing ship and its selected fixture point. Tracers travel along the recorded world path; weapon impacts remain at the recorded endpoint, aligned with the contact normal. Collision is already resolved when these effects start.

## Historical implementation and pooling

Use commit `299f5c5dbbaa8230308cff782760d22d6ce8da21` as an implementation reference, not a wholesale restore. Its `sMPGWM.Game/Scripts/VfxSystem/` files `ShieldFlash.cs`, `ShotPresentationController.cs`, and `WeaponPresentationProfile.cs` contain the geometry, presentation lifecycle, and resource mapping. `WeaponSystem/AcceptedShot.cs` and `ShotResultFlags.cs` show the old event shape; `sMPGWM.Tests/CombatPresentationTests.cs` covers bounded reuse, expiry, clearing, and event deduplication. Adapt these to current fixture points, item definitions, and story boundaries.

### Extracted reference values

| Resource | Capacity in the reference | Lifetime/behavior in the reference |
| --- | --- | --- |
| Muzzle nodes | 128 per profile | 50 ms; follow the displayed firing ship |
| Tracer nodes | 256 per profile | 80 ms; interpolate origin to endpoint |
| Weapon impact nodes | 128 per profile | SMG 300 ms; laser 425 ms |
| Shield arc nodes | 128 shared across all weapons | 150 ms; endpoint and normal, linear fade |
| Audio voices | 24 shared `AudioStreamPlayer2D` nodes | Reused voices; `MaxPolyphony = 1`, `MaxDistance = 1600`, `VolumeDb = -16` |
| Pending cosmetic events | 512 | Fixed array; drop/count overflow |
| Deduplication entries | 512 | Per-item sequence tracking with a 64-sequence reorder window |

The two profiles total 1,024 weapon visual nodes plus 128 shield nodes: **1,152 visuals**, with audio separate. Inactive visual nodes were hidden and had processing disabled. A central update advanced lifetimes, transforms, sprite frames, and alpha. Acquisition reset transform, global position/rotation, modulation, owner reference, and animation frame; expiry hid the node and released its owner reference. `Clear(cutoffTick)` discarded pending events/deduplication state, hid active effects, stopped audio, and rejected events at or before the cutoff.

The old `Play` method only acquired a weapon impact for `Hull | World | Wreck`; `Shield` separately acquired the arc. **Do not copy that predicate:** a generic confirmed collision must always request the weapon impact, including shield-only hits and hits that do not remove resources. The old shield flag depended on damage absorption; story 4 needs the read-only hit-time availability path above instead.

### Pool and animation implementation requirements

- Preload scenes, textures, animation metadata, and audio before session activation. Instantiate session-owned pools once. Reuse nodes and shared immutable resources; do not load resources, create/free effect nodes, or allocate timers, tweens, closures, or growing collections per shot.
- Keep separate free-index arrays per weapon profile/effect kind and a shared shield pool, with an array-backed active list. Improve on the old full-array scan for every acquisition and update. Shield-pool exhaustion cannot prevent an available weapon impact, or vice versa; attempt each independently.
- Treat the extracted counts as historical starting budgets, not proven capacity for the new behavior. Shield-only contacts now also consume weapon-impact slots. Size each pool from concurrent rate times lifetime plus synchronized-burst allowance, cap it explicitly, and record the selected capacities and high-water/drop counts. Cover five/ten players and up to nine weapons each; the epic's synthetic 60 shots/second ceiling also needs bounded behavior, not a promise of zero visual drops.
- On exhaustion, drop and count only the unavailable cosmetic; never block or roll back an accepted shot. Keep queues bounded and expose per-pool active/high-water/drop metrics so shield and weapon pressure can be distinguished.
- Reset lifetime/start tick, transform/scale, rotation, modulation, visibility, owner/offset/path data, and sprite playback/frame state on acquisition. Release references on expiry; clear queues, deduplication, active nodes, and audio on session reset/disposal. A new session must not inherit effects or retained ship references.
- Drive both layers from the same presentation time. Sample animations by elapsed age, including authored frame durations, so an effect displayed late starts at its correct age and already-expired effects are skipped. Avoid independent autonomous animation clocks competing with the central update.
- Derive sprite completion times from the authored clips rather than blindly copying legacy constants. Current impacts have 9 SMG frames at 40 FPS (225 ms) and 17 laser frames at 40 FPS (425 ms); muzzle clips have 2 SMG frames (50 ms) and 4 laser frames (100 ms). The old 300 ms SMG impact held its last frame, and the old 50 ms laser muzzle truncated its clip. Use the complete authored clips; retain the 80 ms tracer and 150 ms shield defaults. Preserve sprite artwork opacity unless an explicit authored fade calls for otherwise; the shield keeps its linear fade.
- Story 5 owns network deduplication, lateness windows, and recovery cutoffs. Retain the bounded design from the reference without introducing remote firing or wire changes into story 4. Event loss remains cosmetic; one accepted event must not independently replay either layer.

## Acceptance

- Demonstrate each group separately and both together, mixed weapons, None membership, replacement, trigger release/repress, and UI blocking.
- Verify transformed muzzle origins, accepted-shot alternation, forward aim, spread/range, nearest blocker, and the contact matrix above in Godot for both profiles. Laser uses the existing `laser_standard` impact for every contact; SMG uses `smg_standard`.
- An active-shield hit shows exactly one weapon impact plus one arc at the same endpoint/normal. Missing, empty, unpowered, or downtime-disabled shields add no arc. Weapon animation choice is unchanged in every case; a miss has neither effect.
- Verify complete authored sprite playback and the original 150 ms arc geometry/fade in motion, including readability when both effects overlap. Test a moving target to confirm world-space impact placement and a moving shooter to confirm muzzle attachment.
- Validate cadence and bounded pool reuse/exhaustion, including independent shield/weapon exhaustion and multiple simultaneous hits. Record chosen budgets and per-pool high-water/drop counts; no per-shot timers or unbounded queues. Reuse resets animation/pose/alpha, and local session disposal clears effects, audio, references, and item state correctly.
- Health/shields remain unchanged, magazines do not deplete, and firing incurs no discrete energy charge.

Follow-up verification belongs to the relevant later story: story 5 checks delayed/reordered/duplicate events and shield-state changes before display; story 7 checks exact break and overflow (one weapon impact plus one arc), then a subsequent unprotected hit (weapon impact only).

## Excluded

Remote firing and shot replication, supply mechanics, damage/death, additional shield-break animations, ship-sized or target-following shield geometry, projectile simulation, prediction, and lag compensation.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.

Planning update: extracted the reference implementation and checked the existing VFX scene frame counts/rates. This records the revised visual contract and implementation requirements only; no runtime implementation or new visual/performance verification has been completed by this update.
