extends SceneTree

var failures := 0
const SLOT_LABELS := ["Engine", "Hull", "Shield Generator", "Reactor", "Light Weapon Slot 1", "Light Weapon Slot 2", "Heavy Weapon Slot 1", "Heavy Weapon Slot 2"]

func _initialize() -> void:
    run.call_deferred()

func expect(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func settle() -> void:
    for frame in range(6):
        await process_frame

func capture_visual(name: String) -> void:
    if not OS.get_cmdline_user_args().has("--capture-visuals"):
        return
    await RenderingServer.frame_post_draw
    var prefix := "inventory-inv10"
    for argument in OS.get_cmdline_user_args():
        if argument.begins_with("--capture-prefix="):
            prefix = argument.trim_prefix("--capture-prefix=")
    var path := "res://../artifacts/%s-%s-%dx%d.png" % [prefix, name, root.size.x, root.size.y]
    expect(root.get_texture().get_image().save_png(path) == OK, "Visual evidence must save: %s" % path)

func tooltip_visible(node: Node) -> bool:
    if node is Label and node.is_visible_in_tree() and node.text.contains("Development visual fixture."):
        return true
    for child in node.get_children(true):
        if tooltip_visible(child):
            return true
    return false

func pointer_motion(motion: InputEventMouseMotion) -> void:
    if OS.get_cmdline_user_args().has("--capture-visuals"):
        root.warp_mouse(motion.position)
    root.push_input(motion)

func pointer_click(position: Vector2, button := MOUSE_BUTTON_LEFT) -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = position
    pointer_motion(motion)
    await process_frame
    var click := InputEventMouseButton.new()
    click.position = position
    click.button_index = button
    click.pressed = true
    root.push_input(click)
    click = click.duplicate()
    click.pressed = false
    root.push_input(click)
    await settle()

func begin_drag(position: Vector2, context := "placement") -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = position
    pointer_motion(motion)
    await process_frame
    var press := InputEventMouseButton.new()
    press.position = position
    press.button_index = MOUSE_BUTTON_LEFT
    press.pressed = true
    root.push_input(press)
    await process_frame
    motion = InputEventMouseMotion.new()
    motion.position = position + Vector2(24, 0)
    motion.relative = Vector2(24, 0)
    motion.button_mask = MOUSE_BUTTON_MASK_LEFT
    pointer_motion(motion)
    await settle()
    expect(root.gui_is_dragging(), "Real mouse motion across threshold must start native dragging (%s) at %s (hover: %s)" % [context, position, root.gui_get_hovered_control()])
    if context == "placement":
        await capture_visual("drag-preview")

func finish_drag(position: Vector2) -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = position
    motion.button_mask = MOUSE_BUTTON_MASK_LEFT
    pointer_motion(motion)
    await settle()
    var release := InputEventMouseButton.new()
    release.position = position
    release.button_index = MOUSE_BUTTON_LEFT
    release.pressed = false
    root.push_input(release)
    await settle()
    expect(not root.gui_is_dragging(), "Release must end native dragging")

func verify_native_drag(game: Node, canvas: Node, row: Node) -> void:
    var previous_failures := failures
    var fixture = load("res://Scripts/Util/Mocks/InventoryClickSmokeFixture.cs").new()
    fixture.call("Show", game)
    await settle()
    var menu := row.get_child(0)
    var grid := menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    var equipment := menu.get_node("%EquipmentContainer")
    var revision: int = fixture.call("Revision")
    var presses := [0]
    grid.get_child(0).pressed.connect(func(): presses[0] += 1)
    equipment.get_child(0).get_node("%ClickableIcon").pressed.connect(func(): presses[0] += 1)
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    expect(fixture.call("Revision") == revision and fixture.call("CountAt", 0) == 1, "Native pickup must retain authoritative source")
    await finish_drag(equipment.get_child(1).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("Revision") == revision and not fixture.call("Carrying"), "Invalid native drop must cancel without mutation")
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    await finish_drag(equipment.get_child(0).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("EngineEquipped") and fixture.call("Revision") == revision + 1 and not fixture.call("Carrying"), "Native equipment drop must commit exactly once without click pickup")
    expect(presses[0] == 0, "Drag release must not emit source or target button presses")
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    await finish_drag(grid.get_child(2).get_global_rect().get_center())
    expect(fixture.call("Revision") == revision + 1 and not fixture.call("Carrying"), "Full stack native drop must end carrying without mutation")
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    await finish_drag(grid.get_child(1).get_global_rect().get_center())
    expect(fixture.call("CountAt", 1) == 5 and fixture.call("CountAt", 2) == 10 and not fixture.call("Carrying"), "Native merge must preserve overflow and end carrying")
    revision = fixture.call("Revision")
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    await finish_drag(Vector2(5, 5))
    expect(fixture.call("Revision") == revision and not fixture.call("Carrying"), "Outside drag must cancel without click pickup")
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_OUT)
    await finish_drag(grid.get_child(3).get_global_rect().get_center())
    expect(fixture.call("Revision") == revision and not fixture.call("Carrying"), "Focus loss must invalidate native token")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    await begin_drag(grid.get_child(0).get_global_rect().get_center())
    var search := menu.get_node("%InventoryContainer").get_node("FilterToolbar/LineEdit") as LineEdit
    search.text = "changed"
    search.text_changed.emit(search.text)
    await finish_drag(grid.get_child(3).get_global_rect().get_center())
    expect(fixture.call("Revision") == revision and not fixture.call("Carrying"), "Mapping changes must invalidate native token")
    search.text = ""
    search.text_changed.emit(search.text)
    await settle()
    await begin_drag(grid.get_child(0).get_global_rect().get_center(), "scroll")
    var scroll := menu.get_node("%InventoryContainer").get_node("%ItemGrid") as ScrollContainer
    var wheel := InputEventMouseButton.new()
    wheel.position = grid.get_child(0).get_global_rect().get_center()
    wheel.button_index = MOUSE_BUTTON_WHEEL_DOWN
    wheel.pressed = true
    root.push_input(wheel)
    wheel = wheel.duplicate()
    wheel.pressed = false
    root.push_input(wheel)
    await settle()
    expect(scroll.scroll_vertical > 0 and root.gui_is_dragging(), "Wheel scrolling must remain available during native drag")
    await finish_drag(Vector2(5, 5))
    scroll.scroll_vertical = 0
    await settle()
    await begin_drag(grid.get_child(0).get_global_rect().get_center(), "close")
    canvas.call("CloseMenu", 0)
    canvas.call("OpenMenu", 0)
    await settle()
    await finish_drag(Vector2(5, 5))
    expect(fixture.call("Revision") == revision and not fixture.call("Carrying"), "Close/reopen must invalidate native token")
    menu = row.get_child(0)
    grid = menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    await begin_drag(grid.get_child(0).get_global_rect().get_center(), "escape")
    var escape := InputEventKey.new()
    escape.keycode = KEY_ESCAPE
    escape.physical_keycode = KEY_ESCAPE
    escape.pressed = true
    root.push_input(escape)
    escape = escape.duplicate()
    escape.pressed = false
    root.push_input(escape)
    await settle()
    await finish_drag(Vector2(5, 5))
    expect(row.get_child_count() == 0 and fixture.call("Revision") == revision and not fixture.call("Carrying"), "Escape during native drag must close windows and preserve ownership")
    fixture.call("Restore", game)
    if failures == previous_failures:
        print("Inventory native drag checks passed (%s)." % root.size)

func send_key(key: Key, character := 0) -> void:
    var event := InputEventKey.new()
    event.keycode = key
    event.physical_keycode = key
    event.unicode = character
    event.pressed = true
    root.push_input(event)
    event = event.duplicate()
    event.pressed = false
    root.push_input(event)
    await settle()

func verify_browsing(game: Node, canvas: Node, row: Node) -> void:
    var previous_failures := failures
    var fixture = load("res://Scripts/Util/Mocks/InventoryClickSmokeFixture.cs").new()
    fixture.call("Show", game)
    await settle()
    var menu := row.get_child(0)
    var container := menu.get_node("%InventoryContainer")
    var grid := container.get_node("%ItemGrid").get_node("%SlotGrid")
    var search := container.get_node("FilterToolbar/LineEdit") as LineEdit
    var tabs := container.get_node("%CategoryTabs") as TabBar
    var sort := container.get_node("FilterToolbar/SortSelector") as OptionButton
    var revision: int = fixture.call("Revision")
    expect(tabs.tab_count == 9 and tabs.get_tab_title(8) == "Resource", "Tabs must enumerate all catalog categories")
    expect(sort.item_count == 2 and sort.selected == 0, "Name ascending must be the default with descending available")
    search.text = " ENGINE "
    search.text_changed.emit(search.text)
    await settle()
    expect(grid.get_child_count() == 125 and grid.get_child(0).tooltip_text.begins_with("Click Engine"), "Trimmed case-insensitive filtering must show M + C - N cells")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(grid.get_child(1).get_global_rect().get_center())
    expect(fixture.call("CountAt", 0) == 0 and fixture.call("CountAt", 4) == 1, "Filtered empty placement must target genuine address 4")
    expect(grid.get_child(0).tooltip_text.begins_with("Click Engine") and search.text == " ENGINE ", "Placement must repack results and retain search")
    await begin_drag(grid.get_child(0).get_global_rect().get_center(), "filtered placement")
    await finish_drag(grid.get_child(1).get_global_rect().get_center())
    expect(fixture.call("CountAt", 0) == 1 and fixture.call("CountAt", 4) == 0, "Filtered native placement must use newly projected genuine empty address 0")
    tabs.current_tab = 8
    await settle()
    expect(grid.get_child_count() == 124 and container.get_node("EmptyResult").visible, "Empty categories must keep only genuine empty destinations and show a message")
    sort.select(1)
    sort.item_selected.emit(1)
    canvas.call("CloseMenu", 0)
    canvas.call("OpenMenu", 0)
    await settle()
    container = row.get_child(0).get_node("%InventoryContainer")
    search = container.get_node("FilterToolbar/LineEdit")
    expect(search.text == " ENGINE " and container.get_node("%CategoryTabs").current_tab == 8 and container.get_node("FilterToolbar/SortSelector").selected == 1, "All browsing choices must survive reopen")
    expect(fixture.call("Revision") == revision + 2, "Browsing must not mutate domain revisions")
    fixture.call("Fill")
    await settle()
    grid = container.get_node("%ItemGrid").get_node("%SlotGrid")
    expect(grid.get_child_count() == 0 and container.get_node("EmptyResult").visible, "Full inventory with no matches must expose no drop targets")
    await capture_visual("empty-results")
    var bag := container.get_node("%ItemGrid") as ScrollContainer
    expect(bag.scroll_vertical == 0, "Empty results must clamp the scroll position")
    await pointer_click(search.get_global_rect().get_center())
    await send_key(KEY_I, 105)
    await send_key(KEY_M, 109)
    expect(row.get_child_count() == 1 and search.text.contains("im"), "Typing inventory/map shortcuts must edit search without navigating")
    await send_key(KEY_ESCAPE)
    expect(row.get_child_count() == 0, "Escape must close all while search has focus")
    await send_key(KEY_I)
    expect(row.get_child_count() == 1, "Shortcut release tracking must allow reopening after search closes")
    fixture.call("Restore", game)
    if failures == previous_failures:
        print("Inventory browsing checks passed (%s)." % root.size)

func hover_cell(cell: Control) -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = cell.get_global_rect().get_center()
    pointer_motion(motion)
    await settle()

func verify_hotbar_noop(pointer_overlay: Node, fixture: RefCounted, source: Control, target: Control, message: String) -> void:
    var feedback := pointer_overlay.get_node("%ItemPointerFeedback") as Label
    var preview := pointer_overlay.get_node("%ItemPointerPreview") as Control
    var revision: int = fixture.call("Revision")
    var inventory_revision: int = fixture.call("InventoryRevision")
    await pointer_click(source.get_global_rect().get_center())
    expect(feedback.visible and feedback.text == "Cancel placement", "Hovering the original source must offer cancellation")
    await hover_cell(target)
    expect(feedback.visible and feedback.text == message, "Hotbar no-op hover must display: %s" % message)
    await pointer_click(target.get_global_rect().get_center())
    expect(preview.visible and feedback.visible and feedback.text == message, "Hotbar no-op click must retain pickup and contextual feedback")
    await pointer_click(source.get_global_rect().get_center())
    expect(not preview.visible and not feedback.visible, "Clicking the original source must cancel hotbar pickup")
    await begin_drag(source.get_global_rect().get_center(), "hotbar no-op")
    await hover_cell(target)
    expect(feedback.visible and feedback.text == message, "Native no-op hover must agree with click feedback")
    await finish_drag(target.get_global_rect().get_center())
    expect(not preview.visible and not feedback.visible, "Native no-op release must end pickup")
    expect(fixture.call("Revision") == revision and fixture.call("InventoryRevision") == inventory_revision,
        "Hotbar no-op hover, clicks, and drag release must preserve revisions")

func verify_hotbar(game: Node, canvas: Node, row: Node) -> void:
    var previous_failures := failures
    var fixture = load("res://Scripts/Util/Mocks/InventoryHotbarSmokeFixture.cs").new()
    fixture.call("Show", game)
    await settle()
    var buttons := canvas.get_node("%PlayerHud").get_node("%HotbarControl").get_node("%ButtonContainer")
    var menu := row.get_child(0)
    var grid := menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    var equipment := menu.get_node("%EquipmentContainer")
    var inventory_revision: int = fixture.call("InventoryRevision")
    expect(buttons.get_child_count() == 10 and not buttons.get_child(0).disabled, "Inventory must enable actual hotbar buttons for editing")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(buttons.get_child(0).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 0) == "consumable", "Click placement must replace a mock action with a consumable link")
    expect(buttons.get_child(0).get_node("QuantityLabel").text == "15", "Hotbar must aggregate both matching stacks")
    expect(fixture.call("InventoryRevision") == inventory_revision, "Assignment must not transfer inventory ownership")
    await verify_hotbar_noop(canvas.get_node("%ItemPointerOverlay"), fixture, grid.get_child(1), buttons.get_child(0), "Already assigned here")
    await pointer_click(buttons.get_child(0).get_global_rect().get_center())
    await pointer_click(buttons.get_child(1).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 0) == "1" and fixture.call("BindingAt", 1) == "consumable", "Click editing must swap hotbar entries")
    await pointer_click(buttons.get_child(0).get_global_rect().get_center(), MOUSE_BUTTON_RIGHT)
    expect(fixture.call("BindingAt", 0) == "empty", "Right click must clear without restoring a mock action")
    await begin_drag(buttons.get_child(1).get_global_rect().get_center(), "hotbar move")
    await finish_drag(buttons.get_child(0).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 0) == "consumable" and fixture.call("BindingAt", 1) == "empty", "Native hotbar drag must move into an empty position")
    await begin_drag(grid.get_child(1).get_global_rect().get_center(), "consumable assignment")
    await finish_drag(buttons.get_child(1).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 1) == "consumable", "Native item drag must assign a duplicate definition link")
    await verify_hotbar_noop(canvas.get_node("%ItemPointerOverlay"), fixture, buttons.get_child(0), buttons.get_child(1), "These slots already have the same assignment")
    await begin_drag(grid.get_child(2).get_global_rect().get_center(), "inventory weapon rejection")
    await finish_drag(buttons.get_child(2).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 2) == "2", "Inventory weapons cannot be assigned to hotbar")
    await begin_drag(equipment.get_child(4).get_node("%ClickableIcon").get_global_rect().get_center(), "weapon slot assignment")
    await finish_drag(buttons.get_child(2).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 2) == "light_weapon_1", "Equipped weapons must bind their stable slot key")
    await verify_hotbar_noop(canvas.get_node("%ItemPointerOverlay"), fixture, equipment.get_child(4).get_node("%ClickableIcon"), buttons.get_child(2), "Already assigned here")
    var revision: int = fixture.call("Revision")
    await begin_drag(buttons.get_child(2).get_global_rect().get_center(), "outside cancellation")
    await finish_drag(Vector2(5, 5))
    expect(fixture.call("Revision") == revision, "Outside hotbar drops must not clear the link")
    await begin_drag(buttons.get_child(2).get_global_rect().get_center(), "invalid item target")
    await finish_drag(grid.get_child(3).get_global_rect().get_center())
    expect(fixture.call("Revision") == revision and fixture.call("InventoryRevision") == inventory_revision, "Hotbar links cannot be placed into inventory")
    fixture.call("EmptyLinkedSources")
    await settle()
    expect(buttons.get_child(0).get_node("QuantityLabel").text == "0" and buttons.get_child(0).icon != null, "Zero quantity must retain definition visuals")
    expect(buttons.get_child(2).icon != null and buttons.get_child(2).tooltip_text.contains("Light Weapon Slot 1"), "Empty weapon slot link must retain its placeholder and slot label")
    await begin_drag(buttons.get_child(2).get_global_rect().get_center(), "unavailable link move")
    await finish_drag(buttons.get_child(3).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 3) == "light_weapon_1", "Unavailable links must remain movable")
    expect(fixture.call("Activations") == 0, "Neither click nor drag editing may forward gameplay activation")
    await begin_drag(buttons.get_child(3).get_global_rect().get_center(), "hotbar focus loss")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_OUT)
    await finish_drag(buttons.get_child(4).get_global_rect().get_center())
    expect(fixture.call("BindingAt", 3) == "light_weapon_1" and fixture.call("BindingAt", 4) == "4", "Focus loss must reject pending hotbar edits")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    canvas.call("OpenMenu", 1)
    canvas.call("CloseMenu", 0)
    await settle()
    revision = fixture.call("Revision")
    expect(buttons.get_child(0).disabled, "Map alone must disable actual editing buttons")
    await pointer_click(buttons.get_child(0).get_global_rect().get_center(), MOUSE_BUTTON_RIGHT)
    expect(fixture.call("Revision") == revision and fixture.call("Activations") == 0, "Map alone must allow neither edits nor activation")
    canvas.call("CloseAllMenus")
    await settle()
    await pointer_click(buttons.get_child(5).get_global_rect().get_center())
    expect(fixture.call("Activations") == 1, "Closed inventory must restore the legacy activation route")
    await pointer_click(buttons.get_child(0).get_global_rect().get_center(), MOUSE_BUTTON_RIGHT)
    expect(fixture.call("Revision") == revision, "Closed inventory must reject right click clearing")
    fixture.call("Restore", game)
    if failures == previous_failures:
        print("Inventory hotbar checks passed (%s)." % root.size)

func verify_click_placement(game: Node, canvas: Node, row: Node) -> void:
    await verify_native_drag(game, canvas, row)
    var previous_failures := failures
    var fixture = load("res://Scripts/Util/Mocks/InventoryClickSmokeFixture.cs").new()
    fixture.call("Show", game)
    await settle()
    var menu := row.get_child(0)
    var grid := menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    var equipment := menu.get_node("%EquipmentContainer")
    var pointer_overlay := canvas.get_node("%ItemPointerOverlay") as Control
    var preview := pointer_overlay.get_node("%ItemPointerPreview") as Control
    var feedback := pointer_overlay.get_node("%ItemPointerFeedback") as Label
    var revision: int = fixture.call("Revision")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    expect(fixture.call("Carrying"), "Occupied release must pick up an item")
    expect(preview.is_visible_in_tree() and preview.mouse_filter == Control.MOUSE_FILTER_IGNORE, "Carrying preview must be visible and ignore pointer input")
    expect(preview.global_position.is_equal_approx(pointer_overlay.get_global_mouse_position() + Vector2(16, 16)), "Shared preview must follow the cursor")
    expect(fixture.call("Revision") == revision and grid.get_child(0).icon != null, "Pickup must preserve source ownership and icon")
    var target_presses := [0]
    equipment.get_child(0).get_node("%ClickableIcon").pressed.connect(func(): target_presses[0] += 1)
    await pointer_click(equipment.get_child(1).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("Carrying") and fixture.call("Revision") == revision, "Invalid equipment placement must retain carrying without mutation")
    await pointer_click(equipment.get_child(0).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("EngineEquipped") and not fixture.call("Carrying"), "Valid placement must equip once and end carrying")
    expect(not preview.visible and not pointer_overlay.visible, "Successful placement must remove the pointer overlay")
    expect(fixture.call("Revision") == revision + 1, "Placement release must commit exactly once")
    expect(target_presses[0] == 0, "Placement must consume the target button event")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(grid.get_child(2).get_global_rect().get_center())
    expect(fixture.call("Carrying"), "Full matching stack must retain carrying")
    expect(feedback.is_visible_in_tree() and feedback.text == "Stack is full",
        "Full matching inventory stacks must keep their existing feedback")
    await pointer_click(grid.get_child(1).get_global_rect().get_center())
    expect(not fixture.call("Carrying") and fixture.call("CountAt", 1) == 5 and fixture.call("CountAt", 2) == 10, "Partial merge must end carrying with exact counts")
    revision = fixture.call("Revision")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    expect(not fixture.call("Carrying"), "Clicking source must cancel")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(Vector2(5, 5))
    expect(not fixture.call("Carrying"), "Outside click must cancel")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(grid.get_child(1).get_global_rect().get_center(), MOUSE_BUTTON_RIGHT)
    expect(not fixture.call("Carrying"), "Right click must cancel without item action")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    var search := menu.get_node("%InventoryContainer").get_node("FilterToolbar/LineEdit") as LineEdit
    await pointer_click(search.get_global_rect().get_center())
    expect(not fixture.call("Carrying") and search.has_focus(), "Toolbar click must cancel and still focus search")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_OUT)
    expect(not fixture.call("Carrying"), "Focus loss must cancel")
    expect(not pointer_overlay.visible, "Focus loss must hide the shared pointer overlay")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    canvas.call("OpenMenu", 1)
    canvas.call("CloseMenu", 0)
    expect(not pointer_overlay.visible, "Closing inventory must hide its shared pointer presentation")
    canvas.call("OpenMenu", 0)
    await settle()
    expect(not fixture.call("Carrying"), "Close/reopen with Map open must invalidate old pickup")
    expect(canvas.get_node("%ItemPointerOverlay") == pointer_overlay, "Reopening inventory must reuse the canvas-owned overlay")
    canvas.call("CloseMenu", 1)
    await settle()
    menu = row.get_child(0)
    grid = menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    var escape := InputEventKey.new()
    escape.keycode = KEY_ESCAPE
    escape.physical_keycode = KEY_ESCAPE
    escape.pressed = true
    root.push_input(escape)
    escape = escape.duplicate()
    escape.pressed = false
    root.push_input(escape)
    await settle()
    expect(row.get_child_count() == 0 and not fixture.call("Carrying"), "Escape must close all windows and clear carrying")
    expect(not pointer_overlay.visible, "Closing all windows must hide the shared pointer overlay")
    expect(fixture.call("Revision") == revision, "Cancellation paths must preserve inventory revision")
    fixture.call("Restore", game)
    if failures == previous_failures:
        print("Inventory click placement checks passed (%s)." % root.size)

func verify_visual_pointer(canvas: Node, row: Node, fixture: RefCounted) -> void:
    var grid := row.get_child(0).get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    var cell := grid.get_child(7) as Button
    var overlay := cell.get_node("QuantityLabel") as Label
    var motion := InputEventMouseMotion.new()
    motion.position = overlay.get_global_rect().get_center()
    pointer_motion(motion)
    await settle()
    expect(root.gui_get_hovered_control() == cell, "Quantity overlay must pass pointer hover to its icon")
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Hover must display the standard delayed item tooltip")
    await capture_visual("tooltip-counts")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_OUT)
    await settle()
    expect(not tooltip_visible(root), "Focus loss must dismiss the visible item tooltip")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    motion.position = Vector2.ZERO
    pointer_motion(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    pointer_motion(motion)
    cell.disabled = true
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Disabled activation must retain tooltip availability after focus recovery")
    cell.disabled = false
    var presses := [0]
    cell.pressed.connect(func(): presses[0] += 1)
    var click := InputEventMouseButton.new()
    click.position = overlay.get_global_rect().get_center()
    click.button_index = MOUSE_BUTTON_LEFT
    click.pressed = true
    root.push_input(click)
    click = click.duplicate()
    click.pressed = false
    root.push_input(click)
    await settle()
    expect(presses[0] == 1, "Quantity overlay must allow a single native button click")
    motion.position = Vector2.ZERO
    pointer_motion(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    pointer_motion(motion)
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Tooltip must remain available after clicking")
    expect(fixture.call("MoveSource"), "Source move fixture must commit")
    await settle()
    expect(not tooltip_visible(root), "Rebinding a hovered source must dismiss its old tooltip")
    motion.position = Vector2.ZERO
    pointer_motion(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    pointer_motion(motion)
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Rebound cell must show the new item's tooltip")
    canvas.call("CloseMenu", 0)
    await settle()
    expect(not tooltip_visible(root), "Closing inventory must dismiss the visible item tooltip")
    canvas.call("OpenMenu", 0)
    await settle()

func verify_cells(menu: Node, pointer_overlay: Control) -> void:
    var equipment := menu.get_node("%EquipmentContainer")
    var bag := menu.get_node("%InventoryContainer").get_node("%ItemGrid")
    var grid := bag.get_node("%SlotGrid")
    expect(equipment.get_child_count() == 8, "Equipment must replace previews with eight slots")
    expect(grid.get_child_count() == 128, "Bag must replace previews with 128 cells")
    expect(grid.columns == 8, "Bag must retain eight columns")
    var tabs := menu.get_node("%InventoryContainer").get_node("%CategoryTabs") as TabBar
    expect(tabs.tab_count == 9 and tabs.get_tab_icon(0) == null, "Categories must use native tabs with a text-only All tab")
    for index in range(1, tabs.tab_count):
        expect(tabs.get_tab_metadata(index) is String and not tabs.get_tab_metadata(index).is_empty(), "Tabs must retain stable category IDs")
    expect(not menu.has_node("ItemPointerPreview") and not menu.has_node("ItemPointerFeedback"), "Inventory must not own pointer presentation nodes")
    expect(not pointer_overlay.visible and not pointer_overlay.get_node("%ItemPointerPreview").visible and not pointer_overlay.get_node("%ItemPointerFeedback").visible,
        "Canvas-owned pointer presentation must start hidden")
    expect(pointer_overlay.mouse_filter == Control.MOUSE_FILTER_IGNORE and pointer_overlay.get_node("%ItemPointerFeedback").mouse_filter == Control.MOUSE_FILTER_IGNORE,
        "Shared pointer presentation must pass mouse input through")
    expect(pointer_overlay.get_canvas() == menu.get_canvas() and pointer_overlay.get_index() == pointer_overlay.get_parent().get_child_count() - 1, "Pointer presentation must render last in the shared UI canvas")
    for index in range(equipment.get_child_count()):
        var cell := equipment.get_child(index)
        var label := cell.get_node("%Label") as Label
        expect(label.text == SLOT_LABELS[index], "Equipment labels must follow domain order")
        expect(label.size.y >= label.get_minimum_size().y, "Equipment labels must fit their cells")
        expect(cell.get_node("%ClickableIcon").icon == null, "Empty equipment must have no mock texture")
    for cell in grid.get_children():
        expect(cell.icon == null and cell.tooltip_text.is_empty(), "Empty bag cells must have no item texture or tooltip")
        var quantity := cell.get_node("QuantityLabel") as Label
        expect(not quantity.visible and quantity.mouse_filter == Control.MOUSE_FILTER_IGNORE, "Quantity labels must start hidden and ignore pointer input")
    expect(bag.get_v_scroll_bar().max_value > bag.size.y, "Inventory must overflow vertically")
    bag.scroll_vertical = int(bag.get_v_scroll_bar().max_value)
    await settle()
    expect(bag.scroll_vertical > 0, "Inventory must scroll to trailing empty cells")

func verify_window_overflow(canvas: Node, row: HBoxContainer) -> void:
    var scroll := row.get_parent() as ScrollContainer
    var inventory := row.get_child(0) as Control
    var inventory_size := inventory.size
    canvas.call("OpenMenu", 1)
    canvas.call("OpenMenu", 2)
    await settle()
    var row_available_width := scroll.size.x
    if scroll.get_v_scroll_bar().visible:
        row_available_width -= scroll.get_v_scroll_bar().size.x
    expect(scroll.get_h_scroll_bar().visible == (row.get_combined_minimum_size().x > row_available_width),
        "Horizontal scrolling must match whether the smaller windows fit in the menu area")
    expect(inventory.size == inventory_size, "Horizontal overflow must not shrink inventory")
    for index in [2, 0, 1]:
        canvas.call("OpenMenu", index)
        await settle()
        var area := scroll.get_global_rect()
        if scroll.get_v_scroll_bar().visible:
            area.size.x -= scroll.get_v_scroll_bar().size.x
        var bounds: Rect2 = row.get_child(index).get_global_rect()
        expect(bounds.position.x >= area.position.x - 1 and bounds.end.x <= area.end.x + 1, "Focusing an overflowing row must reveal the selected window horizontally")
        expect(bounds.intersects(area), "Focused windows must remain reachable within UiContainer")
    await capture_visual("window-overflow")
    canvas.call("CloseMenu", 1)
    canvas.call("CloseMenu", 2)
    await settle()
    expect(not scroll.get_h_scroll_bar().visible, "Closing extra windows must remove horizontal overflow")
    var available_width := scroll.size.x
    if scroll.get_v_scroll_bar().visible:
        available_width -= scroll.get_v_scroll_bar().size.x
    expect(absf(inventory.get_global_rect().get_center().x - scroll.global_position.x - available_width / 2) <= 1, "Single inventory must recenter in the usable menu area after overflow")

func run() -> void:
    create_timer(60).timeout.connect(func(): quit(1))
    var packed := load("res://scenes/game/main_game.tscn") as PackedScene
    var loading_fixture = load("res://Scripts/Util/Mocks/InventorySlotSmokeFixture.cs").new()
    var demo := packed.instantiate()
    root.add_child(demo)
    await settle()
    expect(loading_fixture.call("VerifyLoadedItems", demo, true), "Seeded startup must use the loaded item catalog")
    demo.get_node("UiCanvas").call("OpenMenu", 0)
    await settle()
    var demo_menu := demo.get_node("UiCanvas/Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow").get_child(0)
    var demo_container := demo_menu.get_node("%InventoryContainer")
    var demo_grid := demo_container.get_node("%ItemGrid").get_node("%SlotGrid")
    var occupied := 0
    for cell in demo_grid.get_children():
        if cell.icon != null:
            occupied += 1
    expect(demo_grid.get_child_count() == 128 and occupied == 34, "PlayerMock must seed examples while keeping free inventory slots")
    var demo_tabs := demo_container.get_node("%CategoryTabs") as TabBar
    for index in range(1, demo_tabs.tab_count):
        demo_tabs.current_tab = index
        await settle()
        expect(demo_grid.get_child_count() > 94, "Every category must contain PlayerMock examples")
    var demo_buttons := demo.get_node("UiCanvas").get_node("%PlayerHud").get_node("%HotbarControl").get_node("%ButtonContainer")
    expect(demo_buttons.get_child_count() == 10, "Default hotbar must retain ten empty slots")
    for cell in demo_buttons.get_children():
        expect(cell.icon == null and not cell.get_node("QuantityLabel").visible, "Default hotbar must have no legacy icons or counts")
    demo.get_node("UiCanvas").call("CloseAllMenus")
    demo.get_node("UiCanvas").call("OpenMenu", 0)
    await settle()
    expect(loading_fixture.call("VerifyLoadedItems", demo, true), "Menu reopen must reuse loaded definitions and preserve quantities")
    demo.queue_free()
    await settle()
    for viewport_size in [Vector2i(1920, 1080), Vector2i(2560, 1440)]:
        root.size = viewport_size
        root.content_scale_size = viewport_size
        if OS.get_cmdline_user_args().has("--capture-visuals"):
            root.grab_focus()
        var game := packed.instantiate()
        game.set("PopulateMockInventory", false)
        root.add_child(game)
        await settle()
        expect(loading_fixture.call("VerifyLoadedItems", game, false), "Unseeded games must load independent catalogs and keep inventory empty")
        var canvas := game.get_node("UiCanvas")
        var row := game.get_node("UiCanvas/Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow")
        canvas.call("OpenMenu", 0)
        await settle()
        var menu := row.get_child(0)
        var hotbar := canvas.get_node("%PlayerHud").get_node("%HotbarControl") as Control
        var overlay := canvas.get_node("%ItemPointerOverlay") as Control
        var scroll := canvas.get_node("Root/MenuLayer/UiContainer/MenuHost/WindowScroll") as ScrollContainer
        var menu_area := canvas.get_node("%UiContainer") as Control
        expect(menu_area.clip_contents and menu_area.get_global_rect().encloses(scroll.get_global_rect()), "UiContainer must bound the menu scroll surface")
        expect(hotbar.get_canvas() == overlay.get_canvas() and hotbar.get_canvas() == menu.get_canvas(), "HUD, windows, and pointer visuals must share one UI canvas")
        expect(hotbar.get_viewport() == root and overlay.get_viewport() == root, "UI must render in the root viewport independently of the world viewport")
        expect(game.get_node("WorldViewPortContainer/WorldViewport").size == Vector2i(1280, 720), "World rendering must retain its independent 720p resolution")
        expect(scroll.get_global_rect().end.y <= hotbar.get_global_rect().position.y, "Window scroll surface must leave the hotbar strip unobstructed")
        expect(overlay.get_parent() == canvas.get_node("Root") and overlay.get_index() == overlay.get_parent().get_child_count() - 1, "Pointer visuals must draw last under the shared UI root")
        await verify_cells(menu, canvas.get_node("%ItemPointerOverlay"))
        await verify_window_overflow(canvas, row)
        var search := menu.get_node("%InventoryContainer").get_node("FilterToolbar/LineEdit") as LineEdit
        expect(search.text.is_empty(), "A new game must have independent view choices")
        search.text = "retained choice"
        search.text_changed.emit(search.text)
        var original_id := menu.get_instance_id()
        canvas.call("OpenMenu", 0)
        expect(row.get_child_count() == 1, "Repeated open must focus the existing window")
        canvas.call("OpenMenu", 1)
        canvas.call("CloseMenu", 0)
        expect(row.get_child_count() == 1, "Closing inventory must leave Map open")
        canvas.call("OpenMenu", 0)
        await settle()
        menu = row.get_child(1)
        expect(menu.get_instance_id() != original_id, "Reopen must create a fresh view")
        expect(menu.get_node("%InventoryContainer").get_node("FilterToolbar/LineEdit").text == "retained choice", "View choices must survive reopening")
        await verify_cells(menu, canvas.get_node("%ItemPointerOverlay"))
        canvas.call("CloseAllMenus")
        canvas.call("OpenMenu", 0)
        await settle()
        await verify_cells(row.get_child(0), canvas.get_node("%ItemPointerOverlay"))
        var fixture = load("res://Scripts/Util/Mocks/InventorySlotSmokeFixture.cs").new()
        expect(fixture.call("Run", game), "Live growth, compact transfer bindings, and detached subscriptions must pass")
        var visuals = load("res://Scripts/Util/Mocks/InventoryVisualSmokeFixture.cs").new()
        visuals.call("Show", game)
        await settle()
        await verify_visual_pointer(canvas, row, visuals)
        expect(visuals.call("Verify", game), "Item visuals, quantity layout, and display reset must pass")
        visuals.call("Restore", game)
        await verify_click_placement(game, canvas, row)
        await verify_browsing(game, canvas, row)
        await verify_hotbar(game, canvas, row)
        game.queue_free()
        await settle()
    if failures == 0:
        print("Inventory slot smoke checks passed (1920x1080 and 2560x1440 UI, 1280x720 world, two game lifetimes).")
    quit(0 if failures == 0 else 1)
