extends SceneTree

var failures := 0
const SLOT_LABELS := [["Engine", "Hull", "Shield Generator", "Reactor", "Battery"], ["Light Weapon Slot 1", "Light Weapon Slot 2", "Heavy Weapon Slot 1", "Heavy Weapon Slot 2"], []] # TEST-SUPPORT: EQUIP-TABS-SMOKE-001 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells

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

func verify_cells(menu: Node, pointer_overlay: Control) -> void:
    var equipment := menu.get_node("%EquipmentContainer") as TabContainer # TEST-SUPPORT: EQUIP-TABS-SMOKE-002 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
    var bag := menu.get_node("%InventoryContainer").get_node("%ItemGrid")
    var grid := bag.get_node("%SlotGrid")
    expect(equipment.get_tab_count() == 3, "Equipment must have Ship, Weapons, and Crew tabs") # TEST-SUPPORT: EQUIP-TABS-SMOKE-003 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
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
    var equipment_size := equipment.size # TEST-SUPPORT: EQUIP-TABS-SMOKE-004 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
    for tab_index in range(3): # TEST-SUPPORT: EQUIP-TABS-SMOKE-005 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        expect(equipment.get_tab_title(tab_index) == ["Ship", "Weapons", "Crew"][tab_index], "Equipment tabs must retain their labels") # TEST-SUPPORT: EQUIP-TABS-SMOKE-006 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        expect(equipment.get_tab_icon(tab_index) != null, "Equipment tabs must have icons") # TEST-SUPPORT: EQUIP-TABS-SMOKE-007 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        equipment.current_tab = tab_index # TEST-SUPPORT: EQUIP-TABS-SMOKE-008 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        await settle() # TEST-SUPPORT: EQUIP-TABS-SMOKE-009 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        expect(equipment.size == equipment_size, "Switching equipment tabs must preserve panel size") # TEST-SUPPORT: EQUIP-TABS-SMOKE-010 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        var page := equipment.get_tab_control(tab_index) # TEST-SUPPORT: EQUIP-TABS-SMOKE-011 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        expect(page.visible, "Selected equipment page must be visible") # TEST-SUPPORT: EQUIP-TABS-SMOKE-012 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        expect(page.get_child_count() == SLOT_LABELS[tab_index].size(), "Equipment slots must be grouped by tab, leaving Crew empty") # TEST-SUPPORT: EQUIP-TABS-SMOKE-013 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        for other_index in range(3): # TEST-SUPPORT: EQUIP-TABS-SMOKE-014 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            expect(equipment.get_tab_control(other_index).visible == (other_index == tab_index), "Only the selected equipment page must be visible") # TEST-SUPPORT: EQUIP-TABS-SMOKE-015 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
        for index in range(page.get_child_count()): # TEST-SUPPORT: EQUIP-TABS-SMOKE-016 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            var cell := page.get_child(index) # TEST-SUPPORT: EQUIP-TABS-SMOKE-017 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            var label := cell.get_node("%Label") as Label # TEST-SUPPORT: EQUIP-TABS-SMOKE-018 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            expect(label.text == SLOT_LABELS[tab_index][index], "Equipment labels must follow domain order within their tab") # TEST-SUPPORT: EQUIP-TABS-SMOKE-019 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            expect(label.size.y >= label.get_minimum_size().y, "Equipment labels must fit their cells") # TEST-SUPPORT: EQUIP-TABS-SMOKE-020 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
            expect(cell.get_node("%ClickableIcon").icon == null, "Empty equipment must have no mock texture") # TEST-SUPPORT: EQUIP-TABS-SMOKE-021 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
    equipment.current_tab = 0 # TEST-SUPPORT: EQUIP-TABS-SMOKE-022 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
    await settle() # TEST-SUPPORT: EQUIP-TABS-SMOKE-023 | Tests: sMPGWM.Tests.UiSystem.inventory_smoke.verify_cells
    for cell in grid.get_children():
        expect(cell.icon == null and cell.tooltip_text.is_empty(), "Empty bag cells must have no item texture or tooltip")
        var quantity := cell.get_node("QuantityLabel") as Label
        expect(not quantity.visible and quantity.mouse_filter == Control.MOUSE_FILTER_IGNORE, "Quantity labels must start hidden and ignore pointer input")
    expect(bag.get_v_scroll_bar().max_value > bag.size.y, "Inventory must overflow vertically")
    bag.scroll_vertical = int(bag.get_v_scroll_bar().max_value)
    await settle()
    expect(bag.scroll_vertical > 0, "Inventory must scroll to trailing empty cells")

func verify_window_overflow(canvas: Node, row: HFlowContainer) -> void:
    var scroll := row.get_parent() as ScrollContainer
    var inventory := row.get_child(0) as Control
    var inventory_size := inventory.size
    canvas.call("OpenMenu", 1)
    await settle()
    expect(not scroll.get_h_scroll_bar().visible, "Menus must never scroll horizontally")
    expect(inventory.size == inventory_size, "Wrapping must preserve inventory size")
    var map := row.get_child(1) as Control
    var available_width := scroll.size.x
    if scroll.get_v_scroll_bar().visible:
        available_width -= scroll.get_v_scroll_bar().size.x
    if inventory.size.x + map.size.x + 20 > available_width:
        expect(map.position.y >= inventory.position.y + inventory.size.y + 19, "Menu must wrap beneath inventory when there is insufficient width")
    else:
        expect(map.position.x >= inventory.position.x + inventory.size.x + 19, "Menus must stay adjacent when they fit")
    for index in [0, 1]:
        canvas.call("OpenMenu", index)
        await settle()
        var area := scroll.get_global_rect()
        if scroll.get_v_scroll_bar().visible:
            area.size.x -= scroll.get_v_scroll_bar().size.x
        var bounds: Rect2 = row.get_child(index).get_global_rect()
        expect(bounds.position.x >= area.position.x - 1 and bounds.end.x <= area.end.x + 1, "Wrapped windows must fit horizontally")
        expect(bounds.intersects(area), "Focused windows must remain reachable vertically")
    await capture_visual("window-overflow")
    canvas.call("CloseMenu", 1)
    canvas.call("CloseMenu", 2)
    await settle()
    expect(not scroll.get_h_scroll_bar().visible, "Closing extra windows must remove horizontal overflow")
    available_width = scroll.size.x
    if scroll.get_v_scroll_bar().visible:
        available_width -= scroll.get_v_scroll_bar().size.x
    expect(absf(inventory.get_global_rect().get_center().x - scroll.global_position.x - available_width / 2) <= 1, "Single inventory must recenter in the usable menu area after overflow")

func run() -> void:
    create_timer(60).timeout.connect(func(): quit(1))
    var packed := load("res://scenes/game/main_game.tscn") as PackedScene
    var demo := packed.instantiate()
    root.add_child(demo)
    demo.call("SetGameState", 0)
    await settle()
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
        # This UI-only scene has no network session to activate gameplay.
        game.call("SetGameState", 0)
        await settle()
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
        game.queue_free()
        await settle()
    if failures == 0:
        print("Inventory slot smoke checks passed (1920x1080 and 2560x1440 UI, 1280x720 world, two game lifetimes).")
    quit(0 if failures == 0 else 1)
