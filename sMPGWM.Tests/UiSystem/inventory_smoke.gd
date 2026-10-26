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

func tooltip_visible(node: Node) -> bool:
    if node is Label and node.is_visible_in_tree() and node.text.contains("Development visual fixture."):
        return true
    for child in node.get_children(true):
        if tooltip_visible(child):
            return true
    return false

func pointer_click(position: Vector2, button := MOUSE_BUTTON_LEFT) -> void:
    var motion := InputEventMouseMotion.new()
    motion.position = position
    root.push_input(motion)
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

func verify_click_placement(game: Node, canvas: Node, row: Node) -> void:
    var previous_failures := failures
    var fixture = load("res://Scripts/Util/Mocks/InventoryClickSmokeFixture.cs").new()
    fixture.call("Show", game)
    await settle()
    var menu := row.get_child(0)
    var grid := menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid")
    var equipment := menu.get_node("%EquipmentContainer")
    var revision: int = fixture.call("Revision")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    expect(fixture.call("Carrying"), "Occupied release must pick up an item")
    expect(menu.get_node("ItemPointerPreview").visible and menu.get_node("ItemPointerPreview").mouse_filter == Control.MOUSE_FILTER_IGNORE, "Carrying preview must be visible and ignore pointer input")
    expect(fixture.call("Revision") == revision and grid.get_child(0).icon != null, "Pickup must preserve source ownership and icon")
    var target_presses := [0]
    equipment.get_child(0).get_node("%ClickableIcon").pressed.connect(func(): target_presses[0] += 1)
    await pointer_click(equipment.get_child(1).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("Carrying") and fixture.call("Revision") == revision, "Invalid equipment placement must retain carrying without mutation")
    await pointer_click(equipment.get_child(0).get_node("%ClickableIcon").get_global_rect().get_center())
    expect(fixture.call("EngineEquipped") and not fixture.call("Carrying"), "Valid placement must equip once and end carrying")
    expect(not menu.get_node("ItemPointerPreview").visible, "Successful placement must remove the pointer preview")
    expect(fixture.call("Revision") == revision + 1, "Placement release must commit exactly once")
    expect(target_presses[0] == 0, "Placement must consume the target button event")
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    await pointer_click(grid.get_child(2).get_global_rect().get_center())
    expect(fixture.call("Carrying"), "Full matching stack must retain carrying")
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
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    await pointer_click(grid.get_child(0).get_global_rect().get_center())
    canvas.call("OpenMenu", 1)
    canvas.call("CloseMenu", 0)
    canvas.call("OpenMenu", 0)
    await settle()
    expect(not fixture.call("Carrying"), "Close/reopen with Map open must invalidate old pickup")
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
    root.push_input(motion)
    await settle()
    expect(root.gui_get_hovered_control() == cell, "Quantity overlay must pass pointer hover to its icon")
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Hover must display the standard delayed item tooltip")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_OUT)
    await settle()
    expect(not tooltip_visible(root), "Focus loss must dismiss the visible item tooltip")
    canvas.notification(NOTIFICATION_APPLICATION_FOCUS_IN)
    motion.position = Vector2.ZERO
    root.push_input(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    root.push_input(motion)
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
    root.push_input(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    root.push_input(motion)
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Tooltip must remain available after clicking")
    expect(fixture.call("MoveSource"), "Source move fixture must commit")
    await settle()
    expect(not tooltip_visible(root), "Rebinding a hovered source must dismiss its old tooltip")
    motion.position = Vector2.ZERO
    root.push_input(motion)
    await settle()
    motion.position = cell.get_global_rect().get_center()
    root.push_input(motion)
    await create_timer(1.0).timeout
    expect(tooltip_visible(root), "Rebound cell must show the new item's tooltip")
    canvas.call("CloseMenu", 0)
    await settle()
    expect(not tooltip_visible(root), "Closing inventory must dismiss the visible item tooltip")
    canvas.call("OpenMenu", 0)
    await settle()

func verify_cells(menu: Node) -> void:
    var equipment := menu.get_node("%EquipmentContainer")
    var bag := menu.get_node("%InventoryContainer").get_node("%ItemGrid")
    var grid := bag.get_node("%SlotGrid")
    expect(equipment.get_child_count() == 8, "Equipment must replace previews with eight slots")
    expect(grid.get_child_count() == 128, "Bag must replace previews with 128 cells")
    expect(grid.columns == 6, "Bag must retain six columns")
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

func run() -> void:
    create_timer(60).timeout.connect(func(): quit(1))
    var packed := load("res://scenes/game/main_game.tscn") as PackedScene
    for viewport_size in [Vector2i(1280, 720), Vector2i(1920, 1080)]:
        root.size = viewport_size
        root.content_scale_size = viewport_size
        var game := packed.instantiate()
        root.add_child(game)
        await settle()
        var canvas := game.get_node("UiCanvas")
        var row := game.get_node("UiCanvas/Root/MenuLayer/MenuHost/WindowScroll/WindowRow")
        canvas.call("OpenMenu", 0)
        await settle()
        var menu := row.get_child(0)
        await verify_cells(menu)
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
        await verify_cells(menu)
        canvas.call("CloseAllMenus")
        canvas.call("OpenMenu", 0)
        await settle()
        await verify_cells(row.get_child(0))
        var fixture = load("res://Scripts/Util/Mocks/InventorySlotSmokeFixture.cs").new()
        expect(fixture.call("Run", game), "Live growth, compact transfer bindings, and detached subscriptions must pass")
        var visuals = load("res://Scripts/Util/Mocks/InventoryVisualSmokeFixture.cs").new()
        visuals.call("Show", game)
        await settle()
        await verify_visual_pointer(canvas, row, visuals)
        expect(visuals.call("Verify", game), "Item visuals, quantity layout, and display reset must pass")
        visuals.call("Restore", game)
        await verify_click_placement(game, canvas, row)
        game.queue_free()
        await settle()
    if failures == 0:
        print("Inventory slot smoke checks passed (1280x720 and 1920x1080, two game lifetimes).")
    quit(0 if failures == 0 else 1)
