# Weapons | Equipment Groups

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 3 of 7  
Dependencies: Story 1.

## Outcome

Let players assign equipped weapon slots to fire groups, independently of weapon archetype. Define and implement the equipment-group UI as part of this story; schema fields alone do not complete it.

## Scope

- Two groups: each slot belongs to group 1, group 2, or neither, never both. New slots default to group 1; demonstration loadout explicitly puts the laser in group 2.
- Store membership in ship loadout configuration. Replacement retains slot membership and increments binding/configuration revision. Apply changes transactionally with equipment changes.
- Preserve group configuration through supported capture/restoration. Validate slot/item identity and expected revision.
- Proposed UI: a selector per equipped weapon row (`None`, `Group 1`, `Group 2`) and visible group membership. Define placement, interaction, empty/replaced-slot behavior, and feedback for rejected/stale changes before implementation; record the final UI decision in this story.
- Keep existing multiplayer loadout authority intact; any new group request uses reliable, bounded, idempotent host validation and positional array/scalar payloads. Firing replication is deferred to story 5.

## Acceptance

- Both scoped weapon types can be assigned to either group or neither; replacing or unequipping does not corrupt slot membership.
- UI reflects accepted configuration and cannot mutate another player's loadout or silently apply stale assignments.
- Group state survives supported restore/rejoin paths and duplicate requests have stable outcomes.
- A concrete UI definition and implementation are included; weapons still do not fire.

## Excluded

Trigger execution, firing visuals, ammunition/reload UI, per-shot energy, damage, and shot-event replication.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
