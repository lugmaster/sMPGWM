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
- Apply one authoritative death transition: clear actions, cancel reloads, stop regeneration/abilities/firing/movement, zero velocity, and retain an inert ray-blocking wreck.
- Enforce consistent health/death mutation and restoration without implicit revival, while retaining bounded resource-only fixture behavior.
- Replicate final health/shield/death revisions reliably through changed-state updates and full baselines; cosmetic packet loss cannot lose damage. Update damage-source descriptions to match implemented behavior.
- Finish epic-wide regression and measured performance validation, including damage/death, mixed supply, impaired networks, and both transports.

## Acceptance

- Cover shield-only, exact break, overflow, unavailable charged shields, armor 0/50/100/200, and fractional health damage. Incoming 20 with active shield 5 and armor 100 removes 7.5 health.
- One impact remains one effect even on shield break/overflow; misses/allies take no damage. No shield-specific effect is added.
- Death happens once, stops existing velocity, cancels reloads, prevents regeneration/firing, and leaves a blocking wreck. Same-tick trades work.
- All peers and joining/recovering clients converge on authoritative resources/death under loss/reordering; restoration rejects inconsistent live/dead state.
- Cosmetic queue/pool exhaustion cannot suppress accepted damage. Record five/ten-player performance and transport limitations against epic ?11.

## Excluded

Respawn, despawn, loot, friendly fire, new damage-type multipliers, separate shield effects, and projectile weapons.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
