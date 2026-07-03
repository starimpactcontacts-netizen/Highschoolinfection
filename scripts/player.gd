extends CharacterBody3D
## A networked player. Every player uses this same script.
## The player is a "human" until infected, then becomes a "zombie".
## The peer that owns this node (its multiplayer authority) drives movement;
## everyone else just smooths toward the last synced transform.

const HUMAN_SPEED := 5.0
const ZOMBIE_SPEED := 5.7
const JUMP_VELOCITY := 4.5
const MOUSE_SENS := 0.0025
const INFECT_INTERVAL := 0.2

var is_zombie := false
var peer_id := 1

@onready var head: Node3D = $Head
@onready var camera: Camera3D = $Head/Camera3D
@onready var body_mesh: MeshInstance3D = $Body
@onready var infect_area: Area3D = $InfectArea

var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))
var _sync_pos: Vector3
var _sync_yaw: float
var _sync_pitch: float
var _infect_cooldown := 0.0

func setup(id: int) -> void:
	peer_id = id
	_apply_team_look()

func _ready() -> void:
	_sync_pos = global_position
	_sync_yaw = rotation.y
	if is_multiplayer_authority():
		camera.current = true
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	else:
		camera.current = false
	_apply_team_look()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and camera.current:
		rotate_y(-event.relative.x * MOUSE_SENS)
		head.rotate_x(-event.relative.y * MOUSE_SENS)
		head.rotation.x = clampf(head.rotation.x, -1.4, 1.4)
	if event.is_action_pressed("ui_cancel") and is_multiplayer_authority():
		if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		else:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _physics_process(delta: float) -> void:
	if is_multiplayer_authority():
		_drive_local(delta)
		remote_state.rpc(global_position, rotation.y, head.rotation.x)
		if is_zombie:
			_try_infect(delta)
	else:
		global_position = global_position.lerp(_sync_pos, 0.25)
		rotation.y = lerp_angle(rotation.y, _sync_yaw, 0.25)
		head.rotation.x = lerp_angle(head.rotation.x, _sync_pitch, 0.25)

func _drive_local(delta: float) -> void:
	if not is_on_floor():
		velocity.y -= _gravity * delta
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var dir := (transform.basis * Vector3(input_dir.x, 0.0, input_dir.y)).normalized()
	var speed := ZOMBIE_SPEED if is_zombie else HUMAN_SPEED
	if dir != Vector3.ZERO:
		velocity.x = dir.x * speed
		velocity.z = dir.z * speed
	else:
		velocity.x = move_toward(velocity.x, 0.0, speed)
		velocity.z = move_toward(velocity.z, 0.0, speed)
	move_and_slide()

## Zombie authority scans for humans it is touching and asks the server to infect them.
func _try_infect(delta: float) -> void:
	_infect_cooldown -= delta
	if _infect_cooldown > 0.0:
		return
	_infect_cooldown = INFECT_INTERVAL
	for body in infect_area.get_overlapping_bodies():
		if body is CharacterBody3D and body != self and not body.is_zombie:
			var game := get_tree().get_first_node_in_group("game")
			if game:
				game.request_infect.rpc_id(1, int(str(body.name)))

@rpc("authority", "unreliable_ordered")
func remote_state(pos: Vector3, yaw: float, pitch: float) -> void:
	_sync_pos = pos
	_sync_yaw = yaw
	_sync_pitch = pitch

## Called by the server (via game.gd) when this player gets infected.
func become_zombie() -> void:
	if is_zombie:
		return
	is_zombie = true
	_apply_team_look()

func _apply_team_look() -> void:
	if body_mesh == null:
		return
	var mat := StandardMaterial3D.new()
	if is_zombie:
		mat.albedo_color = Color(0.65, 0.15, 0.15)
		mat.emission_enabled = true
		mat.emission = Color(0.5, 0.05, 0.05)
		mat.emission_energy_multiplier = 0.6
	else:
		mat.albedo_color = Color(0.30, 0.50, 0.90)
	body_mesh.set_surface_override_material(0, mat)
