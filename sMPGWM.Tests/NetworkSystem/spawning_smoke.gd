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
    var deadline := Time.get_ticks_msec() + 90000
    var active_since := 0
    var silence_applied := false
    while Time.get_ticks_msec() < deadline:
        await process_frame
        if current_scene == null:
            continue
        var manager := current_scene.get_node_or_null("WorldViewPortContainer/WorldViewport/WorldScene/GameWorld")
        if manager == null:
            continue
        var network := current_scene.find_child("NetworkManager", true, false)
        var heartbeat := network.get_node_or_null("HeartbeatMonitor") if network != null else null
        if flow.get("GameFlowState") == 2 and active_since == 0:
            active_since = Time.get_ticks_msec()
        # Disabling only monitor processing on the host suppresses requests while
        # ENet stays connected. On a client, disable RPC receipt on this node.
        if heartbeat != null and active_since > 0 and Time.get_ticks_msec() - active_since > 12000 and not silence_applied:
            if OS.get_cmdline_user_args().has("silent-host") and host:
                heartbeat.process_mode = Node.PROCESS_MODE_DISABLED
                silence_applied = true
            if OS.get_cmdline_user_args().has("silent-client") and not host:
                heartbeat.rpc_config("HeartbeatRequest", {})
                silence_applied = true
            if OS.get_cmdline_user_args().has("pause"):
                paused = true
        if OS.get_cmdline_user_args().has("silent-host") and not host and flow.get("GameFlowState") == 3:
            print("HEARTBEAT_SMOKE host loss: verify blocking alert")
            quit(0)
            return
        # InGame must always have the local ship ready.
        if flow.get("GameFlowState") == 2 and manager.get_child_count() == 0:
            push_error("Entered InGame before spawning")
            quit(1)
            return
        if manager.get_child_count() == 3:
            saw_three = true
        if saw_three and not host and flow.get("GameFlowState") == 2 and active_since > 0 and Time.get_ticks_msec() - active_since > 30000 and not OS.get_cmdline_user_args().has("silent-client") and not OS.get_cmdline_user_args().has("silent-host"):
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
