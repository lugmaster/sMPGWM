# UI and player input

`GamePlayManager` connects the scene's UI, input controller, and player action controller. Its session state is either `Active` or `Inactive`; opening a window does not change session state or pause the world.

## Responsibilities

| Component | Responsibility |
| --- | --- |
| `UiCanvas` | Exposes UI presentation methods and forwards UI requests; receives data from the gameplay wiring. |
| `UiWindowManager` | Creates, closes, and focuses windows in a centered row; handles menu shortcuts and scrolls focused windows into view. |
| `UiWindowRegistry` | Tracks open window identities, focus order, and whether any window blocks player controls. Contains no Godot dependencies. |
| `PlayerInputPolicy` | Derives `CanControlPlayer` from an active session and no blocking windows. Contains no Godot dependencies. |
| `InputController` | Gates continuous movement/combat input, clears outputs when blocked or unfocused, and suppresses mouse attacks over UI. |
| `PlayerActionController` | Accepts hotbar activation requests and checks policy and current input availability before invoking gameplay. |

## Window behavior

- Inventory, map, and main menu can coexist. Each currently blocks player controls.
- `Open(id)` creates one instance or gives the existing instance keyboard focus and scrolls it into view.
- `Toggle(id)` opens or closes only that window. Close buttons close their own window.
- Clicking a window updates focus without changing its position in the row. Escape closes all open windows at once, or opens the main menu if none is open.
- Menu shortcuts execute once per press and require release before another toggle. Duplicate non-repeat key events and keyboard auto-repeat are ignored. Releases are tracked before GUI handling; disabling navigation or losing application/window focus clears the guards. No timed cooldown is used.
- Closing the final blocking window restores player controls only if the session is active.
- Entering `Inactive` closes windows and prevents reopening them.
- HUD processing continues while windows are open; hotbar activation buttons are disabled.
- Held movement resumes when controls become available. Held combat actions require release and a fresh press.

Windows use an `HBoxContainer` inside a `ScrollContainer`. The row is centered horizontally with a 24-pixel gap, and each window is centered vertically. Windows remain in opening order; closing and reopening one appends it on the right. Opening, closing, content size changes, and viewport resizing automatically update placement. Rows wider than the viewport scroll horizontally without shrinking windows; focused windows scroll into view. Empty rows are hidden so the scroll container does not intercept HUD or world input.

Placement changes are immediate. Dragging, resizing, modal dialogs, and item dragging are deferred.

## Adding features

Add a window identity and factory case to introduce another window. Keep window navigation inside the UI. The registry supports nonblocking windows, although all current factory cases use blocking windows.

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
