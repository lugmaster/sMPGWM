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
    create_timer(30).timeout.connect(func(): quit(1))
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
        game.queue_free()
        await settle()
    if failures == 0:
        print("Inventory slot smoke checks passed (1280x720 and 1920x1080, two game lifetimes).")
    quit(0 if failures == 0 else 1)
