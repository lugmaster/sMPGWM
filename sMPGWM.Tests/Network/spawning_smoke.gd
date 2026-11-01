extends SceneTree

# Run one host and then two clients using --script <this file> -- host|client.
func _initialize() -> void:
    run.call_deferred()

func run() -> void:
    await process_frame
    var host := OS.get_cmdline_user_args().has("host")
    var flow := root.get_node("GameFlowManager")
    flow.StartGame(0 if host else 1, 0)
    var saw_three := false
    var deadline := Time.get_ticks_msec() + 15000
    while Time.get_ticks_msec() < deadline:
        await process_frame
        if current_scene == null:
            continue
        var manager := current_scene.get_node_or_null("WorldViewPortContainer/WorldViewport/WorldScene/MpManager")
        if manager == null:
            continue
        # InGame must always have the local placeholder ready.
        if flow.get("GameFlowState") == 2 and manager.get_child_count() == 0:
            push_error("Entered InGame before spawning")
            quit(1)
            return
        if manager.get_child_count() == 3:
            saw_three = true
        if saw_three and not host and flow.get("GameFlowState") == 2:
            await create_timer(1.0).timeout
            print("SPAWN_SMOKE client passed")
            quit(0)
            return
        if host and saw_three and manager.get_child_count() == 1:
            print("SPAWN_SMOKE host passed: late join and disconnect cleanup")
            quit(0)
            return
    push_error("SPAWN_SMOKE timed out")
    quit(1)
