# Friendly fire implementation handoff

Date: 2026-11-14  
Status: production implementation complete; acceptance validation partial  
Repository inspected at: `0b925811af992c891b25e507901171e390f4a653`

Add a configurable friendly-fire rule for the existing host-authoritative hitscan weapons. The proposed first slice defaults to disabled, captures the rule when a session is created, and retains the existing requirement that peers have matching effective configuration. These are implementation defaults based on the preceding scope discussion; this handoff does not authorize implementation by itself.

## Behavior

Add the following packaged setting in `sMPGWM.Game/config/config.cfg`:

```ini
[combat]
friendly_fire = false
```

Expose it as `GameConfig.FriendlyFire` with a private setter. The setting applies to every current hitscan weapon, including ammunition and energy weapons, regardless of fire group.

| Target | Disabled | Enabled |
| --- | --- | --- |
| Shooter's own body | Excluded | Excluded |
| Living allied ship | Ray passes through; no damage | Intercepts the ray and receives normal damage |
| Living hostile ship | Intercepts and receives damage | Same |
| Wreck, including an allied wreck | Blocks the ray; no further damage | Same |
| World obstacle | Blocks the ray | Same |

Use the existing `ShipRuntime.IsAllyOf` relationship. Distinct ships are allies when they share a nonzero faction; sharing faction zero does not make them allies. Keep self exclusion explicit and unconditional.

When enabled, friendly damage uses existing shield absorption, hull mitigation, death handling, resource replication, and shot-result effects. There is no friendly-damage multiplier. Preserve resolution of all accepted shots before damage application, so simultaneous attacks retain their existing ordering.

This slice excludes a settings UI, live toggling, automatic client adoption of host rules, self damage, projectile or area damage, and changes to ability targeting or faction relationships.

## Current integration points

Paths below are relative to `sMPGWM.Game/`.

| File | Relevant behavior |
| --- | --- |
| `Scripts/GameConfigSystem/GameConfig.cs` | Loads typed config properties and computes configuration compatibility data. |
| `Scripts/GameConfigSystem/ConfigLoader.cs` | Falls back to packaged defaults for missing user keys; hashes effective values from both configs. |
| `Scripts/GamePlaySystem/GameSessionFactory.cs` | Constructs `GameplaySimulation` for fresh and restored sessions. |
| `Scripts/GamePlaySystem/GameplaySimulation.cs` | Owns `WeaponSystem` and the authoritative tick ordering. |
| `Scripts/WeaponSystem/WeaponSystem.cs` | Owns the reusable hitscan query, admits shots, applies damage, and publishes results. |
| `Scripts/CollisionSystem/HitscanQuery.cs` | Currently excludes the shooter and every living ally from the ray query. This is the behavior change point. |
| `Scripts/NetworkSystem/Compatibility.cs` | Rejects different configuration checksums; development mode only relaxes assembly equality. |
| `Scripts/WorldSystem/RestoredWorldState.cs` | Stores world continuity and weapon state, but no configuration or session-rule snapshot. |

The previous [hitscan handoff](hitscan-combat-and-weapon-systems.md) describes friendly fire as always off. After implementation, update that decision entry to describe the configurable rule and link here.

## Implementation sequence

1. **Load the setting.** Add the packaged default and the `GameConfig.FriendlyFire` property. Require a boolean value and report a clear configuration error for another type, rather than relying on permissive conversion. Reuse the existing default fallback; do not reset a user's config just to introduce this key.
2. **Capture it at session construction.** Pass `GameConfig.FriendlyFire` from `GameSessionFactory` into `GameplaySimulation`, then into `WeaponSystem` and its `HitscanQuery`. Store the query's rule read-only. A boolean is sufficient; a general session-rules framework is unnecessary for this slice. Keep existing direct construction sites explicitly disabled, or use a documented optional `false` constructor argument for backwards-compatible callers. Tests must be able to construct both modes without mutating global config.
3. **Conditionalize ally exclusion.** Keep the shooter excluded. Exclude other living allied ships only when friendly fire is disabled. Continue using the existing ray mask and nearest-hit result. Do not add a second ally filter in damage application: enabled allies must block shots and receive the same damage pipeline as enemies.
4. **Preserve networking contracts.** Retain the config checksum comparison and existing shot/resource payloads. Do not add a per-shot friendly-fire field or an RPC for this setting. Update the existing gameplay compatibility marker in `GameConfig.EnsureCompatibilityData` to identify the new combat semantics; no wire-layout version change is required. This also prevents an older development build supplied with the new config file from appearing compatible while ignoring the flag.
5. **Verify and document.** Add the coverage below, run the relevant tests and multiplayer check, and update the earlier hitscan decision entry. Record any validation limitation rather than treating headless tests as rendered-client or Steam transport coverage.

## Configuration and session lifetime

An older user config lacking `[combat] friendly_fire` resolves to the packaged `false` value through `ConfigLoader.GetValue`. `CaptureValues` includes that effective value, so a missing user key and an explicitly stored boolean `false` should produce the same checksum when all other settings match.

The shipped `reload_config_on_start = true` currently replaces user config with defaults. When verifying persisted overrides and missing-key fallback, use an isolated config with that setting disabled. Preserve this existing policy; changing it is outside the friendly-fire slice.

All peers must have the same effective setting before joining. A client cannot turn friendly fire off locally in a session configured with it enabled. The host remains the only authority that resolves shots and applies damage. Existing authoritative resource and cosmetic event replication supplies the client outcome.

The active simulation retains the value captured at construction. Editing or reloading config must not change its collision behavior halfway through a session. Restart the session through the normal configuration-loading path to use a different value; do not introduce a live reload path.

**Restoration policy for this slice:** a restored world uses the setting of the newly created session. Existing `RestoredWorldState` snapshots do not preserve the old rule. Loading the same snapshot under a different agreed config can therefore change friendly-fire behavior. Retaining the original saved rule would require a separate extension to restoration metadata and compatibility handling.

## Acceptance tests

Use existing geometry, combat-resource, compatibility, and restored-session tests as the starting points. Keep tests focused on observable results.

| Area | Required coverage |
| --- | --- |
| Defaults and parsing | Packaged default is false; an old user config falls back to false; explicit true loads; non-boolean values fail clearly. |
| Compatibility | Missing-key fallback and explicit false have equal effective hashes; false and true differ and cannot join each other; matching true configurations can join. |
| Ray selection | With a living ally in front of a hostile, disabled mode hits the hostile and enabled mode hits the ally. Verify the shooter is excluded in both modes. |
| Obstacles and wrecks | A nearer world obstacle blocks in both modes. A dead allied ship blocks in both modes and receives no additional damage. |
| Damage | An admitted friendly hit reduces shield first, then mitigated hull health when appropriate; lethal hits produce the usual death state and shot flags. Disabled mode leaves ally resources unchanged. |
| Weapon coverage | Exercise an ammunition weapon and an energy weapon through authoritative firing. Supply costs, cooldown, reload, and group selection retain their existing behavior. |
| Session lifetime | Construct independent enabled and disabled simulations and verify each retains its own rule. Configuration changes do not alter an already constructed simulation. |
| Restoration | Fresh and restored worlds obey the constructor-supplied rule in both modes; restoration does not silently fall back to disabled. |
| Multiplayer | In a two-peer session with true, the host resolves a friendly hit and both peers converge on the target's resources and death state. Repeat with false and verify ally pass-through. |

Every executable line or declaration introduced solely for automated or manual verification must have a unique `TEST-SUPPORT` ID and the fully qualified test method name, as required by `AGENTS.md`. Prefer production constructor parameters over test-only setters. Isolate config fixtures and restore test-owned resources so verification does not modify real user settings.

Run the normal test command, which includes the Godot integration wrapper:

```powershell
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --no-restore
```

Preserve 60 Hz physics and the 30 Hz publication cadence. The new condition must not allocate per ray or add another world scan. For the enabled multiplayer check, record coordinator timing and allocation/GC impact at the supported player scale with actual allied hits. Keep the workload bounded so ship death does not turn the measurement into an idle run. Existing mostly-miss combat measurements do not establish friendly-hit performance.

## Completion checklist

- [ ] Config defaults, loading, validation, and old-user-config fallback covered.
- [ ] Rule captured once and passed through the production session construction path.
- [ ] Collision behavior matches the target table in both modes.
- [ ] Existing damage, supply, simultaneous-fire ordering, and wire formats preserved.
- [ ] Compatibility marker updated and setting mismatches rejected.
- [ ] Fresh and restored-session behavior verified under the stated restoration policy.
- [ ] Focused tests, Godot integration, and multiplayer checks completed with evidence.
- [ ] Previous hitscan handoff updated to point to the configurable rule.

## Implementation evidence (2026-11-14)

Implemented strict boolean loading, packaged false default, constructor capture through the session factory, conditional living-ally exclusion, and gameplay compatibility marker 8. Existing direct constructors default to false. Fresh and restored sessions share the factory path. No damage ordering or wire payload changes were needed.

`dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --no-restore` passed (378 tests, including the Godot wrapper). Geometry coverage now runs with both rule values for living allies, self exclusion, wrecks, and world blockers.

Remaining validation: dedicated configuration/hash cases, friendly damage and weapon-mode cases, session/restoration acceptance cases, two-peer friendly-hit convergence, and supported-scale timing/allocation measurements. Headless geometry coverage does not establish Steam or rendered-client behavior.
