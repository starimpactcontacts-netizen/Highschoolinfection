class_name PlayerFP
extends CharacterBody3D
## First-person walker. Builds its own camera rig in code.
## WASD move, Shift run, mouse look (click to capture), E to interact with
## raycast targets, subtle head-bob while moving, camera jitter at low sanity.

const WALK_SPEED := 3.6
const RUN_SPEED := 6.4
const REACH := 2.6

var head: Node3D
var camera: Camera3D

var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))
var _bob_t := 0.0
var _base_fov := 72.0
var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	var cs := CollisionShape3D.new()
	var cap := CapsuleShape3D.new()
	cap.radius = 0.35
	cap.height = 1.7
	cs.shape = cap
	cs.position = Vector3(0, 0.85, 0)
	add_child(cs)

	head = Node3D.new()
	head.position = Vector3(0, 1.55, 0)
	add_child(head)

	camera = Camera3D.new()
	camera.fov = _base_fov
	camera.current = true
	head.add_child(camera)

	_rng.randomize()
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

func _unhandled_input(event: InputEvent) -> void:
	if State.ui_open:
		return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED and not State.camera_mode:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * State.mouse_sens)
		head.rotate_x(-event.relative.y * State.mouse_sens)
		head.rotation.x = clampf(head.rotation.x, -1.45, 1.45)

func _physics_process(delta: float) -> void:
	if not is_on_floor():
		velocity.y -= _gravity * delta

	var moving := false
	if not State.ui_open and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
		var dir := (transform.basis * Vector3(input_dir.x, 0.0, input_dir.y)).normalized()
		var running := Input.is_action_pressed("run")
		var speed := RUN_SPEED if running else WALK_SPEED
		if dir != Vector3.ZERO:
			velocity.x = dir.x * speed
			velocity.z = dir.z * speed
			moving = true
		else:
			velocity.x = move_toward(velocity.x, 0.0, speed)
			velocity.z = move_toward(velocity.z, 0.0, speed)
		camera.fov = lerpf(camera.fov, _base_fov + (6.0 if running and moving else 0.0), 8.0 * delta)
	else:
		velocity.x = move_toward(velocity.x, 0.0, WALK_SPEED)
		velocity.z = move_toward(velocity.z, 0.0, WALK_SPEED)
	move_and_slide()

	_head_motion(delta, moving)
	_update_interaction()

## Head bob while walking + shaky camera when sanity is low.
func _head_motion(delta: float, moving: bool) -> void:
	var y := 1.55
	if moving:
		_bob_t += delta * (11.0 if Input.is_action_pressed("run") else 8.0)
		y += sin(_bob_t) * 0.045
	var s := State.sanity / 100.0
	var jitter := 0.0
	if s < 0.5:
		jitter = (0.5 - s) * 0.05
	head.position.y = lerpf(head.position.y, y, 10.0 * delta)
	camera.rotation.z = lerpf(camera.rotation.z,
		_rng.randf_range(-jitter, jitter), 4.0 * delta)

## Raycast forward; publish the prompt, fire flavor text on E.
func _update_interaction() -> void:
	State.prompt = ""
	if State.ui_open or Input.mouse_mode != Input.MOUSE_MODE_CAPTURED:
		return
	var from := camera.global_position
	var to := from - camera.global_transform.basis.z * REACH
	var query := PhysicsRayQueryParameters3D.create(from, to)
	query.exclude = [get_rid()]
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	if hit.is_empty():
		return
	var collider: Object = hit.get("collider")
	if collider and collider.has_meta("iname"):
		var label: String = collider.get_meta("iname")
		State.prompt = "[E]  %s" % label
		if Input.is_action_just_pressed("interact"):
			State.toast_request = _flavor(label)

func _flavor(label: String) -> String:
	if label.ends_with("(locked)"):
		return "It's locked. Class hasn't started yet."
	match label:
		"Make a wish":
			return "You toss in a coin. Nothing happens... probably."
		"Buy a drink":
			return "The machine hums. You're not thirsty right now."
		"Buy a snack":
			return "Sold out. Typical."
		"Your shoe locker":
			return "Just your indoor shoes. No love letters today."
		"The incinerator":
			return "It's warm. Better not to ask why."
		"An ominous altar":
			return "You feel like you're being watched."
		_:
			return "There's no time for that right now."
