# Weapons | Syncing Firing Action over Host

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 5 of 7  
Dependencies: Story 4; uses group authority from story 3.

## Outcome

Participating players request firing through the host. All peers see host-confirmed muzzle firing, streaks, and impacts, with no damage yet.

## Scope

This story connects existing weapon definitions, group authority, hitscan resolution, and pooled presentation from stories 1–4 to multiplayer input and host-confirmed events. Missing prerequisite work belongs to its owning story; this story does not authorize implementing the whole epic.

### Existing input bindings and context arbitration

- Preserve commit `1fb2ac7647528cf2e7ef13343ed3b653e3d9703f`. Use the existing `PrimaryFire` and `SecondaryFire` actions through `InputController.PrimaryFirePressed` and `SecondaryFirePressed`, mapped to primary/secondary group bits respectively. Honor both configurable binding slots; do not introduce replacement actions or use `Action` as fire input.
- Keep context decisions in `PlayerInputPolicy`. `Flight` availability alone is insufficient: ability targeting intentionally leaves flight movement available while capturing pointer actions. Preserve the existing `PlayerController` filter through `CanUseFlightPointerActions` for targeting, waiting for release, and interactive UI, including keyboard-bound firing.
- Shared physical bindings across contexts are intentional. Target confirmation/cancellation, UI interaction, and binding capture must never also produce a firing request. Preserve movement during ability targeting. Evaluate effective fire eligibility when sampling input; `CanControlPlayerChanged` alone cannot detect targeting transitions.
- On focus loss, blocking UI/acknowledgement, targeting capture, or loss of control/readiness, clear effective held-fire state. Require release before firing resumes, so a consumed click or held key cannot leak into flight after the context changes. Verify release handling for remapped keyboard and mouse bindings, not just default mouse buttons.

### Host admission and replication

- Extend existing 30 Hz input with two held-fire bits and configuration revision. Validate ownership/session/control generation; retain redundancy, expiry, and release-on-focus/UI behavior.
- Host alone admits shots at 60 Hz and resolves hits. Clear held input on disconnect/unbind/recovery and require fresh released input after baseline/control transfer.
- Implement bounded separate unreliable shot batches and deduplication keyed by session, item identity, and shot sequence. Use authoritative origins/endpoints and the epic §9 event contract, timeline, late-event, and recovery-cutoff rules.
- Carry the authoritative shield-contact boolean alongside the hit/miss result for the layered presentation in [story 4](inc-04_weapons-firing-visuals.md). The target/shield supplies the read-only collision-time protection-availability sample; story 7 moves that decision to each hit's turn in stable damage resolution, before shield mutation. Clients never recheck current shield state. Weapon presentation uses only the hit result and profile, while generic shield feedback consumes the shield-contact result without a weapon profile. Include the added scalar in codec bounds and compatibility validation.
- Synchronize configuration and current cadence/sequence state needed for join, recovery, and restoration. Do not invent ammunition state before story 6.
- Gate readiness on dependencies; do not replay old effects on joins or movement reconciliation. Use positional arrays/scalars and numeric references; bump protocol and verify codec ceilings.
- Validate both ENet and GodotSteam MultiplayerPeer and record actual coverage. Pool exhaustion or event loss affects cosmetics only.

- Admit against the current authoritative item/binding and configuration revision after movement. A weapon assigned to both held groups fires only once per eligible physics tick. Preserve per-item cadence, fractional phase, alternating muzzle sequence, and sequence continuity across transfers/restoration. No catch-up bursts or client-selected hit geometry.
- Encode the two-bit mask and configuration revision in the existing redundant input stream; duplicate/reordered commands must not create extra admissions. Stale revision input must not fire a replacement slot occupant. Host-local input follows the same admission rules.
- Include session/tick, numeric item and firing-entity references, control generation, shot sequence, compatible profile index, muzzle index, authoritative origin/endpoint/normal, hit/miss, and independent shield-contact result in the positional shot schema. Validate the whole batch before dispatching effects.
- Derive events per packet from the final codec and the proposed 1,200-byte application-payload ceiling. Recompute input/snapshot ceilings rather than assuming existing limits still fit. Bound staging, deduplication, and missing-topology queues; expire entries so long sessions do not permanently stop accepting new weapon identities.
- Install dependent identities/configuration before weapon readiness. A validated baseline's completed tick is the global cosmetic cutoff, including remote shooters. Clear queued effects on recovery/session changes; retain normal out-of-order deduplication after the cutoff. Present remote events on the remote presentation timeline and age or discard late effects.

## Acceptance

- Host and clients can fire either/both groups; every peer sees corresponding firing, streaks, and hit impacts under normal delivery.
- Every contact plays one profile-selected weapon impact; active-shield contacts additionally play one generic shield arc. Verify this under delayed/reordered events and shield-state updates, with no duplicate layer on duplicate shot events and neither impact nor arc on misses.
- Wrong-peer, stale binding/session, malformed, oversized, duplicate, and reordered messages are handled safely. Missing input expires without stuck firing.
- Join/recovery/restoration gives current state without historical visual playback; packet loss permits missed cosmetics, never duplicate authoritative shots.
- Measure five/ten players and up to nine weapons each, including synchronized bursts and bounded queues. No health damage or supply consumption occurs.
- Verify primary only, secondary only, both groups, a weapon in both groups, and two identical weapon items with the same sequence. Each accepted shot is admitted once and each delivered event presents at most once.
- Verify overlapping default bindings and remapped keyboard bindings through targeting confirmation/cancellation, release-wait, interactive UI, blocking menus, required acknowledgements, binding capture, and focus loss. Suppression clears fire, movement remains available during targeting, and returning to flight requires release before a new press.
- Verify stale configuration after replacement/unequip, control transfer, expiry, and baseline recovery while fire is held. Old input cannot fire a newly controlled ship or slot occupant.
- Exercise exact maximum codec sizes, invalid masks/references/non-finite geometry, duplicate and out-of-order events, missing topology, and deduplication capacity over many item identities. Test independent impact/shield pools under exhaustion.
- At ten players with nine 60/s weapons, account for 180 events between publications; at 15/s, account for synchronized 90-shot bursts. Record bytes, queue high-water marks, cosmetic drops, recurring allocations, and tick timing, with transport coverage and limitations stated explicitly.

## Excluded

Energy-per-shot, reload/ammo mechanics and UI, damage/death replication, client prediction, and historical rewind.

Also excluded: recreating bindings/settings from the historical implementation, replacing story 3 group configuration/UI, rebuilding story 4 effects, and broad gameplay/resource replication optimization. Extend only the baseline and compatibility paths needed for this story. Existing continuous equipment resource behavior remains governed by its current systems.

## Historical implementation review

Commit `299f5c5dbbaa8230308cff782760d22d6ce8da21` is an off-branch reference only and will never be merged into this branch. Inspect individual paths with `git show`; do not merge/cherry-pick the commit or treat its whole-epic implementation as this story's scope.

Source review identifies useful patterns, not a validated implementation:

| Historical code | Assessment for this story |
| --- | --- |
| `NetworkManager.Weapons.cs`: fixed 256-event staging and split unreliable batches | Useful bounded staging pattern for the stated player/weapon ceiling. Adapt to current admission and presentation interfaces. Overflow must remain cosmetic. |
| `ReceiveShots`: 64-bit sliding sequence window | Useful out-of-order deduplication algorithm. Adapt session lifecycle and expiry: its 1,024-item dictionary limit simply rejects new identities once full, so it is not sufficient as a long-session retention policy. |
| `WeaponBaselineCutoff` | Useful global tick cutoff and queue-clear pattern; verify it against current join/recovery ordering and remote events. |
| `ShipWeaponController.SetInput` / `ClearInput` | Useful revision check and released-input arming concept. Adapt to current context policy and lifecycle resets; do not copy the controller wholesale. |
| Historical group/controller and weapon-state paths | Not suitable wholesale: they include reload/supply behavior, and group assignment accepts only values 0–2 rather than independent membership flags allowing both groups. Reuse current story 3 authority instead. |
| `ShotEventCodec` | Fixed-size validation is useful as a technique. Redesign against the current event contract: the old codec hard-codes two profile mappings and serializes firing entity GUIDs instead of the required numeric references. Recalculate bounds including independent shield contact. |

Historical runtime tests and measurements are not evidence that this adaptation passes. Reuse an individual algorithm only after checking its current contracts and adding the focused verification above.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
