extends CharacterBody3D
## A networked player. Every player uses this same script.
## Human until infected, then a zombie. The owning peer (multiplayer authority)
## drives movement; everyone else smooths toward the last synced transform.
## Humans can duck into a locker for a few seconds to break line of sight.

const HUMAN_SPEED := 5.0
const ZOMBIE_SPEED := 5.7
const JUMP_VELOCITY := 4.5
const MOUSE_SENS := 0.0025
const INFECT_INTERVAL := 0.2
const LOCKER_REACH := 2.4

var is_zombie := false
var is_hidden := false
var peer_id := 1
var display_name := "Player"

# Local-only helpers the HUD reads off the local player:
var prompt := ""            # e.g. "[E] Hide" / "[E] Leave"
var hide_time_left := 0.0

@onready var head: Node3D = $Head
@onready var camera: Camera3D = $Head/Camera3D
@onready var body_mesh: MeshInstance3D = $Body
@onready var infect_area: Area3D = $InfectArea
@onready var name_tag: Label3D = $Name

var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))
var _sync_pos: Vector3
var _sync_yaw: float
var _sync_pitch: float
var _infect_cooldown := 0.0
var _hide_cooldown := 0.0
var _near_locker: Node = null

func setup(id: int, pname: String) -> void:
	peer_id = id
	display_name = pname
	_apply_team_look()

func _ready() -> void:
	_sync_pos = global_position
	_sync_yaw = rotation.y
	camera.current = is_multiplayer_authority()
	if is_multiplayer_authority():
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
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

func _process(delta: float) -> void:
	if hide_time_left > 0.0:
		hide_time_left = maxf(0.0, hide_time_left - delta)
	if is_multiplayer_authority():
		_hide_cooldown = maxf(0.0, _hide_cooldown - delta)
		_update_interaction()

func _physics_process(delta: float) -> void:
	if not is_multiplayer_authority():
		global_position = global_position.lerp(_sync_pos, 0.25)
		rotation.y = lerp_angle(rotation.y, _sync_yaw, 0.25)
		head.rotation.x = lerp_angle(head.rotation.x, _sync_pitch, 0.25)
		return
	if is_hidden:
		velocity = Vector3.ZERO
		return
	_drive_local(delta)
	remote_state.rpc(global_position, rotation.y, head.rotation.x)
	if is_zombie:
		_try_infect(delta)

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

## Local player: work out the current interaction prompt and act on E.
func _update_interaction() -> void:
	var game := get_tree().get_first_node_in_group("game")
	if is_hidden:
		prompt = "[E] Leave  (%d)" % ceili(hide_time_left)
		if Input.is_action_just_pressed("interact") and game:
			game.request_leave.rpc_id(1)
		return
	_near_locker = _closest_free_locker()
	if is_zombie:
		prompt = ""      # zombies can't hide
	elif _near_locker != null and _hide_cooldown <= 0.0:
		prompt = "[E] Hide"
		if Input.is_action_just_pressed("interact") and game:
			game.request_hide.rpc_id(1, String(_near_locker.name))
	else:
		prompt = ""

func _closest_free_locker() -> Node:
	var best: Node = null
	var best_d := LOCKER_REACH
	for l in get_tree().get_nodes_in_group("locker"):
		if l.occupied:
			continue
		var d := global_position.distance_to(l.global_position)
		if d < best_d:
			best_d = d
			best = l
	return best

## Zombie authority scans for humans it is touching and asks the server to infect them.
func _try_infect(delta: float) -> void:
	_infect_cooldown -= delta
	if _infect_cooldown > 0.0:
		return
	_infect_cooldown = INFECT_INTERVAL
	for body in infect_area.get_overlapping_bodies():
		if body is CharacterBody3D and body != self and not body.is_zombie and not body.is_hidden:
			var game := get_tree().get_first_node_in_group("game")
			if game:
				game.request_infect.rpc_id(1, int(str(body.name)))

@rpc("authority", "unreliable_ordered")
func remote_state(pos: Vector3, yaw: float, pitch: float) -> void:
	_sync_pos = pos
	_sync_yaw = yaw
	_sync_pitch = pitch

## --- State changes driven by the server via game.gd ---

func become_zombie() -> void:
	if is_zombie:
		return
	is_zombie = true
	_apply_team_look()

func set_hidden(hidden: bool, locker: Node, duration: float) -> void:
	is_hidden = hidden
	if hidden:
		if locker:
			global_transform = locker.get_hide_transform()
			locker.set_open(false)
		hide_time_left = duration
		velocity = Vector3.ZERO
		_set_visible(false)
		set_collision_layer_value(1, false)
		infect_area.monitoring = false
	else:
		hide_time_left = 0.0
		_set_visible(true)
		set_collision_layer_value(1, true)
		infect_area.monitoring = true
		if is_multiplayer_authority():
			_hide_cooldown = 3.0

func _set_visible(v: bool) -> void:
	body_mesh.visible = v
	name_tag.visible = v

func _apply_team_look() -> void:
	if name_tag:
		name_tag.text = display_name
		name_tag.modulate = Color(1, 0.5, 0.55) if is_zombie else Color(0.6, 0.85, 1)
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
