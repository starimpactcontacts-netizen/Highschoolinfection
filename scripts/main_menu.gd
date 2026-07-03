extends Control
## Title screen: host or join a game.

@onready var name_edit: LineEdit = $Center/Box/NameEdit
@onready var ip_edit: LineEdit = $Center/Box/IpEdit
@onready var host_button: Button = $Center/Box/HostButton
@onready var join_button: Button = $Center/Box/JoinButton
@onready var info: Label = $Center/Box/Info

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	# In case we came back from a match, drop any old connection.
	Net.leave()
	host_button.pressed.connect(_on_host)
	join_button.pressed.connect(_on_join)

func _on_host() -> void:
	Net.my_name = _name_or_default("Host")
	info.text = "Hosting..."
	if not Net.host_game():
		info.text = "Failed to host. Is the port in use?"

func _on_join() -> void:
	Net.my_name = _name_or_default("Player")
	info.text = "Connecting to %s..." % _ip()
	if not Net.join_game(_ip()):
		info.text = "Could not start connection."

func _name_or_default(fallback: String) -> String:
	var n := name_edit.text.strip_edges()
	return n if n != "" else fallback

func _ip() -> String:
	var v := ip_edit.text.strip_edges()
	return v if v != "" else "127.0.0.1"
