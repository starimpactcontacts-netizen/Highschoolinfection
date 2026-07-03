extends Node3D
## The match. The server owns all authoritative logic:
## spawning players, assigning zombies, running the timer, deciding win/loss,
## and locker occupancy / hiding. Clients mirror everything through RPCs.

const PLAYER_SCENE := preload("res://scenes/player.tscn")
const ROUND_TIME := 300.0     # 5 minutes to survive
const NUM_START_ZOMBIES := 2
const HIDE_TIME := 15.0        # seconds you can stay in a locker
const HIDE_COOLDOWN := 3.0

@onready var players_root: Node3D = $Players
@onready var spawn_points: Node3D = $SpawnPoints
@onready var storm: DirectionalLight3D = $Storm
@onready var clock_label: Label = $HUD/Root/Clock
@onready var status_label: Label = $HUD/Root/Status
@onready var prompt_label: Label = $HUD/Root/Prompt
@onready var start_button: Button = $HUD/Root/StartButton
@onready var infection_log: RichTextLabel = $HUD/Root/InfectionLog

var round_active := false
var time_left := 0.0
var result_text := ""
var _next_spawn := 0

var _names := {}            # server: peer_id -> display name
var _hidden := {}           # server: peer_id -> seconds left
var _player_locker := {}    # everyone: peer_id -> Locker node (for occupancy)

# Storm ambiance (local visual only)
var _storm_timer := 3.0
var _flash := 0.0

func _ready() -> void:
	add_to_group("game")
	start_button.pressed.connect(_on_start_pressed)
	_make_rain()
	if multiplayer.is_server():
		_names[1] = Net.my_name
		_spawn.rpc(1, _spawn_pos(0), Net.my_name)
		multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	else:
		request_roster.rpc_id(1, Net.my_name)

func _process(delta: float) -> void:
	_tick_storm(delta)
	if round_active:
		time_left -= delta
		if multiplayer.is_server():
			_tick_hidden(delta)
			var humans := 0
			for c in players_root.get_children():
				if is_instance_valid(c) and not c.is_zombie:
					humans += 1
			if humans <= 0:
				finish.rpc("ZOMBIES WIN")
			elif time_left <= 0.0:
				finish.rpc("HUMANS WIN")
	_update_hud()

## --- Spawning ---

func _spawn_pos(index: int) -> Vector3:
	var pts := spawn_points.get_children()
	if pts.is_empty():
		return Vector3(0, 1, 0)
	var m := pts[index % pts.size()] as Node3D
	return m.global_position

func _server_spawn(id: int, pname: String) -> void:
	if not multiplayer.is_server():
		return
	var idx := _next_spawn
	_next_spawn += 1
	_spawn.rpc(id, _spawn_pos(idx), pname)

@rpc("any_peer", "call_local", "reliable")
func request_roster(pname: String) -> void:
	if not multiplayer.is_server():
		return
	var sender := multiplayer.get_remote_sender_id()
	_names[sender] = pname
	# Catch the newcomer up on everyone already here.
	for child in players_root.get_children():
		var cid := int(str(child.name))
		_spawn.rpc_id(sender, cid, child.global_position, String(_names.get(cid, "Player")))
	# Spawn the newcomer for everyone.
	_server_spawn(sender, pname)

@rpc("any_peer", "call_local", "reliable")
func _spawn(id: int, pos: Vector3, pname: String) -> void:
	if players_root.has_node(str(id)):
		return
	var p := PLAYER_SCENE.instantiate()
	p.name = str(id)
	p.position = pos
	# Authority must be set before entering the tree so _ready() runs for the owner.
	p.set_multiplayer_authority(id)
	players_root.add_child(p)
	p.setup(id, pname)

func _on_peer_disconnected(id: int) -> void:
	_hidden.erase(id)
	_despawn.rpc(id)

@rpc("any_peer", "call_local", "reliable")
func _despawn(id: int) -> void:
	_release_player_locker(id)
	var n := players_root.get_node_or_null(str(id))
	if n:
		n.queue_free()

## --- Round flow ---

func _on_start_pressed() -> void:
	if multiplayer.is_server():
		_start_round()

func _start_round() -> void:
	if not multiplayer.is_server() or round_active:
		return
	var ids: Array = []
	for c in players_root.get_children():
		ids.append(int(str(c.name)))
	if ids.is_empty():
		return
	ids.shuffle()
	var zombie_count: int = mini(NUM_START_ZOMBIES, maxi(1, ids.size() - 1))
	for i in range(zombie_count):
		net_infect.rpc(ids[i])
	begin.rpc(ROUND_TIME)

@rpc("call_local", "reliable")
func begin(t: float) -> void:
	time_left = t
	result_text = ""
	round_active = true
	if infection_log:
		infection_log.clear()

@rpc("call_local", "reliable")
func finish(msg: String) -> void:
	round_active = false
	result_text = msg

## --- Infection ---

@rpc("any_peer", "reliable")
func request_infect(target_id: int) -> void:
	if not multiplayer.is_server() or not round_active:
		return
	var n := players_root.get_node_or_null(str(target_id))
	if n and not n.is_zombie and not n.is_hidden:
		net_infect.rpc(target_id)

@rpc("call_local", "reliable")
func net_infect(target_id: int) -> void:
	var n := players_root.get_node_or_null(str(target_id))
	if n:
		SFX.play("infect", n.global_position)
		_log_infection(n.display_name)
		n.become_zombie()

func _log_infection(name: String) -> void:
	if infection_log == null:
		return
	var timestamp := Time.get_ticks_msec()
	infection_log.append_text("[color=ff5555][%s] %s infected[/color]\n" % [
		_format_time(timestamp),
		name
	])

## --- Hiding in lockers ---

func _tick_hidden(delta: float) -> void:
	for id in _hidden.keys():
		_hidden[id] -= delta
		if _hidden[id] <= 0.0:
			_hidden.erase(id)
			net_set_hidden.rpc(id, "", 0.0)

## When the server calls an rpc_id(1) on itself, the sender id comes back as 0.
## Fall back to our own id so the host can hide/leave like any client.
func _sender() -> int:
	var s := multiplayer.get_remote_sender_id()
	return s if s != 0 else multiplayer.get_unique_id()

@rpc("any_peer", "reliable")
func request_hide(locker_name: String) -> void:
	if not multiplayer.is_server() or not round_active:
		return
	var sender := _sender()
	var pl := players_root.get_node_or_null(str(sender))
	if pl == null or pl.is_zombie or pl.is_hidden:
		return
	var locker := _find_locker(locker_name)
	if locker == null or locker.occupied:
		return
	_hidden[sender] = HIDE_TIME
	net_set_hidden.rpc(sender, locker_name, HIDE_TIME)

@rpc("any_peer", "reliable")
func request_leave() -> void:
	if not multiplayer.is_server():
		return
	var sender := _sender()
	if _hidden.has(sender):
		_hidden.erase(sender)
		net_set_hidden.rpc(sender, "", 0.0)

@rpc("call_local", "reliable")
func net_set_hidden(id: int, locker_name: String, dur: float) -> void:
	_release_player_locker(id)
	var pl := players_root.get_node_or_null(str(id))
	if dur > 0.0:
		var locker := _find_locker(locker_name)
		if locker:
			locker.occupied = true
			_player_locker[id] = locker
			if pl:
				pl.set_hidden(true, locker, dur)
	else:
		if pl:
			pl.set_hidden(false, null, 0.0)

func _release_player_locker(id: int) -> void:
	if _player_locker.has(id):
		var l = _player_locker[id]
		if is_instance_valid(l):
			l.occupied = false
			l.set_open(false)
		_player_locker.erase(id)

func _find_locker(locker_name: String) -> Node:
	for l in get_tree().get_nodes_in_group("locker"):
		if String(l.name) == locker_name:
			return l
	return null

## --- Storm ambiance (client-side visual only) ---

func _tick_storm(delta: float) -> void:
	if storm == null:
		return
	_storm_timer -= delta
	if _storm_timer <= 0.0:
		_storm_timer = randf_range(4.0, 11.0)
		_flash = 0.18
	if _flash > 0.0:
		_flash -= delta
		storm.light_energy = 3.5 if fmod(_flash * 45.0, 2.0) < 1.0 else 0.4
	else:
		storm.light_energy = 0.12

func _make_rain() -> void:
	var p := GPUParticles3D.new()
	p.name = "Rain"
	p.amount = 220
	p.lifetime = 1.1
	p.visibility_aabb = AABB(Vector3(-10, -6, -26), Vector3(40, 24, 52))
	p.position = Vector3(8, 10, 0)
	var pm := ParticleProcessMaterial.new()
	pm.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_BOX
	pm.emission_box_extents = Vector3(7, 0.2, 24)
	pm.direction = Vector3(0, -1, 0)
	pm.spread = 3.0
	pm.gravity = Vector3(0, -30, 0)
	pm.initial_velocity_min = 12.0
	pm.initial_velocity_max = 16.0
	p.process_material = pm
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.02, 0.5, 0.02)
	var mm := StandardMaterial3D.new()
	mm.albedo_color = Color(0.6, 0.7, 0.9, 0.55)
	mm.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mm.emission_enabled = true
	mm.emission = Color(0.35, 0.45, 0.65)
	mesh.material = mm
	p.draw_pass_1 = mesh
	add_child(p)

## --- Event Logging ---

func _format_time(ms: int) -> String:
	var secs := int(ms / 1000.0)
	var mins := secs / 60
	return "%d:%02d" % [mins, secs % 60]

## --- HUD ---

func _local_player() -> Node:
	return players_root.get_node_or_null(str(multiplayer.get_unique_id()))

func _update_hud() -> void:
	var is_host := multiplayer.is_server()
	start_button.visible = is_host and not round_active and result_text == ""

	var local := _local_player()
	var prompt_text := local.prompt if local else ""

	# Add threat indicator to prompt
	if local and not local.is_zombie and local._threat_level > 0.5:
		var threat_indicator := " ⚠️ DANGER!" if local._threat_level > 0.8 else " ⚠️"
		prompt_text += threat_indicator

	prompt_label.text = prompt_text

	if result_text != "":
		clock_label.text = result_text
		status_label.text = "Round over"
		return

	if round_active:
		var humans := 0
		var zombies := 0
		for c in players_root.get_children():
			if c.is_zombie:
				zombies += 1
			else:
				humans += 1
		var secs := int(max(0.0, time_left))
		clock_label.text = "%02d:%02d" % [secs / 60, secs % 60]
		status_label.text = "🧑 %d   vs   🧟 %d" % [humans, zombies]
	else:
		clock_label.text = "LOBBY"
		var n := players_root.get_child_count()
		if is_host:
			status_label.text = "%d in school — press START ROUND" % n
		else:
			status_label.text = "%d in school — waiting for host…" % n
