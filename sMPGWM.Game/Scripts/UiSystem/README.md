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
- HUD processing continues while windows are open; hotbar activation buttons are disabled.
- Held movement resumes when controls become available. Held combat actions require release and a fresh press.

Windows use an `HBoxContainer` inside a `ScrollContainer`. The row is centered horizontally with a 24-pixel gap, and each window is centered vertically. Windows remain in opening order; closing and reopening one appends it on the right. Opening, closing, content size changes, and viewport resizing automatically update placement. Rows wider than the viewport scroll horizontally without shrinking windows; focused windows scroll into view. Empty rows are hidden so the scroll container does not intercept HUD or world input.

Placement changes are immediate. Window dragging, resizing, and modal dialogs are deferred. Items support click pickup/placement; native dragging follows in INV-07.

## Inventory view binding

`IconSize` is the shared sizing vocabulary: hotbar definitions select `Large` (64-pixel artwork); inventory and equipment select `Medium` (48-pixel artwork). `ClickableIcon` adds theme padding to that size and fits textures proportionally into the content area. Centered sizing flags prevent container stretching. Texture dimensions and empty/occupied state do not determine button dimensions. With the current theme, hotbar buttons are 72×72 and inventory/equipment buttons are 56×56.

`GamePlayManager` supplies its inventory and equipment through internal `UiCanvas` binding hooks. The window manager announces individual window openings before adding them to the tree and identifies closed windows. Inventory views accept data before `_Ready`, replace editor preview cells, subscribe to committed snapshots, and detach on closure and teardown. Opening a view renders current state immediately; reopening preserves game-owned capacity and UI-owned toolbar choices.

`InventoryProjection` packs occupied entries in storage order, followed by genuine empty addresses. Each cell retains an immutable binding with its actual address/slot key, entity expectation, and revision. `ItemGrid` reuses controls during refresh and appends cells on growth. Equipment labels come from domain slot definitions through the existing labeled-icon scene/provider. Generic icons accept opaque bindings without querying gameplay services.

Inventory-specific lifecycle and application focus authorize/revoke domain edit sessions independently of other blocking windows. Map alone does not authorize inventory edits. The public canvas boundary remains its four menu operations. INV-05 adds cached item textures, neutral missing-icon placeholders, tooltips, and bottom-right quantities through generic `ClickableIcon.SetDisplay` input. Stackable items show quantities including one; nonstackable and empty cells have no badge. Large counts truncate to compact labels through the full `long` range, while tooltips retain exact quantities and configured stack-limit information. The delayed Godot popup uses an input-ignoring themed label and is dismissed on rebinding, focus loss, or closure; `DismissTooltip` is available for subsequent placement integration. Tooltip data remains available with activation disabled.

`ItemInteractionController` owns one pending source shared by the inventory and equipment views. It captures location/entity expectations, revisions, edit session, and mapping version; the transfer coordinator owns placement rules. `InventoryMenu` captures the displayed target on press and submits on release, with a pointer-ignoring preview, source highlight, and validated hover feedback. Successful placement ends carrying; incompatible/full-stack targets retain it. Source/outside/right clicks, toolbar changes, snapshot refresh, focus loss, closure, and session revocation cancel pending interactions. Placement and cancellation consume pointer events; toolbar clicks still perform their normal operation. Escape continues to close all windows through the window manager. These internal hooks preserve the four public canvas operations. Hotbar editing and native dragging remain later stories.

The opt-in `InventorySlotSmokeFixture`, `InventoryVisualSmokeFixture`, and `InventoryClickSmokeFixture` are loaded only by `sMPGWM.Tests/UiSystem/inventory_smoke.gd`; normal startup remains empty. The fixtures cover visuals, counts, and real mouse-event placement/cancellation across two game lifetimes. Category/search/sort behavior follows in INV-09.

## Adding features

The public window boundary is `UiCanvas.OpenMenu`, `CloseMenu`, `ToggleMenu`, and `CloseAllMenus`. Window manager operations, HUD configuration, and gameplay integration members are internal; event-forwarding helpers are private. Godot lifecycle overrides remain public. The registry and scene provider are internal, with `InternalsVisibleTo` granting NUnit access to the registry's state logic.

Window identities and Godot Input Map actions share exactly the same names: `UiWindowId.InventoryMenu`, `MapMenu`, and `MainMenu`. The manager enumerates the enum to derive cached `StringName` values and tracks held actions using enum values. There is no separate action-name list or string switch.

To add a menu, create an inherited `game_overlay_menu.tscn` scene and a script extending `GameOverlayMenu`. Add its enum value (for example, `SkillTreeMenu`), its scene creation method and factory case, and an identically named Input Map action with the desired hotkey. Normal menu actions automatically toggle the matching window; `MainMenu` retains Escape's close-all behavior. When renaming an enum value, update the Input Map entry too; the configuration test checks their alignment. Keep window navigation inside the UI. The registry supports nonblocking windows, although all current factory cases use blocking windows.

For example, gameplay code holding a canvas reference opens inventory with `canvas.OpenMenu(UiWindowId.InventoryMenu)`.

Route feature requests to the owning gameplay controller: inventory views request item moves, trade views request offer changes, and passive tree views request allocations. Those systems validate and change authoritative state; the views refresh from the resulting state. Keep selections, hover states, and drag previews in the views.

Hotbar buttons use `PlayerActionController.RequestHotbarActivation`. Future keyboard hotbar bindings should call the same method. The existing mock remains the temporary execution target; this refactor adds no new hotbar bindings or network behavior.

## Verification

From the repository root:

```powershell
dotnet build sMPGWM.sln
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/icon_sizing_smoke.gd
```

Set `$godotExecutable` to the Godot .NET console executable. The smoke script loads the actual game scene twice and checks row placement, scrolling, window navigation, focus, input blocking, hotbar interaction, and teardown. It runs the project's normal autoloads, including Steam initialization.

The sizing smoke checks hotbar, inventory, and equipment at both viewport sizes with 16-, 64-, and 256-pixel textures, a rectangular texture, and empty slots. It verifies that texture changes preserve the configured slot dimensions, including the hotbar while inventory is open.
