# Ships | Muzzle Offsets | Refactor Schema

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 2 of 7  
Dependencies: None; can proceed alongside story 1.

## Outcome

Add required authored `muzzle_offsets` to `ships.yml`, its schema, and compiled ship definitions/data (epic ?4.3?4.4).

## Scope

- Author fighter offsets as `[1, -15]` and `[1, 15]`, relative to the ship origin; local +X is forward.
- Require exactly two finite two-component vectors for firing-capable ships and store them once in immutable compiled ship data.
- Update ship schema, loader, fixtures, version diagnostics, and compatibility data capture. Planned format transition is `2.0.0` to `3.0.0`; inspect intervening changes first.
- Reject old user definitions with an actionable missing-field migration example; do not silently assign fighter geometry to other ships.

## Acceptance

- Packaged fighter loads and exposes both exact offsets through the normal ship-data path.
- Missing offsets, wrong counts/dimensions, and nonfinite components fail validation clearly.
- Existing ship loading and construction continue to work after fixtures migrate.

## Excluded

Firing, alternating barrel state, coordinate-transform execution, scene anchors, visual changes, and weapon behavior. Story 4 consumes this geometry.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
