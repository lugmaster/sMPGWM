extends SceneTree

var failures := 0

class ReleaseConsumer extends Control:
    var release_count := 0

    func _gui_input(event: InputEvent) -> void:
        if event is InputEventKey and not event.pressed:
            release_count += 1
            accept_event()

func _initialize() -> void:
    run.call_deferred()

func expect(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func settle() -> void:
    await process_frame
    await process_frame
    await process_frame

func menu_action(action: String) -> void:
    var event := InputEventAction.new()
    event.action = action
    event.pressed = true
    Input.parse_input_event(event)
    event = InputEventAction.new()
    event.action = action
    event.pressed = false
    Input.parse_input_event(event)

func menu_key(keycode: int, pressed: bool, unicode: int = 0, echo: bool = false) -> void:
    var event := InputEventKey.new()
    event.keycode = keycode
    event.physical_keycode = keycode
    event.unicode = unicode
    event.pressed = pressed
    event.echo = echo
    root.push_input(event, true)

func click_at(position: Vector2) -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = position
    motion.global_position = position
    root.push_input(motion, true)
    var event := InputEventMouseButton.new()
    event.button_index = MOUSE_BUTTON_LEFT
    event.position = position
    event.global_position = position
    event.pressed = true
    root.push_input(event, true)
    event = event.duplicate()
    event.pressed = false
    root.push_input(event, true)

func expect_row_layout(scroll: ScrollContainer, row: HBoxContainer, centered: bool = true) -> void:
    var children := row.get_children()
    for index in range(children.size()):
        var window := children[index] as Control
        expect(window.size.x > 0 and window.size.y > 0, "Window should have a visible size")
        expect(absf(window.position.y + window.size.y / 2 - row.size.y / 2) <= 1, "Window should be vertically centered")
        if index > 0:
            var previous := children[index - 1] as Control
            expect(absf(window.position.x - previous.position.x - previous.size.x - 24) <= 1, "Adjacent windows should have a 24-pixel gap")
            expect(not previous.get_rect().intersects(window.get_rect()), "Windows should never overlap")
        if centered:
            expect_window_visible(scroll, window)
    if centered and not children.is_empty():
        var first := children.front() as Control
        var last := children.back() as Control
        var center := (first.get_global_rect().position.x + last.get_global_rect().end.x) / 2
        expect(absf(center - scroll.get_global_rect().get_center().x) <= 1, "Window row should be horizontally centered")

func expect_window_visible(scroll: ScrollContainer, window: Control) -> void:
    expect(scroll.get_global_rect().grow(1).encloses(window.get_global_rect()), "Focused or fitting window should be fully visible")

func verify_menu_press_guards(game: Node, windows: Node, row: HBoxContainer) -> void:
    var canvas := game.get_node("UiCanvas")
    menu_key(KEY_ESCAPE, true, 0, true)
    expect(row.get_child_count() == 0, "Auto-repeat without an initial press should not open a menu")
    menu_key(KEY_ESCAPE, true)
    await settle()
    menu_key(KEY_ESCAPE, true, 176)
    menu_key(KEY_ESCAPE, true, 0, true)
    expect(row.get_child_count() == 1, "Duplicate and repeated Escape presses across frames should not toggle again")
    menu_key(KEY_ESCAPE, false)

    menu_key(KEY_ESCAPE, true)
    menu_key(KEY_ESCAPE, false)
    expect(row.get_child_count() == 0, "A fresh Escape press should close the menu")
    menu_key(KEY_ESCAPE, true)
    menu_key(KEY_ESCAPE, false)
    expect(row.get_child_count() == 1, "A second intentional press in the same frame should open main menu")
    canvas.call("CloseAllMenus")

    for keycode in [KEY_I, KEY_M]:
        menu_key(keycode, true)
        menu_key(keycode, true)
        menu_key(keycode, true, 0, true)
        await settle()
        expect(row.get_child_count() == 1, "Duplicate inventory/map presses should toggle only once")
        menu_key(keycode, false)
        menu_key(keycode, true)
        menu_key(keycode, false)
        expect(row.get_child_count() == 0, "Inventory/map release should allow another intentional toggle")

    menu_key(KEY_I, true)
    menu_key(KEY_M, true)
    menu_key(KEY_I, true)
    expect(row.get_child_count() == 2, "Each menu action should have its own press guard")
    menu_key(KEY_I, false)
    menu_key(KEY_M, false)
    canvas.call("CloseAllMenus")

    var consumer := ReleaseConsumer.new()
    consumer.focus_mode = Control.FOCUS_ALL
    game.add_child(consumer)
    menu_key(KEY_I, true)
    consumer.grab_focus()
    menu_key(KEY_I, false)
    expect(consumer.release_count == 1, "Focused GUI control should consume the release event")
    menu_key(KEY_I, true)
    menu_key(KEY_I, false)
    expect(row.get_child_count() == 0, "GUI-consumed releases should still reset the menu guard")
    consumer.queue_free()
    await settle()

    for notification in [Node.NOTIFICATION_APPLICATION_FOCUS_OUT, Node.NOTIFICATION_WM_WINDOW_FOCUS_OUT]:
        menu_key(KEY_ESCAPE, true)
        expect(row.get_child_count() == 1, "Escape should open a window before focus loss")
        windows.notification(notification)
        menu_key(KEY_ESCAPE, true)
        menu_key(KEY_ESCAPE, false)
        expect(row.get_child_count() == 0, "Focus loss should clear guards when key release is missed")

    menu_key(KEY_I, true)
    game.call("SetGameState", 1)
    game.call("SetGameState", 0)
    menu_key(KEY_I, true)
    menu_key(KEY_I, false)
    expect(row.get_child_count() == 1, "Disabling navigation should clear guards when key release is missed")
    canvas.call("CloseAllMenus")
    canvas.call("ToggleMenu", 0)
    expect(row.get_child_count() == 1, "Canvas facade should toggle inventory open")
    canvas.call("ToggleMenu", 0)
    expect(row.get_child_count() == 0, "Canvas facade should toggle inventory closed")
    await settle()

func run() -> void:
    create_timer(15).timeout.connect(func(): quit(1))
    var packed := load("res://scenes/game/main_game.tscn") as PackedScene
    for cycle in range(2):
        var game := packed.instantiate()
        root.add_child(game)
        await settle()
        var canvas := game.get_node("UiCanvas")
        var input := game.get_node("InputController")
        var windows := game.get_node("UiCanvas/Root/MenuLayer/MenuHost")
        var scroll := windows.get_node("WindowScroll") as ScrollContainer
        var row := scroll.get_node("WindowRow") as HBoxContainer
        var hud := canvas.get_node("%PlayerHud")
        var hotbar := hud.get_node("%HotbarControl")
        var buttons := hotbar.get_node("%ButtonContainer")
        expect(buttons.get_child_count() == 10, "Hotbar should contain ten icons")
        expect(not buttons.get_child(0).disabled, "Active session should enable hotbar")

        Input.action_press("Forwards")
        Input.action_press("Action")
        await settle()
        expect(input.get("Direction") != Vector2.ZERO, "Active session should read movement")
        expect(input.get("ActionButtonPressed"), "Active session should read attacks")

        menu_action("InventoryMenu")
        await settle()
        expect(row.get_child_count() == 1, "Inventory shortcut should open a window")
        var inventory := row.get_child(0) as Control
        expect_row_layout(scroll, row)
        var single_window_position: Vector2 = inventory.position
        expect(not buttons.get_child(0).disabled, "Inventory should keep hotbar buttons interactive for editing")
        var activation_probe = load("res://Scripts/Util/Mocks/InventoryHotbarSmokeFixture.cs").new()
        activation_probe.call("Observe", canvas)
        click_at(buttons.get_child(0).get_global_rect().get_center())
        await settle()
        expect(activation_probe.call("Activations") == 0, "Editing an enabled hotbar button must not request gameplay activation")
        activation_probe.call("StopObserving")
        click_at(Vector2(5, 5))
        await settle()
        expect(input.get("Direction") == Vector2.ZERO, "Opening inventory should clear movement")
        expect(not input.get("ActionButtonPressed"), "Opening inventory should clear attacks")
        expect(hud.process_mode != Node.PROCESS_MODE_DISABLED, "HUD processing should continue")

        menu_action("MapMenu")
        await settle()
        expect(row.get_child_count() == 2, "Map should coexist with inventory")
        var map := row.get_child(1) as Control
        expect(inventory.position.x < single_window_position.x, "Opening map should shift inventory left")
        expect_row_layout(scroll, row)
        var inventory_position := inventory.position
        var map_position := map.position

        canvas.call("OpenMenu", 0)
        expect(row.get_child_count() == 2, "Reopening inventory should reuse its instance")
        expect(row.get_child(0) == inventory, "Focusing inventory should preserve opening order")
        expect(inventory.position == inventory_position and map.position == map_position, "Focus should not move windows")
        expect(root.gui_get_focus_owner() == inventory.get_node("%CloseButton"), "Reopening should focus the selected window")

        canvas.call("OpenMenu", 1)
        click_at(inventory.get_global_rect().position + Vector2(8, 8))
        await settle()
        expect(root.gui_get_focus_owner() == inventory.get_node("%CloseButton"), "Clicking inventory should focus it")
        expect(inventory.position == inventory_position and map.position == map_position, "Clicking should not reorder the row")

        var panel := inventory.get_node("CenterContainer/MenuPanel") as Control
        var original_minimum := panel.custom_minimum_size
        panel.custom_minimum_size = Vector2(700, original_minimum.y)
        await settle()
        expect(inventory.size.x >= 700, "Window should report changing content minimum size")
        expect_row_layout(scroll, row)
        panel.custom_minimum_size = original_minimum
        await settle()

        var two_window_position: Vector2 = inventory.position
        canvas.call("OpenMenu", 2)
        await settle()
        expect(row.get_child_count() == 3, "All three windows should coexist")
        expect(inventory.position.x < two_window_position.x, "Opening third window should shift existing windows left")
        expect_row_layout(scroll, row)

        var original_scale_size := root.content_scale_size
        root.content_scale_size = Vector2i(1100, 1080)
        await settle()
        expect(scroll.get_h_scroll_bar().visible, "Narrow viewport should enable horizontal scrolling")
        expect_row_layout(scroll, row, false)
        expect_window_visible(scroll, row.get_child(2))
        canvas.call("OpenMenu", 0)
        await settle()
        expect_window_visible(scroll, inventory)
        expect(row.get_child(0) == inventory, "Scrolling to focused window should preserve row order")
        canvas.call("OpenMenu", 2)
        await settle()
        expect_window_visible(scroll, row.get_child(2))
        root.content_scale_size = original_scale_size
        await settle()
        expect(not scroll.get_h_scroll_bar().visible, "Wider viewport should remove unnecessary scrollbar")
        expect_row_layout(scroll, row)

        canvas.call("CloseMenu", 1)
        await settle()
        expect(row.get_child_count() == 2, "Closing middle window should leave two windows")
        expect_row_layout(scroll, row)
        canvas.call("OpenMenu", 1)
        await settle()
        map = row.get_child(2) as Control
        expect(map != inventory, "Reopening map should append a new instance")
        expect_row_layout(scroll, row)
        canvas.call("CloseMenu", 2)
        await settle()
        expect(row.get_child(0) == inventory and row.get_child(1) == map, "Closing main menu should preserve remaining order")
        expect_row_layout(scroll, row)

        inventory.get_node("%CloseButton").emit_signal("pressed")
        await settle()
        expect(row.get_child_count() == 1 and row.get_child(0) == map, "Close button should close its own window")
        expect(buttons.get_child(0).disabled, "Remaining map should keep hotbar blocked")
        expect_row_layout(scroll, row)

        canvas.call("OpenMenu", 0)
        canvas.call("OpenMenu", 2)
        await settle()
        expect(row.get_child_count() == 3, "All three windows should be open before Escape")
        menu_key(KEY_ESCAPE, true, 94)
        menu_key(KEY_ESCAPE, true)
        menu_key(KEY_ESCAPE, false)
        await settle()
        expect(row.get_child_count() == 0, "Duplicate non-repeat Escape presses should close all windows without opening main menu")
        expect(not buttons.get_child(0).disabled, "Closing last window should enable hotbar")
        expect(not scroll.visible, "Empty scroll container should not intercept gameplay or HUD input")
        expect(input.get("Direction") != Vector2.ZERO, "Movement should resume from held WASD input")
        expect(not input.get("ActionButtonPressed"), "Held attack should stay cleared after closing windows")
        Input.action_release("Action")
        await settle()
        Input.action_press("Action")
        await settle()
        expect(input.get("ActionButtonPressed"), "Attack should resume after release and fresh press")
        Input.action_release("Action")
        Input.action_release("Forwards")
        await settle()

        click_at(buttons.get_child(0).get_global_rect().get_center())
        await settle()
        expect(canvas.call("IsPointerOverInteractiveUi"), "Hotbar should be recognized as interactive UI")
        Input.action_press("Action")
        await settle()
        expect(not input.get("ActionButtonPressed"), "Mouse input over hotbar should not attack the world")
        Input.action_release("Action")
        click_at(Vector2(8, 8))
        await settle()

        menu_action("MainMenu")
        await settle()
        expect(row.get_child_count() == 1, "Escape with no windows should open main menu")
        expect(row.get_child(0).get_script().resource_path.ends_with("/MainMenu.cs"), "Escape should open only the main menu")
        menu_action("MainMenu")
        await settle()
        expect(row.get_child_count() == 0, "Another Escape press should close the main menu")
        menu_action("MainMenu")
        await settle()
        menu_action("InventoryMenu")
        await settle()
        menu_action("InventoryMenu")
        await settle()
        expect(row.get_child_count() == 1, "Inventory shortcut should toggle only inventory")

        game.call("SetGameState", 1)
        expect(row.get_child_count() == 0, "Inactive session should close all windows")
        expect(buttons.get_child(0).disabled, "Inactive session should block hotbar")
        canvas.call("OpenMenu", 0)
        expect(row.get_child_count() == 0, "Inactive session should reject opening windows")
        menu_action("MainMenu")
        await settle()
        expect(row.get_child_count() == 0, "Escape should not open menus during an inactive session")
        game.call("SetGameState", 0)
        expect(not buttons.get_child(0).disabled, "Active session should restore hotbar")

        await verify_menu_press_guards(game, windows, row)

        canvas.call("OpenMenu", 0)
        canvas.call("OpenMenu", 1)
        await settle()
        game.queue_free()
        await settle()

    if failures == 0:
        print("UI architecture smoke checks passed (two scene lifecycles).")
    quit(0 if failures == 0 else 1)
