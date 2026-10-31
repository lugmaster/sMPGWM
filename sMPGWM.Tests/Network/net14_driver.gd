extends Node

func _receive_auth(_peer: int, _data: PackedByteArray) -> void:
    pass

# @test-context: drives production scene lifecycle from the external integration test.
func _process(_delta: float) -> void:
    var path := OS.get_environment("NET14_COMMAND_FILE")
    if path.is_empty() or not FileAccess.file_exists(path):
        return
    var command := FileAccess.get_file_as_string(path)
    DirAccess.remove_absolute(path)
    var manager := get_node_or_null("/root/MainGame/NetworkManager")
    var flow := get_node("/root/GameFlowManager")
    if command.begins_with("auth|"):
        var parts := command.split("|")
        var api := multiplayer as SceneMultiplayer
        api.auth_callback = _receive_auth
        # Longer than the production host deadline: only the host may end this wait.
        api.auth_timeout = 30.0
        api.peer_authentication_failed.connect(func(_peer: int):
            var evidence := FileAccess.open(path + ".auth-failed", FileAccess.WRITE)
            evidence.store_string("disconnected"))
        if parts[1] == "malformed":
            api.peer_authenticating.connect(func(peer: int):
                assert(api.send_auth(peer, "invalid-auth-marker".to_utf8_buffer()) == OK))
        var peer := ENetMultiplayerPeer.new()
        assert(peer.create_client("127.0.0.1", int(parts[2])) == OK)
        api.multiplayer_peer = peer
        return
    if command.begins_with("reply|"):
        var parts := command.split("|")
        manager.get_node("SessionProtocol").rpc_id(int(parts[1]), "DiagnosticReply", int(parts[2]), parts[3], parts[4], parts[5])
        return
    match command:
        "normal-wait":
            flow.call("StartGame", 1, 0)
            await get_tree().scene_changed
            assert(get_node("/root/MainGame").get("GameState") == 1)
            await get_tree().create_timer(0.3).timeout
            assert(get_node("/root/MainGame").get("GameState") == 1)
            print("NET lifecycle gameplay inactive while connecting")
        "normal-host", "normal-client", "normal-steam-host":
            flow.call("StartGame", 1 if command == "normal-client" else 0, 1 if command == "normal-steam-host" else 0)
            await get_tree().scene_changed
            var deadline = Time.get_ticks_msec() + 10000
            while get_node("/root/MainGame").get("GameState") != 0 and Time.get_ticks_msec() < deadline:
                await get_tree().process_frame
            assert(get_node("/root/MainGame").get("GameState") == 0)
            print("NET lifecycle gameplay active after readiness")
        "normal-stop":
            manager.call("StopSession")
            assert(get_node("/root/MainGame").get("GameState") == 1)
            print("NET lifecycle gameplay inactive after stop")
            flow.call("QuitToStartMenuBlocking", "Normal session stopped", false)
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
            manager.call("FailSession", "NET14 deliberate cancellation")
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
