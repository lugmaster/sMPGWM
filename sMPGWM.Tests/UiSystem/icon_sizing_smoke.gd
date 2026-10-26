extends SceneTree

var failures := 0

func _initialize() -> void:
    run.call_deferred()

func expect(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func settle() -> void:
    for frame in range(6):
        await process_frame

func texture(width: int, height: int) -> ImageTexture:
    var image := Image.create_empty(width, height, false, Image.FORMAT_RGBA8)
    image.fill(Color.WHITE)
    return ImageTexture.create_from_image(image)

func verify_sizes(cells: Array, expected: Vector2, surface: String) -> void:
    var samples := [texture(16, 16), texture(64, 64), texture(256, 256), texture(256, 64), null]
    for index in range(samples.size()):
        cells[index].set("IconTexture", samples[index])
        expect(cells[index].icon == samples[index], "%s must apply the replacement texture before checking layout" % surface)
    await settle()
    for cell in cells:
        expect(cell.size.is_equal_approx(expected), "%s slot must remain %s regardless of texture size or emptiness; got %s" % [surface, expected, cell.size])

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
        var hotbar := canvas.get_node("%PlayerHud").get_node("%HotbarControl")
        var hotbar_cells := hotbar.get_node("%ButtonContainer").get_children()
        await verify_sizes(hotbar_cells, Vector2(72, 72), "Hotbar")
        canvas.call("OpenMenu", 0)
        await settle()
        var row := canvas.get_node("Root/MenuLayer/MenuHost/WindowScroll/WindowRow")
        var menu := row.get_child(0)
        var bag_cells := menu.get_node("%InventoryContainer").get_node("%ItemGrid").get_node("%SlotGrid").get_children()
        await verify_sizes(bag_cells, Vector2(56, 56), "Inventory")
        var equipment_cells: Array = []
        for slot in menu.get_node("%EquipmentContainer").get_children():
            equipment_cells.append(slot.get_node("%ClickableIcon"))
        await verify_sizes(equipment_cells, Vector2(56, 56), "Equipment")
        await verify_sizes(hotbar_cells, Vector2(72, 72), "Hotbar with inventory open")
        game.queue_free()
        await settle()
    if failures == 0:
        print("Icon sizing smoke checks passed (16/64/256-pixel, rectangular, and empty textures; both resolutions).")
    quit(0 if failures == 0 else 1)
