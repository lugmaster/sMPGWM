# UI and player input

`GamePlayManager` connects the scene's UI, input controller, and player action controller. Singleton dependencies use `InputController.Instance` and `PlayerMock.Instance`; ordinary scene nodes still use `GetNode`. Its session state is either `Active` or `Inactive`; opening a window does not change session state or pause the world.

## Responsibilities

| Component | Responsibility |
| --- | --- |
| `UiCanvas` | Exposes four public menu operations and forwards UI requests; receives data through internal gameplay integration methods. |
| `UiWindowManager` | Creates, closes, and focuses windows in a centered row; handles menu shortcuts and scrolls focused windows into view. |
| `UiWindowRegistry` | Tracks open window identities, focus order, and whether any window blocks player controls. Contains no Godot dependencies. |
| `PlayerInputPolicy` | Derives `CanControlPlayer` from an active session and no blocking windows. Contains no Godot dependencies. |
| `InputController` | Gates continuous movement/combat input, clears outputs when blocked or unfocused, and suppresses mouse attacks over UI. |
| `PlayerActionController` | Accepts hotbar activation requests and checks policy and current input availability before invoking gameplay. |

## Window behavior

- Inventory, map, and main menu can coexist. Each currently blocks player controls.
- `UiCanvas.OpenMenu(id)` creates one instance or gives the existing instance keyboard focus and scrolls it into view.
- `UiCanvas.CloseMenu(id)` closes that window; `ToggleMenu(id)` opens or closes it. Close buttons close their own window.
- `UiCanvas.CloseAllMenus()` closes every window.
- Clicking a window updates focus without changing its position in the row. Escape closes all open windows at once, or opens the main menu if none is open.
- Menu shortcuts execute once per press and require release before another toggle. Duplicate non-repeat key events and keyboard auto-repeat are ignored. Releases are tracked before GUI handling; disabling navigation or losing application/window focus clears the guards. No timed cooldown is used.
- Closing the final blocking window restores player controls only if the session is active.
- Entering `Inactive` closes windows and prevents reopening them.
- HUD processing continues while windows are open. Inventory keeps hotbar buttons interactive for editing while activation stays blocked; other blocking windows alone disable them.
- Held movement resumes when controls become available. Held combat actions require release and a fresh press.

Windows use an `HBoxContainer` inside a `ScrollContainer`. The row is centered horizontally with a 24-pixel gap, and each window is centered vertically. Windows remain in opening order; closing and reopening one appends it on the right. Opening, closing, content size changes, and viewport resizing automatically update placement. Rows wider than the viewport scroll horizontally without shrinking windows; focused windows scroll into view. Empty rows are hidden so the scroll container does not intercept HUD or world input.

Placement changes are immediate. Window dragging, resizing, and modal dialogs are deferred. Items support click pickup/placement and native Godot drag-and-drop. The inventory uses eight columns.

## Inventory view binding

`IconSize` is the shared sizing vocabulary: hotbar definitions select `Large` (64-pixel artwork); inventory and equipment select `Medium` (48-pixel artwork). `ClickableIcon` adds theme padding to that size and fits textures proportionally into the content area. Centered sizing flags prevent container stretching. Texture dimensions and empty/occupied state do not determine button dimensions. With the current theme, hotbar buttons are 72×72 and inventory/equipment buttons are 56×56.

`GamePlayManager` supplies its inventory and equipment through internal `UiCanvas` binding hooks. The window manager announces individual window openings before adding them to the tree and identifies closed windows. Inventory views accept data before `_Ready`, replace editor preview cells, subscribe to committed snapshots, and detach on closure and teardown. Opening a view renders current state immediately; reopening preserves game-owned capacity and UI-owned toolbar choices.

`InventoryProjection` combines catalog-derived category tabs with trimmed, case-insensitive item-name search and Name ascending/descending sorting. Name ties retain definition-ID then entity-ID order in both directions. Matching occupied results pack together, followed by genuine empty addresses; hidden items never become drop destinations. A no-match message can coexist with genuine empties, while a full bag with no matches exposes no cells. These operations preserve storage and revisions. Each cell retains an immutable binding with its actual address/slot key, entity expectation, and revision. `ItemGrid` reuses controls during refresh and appends cells on growth. Equipment labels come from domain slot definitions through the existing labeled-icon scene/provider. Generic icons accept opaque bindings without querying gameplay services.

Inventory-specific lifecycle and application focus authorize/revoke domain edit sessions independently of other blocking windows. Map alone does not authorize inventory edits. The public canvas boundary remains its four menu operations. Shared item presentation adds resource-loaded textures, neutral missing-icon placeholders, tooltips, and bottom-right quantities through generic `ClickableIcon.SetDisplay` input. Stackable items show quantities including one; nonstackable and empty cells have no badge. Large counts truncate to compact labels through the full `long` range, while tooltips retain exact quantities and configured stack-limit information. The delayed Godot popup uses an input-ignoring themed label and is dismissed on rebinding, focus loss, or closure; `DismissTooltip` also closes tooltips when placement starts. Tooltip data remains available with activation disabled.

`ItemInteractionController` owns one pending source shared by inventory, equipment, and hotbar views, with explicit idle, click-placement, and native-drag modes. It captures location/entity expectations, revisions, edit session, and mapping version; the transfer coordinator owns placement rules. `InventoryMenu` captures the displayed target on press and submits on release, with a pointer-ignoring preview, source highlight, and validated hover feedback. Successful click placement ends carrying; incompatible/full-stack click targets retain it. Source/outside/right clicks, toolbar changes, snapshot refresh, focus loss, closure, and session revocation cancel pending interactions. Placement and cancellation consume pointer events; toolbar clicks still perform their normal operation. Escape continues to close all windows through the window manager. These internal hooks preserve the four public canvas operations.

`ItemCellBinding` supplies the actual storage location or hotbar position, entity expectation, and revision for every interactive cell. Cells forward virtual Godot GUI/drag callbacks and hover/tree notifications through plain C# events and delegates registered once at creation; refreshed data does not rebuild those handlers. The menu retains source and hover controls directly and validates feedback on target transitions. Per-frame work only positions the pointer preview and feedback, which are authored in the inventory scene. Hotbar control references are cached. All handlers detach on cell removal or view teardown. Cell dispatch does not use Godot signals, Callable wrappers, or SetDragForwarding.

`UiSystem/Items` contains shared visual formatting and icon resolution for inventory, equipment, and the hotbar. Resolution uses the item asset first, then its category asset/key, then a shared placeholder. Native category tabs carry optional resolved icons and stable category-ID metadata. Iconless categories remain text-only. `ResourceLoader` supplies texture caching; views do not maintain parallel texture dictionaries. YAML loading validates icon keys and references from the shared equipment slot definitions before gameplay construction and returns structured errors with source paths.

Transfer hover checks evaluate the two affected locations without creating entities, slot arrays, or global identity sets. Commit revalidates the request, prepares immutable snapshots, verifies global identity uniqueness, installs both owners, then publishes notifications. `InventoryChangePlan` remains internal because its owner/before-snapshot checks protect installation; `ItemTooltipData` remains an immutable domain presentation snapshot. Typed IDs and separate inventory/equipment ownership remain unchanged. The redundant category-tab model and separate inventory, equipment, and hotbar cell binding records have been removed.

`GamePlayManager` owns the ten-position `ItemHotbarState` and gameplay composition adapter. Consumable links sum matching inventory stacks with `long` quantities and retain their definition at zero. Weapon links resolve stable equipment-slot keys on each committed refresh and retain their slot placeholder when empty. Mock action bindings live in gameplay integration and preserve original action identity through rearrangement. Item activation produces a typed integration request/log; it does not consume items or execute effects. PlayerActionController remains the final activation gate.

While inventory is open in an active, focused session, the menu's click/native drag adapters can assign eligible sources, move/swap existing hotbar entries, or right-click to cancel and clear one position. Assignments replace the destination without changing ownership. Inventory weapons and unsupported categories are rejected. Links cannot transfer into inventory/equipment; outside drops cancel. Hotbar edits use the same short-lived edit session plus hotbar/source revisions. The HUD has a dedicated hotbar canvas layer so the menu scroll surface cannot intercept its pointer input. Closing inventory revokes editing immediately, including when Map remains open; activation permissions are independent. Generic ClickableIcon still contains no item services.

Slot-specific delegates registered through `SetDragForwarding` adapt native Godot drag callbacks without adding inventory dependencies to `ClickableIcon`. Godot controls the drag threshold and preview; payloads contain a single-use opaque token scoped to the controller's edit session. Hover validation is read-only, and drops revalidate through the same transfer coordinator as clicks. Every native drag end clears carrying, including invalid, full-stack, and outside drops. Source state remains authoritative until a successful commit. View/session changes cancel the native drag and invalidate its token immediately. Native release does not also trigger click pickup, and wheel scrolling stays available through the existing inventory scroll container.

The opt-in `InventorySlotSmokeFixture`, `InventoryVisualSmokeFixture`, `InventoryClickSmokeFixture`, and `InventoryHotbarSmokeFixture` provide test data for scene smoke checks; normal startup remains empty. The fixtures cover visuals, counts, hotbar permissions, browsing, and real mouse-event placement/cancellation across two game lifetimes. Toolbar choices survive transfers and window reopen. Toolbar changes cancel pending placement before refreshing the mapping. Search accepts Inventory/Map shortcut letters as text; Escape still closes all windows and input releases still clear navigation guards. Filtering refreshes on input or committed snapshots, with scroll preservation/clamping supplied by the existing Godot scroll container.

## Adding features

The public window boundary is `UiCanvas.OpenMenu`, `CloseMenu`, `ToggleMenu`, and `CloseAllMenus`. Window manager operations, HUD configuration, and gameplay integration members are internal; event-forwarding helpers are private. Godot lifecycle overrides remain public. The registry and scene provider are internal, with `InternalsVisibleTo` granting NUnit access to the registry's state logic.

Window identities and Godot Input Map actions share exactly the same names: `UiWindowId.InventoryMenu`, `MapMenu`, and `MainMenu`. The manager enumerates the enum to derive cached `StringName` values and tracks held actions using enum values. There is no separate action-name list or string switch.

To add a menu, create an inherited `game_overlay_menu.tscn` scene and a script extending `GameOverlayMenu`. Add its enum value (for example, `SkillTreeMenu`), its scene creation method and factory case, and an identically named Input Map action with the desired hotkey. Normal menu actions automatically toggle the matching window; `MainMenu` retains Escape's close-all behavior. When renaming an enum value, update the Input Map entry too; the configuration test checks their alignment. Keep window navigation inside the UI. The registry supports nonblocking windows, although all current factory cases use blocking windows.

For example, gameplay code holding a canvas reference opens inventory with `canvas.OpenMenu(UiWindowId.InventoryMenu)`.

Route feature requests to the owning gameplay controller: inventory views request item moves, trade views request offer changes, and passive tree views request allocations. Those systems validate and change authoritative state; the views refresh from the resulting state. Keep selections, hover states, and drag previews in the views.

Hotbar buttons use `PlayerActionController.RequestHotbarActivation`. Future keyboard hotbar bindings should call the same method. Gameplay resolves each display position to its original mock action or its currently available item link. Item effects and networking remain deferred.

## Verification

From the repository root:

```powershell
dotnet build sMPGWM.sln
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/icon_sizing_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/inventory_smoke.gd
```

Set `$godotExecutable` to the Godot .NET console executable. The smoke script loads the actual game scene twice and checks row placement, scrolling, window navigation, focus, input blocking, hotbar interaction, and teardown. It runs the project's normal autoloads, including Steam initialization.

The sizing smoke checks hotbar, inventory, and equipment at both viewport sizes with 16-, 64-, and 256-pixel textures, a rectangular texture, and empty slots. It verifies that texture changes preserve the configured slot dimensions, including the hotbar while inventory is open.

The inventory smoke checks 1280x720 and 1920x1080 across two game lifetimes, using opt-in fixtures and actual mouse input for click/native drag transfers, cancellation, hotbar editing, and filtered address mapping. It also checks growth, detached subscriptions, delayed tooltip dismissal, vertical bag scrolling, horizontal multi-window overflow, focus visibility, and recentering. The architecture smoke derives a fitting viewport from the row's actual minimum width before narrowing it to test overflow; three current windows exceed 1920 pixels together.

For rendered visual evidence, run `& $godotExecutable --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/inventory_smoke.gd -- --capture-visuals`. This saves tooltip/count, drag-preview, empty-result, and window-overflow screenshots for each resolution to `artifacts/inventory-inv10-*.png`. It requires a desktop, focuses the test window, and moves the OS cursor to match injected pointer events; leave the pointer idle during the run. Automated rendered checks and captured-image inspection passed for INV-10; a separate manual playthrough was not performed. Fixture artwork is temporary. Production remains empty until gameplay supplies items; item-definition YAML loading and actual item effects remain deferred.
