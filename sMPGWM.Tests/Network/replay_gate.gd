extends SceneTree
var frames := 0
var body: CharacterBody2D
func _initialize():
    var world := Node2D.new()
    root.add_child.call_deferred(world)
    await process_frame
    var wall := StaticBody2D.new()
    var collider := CollisionShape2D.new()
    var shape := RectangleShape2D.new()
    shape.size = Vector2(10, 200)
    collider.shape = shape
    wall.add_child(collider)
    wall.position = Vector2(50, 0)
    world.add_child(wall)
    body = CharacterBody2D.new()
    var polygon := CollisionPolygon2D.new()
    polygon.polygon = PackedVector2Array([Vector2(-5,-5), Vector2(5,-5), Vector2(5,5), Vector2(-5,5)])
    body.add_child(polygon)
    world.add_child(body)
func step():
    var motion := Vector2(10, 3)
    for i in range(4):
        var hit := body.move_and_collide(motion)
        if hit == null: break
        motion = hit.get_remainder().slide(hit.get_normal())
func _physics_process(_delta):
    if body == null: return false
    frames += 1
    if frames < 3: return false
    for i in range(12): step()
    var first := body.position
    body.position = Vector2.ZERO
    for i in range(12): step()
    var second := body.position
    if first.distance_to(second) > 0.001 or first.x > 40.01 or first.y < 30:
        push_error("Replay mismatch " + str(first) + " " + str(second))
        quit(1)
    else:
        print("REPLAY_GATE passed: polygon, wall slide, restore and 12 explicit steps in one physics callback ", first)
        quit(0)
    return false
