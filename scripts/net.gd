extends Node
## Global networking + input setup. Autoloaded as "Net".
## Handles hosting, joining, and switching into the game scene.

const PORT := 24565
const MAX_PLAYERS := 20
const GAME_SCENE := "res://scenes/game.tscn"

var my_name := "Player"

func _ready() -> void:
	_setup_input()
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)

## Build the input actions in code so we don't depend on editor input maps.
func _setup_input() -> void:
	_bind("move_forward", KEY_W)
	_bind("move_back", KEY_S)
	_bind("move_left", KEY_A)
	_bind("move_right", KEY_D)
	_bind("jump", KEY_SPACE)
	_bind("interact", KEY_E)

func _bind(action: String, keycode: Key) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action)
	var ev := InputEventKey.new()
	ev.physical_keycode = keycode
	InputMap.action_add_event(action, ev)

## --- Hosting / joining ---

func host_game() -> bool:
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_server(PORT, MAX_PLAYERS)
	if err != OK:
		push_error("Could not host on port %d (error %d)" % [PORT, err])
		return false
	multiplayer.multiplayer_peer = peer
	get_tree().change_scene_to_file(GAME_SCENE)
	return true

func join_game(ip: String) -> bool:
	if ip.strip_edges() == "":
		ip = "127.0.0.1"
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_client(ip, PORT)
	if err != OK:
		push_error("Could not connect to %s (error %d)" % [ip, err])
		return false
	multiplayer.multiplayer_peer = peer
	return true

func leave() -> void:
	multiplayer.multiplayer_peer = null

## --- Client-side connection callbacks ---

func _on_connected_to_server() -> void:
	get_tree().change_scene_to_file(GAME_SCENE)

func _on_connection_failed() -> void:
	push_error("Connection failed.")
	multiplayer.multiplayer_peer = null

func _on_server_disconnected() -> void:
	multiplayer.multiplayer_peer = null
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
