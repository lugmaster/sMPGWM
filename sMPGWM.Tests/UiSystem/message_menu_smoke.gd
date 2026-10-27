extends SceneTree

var failures := 0
var ticks := 0

class WorldProbe extends Node:
    var steps := 0
    func _physics_process(_delta: float) -> void:
        steps += 1

func _initialize() -> void:
    run.call_deferred()

func expect(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func settle() -> void:
    for frame in range(8):
        await process_frame
        ticks += 1

func action(name: String) -> void:
    var event := InputEventAction.new()
    event.action = name
    event.pressed = true
    root.push_input(event, true)
    event = event.duplicate()
    event.pressed = false
    root.push_input(event, true)

func key(code: int) -> void:
    var event := InputEventKey.new()
    event.keycode = code
    event.physical_keycode = code
    event.pressed = true
    root.push_input(event, true)
    event = event.duplicate()
    event.pressed = false
    root.push_input(event, true)

func click(button: Control) -> void:
    var point := button.get_global_rect().get_center()
    var motion := InputEventMouseMotion.new()
    motion.position = point
    if OS.get_cmdline_user_args().has("--capture-visuals"):
        root.warp_mouse(point)
    root.push_input(motion, true)
    await process_frame
    var event := InputEventMouseButton.new()
    event.position = point
    event.global_position = point
    event.button_index = MOUSE_BUTTON_LEFT
    event.pressed = true
    root.push_input(event, true)
    event = event.duplicate()
    event.pressed = false
    root.push_input(event, true)
    await settle()

func capture(label: String) -> void:
    if not OS.get_cmdline_user_args().has("--capture-visuals"):
        return
    await RenderingServer.frame_post_draw
    var path := "res://../artifacts/messages-%s-%dx%d.png" % [label, root.size.x, root.size.y]
    expect(root.get_texture().get_image().save_png(path) == OK, "Save capture " + path)

func run() -> void:
    create_timer(55).timeout.connect(func(): quit(1))
    var packed := load("res://scenes/game/main_game.tscn") as PackedScene
    for resolution in [Vector2i(1920, 1080), Vector2i(2560, 1440)]:
        root.size = resolution
        root.content_scale_size = resolution
        var game := packed.instantiate()
        root.add_child(game)
        await settle()
        var world_probe := WorldProbe.new()
        game.get_node("WorldViewPortContainer/WorldViewport/WorldScene").add_child(world_probe)
        expect(game.get_node("WorldViewPortContainer/WorldViewport").size == Vector2i(1280, 720), "World viewport remains 1280 by 720")
        var fixture = load("res://Scripts/Util/Mocks/MessageSmokeFixture.cs").new()
        fixture.call("Bind", game)
        var canvas := game.get_node("UiCanvas")
        var row := canvas.get_node("Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow")
        var toolbar := canvas.get_node("%PlayerHud/%MenuToolbar")
        var notification := toolbar.get_node("ButtonContainer/NotificationContainer/NotificationIcon")
        var inventory_button := toolbar.get_node("ButtonContainer/MenuButtonContainer/InventoryButton")
        var map_button := toolbar.get_node("ButtonContainer/MenuButtonContainer/MapButton")
        var main_button := toolbar.get_node("ButtonContainer/MenuButtonContainer/MainMenuButton")
        var input := game.get_node("InputController")
        expect(fixture.call("Count") == 1 and fixture.call("Unread") == 1, "Fresh session contains exactly one unread welcome")
        expect(row.get_child_count() == 0, "Welcome must not open a menu")
        expect(notification.get_node("QuantityLabel").text == "1", "Badge includes a count of one")
        expect(notification.get_theme_color("icon_normal_color") == Color.YELLOW, "Unread icon is yellow")
        expect(toolbar.get_global_rect().end.x <= resolution.x and toolbar.get_global_rect().position.x > resolution.x / 2, "Toolbar stays at upper right")
        for button in [inventory_button, map_button, main_button, notification]:
            expect(button.icon != null and button.tooltip_text.length() > 0, "Toolbar buttons have artwork and tooltips")
        main_button.grab_focus()
        expect(main_button.has_focus() and main_button.get_theme_stylebox("focus") != null, "Toolbar supports visible keyboard focus")
        var hover := InputEventMouseMotion.new()
        hover.position = notification.get_global_rect().get_center()
        root.push_input(hover, true)
        Input.action_press("Action")
        await settle()
        expect(not input.get("ActionButtonPressed"), "Hovering toolbar suppresses gameplay attacks")
        Input.action_release("Action")
        await capture("welcome")
        await click(notification)
        expect(row.get_child_count() == 1 and fixture.call("Read", "welcome"), "Pointer opens and reads welcome")
        var menu := row.get_child(0)
        expect(menu.get_node("%TitleLabel").text == "Messages", "Inherited title is Messages")
        expect(fixture.call("Unread") == 0 and not notification.get_node("QuantityLabel").visible, "Last read hides badge")
        expect(notification.get_theme_color("icon_normal_color") == Color.GRAY, "Zero unread icon is grey")
        fixture.call("Receive", "ordinary-one", false)
        fixture.call("Receive", "ordinary-two", false)
        await settle()
        expect(fixture.call("Selected") == "Welcome" and fixture.call("Unread") == 2, "Open arrivals neither select nor read")
        action("MessageMenu")
        await settle()
        expect(fixture.call("Selected") == "ordinary-two" and fixture.call("Unread") == 1, "Explicit open reads only first unread")
        var group := menu.get_node("%SenderList").get_child(0)
        await click(group.get_node("SenderButton"))
        expect(not group.get_node("MessageList").visible, "Header folds sibling rows")
        fixture.call("Receive", "ordinary-three", false)
        await settle()
        group = menu.get_node("%SenderList").get_child(0)
        expect(not group.get_node("MessageList").visible and fixture.call("Unread") == 2, "Arrival preserves folding and unread state")
        await click(notification)
        expect(fixture.call("Selected") == "ordinary-three" and fixture.call("Unread") == 1, "Visible notification focuses first unread")
        expect(menu.get_node("%SenderList").get_child(0).get_node("MessageList").visible, "Automatic selection expands its group")
        await click(inventory_button)
        await click(map_button)
        expect(row.get_child_count() == 3, "Inventory, Map, Messages coexist")
        canvas.call("OpenMenu", 3)
        await settle()
        await click(menu.get_node("%CloseButton"))
        expect(row.get_child_count() == 2, "Message close button closes only its own window")
        await click(notification)
        expect(row.get_child_count() == 3, "Messages can reopen alongside Inventory and Map")
        await click(main_button)
        expect(row.get_child_count() == 1 and row.get_child(0).name == "MainMenu", "Main Menu toolbar replaces all ordinary windows on first click")
        expect(not main_button.disabled, "Main Menu button stays enabled while Main Menu is open")
        await click(main_button)
        expect(row.get_child_count() == 0, "Second Main Menu click closes it")
        for request in [[inventory_button, "/InventoryMenu.cs"], [map_button, "/MapMenu.cs"], [notification, "/MessageMenu.cs"]]:
            await click(main_button)
            expect(row.get_child_count() == 1 and row.get_child(0).name == "MainMenu", "Toolbar opens Main Menu")
            await click(request[0])
            expect(row.get_child_count() == 1 and row.get_child(0).get_script().resource_path.ends_with(request[1]), "Other toolbar menus replace Main Menu on first click")
        await click(main_button)
        action("MessageMenu")
        await settle()
        expect(row.get_child_count() == 1 and row.get_child(0).name == "MessageMenu", "Messages shortcut replaces Main Menu")
        var inventory_fixture = load("res://Scripts/Util/Mocks/InventoryClickSmokeFixture.cs").new()
        inventory_fixture.call("Show", game)
        await settle()
        var inventory_menu := row.get_child(0)
        var grid := inventory_menu.get_node("%InventoryContainer/%ItemGrid/%SlotGrid")
        var revision: int = inventory_fixture.call("Revision")
        await click(grid.get_child(0))
        expect(inventory_fixture.call("Carrying"), "Inventory fixture begins click carrying")
        await click(notification)
        expect(row.get_child_count() == 2 and not inventory_fixture.call("Carrying"), "Toolbar activates and cancels carrying on first click")
        expect(inventory_fixture.call("Revision") == revision, "Toolbar navigation cannot transfer carried items")
        expect(not canvas.get_node("%ItemPointerOverlay").visible, "Navigation clears the item preview")
        canvas.call("CloseAllMenus")
        canvas.call("OpenMenu", 0)
        await settle()
        inventory_menu = row.get_child(0)
        grid = inventory_menu.get_node("%InventoryContainer/%ItemGrid/%SlotGrid")
        var source: Vector2 = grid.get_child(0).get_global_rect().get_center()
        var motion := InputEventMouseMotion.new()
        motion.position = source
        root.push_input(motion, true)
        var press := InputEventMouseButton.new()
        press.position = source
        press.button_index = MOUSE_BUTTON_LEFT
        press.pressed = true
        root.push_input(press, true)
        await process_frame
        motion = InputEventMouseMotion.new()
        motion.position = source + Vector2(28, 0)
        motion.relative = Vector2(28, 0)
        motion.button_mask = MOUSE_BUTTON_MASK_LEFT
        root.push_input(motion, true)
        await settle()
        expect(root.gui_is_dragging(), "Inventory fixture begins native drag")
        action("MessageMenu")
        await settle()
        expect(not root.gui_is_dragging() and not inventory_fixture.call("Carrying"), "Message shortcut cancels native drag")
        expect(inventory_fixture.call("Revision") == revision, "Native drag navigation cannot transfer items")
        press.pressed = false
        root.push_input(press, true)
        inventory_fixture.call("Restore", game)
        inventory_fixture = null
        canvas.call("OpenMenu", 2)
        fixture.call("Receive", "required-one", true)
        fixture.call("Receive", "required-two", true)
        await settle()
        menu = row.get_child(0)
        expect(row.get_child_count() == 1 and fixture.call("Selected") == "required-one", "Blocking arrival preempts Main Menu and queues successors")
        expect(fixture.call("Read", "required-one") and not fixture.call("Read", "required-two"), "Only displayed blocker becomes read")
        expect(not menu.get_node("%CloseButton").visible and menu.get_node("%AcknowledgeButton").visible, "Required message offers only OK")
        expect(canvas.get_node("Root/MenuLayer/ModalBlocker").visible, "Required message retains the full-screen input blocker")
        for button in [main_button, inventory_button, map_button, notification]:
            await click(button)
        expect(row.get_child_count() == 1 and fixture.call("Selected") == "required-one", "Required message still blocks every toolbar button")
        Input.action_press("Forwards")
        Input.action_press("Action")
        await settle()
        expect(input.get("Direction") == Vector2.ZERO and not input.get("ActionButtonPressed"), "Acknowledgement blocks local controls")
        var prior_ticks: int = world_probe.steps
        expect(not paused and game.get("GameState") == 0, "World and active session keep running")
        await settle()
        expect(world_probe.steps > prior_ticks, "World physics continues during acknowledgement")
        for method in ["CloseAllMenus", "CloseMenu", "ToggleMenu", "OpenMenu"]:
            if method == "CloseAllMenus": canvas.call(method)
            else: canvas.call(method, 2)
        canvas.call("CloseMenu", 3)
        canvas.call("ToggleMenu", 3)
        key(KEY_ESCAPE)
        key(KEY_I)
        action("MessageMenu")
        key(KEY_ENTER)
        fixture.call("Receive", "ordinary-during-required", false)
        await settle()
        expect(row.get_child_count() == 1 and fixture.call("Selected") == "required-one", "All dismiss/replace paths preserve required content")
        await capture("blocking")
        await click(menu.get_node("%AcknowledgeButton"))
        expect(fixture.call("Selected") == "required-two" and input.get("Direction") == Vector2.ZERO, "First OK advances without releasing controls")
        await click(menu.get_node("%AcknowledgeButton"))
        expect(row.get_child_count() == 0, "Final OK closes mandatory presentation")
        expect(input.get("Direction") != Vector2.ZERO and not input.get("ActionButtonPressed"), "Movement resumes but held attack requires a new press")
        Input.action_release("Forwards")
        Input.action_release("Action")
        canvas.call("OpenMenu", 0)
        canvas.call("OpenMenu", 1)
        fixture.call("Receive", "ordinary-interruption", true)
        await settle()
        menu = row.get_child(0)
        expect(row.get_child_count() == 1 and fixture.call("Selected") == "ordinary-interruption", "Blocking arrival also preempts ordinary windows")
        await click(menu.get_node("%AcknowledgeButton"))
        expect(row.get_child_count() == 0, "Acknowledgement never restores interrupted menus")
        fixture.call("Populate")
        action("MessageMenu")
        await settle()
        menu = row.get_child(0)
        expect(menu.get_node("%SenderList").get_child_count() == 5, "Stable IDs separate same-name senders")
        var long_row := menu.get_node("%SenderList").get_child(2).get_node("MessageList").get_child(0)
        await click(long_row)
        var body := menu.get_node("%MessageEntry/BodyScrollContainer") as ScrollContainer
        expect(body.get_v_scroll_bar().max_value > body.size.y, "Long body scrolls within bounded pane")
        body.scroll_vertical = 150
        fixture.call("Receive", "unrelated", false)
        await settle()
        expect(body.scroll_vertical == 150, "Unrelated arrival preserves detail scrolling")
        await capture("groups")
        fixture.call("PrepareReadHistory")
        await settle()
        body.scroll_vertical = 150
        var retained_subject: String = fixture.call("Selected")
        canvas.call("CloseAllMenus")
        action("MessageMenu")
        await settle()
        menu = row.get_child(0)
        body = menu.get_node("%MessageEntry/BodyScrollContainer")
        expect(fixture.call("Selected") == retained_subject and body.scroll_vertical == 150, "Reopening read history preserves selection and body scroll")
        canvas.call("CloseAllMenus")
        fixture.call("LargeCount")
        await settle()
        expect(notification.get_node("QuantityLabel").text.ends_with("K"), "Large badge uses shared compact formatter")
        expect(notification.tooltip_text.contains(str(fixture.call("Unread"))), "Tooltip retains exact count")
        await capture("large-count")
        fixture.call("BindEmpty")
        action("MessageMenu")
        await settle()
        menu = row.get_child(0)
        expect(menu.get_node("%SenderList").get_child(0).text == "No messages", "Explicit empty store clears sender content")
        expect(menu.get_node("%MessageEntry/Heading").text == "No message selected", "Explicit empty store clears details")
        await capture("empty")
        fixture.call("Restore")
        await settle()
        expect(menu.get_node("%MessageEntry/Heading").text != "No message selected", "Rebinding reapplies initial selection")
        canvas.call("CloseAllMenus")
        game.call("SetGameState", 1)
        action("MessageMenu")
        await click(notification)
        expect(row.get_child_count() == 0, "Inactive session ignores key and toolbar")
        game.call("SetGameState", 0)
        fixture.call("Receive", "teardown-required", true)
        await settle()
        game.queue_free()
        await settle()
        fixture = null
    print("Message smoke failures: ", failures)
    quit(1 if failures else 0)
