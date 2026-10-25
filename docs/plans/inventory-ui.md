# Inventory, Equipment, and Hotbar

Status: implemented local prototype. Item gameplay behavior will be finalized in another story.

## Domain and confirmed definitions

Scripts/ItemSystem owns item definitions, inventory stacks, equipment rules/state, item hotbar bindings, and validated commands. These are ordinary C# types without Godot dependencies. Views render state and submit requests; they do not decide compatibility or effects.

| Equipment slot | Accepted item type |
| --- | --- |
| Engine | Engine |
| Hull | Hull |
| Shield Generator | ShieldGenerator |
| Reactor | Reactor |
| Light Weapon Slot 1 | LightWeapon |
| Light Weapon Slot 2 | LightWeapon |
| Heavy Weapon Slot 1 | HeavyWeapon |

The seventh type is Consumable. Inventory tabs are All plus one per item type; there is no separate category taxonomy. These three typed weapon slots supersede the original four generic weapon slots.

Definitions contain only an ID, name, icon path, type, stack limit, and demo weight/worth for sorting. Equipment is nonstackable. No component subclasses, stats, effects, or generalized extension framework are implemented.

## Prototype behavior

- One window contains the labeled character sheet on the left and tabs, search, explicit sorting, and vertically scrolling inventory on the right.
- Defaults: 60 storage slots, six columns, 64-pixel cells, ten hotbar entries, Name ascending sorting. Capacity and grid columns are configurable in their owning objects.
- Drag items to move or swap. Matching inventory stacks merge to their limit, retaining overflow in the source. Equipment swaps validate both placements atomically.
- Shift-click opens a split overlay with the lower half selected. Number and slider stay synchronized. Confirmation creates a distinct stack in the first genuinely empty slot; full inventory prevents confirmation.
- Filtering/search do not mutate storage or expose hidden occupied slots as empty. Visible cells retain storage indices. Sort affects the whole bag, packs empties last, preserves identities, and never merges stacks.
- Drag payloads carry session identity, revision, and source address. Hover validation is read-only; commit revalidates. Changed state or inventory close/reopen invalidates old payloads. Starting/cancelling drags never changes ownership.
- Consumables are assigned from inventory without moving them. Shortcuts reference definitions and aggregate quantities across inventory stacks, retaining zero-count bindings.
- Weapons are assigned only from occupied equipment slots. Shortcuts reference the equipment slot: replacement updates them, emptying disables them, and re-equipping restores availability.
- Editing requires inventory to be open. Drag hotbar entries to move/swap; right-click clears. Dropping outside cancels. Existing mock skills remain supported.
- Activation still passes through PlayerActionController and the existing player-input policy. Blocking windows prevent activation. Consumables remove one from the lowest-index matching stack and log demo use; weapons only log requests.
- Closing/reopening preserves session state and UI selections. A new game scene receives independent demo state.

## Integration and lifecycle

GamePlayManager constructs session/demo state and connects activation. UiCanvas binds state internally and coordinates inventory lifetime with hotbar edit permission. Its public menu API remains OpenMenu, CloseMenu, ToggleMenu, and CloseAllMenus.

UiWindowManager binds new views before interaction and emits internal lifecycle notifications. Existing centered rows, 24-pixel gaps, horizontal overflow scrolling, and opening order remain intact. Empty row space allows HUD interaction; windows and scrollbars remain interactive.

ItemSlotControl renders inventory, equipment, and hotbar cells and adapts native Godot drag data to domain requests. InventoryPresentation retains UI-only filter/search/sort selections. Textures and Godot nodes never enter domain state.

The split overlay blocks underlying interactions. Inventory/map shortcuts are suppressed while it is open, but releases are still tracked. Escape retains close-all behavior. Inventory closure or focus loss cancels drag/split interactions. Closing inventory locks hotbar editing even while map stays open. Views detach subscriptions during teardown.

## Validation

- NUnit covers all 49 equipment compatibility combinations, atomic operations, merge overflow, split boundaries/full inventory/stale requests, read-only validation, filtering, deterministic sorting, shortcut quantities, weapon-slot replacement, edit permissions, activation, and independent sessions.
- inventory_smoke.gd exercises native pointer drag/drop, invalid equipment, weapon/consumable shortcuts, splitting, merging, hotbar rearrangement/clearing, filtering, sorting, activation, cancellation, and reopening at logical 1280x720 and 1920x1080.
- ui_architecture_smoke.gd preserves existing window/input regressions over two scene lifetimes. Assertions account for expanded inventory width and distinguish editing from activation blocking.

From the repository root:

```powershell
dotnet build sMPGWM.sln
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
$godotExecutable = 'C:/Users/lober/godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe'
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/inventory_smoke.gd
```

Godot checks load normal autoloads, including Steam. Existing nullable/source-generator build warnings remain.

## Deferred

Saving, networking, combat effects, equipment bonuses, cooldowns, trading, world drops, window dragging/resizing, finalized item behavior, and a generalized scripting framework remain outside this story.
