# Weapons | Firing Visuals

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 4 of 7  
Dependencies: Stories 1, 2, and 3.

## Outcome

Holding a group trigger fires all eligible weapons assigned to that group in a local host session, using their own cadence. Show muzzle flashes, streaks/tracers, and impacts; damage is excluded.

## Scope

- Implement fixed-forward automatic hitscan, nearest valid collision, shooter/ally exclusions, range and spread, per-item sequence, and alternating authored muzzles (epic ?6.1 and ?7).
- Both triggers can be held together; each weapon fires at most once per physics tick. Grouped weapons start together when eligible but retain their individual rates rather than sharing one cadence.
- First shot is immediate; Vulcan fires every 4 ticks and laser every 20 at 60 Hz. Preserve fractional cadence, cooldown across equipment changes, and no idle catch-up bursts.
- Require existing power availability but temporarily omit ammunition/reload and per-shot energy admission. This intentional intermediate behavior is replaced by story 6.
- Use LMB for Fire Group 1, F for Fire Group 2; preserve RMB steering. Merge configurable bindings without overwriting user settings. UI/focus blocking releases both triggers.
- Prepare the two exact profiles and existing audio from epic ?4.3. Use bounded reusable pools and lifecycle cleanup. Hits produce one ordinary impact; misses none. Tracers never govern collision timing.
- Integrate only into the authoritative simulation, never prediction replay. Keep multiplayer firing disabled until story 5 supplies its authority/replication path.

## Acceptance

- Demonstrate each group separately and both together, mixed weapons, None membership, replacement, trigger release/repress, and UI blocking.
- Verify transformed muzzle origins, accepted-shot alternation, forward aim, spread/range, nearest blocker, and hit/miss presentation in Godot.
- Validate cadence and bounded pool reuse/exhaustion; no per-shot timers or unbounded queues. Local session disposal clears effects and item state correctly.
- Health/shields remain unchanged, magazines do not deplete, and firing incurs no discrete energy charge.

## Excluded

Remote firing and shot replication, supply mechanics, damage/death, projectile simulation, prediction, and lag compensation.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
