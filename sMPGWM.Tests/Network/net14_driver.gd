extends Node

class DelayedHost extends Node:
    var sender := 0
    var token := ""

    @rpc("any_peer", "call_remote", "reliable")
    func Hello(value: String) -> void:
        sender = multiplayer.get_remote_sender_id()
        token = value
        print("NET hello received")

    @rpc("authority", "call_remote", "reliable")
    func HelloReply(_value: String) -> void:
        pass

var fake_host: DelayedHost

func _ready() -> void:
    print("NET driver ready")

func _authenticate(peer: int, data: PackedByteArray) -> void:
    var api := multiplayer as SceneMultiplayer
    assert(api.send_auth(peer, data) == OK)
    assert(api.complete_auth(peer) == OK)

func _ignore_auth(_peer: int, _data: PackedByteArray) -> void:
    pass

# Drives production state transitions; only the delayed server is a test double.
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
        api.auth_callback = _ignore_auth
        api.auth_timeout = 30.0
        if parts[1] == "malformed":
            api.peer_authenticating.connect(func(peer: int):
                assert(api.send_auth(peer, "invalid".to_utf8_buffer()) == OK))
        var peer := ENetMultiplayerPeer.new()
        assert(peer.create_client("127.0.0.1", int(parts[2])) == OK)
        api.multiplayer_peer = peer
        return
    if command.begins_with("fake-host|"):
        var root := Node.new()
        root.name = "MainGame"
        get_tree().root.add_child(root)
        fake_host = DelayedHost.new()
        fake_host.name = "NetworkManager"
        root.add_child(fake_host)
        var api := multiplayer as SceneMultiplayer
        api.auth_callback = _authenticate
        var peer := ENetMultiplayerPeer.new()
        assert(peer.create_server(int(command.split("|")[1]), 9) == OK)
        api.multiplayer_peer = peer
        print("NET fake host ready")
        return
    match command:
        "reply", "wrong-reply":
            fake_host.rpc_id(fake_host.sender, "HelloReply", fake_host.token if command == "reply" else "wrong")
        "check-loading":
            assert(flow.get("GameFlowState") == 1)
            assert(get_node("/root/MainGame").get("GameState") == 1)
            print("NET loading keeps gameplay inactive")
        "check-ingame":
            assert(flow.get("GameFlowState") == 2)
            assert(get_node("/root/MainGame").get("GameState") == 0)
            print("NET InGame enables gameplay")
        "late-ready":
            flow.call("CompleteLoading")
            assert(flow.get("GameFlowState") == 0)
            print("NET late completion ignored")
        "delayed-leave":
            flow.call("QuitToStartMenu", "Delayed leave", 1500)
            assert(flow.get("GameFlowState") == 3)
            assert(get_node("/root/MainGame").get("GameState") == 1)
            print("NET Disconnecting disables gameplay")
            flow.call("QuitToStartMenuBlocking", "Immediate leave", false)
        "leave":
            flow.call("QuitToStartMenuBlocking", "Deliberate leave", false)
        "restart":
            flow.call("StartFromArguments")
        "duplicate":
            manager.call("CreateNetwork")
            flow.call("CompleteLoading")
