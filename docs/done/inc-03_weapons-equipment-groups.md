# Weapons | Equipment Groups

Status: implemented; automated regression checks passed; live multiplayer and visual acceptance verification pending.
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 3 of 7  
Dependencies: Story 1.

## Outcome

Let players independently assign each equipped weapon to the primary and secondary weapon groups through the equipment UI, with host-authoritative synchronization. Preserve the existing slot mechanic, capacity, and equipment rules.

## Scope

- Two independent groups: primary and secondary. Each equipped weapon can belong to primary only, secondary only, both, or neither. Toggling one group does not change the other. Membership is independent of weapon archetype and slot capacity consumption.
- Store both membership flags in ship loadout configuration, bound to the equipped weapon identity and slot binding, not immutable weapon definitions or the empty slot. Weapons equipped during play default to primary enabled and secondary disabled; session initialization uses the selected loadout's group definition, and restoration uses captured membership as described below. Replacement removes the old weapon's membership and initializes the new weapon's membership; it does not inherit the old occupant's choices.
- Every weapon removal clears its membership in both groups and removes its UI entry, regardless of cause. Manual unequip and removal caused by unequipping the weapon fixture use the same group cleanup. Fixture-driven weapon removal is already implemented; integrate group cleanup with the existing equipment-removal lifecycle. Re-equipping uses the new-equipment defaults.
- Apply group and equipment changes transactionally and increment the binding/configuration revision as appropriate. Preserve accepted group configuration through supported capture/restoration and rejoin paths. Validate slot/item identity and expected revision so delayed requests cannot affect a replacement weapon.
- Keep existing host-authoritative multiplayer loadout behavior. The owning player requests membership changes; the host validates and applies them, then synchronizes the active weapon membership of both groups to the owner and other players. Include complete group state in join/rejoin baselines, and propagate unequip/fixture-removal cleanup. Requests use reliable, bounded, idempotent validation; payloads contain only positional arrays and scalars with numeric game-object references. Actual firing replication remains deferred to story 5.

## Session initialization and restoration

- Both `DevelopmentLoadout` and `StartLoadout` must provide a group definition specifying which of the currently equipped weapons belong to the primary and secondary groups. Definitions support primary only, secondary only, both, and neither; they do not equip weapons or alter the slot mechanic.
- `StartLoadout` equips Vulcan Mk1 in `weapon_1`. `DevelopmentLoadout` equips Vulcan Mk1 in `weapon_1`, Laser Mk1 in `weapon_2`, and the standard heavy weapon in `weapon_3`.
- Group definitions use the following assignments. A weapon omitted from a group is not a member of that group; omission from both means neither. Empty groups are valid, including when all weapon slots are occupied.

| Loadout | Primary group | Secondary group |
| --- | --- | --- |
| `StartLoadout` | Vulcan Mk1 | Empty |
| `DevelopmentLoadout` | Vulcan Mk1, standard heavy weapon | Laser Mk1 |

- During fresh session initialization, the host loads the selected loadout's group definition after that loadout has finished equipping its weapons. Resolve assignments against the actual equipped weapon identities and slot bindings, distinguishing multiple equipped copies of the same weapon. Inventory-only weapons and empty slots cannot be group members. The development definition applies to the final development equipment, including equipment initialized through `StartLoadout`.
- Publish the resulting membership with the initial authoritative loadout state so the equipment UI and other players receive the same group configuration.
- Capture both group memberships with the equipped weapon bindings and configuration revision. During restoration, validate and restore groups together with equipment before publishing state or rendering the group entries. Captured membership takes precedence over `DevelopmentLoadout`/`StartLoadout` definitions and new-equipment defaults, preserving all four membership combinations without re-adding unequipped weapons.
- Join/rejoin and restoration preserve empty group assignments for equipped weapons; empty groups must not trigger automatic assignment or startup defaults.

## Equipment UI

- Use `scenes/ui/base/labeled_button_pair.tscn` for every equipped weapon. During session initialization, remove the existing placeholder `Weapons/VBoxContainer/LabeledButtonPair` in `equipment_container.tscn`, even when no weapons are equipped. Generate the equipped-weapon entries in that same container below the separator from initialized or restored authoritative state. Repeated session initialization must not retain entries from the previous session or create duplicates.
- Each entry displays the weapon name in `Label`, a `Primary` toggle in `FirstButton`, and a `Secondary` toggle in `SecondButton`. These are independent toggles, not an exclusive button group.
- Generate exactly one entry per equipped weapon in equipment-slot order, including separate entries for weapons with identical names. Empty slots have no entry; a weapon consuming multiple capacity units still has only one entry. Keep the existing slot grid and capacity display behavior intact.
- Render accepted membership when opening or refreshing the equipment menu. While a request is pending, disable that entry's toggles until its outcome arrives. On rejection or a stale revision, refresh from authoritative state and show a concise reason. Removal invalidates any pending interaction with that weapon.

## Acceptance

- Every equipped weapon has one labeled button pair showing its name and independent primary/secondary toggles; no placeholder or empty-slot entries remain.
- Session initialization removes the existing placeholder before displaying generated entries, including for an empty loadout. Starting another session does not retain stale entries or duplicate rows.
- Both `DevelopmentLoadout` and `StartLoadout` equip the specified weapons and apply the group assignments above. Fresh sessions synchronize those assignments to all players; inventory-only weapons are excluded and identical equipped weapons remain individually assignable.
- Every equipped weapon supports all four combinations: primary only, secondary only, both, and neither. Changing membership leaves equipment slots, capacity, and equipment rules unchanged.
- Omitted assignments leave weapons outside the respective groups. Joining, rejoining, and restoring a player with all weapons equipped and both groups empty preserves that state on every peer.
- Manual unequip and fixture-driven weapon removal produce identical group cleanup for each removed weapon: both memberships and the UI entry are cleared, and the changes are synchronized to all players. Replacement and re-equipping follow the documented defaults.
- UI reflects accepted configuration and cannot mutate another player's loadout or silently apply stale assignments.
- Host-approved membership changes and removals converge on the owner and other players. Late join/rejoin receives the complete current group state; stale updates cannot restore removed membership.
- Group state survives supported capture/restoration with weapon bindings and configuration revision intact. Verify primary only, secondary only, both, and neither after restoration; saved membership must not be overwritten by startup definitions or defaults, and removed weapons must remain absent from groups.
- Duplicate requests have stable outcomes. Verify races between group changes and weapon replacement/fixture removal.
- A concrete UI definition and implementation are included; weapons still do not fire.

## Excluded

Input mapping and key bindings, trigger execution, firing visuals, ammunition/reload UI, per-shot energy, damage, and shot-event replication.

## Verification

Verify the acceptance criteria and relevant regressions. Mark test-support code according to `AGENTS.md` and record results and limitations here.

### Implementation verification (2026-11-15)

- Implemented independent membership as an immutable `WeaponGroupConfiguration` owned by the equipment controller within the ship loadout. Its compact bindings contain only equipped weapons and identify both slot and item identity. `ItemEntity`, inventory storage, and generic item wire rows contain no weapon-group state. Configuration changes share the equipment revision and commit before publication; capture/restoration carries the configuration explicitly in `RestoredShipState`.
- Equipment rows instantiate `labeled_button_pair.tscn`, remove the placeholder, display accepted state, and disable controls during requests. Rejections use the existing item-feedback channel.
- `dotnet test sMPGWM.sln --no-restore`: passed (574 top-level tests, including the headless Godot integration runner). Godot cases cover all four captured memberships, transactional cleanup/rollback, replacement defaults, independent identical weapons, stale/invalid configuration rejection, unchanged item object identity, separate three-field item wire rows, malformed configuration rollback, and full world capture/restoration. The original unequip reference-preservation assertion is retained.
- `git diff --check`: passed. Existing `UiIcon.Size` compiler warning remains.
- Limitations: no interactive UI inspection or live Steam multi-peer/rejoin exercise was performed. Dedicated transport duplicate/race and repeated-session UI tests remain to be added; these acceptance scenarios are not claimed as verified by the regression run. Optional performance scenarios were not run.
