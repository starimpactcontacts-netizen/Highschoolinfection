extends Node3D
## The match. The server owns all authoritative logic:
## spawning players, assigning zombies, running the timer, deciding win/loss.
## Clients mirror everything through RPCs.

const PLAYER_SCENE := preload("res://scenes/player.tscn")
const ROUND_TIME := 300.0   # 5 minutes to survive
const NUM_START_ZOMBIES := 2

@onready var players_root: Node3D = $Players
@onready var spawn_points: Node3D = $SpawnPoints
@onready var status_label: Label = $HUD/Root/Status
@onready var start_button: Button = $HUD/Root/StartButton

var round_active := false
var time_left := 0.0
var result_text := ""
var _next_spawn := 0

func _ready() -> void:
	add_to_group("game")
	start_button.pressed.connect(_on_start_pressed)
	if multiplayer.is_server():
		_spawn.rpc(1, _spawn_pos(0))
		multiplayer.peer_disconnected.connect(_on_peer_disconnected)
	else:
		# Ask the server to catch us up on everyone already here.
		request_roster.rpc_id(1)

func _process(delta: float) -> void:
	if round_active:
		time_left -= delta
		if multiplayer.is_server():
			var humans := 0
			for c in players_root.get_children():
				if is_instance_valid(c) and not c.is_zombie:
					humans += 1
			if humans <= 0:
				finish.rpc("ZOMBIES WIN")
			elif time_left <= 0.0:
				finish.rpc("HUMANS WIN")
	_update_hud()

## --- Spawn positioning ---

func _spawn_pos(index: int) -> Vector3:
	var pts := spawn_points.get_children()
	if pts.is_empty():
		return Vector3(0, 1, 0)
	var m := pts[index % pts.size()] as Node3D
	return m.global_position

## --- Server-authoritative spawning ---

func _server_spawn(id: int) -> void:
	if not multiplayer.is_server():
		return
	var idx := _next_spawn
	_next_spawn += 1
	_spawn.rpc(id, _spawn_pos(idx))

@rpc("any_peer", "call_local", "reliable")
func request_roster() -> void:
	if not multiplayer.is_server():
		return
	var sender := multiplayer.get_remote_sender_id()
	# Send every existing player to just the new peer.
	for child in players_root.get_children():
		_spawn.rpc_id(sender, int(str(child.name)), child.global_position)
	# Now spawn the newcomer for everyone.
	_server_spawn(sender)

@rpc("any_peer", "call_local", "reliable")
func _spawn(id: int, pos: Vector3) -> void:
	if players_root.has_node(str(id)):
		return
	var p := PLAYER_SCENE.instantiate()
	p.name = str(id)
	p.position = pos
	# Authority must be set before the node enters the tree so _ready()
	# (camera, mouse capture, input) runs for the correct owner.
	p.set_multiplayer_authority(id)
	players_root.add_child(p)
	p.setup(id)

func _on_peer_disconnected(id: int) -> void:
	_despawn.rpc(id)

@rpc("any_peer", "call_local", "reliable")
func _despawn(id: int) -> void:
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

@rpc("call_local", "reliable")
func finish(msg: String) -> void:
	round_active = false
	result_text = msg

## Infection request from a zombie's owner; only the server acts on it.
@rpc("any_peer", "reliable")
func request_infect(target_id: int) -> void:
	if not multiplayer.is_server() or not round_active:
		return
	var n := players_root.get_node_or_null(str(target_id))
	if n and not n.is_zombie:
		net_infect.rpc(target_id)

@rpc("call_local", "reliable")
func net_infect(target_id: int) -> void:
	var n := players_root.get_node_or_null(str(target_id))
	if n:
		n.become_zombie()

## --- HUD ---

func _update_hud() -> void:
	var is_host := multiplayer.is_server()
	start_button.visible = is_host and not round_active and result_text == ""
	if result_text != "":
		status_label.text = result_text
	elif round_active:
		var humans := 0
		var zombies := 0
		for c in players_root.get_children():
			if c.is_zombie:
				zombies += 1
			else:
				humans += 1
		var secs := int(max(0.0, time_left))
		status_label.text = "%02d:%02d   Humans %d   Zombies %d" % [secs / 60, secs % 60, humans, zombies]
	else:
		var n := players_root.get_child_count()
		if is_host:
			status_label.text = "Lobby — %d in school. Press START ROUND." % n
		else:
			status_label.text = "Lobby — %d in school. Waiting for host..." % n
