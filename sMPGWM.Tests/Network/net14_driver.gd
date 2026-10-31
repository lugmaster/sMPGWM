extends Node

# @test-context: drives production scene lifecycle from the external integration test.
func _process(_delta: float) -> void:
    var path := OS.get_environment("NET14_COMMAND_FILE")
    if path.is_empty() or not FileAccess.file_exists(path):
        return
    var command := FileAccess.get_file_as_string(path)
    DirAccess.remove_absolute(path)
    var manager := get_node_or_null("/root/MainGame/NetworkManager")
    var flow := get_node("/root/GameFlowManager")
    if command.begins_with("reply|"):
        var parts := command.split("|")
        manager.get_node("SessionProtocol").rpc_id(int(parts[1]), "DiagnosticReply", int(parts[2]), parts[3], parts[4], parts[5])
        return
    match command:
        "steam-cancel":
            flow.call("StartGame", 0, 1)
            await get_tree().scene_changed
            get_node("/root/MainGame/NetworkManager").call("CreateNetwork")
            flow.call("QuitToStartMenuBlocking", "NET01 canceled native create", false)
        "steam-host":
            var join_menu = load("res://scenes/ui/menu/start_menu/join_steam_game_menu.tscn").instantiate()
            get_tree().root.add_child(join_menu)
            var join_button = join_menu.get_node("%JoinSteamGameButton")
            assert(join_button.disabled)
            var list = join_menu.get_node("%LobbyListPanel")
            assert(list.get_node("VBoxContainer/LobbyScroll/LobbyEntries").get_child_count() == 0)
            var toolbar = list.get_node("VBoxContainer").get_child(0)
            toolbar.get_child(0).text = "invalid"
            toolbar.get_child(1).pressed.emit()
            assert(join_button.disabled)
            join_menu.queue_free()
            var host_menu = load("res://scenes/ui/menu/start_menu/host_steam_game_menu.tscn").instantiate()
            get_tree().root.add_child(host_menu)
            host_menu.get_node("%HostSteamGameButton").pressed.emit()
            host_menu.queue_free()
        "delayed-leave":
            flow.call("QuitToStartMenu", "NET01 delayed quit", 1500)
            manager.call("StopSession")
            flow.call("QuitToStartMenuBlocking", "NET01 immediate quit", false)
        "leave":
            flow.call("QuitToStartMenuBlocking", "NET14 deliberate leave", false)
        "restart":
            flow.call("StartDiagnosticFromArguments")
        "duplicate":
            manager.call("CreateNetwork")
            manager.call("AssignRole")
            manager.get_node("SessionProtocol").call("SendHello")
        "cancel":
            manager.call("CancelDiagnostic", "NET14 deliberate cancellation")
        "quit":
            flow.call("QuitGame", "NET14 finished")
        "suppress":
            manager.get_node("SessionProtocol").call("Stop")
        "send-error":
            var script = load("res://Scripts/Network/SessionProtocolNode.cs")
            var endpoint = script.new()
            add_child(endpoint)
            endpoint.call("SendHello")
            endpoint.queue_free()
