# Weapons | Damage

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 7 of 7  
Dependencies: Story 6.

## Outcome

Apply authoritative damage at hitscan impact and synchronize health, shield, and death state. Presentation remains cosmetic and does not delay damage.

## Scope

- Use epic ?8: available shields absorb first; overflow reaches health with multiplier `100 / (100 + max(0, Armor))`. Preserve fractional damage, shield downtime, and existing availability rules. Physical/laser share effectiveness in this epic.
- Admit all eligible shots before applying damage in stable order, allowing same-tick trades. Already accepted hits never retarget through a newly dead victim.
- Finalize the shield-contact result from the target's `ShieldProtectionAvailable` at each hit's turn in that stable damage order, captured before that hit mutates shield state. This replaces story 4's collision-time availability sample; it does not require a positive absorbed-damage amount. Preserve [story 4's presentation contract](inc-04_weapons-firing-visuals.md): one profile-selected weapon impact on every contact, plus one generic shield arc when protection was active for that hit. The target/shield owns this outcome; weapon presentation does not inspect shield state, and shield feedback does not inspect the weapon profile.
- Apply one authoritative death transition: clear actions, cancel reloads, stop regeneration/abilities/firing/movement, zero velocity, and retain an inert ray-blocking wreck.
- Enforce consistent health/death mutation and restoration without implicit revival, while retaining bounded resource-only fixture behavior.
- Replicate final health/shield/death revisions reliably through changed-state updates and full baselines; cosmetic packet loss cannot lose damage. Update damage-source descriptions to match implemented behavior.
- Finish epic-wide regression and measured performance validation, including damage/death, mixed supply, impaired networks, and both transports.

## Acceptance

- Cover shield-only, exact break, overflow, unavailable charged shields, armor 0/50/100/200, and fractional health damage. Incoming 20 with active shield 5 and armor 100 removes 7.5 health.
- Shield-only, exact-break, and overflow hits each produce one weapon impact plus one shield arc when protection was active immediately before that hit's damage resolution; overflow never duplicates the weapon impact. Unavailable shields and later same-tick hits after depletion produce only the weapon impact. Misses/allies take no damage; misses produce neither impact nor arc.
- Death happens once, stops existing velocity, cancels reloads, prevents regeneration/firing, and leaves a blocking wreck. Same-tick trades work.
- All peers and joining/recovering clients converge on authoritative resources/death under loss/reordering; restoration rejects inconsistent live/dead state.
- Cosmetic queue/pool exhaustion cannot suppress accepted damage. Record five/ten-player performance and transport limitations against epic ?11.

## Excluded

Respawn, despawn, loot, friendly fire, new damage-type multipliers, additional shield-break effects, and projectile weapons. The generic shield arc is already introduced by story 4.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
