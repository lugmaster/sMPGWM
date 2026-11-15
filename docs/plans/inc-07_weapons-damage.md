# Weapons | Damage

Status: implemented on 2026-11-15; build/run verification and remaining coverage limits recorded below.
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


## Implementation and verification record (2026-11-15)

Implemented shield-first damage with fractional armor mitigation, shield contact sampled immediately before each hit resolves, and stable ship/slot admission before damage so same-tick trades survive. The bounded damage staging holds the supported ten ships by nine weapons independently of cosmetic staging. Damage resolves before cosmetic callbacks, including when no cosmetic consumer exists. Dead ships retain collision bodies, clear actions/velocity/effects/reloads, reject implicit revival, and cannot regenerate, fire, move or activate abilities. Reliable gameplay snapshots carry final resources/death; restoration rejects inconsistent positive-capacity health/death state. Replica movement discards pre-death samples and stops interpolation on the wreck. Physical and laser descriptions now match their shared effectiveness.

Runtime and Godot tests cover shield-only/break/overflow, unavailable shields, armor 0/50/100/200, fractional damage, zero damage contact, same-tick shield depletion, lethal trades without a cosmetic consumer, death movement and restoration consistency. Existing presentation impact/arc tests remain in the regression suite. Resource-only fixtures retain their bounded behavior.

Final `dotnet test sMPGWM.sln --no-restore --verbosity minimal` rebuilt the game and tests successfully: 628 selected outer tests passed, including 370 in-engine Godot cases. The three explicit long-running tests are excluded by the normal suite; the ENet runs below were performed separately. The pre-existing `UiIcon.Size` hiding warning and unsupported mock-ability startup diagnostics remain. `git diff --check` passes.

Normal startup with `Godot_v4.7.2-stable_mono_win64_console.exe --headless --path sMPGWM.Game --quit-after 180` exited 0, initialized Steam and loaded StartMenu. This verifies executable startup; it is not a manual graphical playtest or a Steam multiplayer transport test.

Local normal-startup ENet measurements used the development Laser/Vulcan loadout, Debug builds, one machine, approximately 20-second runs, and the existing firing harness. Tick/allocations statistics use seconds 10?18 and include the full simulation/network step and Godot query allocations.

| Workload | Accepted host shots | Shot bytes published | Tick p99 | Mean allocated bytes/tick | Publication p99 | Result |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Five peers | 947 | 252,495 | 1.223 ms | 24,031 | 39.942 ms | All exits 0 |
| Ten peers, repeat | 1,635 | 924,944 | 1.646 ms | 31,302 | 34.180 ms | All exits 0 |
| Five peers, moderate impairment | 752 | 193,778 | 1.105 ms | 23,976 | 33.967 ms | All exits 0 |

Moderate impairment adds 20?60 ms each direction and 1% loss, with delay-induced reordering. Cosmetic staging dropped no events; recorded staging high-water was 4/9/6 respectively. The impaired run's pending cosmetic high-water was at most 11 with no queue drops. The proxy reported 47/70 dropped datagrams, 54 client-to-host socket errors around the run/teardown and no proxy exception. No reliable-backlog measurement is claimed.

The first ten-peer attempt passed every workload assertion but one client crashed during native Godot shutdown with outstanding script bindings/KinematicCollision2D references. A repeat exited cleanly before the subsequent explicit movement-collision wrapper disposal change. The impaired run includes that disposal change. The cause and elimination of the intermittent shutdown crash are not proven; native ENet channel/resource teardown warnings remain recorded.

Raw logs, staged sources/configuration, workload assertions and summaries are retained locally under `docs/measurements/2026-11-15-combat-enet`, `...-retry`, and `...-moderate` (ignored generated measurement directories).

Validation limits: these are allied firing workloads, so they exercise mixed supply/reload and delivery rather than hostile damage/death convergence under impaired delivery. Hostile combat is covered by in-engine tests. No live GodotSteam MultiplayerPeer session, prolonged release soak, nine-weapon performance matrix or manual visual acceptance was performed. Build/run DOD is verified separately from these outstanding epic-wide validation items.
