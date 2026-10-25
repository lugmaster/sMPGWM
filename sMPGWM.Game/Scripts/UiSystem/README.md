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

Placement changes are immediate. Dragging, resizing, modal dialogs, and item dragging are deferred.

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
```

Set `$godotExecutable` to the Godot .NET console executable. The smoke script loads the actual game scene twice and checks row placement, scrolling, window navigation, focus, input blocking, hotbar interaction, and teardown. It runs the project's normal autoloads, including Steam initialization.
