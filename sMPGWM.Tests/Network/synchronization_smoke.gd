extends SceneTree

func _initialize():
    run.call_deferred()

func run():
    await process_frame
    var host = OS.get_cmdline_user_args().has("host")
    var players = 3
    for argument in OS.get_cmdline_user_args():
        if argument.begins_with("players="): players = int(argument.trim_prefix("players="))
    root.get_node("GameFlowManager").StartGame(0 if host else 1, 0)
    var driver = load("res://Scripts/Util/Mocks/SynchronizationSmokeInput.cs").new()
    root.add_child(driver)
    var initial = {}
    var moved = {}
    var start = Time.get_ticks_msec()
    var observed = false
    var duration = (players * 4000 + 8000) if host else (players * 2000 + 5000)
    while Time.get_ticks_msec() - start < duration + 5000:
        await process_frame
        if current_scene == null: continue
        var sync = current_scene.find_child("SynchronizationCoordinator", true, false)
        var manager = current_scene.find_child("MpManager", true, false)
        if sync == null or manager == null: continue
        for participant in manager.get_children():
            for ship in participant.get_children():
                if not ship is CharacterBody2D: continue
                var entity = str(ship.get("EntityId"))
                if not initial.has(entity):
                    initial[entity] = ship.global_position
                if ship.global_position.distance_to(initial[entity]) > 10: moved[entity] = true
        if manager.get_child_count() == players: observed = true
        if Time.get_ticks_msec() - start > duration and observed:
            print("SYNC_METRICS host=", host, " tick=", sync.get("CompletedTick"),
                " predictions=", sync.get("PredictionTick"), " publications=", sync.get("PublicationCount"),
                " corrections=", sync.get("Corrections"), " recoveries=", sync.get("Recoveries"),
                " moved=", moved.size(), " rtt=", sync.get("Rtt"),
                " max_history=", sync.get("MaximumHistory"), " max_queue=", sync.get("MaximumQueue"),
                " max_replay_ms=", sync.get("MaximumReplayMilliseconds"), " max_correction=", sync.get("MaximumCorrectionDistance"),
                " max_publication_ms=", sync.get("MaximumPublicationMilliseconds"))
            if moved.size() < players:
                push_error("Not every ship moved")
                quit(1)
            else:
                print("SYNC_SMOKE passed")
                quit(0)
            return
    push_error("SYNC_SMOKE timeout")
    quit(1)
