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
