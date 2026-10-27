# Message menu and HUD toolbar implementation plan

Status: implemented and verified on 2026-10-27. MSG01 through MSG07 are complete.

Starting point: `6e7e7ea314da28ca9ab04747dde86bec08b9fe9a` (`MessageSystem init.`), which is the inspected HEAD. Build on its scenes and scripts. Existing UI behavior is documented in [UI and player input](../../sMPGWM.Game/Scripts/UiSystem/README.md).

## Epic goal

Give the player a toolbar in the upper-right corner of `PlayerHud` for existing menu actions and typed notifications. Deliver its first notification type, unread messages, together with a message menu containing collapsible sender groups on the left and the selected message on the right.

The toolbar and keyboard use the same menu action rules. Message content and read status belong to a game-owned store for the current game session; opening or closing a menu must not recreate them. Views own selection, group expansion, and scrolling. Starting a new game clears all messages and browsing state; messages are not persisted across sessions.

## Scope and proposed defaults

Confirmed requirements are the upper-right toolbar, menu buttons followed by a separator and notification icons when read from right to left, Map/Inventory/Main Menu actions, typed notifications, an unread-count badge inside the messages icon, grey/yellow notification states, and a two-pane message menu with foldable sender groups.

The following message behavior is confirmed:

- Messages have two types: blocking and nonblocking.
- A nonblocking arrival updates the notification without opening a menu, changing selection, or affecting gameplay.
- Opening Messages through its menu button, input action, or notification icon selects the first unread message, displays its content, and marks that message read. Other unread messages require individual selection. Displaying content in the menu content area is the read transition; folding a group or merely receiving a message never reads it.
- A blocking arrival closes every existing menu and opens the message menu with that message displayed. Only clicking the footer's `OK` button acknowledges it and permits local player controls to resume, subject to the remaining acknowledgement queue and input policy. Reading and acknowledgement are separate states.
- The target is multiplayer: the world continues running during all message presentation, including blocking messages. Blocking suspends only local player controls; it never pauses the simulation or other players. The message UI remains responsive. World activity can continue to affect the player while they read.
- Unrecoverable errors may use this blocking presentation later; error handling and recovery are outside this epic.
- Message lifetime is session-based. Each new game clears prior messages, read flags, pending acknowledgements, and message view state, then adds exactly one unread, nonblocking test message. Use subject `Welcome`, body `Welcome, this is test message`, and a stable system sender such as `System`. This startup message updates the notification to yellow with a count of one without opening Messages or interrupting gameplay. Opening or closing menus never reseeds it. Save/load persistence is outside this epic.

First unread means the first unread in the displayed sender/message order.

Use these proposed defaults for remaining presentation details unless changed during refinement:

- Add an identically named `MessageMenu` Input Map action with an initially unbound shortcut. This preserves the enum/action convention without choosing an unsolicited key binding.
- Sender groups sort by display name, then stable sender ID. Messages within each group show newest first, with arrival sequence as a deterministic tie-breaker.
- Each explicit open selects the first unread message again, including when the menu is already visible. If none is unread, retain the previous valid selection, otherwise select the newest message. Refreshes never automatically advance to another unread message after marking the selected message read.
- New groups start expanded. Automatically selecting a message expands its group. Collapsing the selected message's group keeps the detail visible.

The supplied wireframes establish structure and visual hierarchy. Adapt them to the existing game theme. The unread marker shown on a selected row in the reference becomes read after display under the confirmed read rule.

Exclude message composition, replies, deletion, attachments, mission execution, mark-all-read, search/filtering, new notification types, and custom controller navigation from this epic. A mission-related message remains informational text.

## Toolbar specification

Preserve the starter scene's order. Left to right it is:

```text
[Messages + unread badge] | [Inventory] [Map] [Main Menu]
```

From the right edge, the order is Main Menu, Map, Inventory, separator, Messages. Keep the toolbar anchored to the upper-right HUD edge and growing leftward. Use the current theme's medium icon sizing and padding, with usable button targets and a centered vertical separator. The starter's 560 by 48 anchor is a layout starting point; final size must follow combined minimum sizes rather than stretch the icons.

All toolbar buttons use icons rather than visible menu-name text. Each icon has a tooltip naming the menu it opens: `Inventory`, `Map`, `Main Menu`, or `Messages`. The Messages notification tooltip also includes its unread count as specified below. Include configured shortcut text where available; tooltips must not claim that an unbound action has a key. Use recognizable artwork and maintain a clear keyboard focus indicator. Decorative children and badges ignore mouse input; only button targets intercept input. Empty anchor space must leave world input available.

Menu navigation depends on active-session navigation availability, separately from gameplay control availability. Opening any blocking menu must not disable the toolbar simply because movement/combat is blocked. Main Menu's existing modal blocker remains above the HUD, so toolbar clicks cannot pass through it.

The messages notification and Messages button perform an open/focus request with first-unread selection, rather than toggling the menu closed. The Messages input action performs the same request. Inventory, Map, and Main Menu retain their existing action rules.

### Shared menu action behavior

Extract the action decision currently inside `UiWindowManager._UnhandledInput` into one internal activation operation. Keyboard dispatch retains input-specific guards and invokes that operation; toolbar requests reach it through `MenuToolbarControl` to `PlayerHud` to `UiCanvas`. Keep `UiCanvas`'s four public menu operations unchanged. Do not synthesize key events or change the meaning of programmatic `OpenMenu` and `ToggleMenu`.

| State | Inventory or Map action | Messages action | Main Menu action |
| --- | --- | --- | --- |
| Active session, no window open | Open the requested window | Open and display first unread | Open Main Menu |
| Active session, ordinary windows open | Toggle only the requested window | Open/focus and display first unread | Close every open window |
| Main Menu open | Reject ordinary menu actions | Reject | Close every open window |
| Blocking message awaiting OK | Reject | Keep required message displayed | Ignore; cannot dismiss the message |
| Inactive session | Ignore | Ignore | Ignore |

During ordinary browsing, Inventory, Map, and Messages can coexist. Every one blocks player controls without pausing the world. Their close buttons close only their own window. Escape retains its existing close-all/open-Main-Menu behavior except during mandatory message acknowledgement. Opening Main Menu programmatically remains exclusive and closes ordinary windows, except while a blocking message is awaiting OK.

### Blocking message acknowledgement

A blocking arrival takes precedence over every ordinary menu, including Main Menu. Cancel pending inventory click/drag interactions, close all windows, and open the message menu with the required message selected. The modal input barrier covers the HUD and underlying game. Hide or disable the close control and show an `OK` button in the inherited footer. Prevent Escape, menu shortcuts, toolbar actions, outside clicks, and ordinary programmatic close/toggle requests from dismissing or replacing this window. Session teardown remains able to release it safely.

Keep the required message displayed until OK; disable browsing other messages during acknowledgement. Mark the message read when its content is shown, but retain the mandatory block independently of its read flag. A notification count reaching zero cannot restore local player controls. Only the footer button's click acknowledges the current blocking message; shortcut dismissal or implicit acknowledgement through close is not permitted.

If more blocking messages arrive, queue them by arrival sequence without replacing the current content. Each OK acknowledges one message and displays the next; allow local player controls to resume only after the queue is empty and the input policy permits it. Do not restore menus that were closed by the interruption. Nonblocking arrivals during this flow update stored messages and counts without stealing the required selection.

The implementation must represent pending acknowledgement independently from ordinary window blocking. Releasing it restores local player controls only if the surrounding game/session policy permits it. Keep the session active and world processing running throughout acknowledgement; do not pause the scene tree or request a multiplayer simulation pause. Do not equate closing windows with permission to restore controls or reuse an inactive-session transition that would disable the required UI. Networking implementation is outside this epic.

Keyboard auto-repeat and duplicate press suppression remain at the keyboard boundary, with release tracking and focus-loss clearing. Shortcut letters remain available to focused text fields. Clicking a toolbar button deliberately activates it even if a text field previously had focus. Consume pointer activation so it cannot also trigger gameplay attacks or item placement.

### Notification contract

Represent notification identity with a small typed value, initially `UnreadMessages`. Map it to its icon, tooltip, state, count, and target menu in UI composition. `NotificationIcon` accepts presentation data and emits activation; it does not query message services or decide menu policy. Adding a future type should not require a second toolbar implementation.

| Total unread messages | Icon state | Badge | Tooltip |
| --- | --- | --- | --- |
| 0 | Grey | Hidden | Messages, no unread messages |
| 1 or more | Yellow | Unread count | Messages, exact unread count |

The icon remains visible and clickable at zero. Use the existing stacked-item count placement and formatting conventions, including a visible count of one and compact large values. Keep exact totals in the tooltip. Apply state color to the artwork, preserving badge readability, focus styling, and normal hover feedback. Color and the numerical badge jointly communicate unread state.

## Message menu specification

Use the inherited overlay title and ordinary close control, with the visible title `Messages`. Blocking presentation replaces dismissal with the footer OK control as specified above. Preserve the supplied two-pane split: independently scrollable sender list on the left, and heading, sender attribution, separator, and scrollable body on the right. Use wrapping text and bounded pane dimensions so long content does not enlarge the menu beyond the existing window area. Keep horizontal scrolling disabled and long titles readable through wrapping or ellipsis with a full-text tooltip.

Each sender group has a full-width header button containing an expansion chevron, sender name, and unread total. Hide a zero unread total. The message list is a sibling below the header, indented under it. Collapsing the group hides only its rows. Each message row shows its title and a distinct unread marker; unread rows also use stronger text emphasis. Selection has a separate highlight that survives the unread-to-read transition.

Selecting a row renders that message's subject, sender display name, and body, then marks only that message read. Update row styling, sender unread count, and toolbar total from the same committed state. Re-selecting a read message is a no-op for read status. Reading the final unread message immediately returns the icon to grey and hides its badge.

On a genuinely new selection, reset the detail body's scroll to the top. Preserve its scroll during unrelated arrivals or count updates. Retain sender-list scrolling during updates and clamp it when content shrinks. Preserve group expansion on reopening, expanding and revealing the first unread selection as needed. Restore previous selection only when no unread message exists. Clear retained view state when starting a new game.

With no messages, retain both panes: show `No messages` on the left and `No message selected` on the right. With unavailable/unbound data, clear stale content and show a neutral empty state. If a restored selected ID is absent after rebinding, apply the initial selection rule. Preserve selection by stable ID through reordering and refreshes.

Incoming nonblocking messages while closed update the toolbar immediately. While open, add the appropriate row and update totals without changing selection or reading the arrival. Do not expand a deliberately collapsed group because a new nonblocking message arrived. Blocking arrivals use the mandatory presentation flow. Message bodies are plain display text in this scope.

## Ownership and integration

Names below are implementation guidance; responsibilities and behavior are the contract.

| Owner | Responsibility |
| --- | --- |
| New `Scripts/MessageSystem` | Plain C# player message store, stable message/sender identities, immutable records and snapshots, receive and mark-read operations, change events. No Godot or UI dependency. |
| `GamePlayManager` | Creates a fresh message store per game session, seeds the single welcome test message, supplies arrivals, binds/unbinds UI, owns acknowledgement gating, and tears down session state. No new autoload or static store. |
| `UiCanvas` | Internal bind/unbind lifecycle, passes store/view state to message windows before tree entry, keeps HUD notification state current while the menu is closed, forwards menu activation. |
| `PlayerHud` and `MenuToolbarControl` | Present menu controls and typed notifications; forward requests independently of gameplay-input gating. |
| Existing `MessageStackSystem` view scripts | `MessageMenu` coordinates rendering and selection; `SenderGroup` manages its header and rows; `MessageListItem` presents a message; `MessageEntry` presents details. |
| `NotificationIcon` | Generic count, color, tooltip, sizing, and activation presentation. |
| `UiWindowManager` and provider | Create/register `MessageMenu`, apply shared action rules, focus/scroll windows, and retain existing input policy. |

A message needs stable message ID, stable sender ID, sender display name, subject, body, received time, arrival sequence, message type (`Blocking` or `NonBlocking`), and read status. Track required acknowledgement separately from read state. Sender grouping uses ID rather than display text, so two senders may share a name. Keep sender display metadata consistent per ID through one sender descriptor. Reject missing identities or unknown senders before publishing; an empty body is valid. Receive creates unread messages. A duplicate message ID is a rejected no-op and cannot overwrite text, reset read state, or enqueue another acknowledgement. Marking an unknown/already-read message is a no-op.

Commands run synchronously on the game thread. Publish a complete read-only snapshot only after committing changes, so observers see consistent message flags and totals. Derive sender and global unread totals from that state. Prevent nested read notifications from corrupting refresh: defer the selected-message read until the current view refresh completes or guard/coalesce refreshes explicitly. Reuse existing project conventions for observer error handling. No transaction framework or command bus is needed.

Bind the HUD once for live notification changes. Bind each message window on `WindowOpening`, and detach it on closure. `UiCanvas` owns lightweight message view state so recreated windows retain browsing context while honoring first-unread selection on each open. Unbind old data before rebinding, remove all handlers on teardown, and clear stale notification state. No messages, pending acknowledgements, view state, or event handlers carry forward to a new game session. Seed the welcome test message once at game startup through the normal nonblocking receive path; the HUD's initial render must reflect it even if binding occurs after seeding.

## Starter corrections

These are part of implementation, before adding runtime behavior:

- Add `MessageMenu` to `UiWindowId`, the factory switch, and Input Map. Add a provider factory pointing at the existing `res://scenes/ui/player_hud/message_menu.tscn`.
- Replace the stale `CreateMessageStack`/message-stack paths in `GameOverlayProvider` with factories for the actual menu and child scenes under `scenes/ui/menu/game_overlay_menu/message_menu/`. Check callers before removing old members. Correct or remove the stale `FilePaths.MessageEntryScene` path.
- Align `MessageEntry` with its existing `VBoxContainer` scene root and attach its script; use a child panel for styling if needed. Add the sender attribution and separator.
- Move `SenderGroup/MessageList` out from under `SenderButton`, leaving header and list as sibling children of the group.
- Correct `TitelLabel` to `TitleLabel`, give row content a proper filling container layout, and make decorative children ignore pointer input.
- Replace editor sample groups/rows with bound runtime data. Add stable node references following the existing scene conventions.
- Implement the TODO controls and wire the existing `MenuToolbarAnchor`/toolbar into `PlayerHud`. Reuse shared icon sizing and quantity formatting without introducing item-domain dependencies.

## Implementation stories

### MSG01 Repair starter scenes and register the menu

Depends on: none. Correct the scene/script mismatches and paths, add provider factories, register `MessageMenu` and its action, and establish bounded two-pane layout.

Acceptance: the actual menu and all child scenes instantiate without missing resources or script/root type errors; the menu opens through `UiCanvas`, shows the inherited title/close control, and closes through the existing manager. Existing window/action alignment tests pass.

### MSG02 Add message state and arrival types

Depends on: none; can precede MSG01. Implement identities, sender descriptors, immutable messages/snapshots, blocking/nonblocking types, receive, mark-read, and deterministic ordering. Wire per-session ownership and internal binding lifecycle. Start each new game with a fresh store and exactly the specified unread, nonblocking welcome test message. Do not seed additional messages outside opt-in test fixtures; omit save/load persistence.

Acceptance: new messages start unread; duplicate IDs cannot duplicate counts or acknowledgement requests; marking read twice emits one change; sender totals sum to the global total; snapshots cannot mutate the store. Each new game contains exactly one unread, nonblocking welcome test message and no previous messages. It does not open a menu or affect gameplay. Reading it and reopening Messages cannot reseed it. Tests cover both types, invalid inputs, same-name senders, deterministic timestamp ties, and fresh state across game sessions.

### MSG03 Share keyboard and toolbar menu activation

Depends on: MSG01. Introduce the internal common activation operation, preserve the public canvas boundary, wire menu-button events, and supply icons/tooltips and navigation availability.

Acceptance: every toolbar button displays an icon and has a tooltip naming its menu. The action table holds for both input sources; clicking Main Menu with Inventory open closes Inventory and leaves Main Menu closed. A second activation opens Main Menu. Main Menu blocks ordinary requests. Keyboard press/release guards and text-entry behavior remain intact. Toolbar clicks neither attack nor place a carried item.

### MSG04 Render grouped messages and details

Depends on: MSG01, MSG02. Implement groups, row selection, detail binding, unread transitions, empty states, stable ordering, and view-state restoration. Subscribe before the first render and detach on every close/rebind path.

Acceptance: opening through button, key, or notification selects and reads exactly the first unread message. Refreshing after that read never reads the next message automatically. Remaining unread messages require individual selection. Folding never reads messages; refreshes preserve selection; nonblocking arrivals do not steal it; long bodies scroll independently; changing selection resets detail scroll; unrelated arrivals preserve it. Rebinding clears stale content and renders the newly bound store.

### MSG05 Add the typed unread notification

Depends on: MSG02, MSG03, MSG04. Implement `UnreadMessages` presentation and activation, using the existing badge convention and bound store state. Keep the notification subscription alive independently of message-window lifetime.

Acceptance: zero is grey with no badge; one is yellow with `1`; larger totals use shared compact formatting and exact tooltips. Nonblocking arrival while closed updates immediately without affecting gameplay. Reading the last unread message clears yellow and the badge. Clicking opens/focuses Messages with first-unread selection and also works at zero. Badge input cannot steal button clicks.

### MSG07 Enforce blocking message acknowledgement

Depends on: MSG01 through MSG04. Implement required-message presentation, menu preemption, modal navigation guards, explicit footer acknowledgement, and the arrival-ordered queue. Keep read and acknowledgement state separate and integrate the local player control gate with gameplay policy while the world continues running. Keep this enforcement below individual buttons so public canvas calls cannot bypass it.

Acceptance: a blocking arrival closes Map, Inventory, or Main Menu and displays itself. Its content becomes read while local player controls remain suspended and the world continues running. Close, Escape, toolbar, key actions, and programmatic navigation cannot dismiss it. Only clicking footer OK acknowledges it. Multiple queued messages require separate acknowledgements and prevent early restoration of controls. A nonblocking arrival cannot replace the displayed blocking message. Verify world processing advances and message UI remains responsive throughout acknowledgement, including queued messages. Teardown cannot leave a control gate or subscriptions attached. Unrecoverable-error handling remains excluded.

### MSG06 Verify integration and document behavior

Depends on: MSG01 through MSG05 and MSG07. Add focused state tests and Godot scene/input smoke coverage; extend existing UI regressions where the same invariants already live. Add an opt-in fixture with Station Command, Trading Guild, and Security Office examples matching the wireframe, plus empty, same-name sender, long-text, large-count, and blocking-interruption cases. Update the UI README with the new action, acknowledgement gate, and binding lifecycle.

Acceptance: all checks below pass and captured UI images show the toolbar and message menu at both supported test resolutions. Record exact commands, outcomes, and any remaining limitations when implementation is complete.

Delivery order: MSG01 and MSG02; MSG03 and MSG04; MSG05 and MSG07; MSG06. Story IDs remain stable even though MSG07 precedes final integration checks.

## Verification and completion criteria

Run `dotnet build sMPGWM.sln` and `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj`. Preserve the four-method `UiCanvas` boundary test. Extend the UI architecture, icon-sizing, and inventory smoke checks and add a message-menu smoke fixture as needed, using the project's Godot .NET executable.

Verify actual pointer input and keyboard input, rather than only calling menu methods. Cover coexistence and wrapping of Inventory/Map/Messages, focus visibility, closing one versus all windows, Main Menu exclusivity, inactive-session rejection, held/repeated keys, focused text entry, and mouse attack suppression over the toolbar. Exercise menu activation during inventory click/native drag carrying to ensure navigation never submits an item transfer or leaves an invalid preview behind.

Check nonblocking arrivals both closed and open, read/count consistency, folding with unread children, zero unread, first-unread selection on every open, absence of automatic read cascades, empty data, long text, and compact badge values. Exercise blocking interruption while ordinary menus and Main Menu are open, footer-only acknowledgement, bypass attempts, queued blockers, and nonblocking arrivals during acknowledgement. Across two game sessions, verify each starts with only its single unread, nonblocking welcome test message, a yellow notification count of one, and no automatically opened message menu. Assert clean rebinding and teardown with no duplicate handlers, retained messages, pending acknowledgements, or stale browsing state. Use an explicitly empty store for empty-state fixtures.

Use UI viewports of 1920x1080 and 2560x1440 with the existing 1280x720 world viewport. Inspect rendered captures for upper-right placement, grey/yellow artwork, badge readability, separator alignment, row/selection/unread distinction, clipping, scroll behavior, and compatibility with the other HUD elements. Keep the existing shared-canvas ordering and menu-area anchors.

The epic is complete when all seven stories meet their acceptance criteria, the regressions pass, and the supplied scenes form a usable toolbar/message flow with the agreed message lifetime and mandatory acknowledgement behavior.


## Implementation completion record (2026-10-27)

All seven stories are implemented. `MessageStore` publishes immutable committed snapshots, and gameplay owns a separate arrival-ordered acknowledgement queue and local-control gate. The HUD uses the shared icon sizing and quantity formatter. The repaired scenes bind runtime groups and details, preserve browsing state, and share menu activation policy with keyboard input. `UiCanvas` retains its four public operations. No new autoload, simulation pause, save persistence, or normal-startup fixture messages were added.

Verification used this project's Godot .NET/Steam executable:

```powershell
$godotExecutable = 'C:/Users/lober/godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe'
dotnet build sMPGWM.sln
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/ui_architecture_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/icon_sizing_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/inventory_smoke.gd
& $godotExecutable --headless --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/message_menu_smoke.gd
& $godotExecutable --path sMPGWM.Game --script ../sMPGWM.Tests/UiSystem/message_menu_smoke.gd -- --capture-visuals
```

| Check | Outcome |
| --- | --- |
| Solution build | Passed, zero errors; existing nullable/source-generator warnings remain. |
| NUnit suite | Passed, 416 tests including message state, ordering, validation, snapshot immutability, observer isolation, acknowledgement queue, independent control gating, and the four-method canvas boundary. |
| UI architecture | Passed across two scene lifetimes, including key-repeat/release guards, text entry, focus, coexistence/wrapping, and Main Menu exclusivity. |
| Icon sizing | Passed at both resolutions; extended to toolbar and notification targets with 16/64/256-pixel, rectangular, and empty textures. |
| Inventory regression | Passed at both resolutions across two game lifetimes, including click/native drag, hotbar editing, cancellation, layout, and teardown. |
| Message headless and rendered smoke | Passed with zero failures at 1920x1080 and 2560x1440; world viewport remains 1280x720. |

Message smoke uses actual pointer and keyboard events. It verifies the single welcome seed in each new session; toolbar icons/tooltips/focus and attack suppression; explicit first-unread reads without cascades; live arrivals, folding, read-history reopening and scroll preservation; independent close controls and three-window coexistence; Main Menu action rules; same-name sender identities; long subjects and bodies; exact/compact counts; empty/rebound stores; inactive navigation; and teardown with a required message pending. Inventory click carrying and native drag navigation cancel without changing ownership or leaving previews. Blocking cases preempt ordinary menus and Main Menu, reject public API and shortcut dismissal attempts, require individual pointer OK acknowledgements, retain selection through nonblocking arrivals, and restore controls only after the queue drains. A physics-processing probe inside the actual world scene advances during acknowledgement while movement and combat remain blocked.

Rendered captures were generated and inspected for toolbar placement, badge colors/readability, pane clipping, long-title ellipsis, selection/unread distinction, scrolling, and compatibility with the HUD. Local evidence is in ignored `artifacts/messages-{welcome,blocking,groups,large-count,empty}-{1920x1080,2560x1440}.png`. Build/unit logs are `artifacts/message-build.log` and `artifacts/message-unit-tests.log`; regression logs are `artifacts/message-final-{architecture,icon-sizing,inventory,rendered}.log`, with the final headless message run in `artifacts/message-reopen.log`.

A separate manual gameplay playthrough was not performed. Network transport and save/load behavior remain outside this epic's scope, as planned.
