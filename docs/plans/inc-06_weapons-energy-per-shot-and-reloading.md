# Weapons | EnergyPerShot and Reloading

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 6 of 7  
Dependencies: Story 5.

## Outcome

Enforce the authored supply mode on the host and synchronize its results. Laser consumes energy per accepted shot; Vulcan consumes magazine rounds and reloads from unlimited reserves.

## Scope

- Add atomic full-cost energy spending: laser pays 4 per shot, with no payment/sequence/effect on failure. Preserve continuous equipped energy demand and zero initial stored energy. Resolve competing slots in stable order after same-tick generation.
- Add per-item magazine and reload state without using item counts or container revisions. New Vulcan starts with 60 rounds; each accepted hit or miss costs one.
- Automatic reload begins with the final round; manual R reloads all eligible ammunition weapons. Duration is 1500 ms/90 ticks; partial magazines retain rounds until completion. Duplicate requests never restart reload.
- Apply epic ?6.2 cancellation, transfer, cooldown, and restoration rules; future death cancellation hooks are integrated in story 7.
- Add bounded reliable idempotent reload requests, absolute owner-state replication, complete join/recovery baselines, and restoration of unequipped item state. Optimize dirty gameplay publication to changed rows for ready peers while retaining full join baselines as specified in epic ?9.2.
- Define and implement rounds/reload progress and energy-starvation feedback in equipment/weapon UI. Progress uses authoritative ticks; UI never completes reloads.

## Acceptance

- Exactly four energy units permit one laser shot; insufficient charge changes neither energy nor accepted-shot sequence. Both modes still require power.
- Vulcan fires its 60th round at tick 236 when starting at 0, starts reload immediately, and can fire again at tick 326.
- Verify partial/manual/automatic reload, cancellation on unequip/power loss, duplicate commands, transfer, re-equip, and restore without free refill or cooldown reset.
- Host/owner energy, rounds, and reload state converge under packet loss, join, and recovery independently of cosmetic events.
- Supply failure emits no shot; health and shields remain undamaged.

## Excluded

Damage/death behavior, ammunition inventory items, finite reserve supplies, and new weapon types.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
