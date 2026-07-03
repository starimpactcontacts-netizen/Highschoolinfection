extends Control
## Title screen: host, join, or customize your character.

const PINK := Color(1.0, 0.42, 0.72)

@onready var name_edit: LineEdit = $Center/Box/NameEdit
@onready var ip_edit: LineEdit = $Center/Box/IpEdit
@onready var host_button: Button = $Center/Box/HostButton
@onready var join_button: Button = $Center/Box/JoinButton
@onready var customize_button: Button = $Center/Box/CustomizeButton
@onready var info: Label = $Center/Box/Info

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	# In case we came back from a match, drop any old connection.
	Net.leave()

	# Falling sakura petals, tucked behind the menu box.
	var petals := preload("res://scripts/petals.gd").new()
	add_child(petals)
	move_child(petals, 2)

	host_button.pressed.connect(_on_host)
	join_button.pressed.connect(_on_join)
	customize_button.pressed.connect(_on_customize)

	_style_button(host_button, Color(0.20, 0.06, 0.16))
	_style_button(join_button, Color(0.14, 0.05, 0.13))
	_style_button(customize_button, Color(0.10, 0.05, 0.14))

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

func _on_customize() -> void:
	get_tree().change_scene_to_file("res://scenes/customization.tscn")

func _name_or_default(fallback: String) -> String:
	var n := name_edit.text.strip_edges()
	return n if n != "" else fallback

func _ip() -> String:
	var v := ip_edit.text.strip_edges()
	return v if v != "" else "127.0.0.1"

func _style_button(b: Button, bg: Color) -> void:
	b.add_theme_color_override("font_color", Color(1, 0.88, 0.96))
	b.add_theme_stylebox_override("normal", _sb(bg, PINK, 2))
	b.add_theme_stylebox_override("hover", _sb(bg.lightened(0.12), PINK, 2))
	b.add_theme_stylebox_override("pressed", _sb(bg.darkened(0.2), PINK, 2))

func _sb(bg: Color, border: Color, width: int) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = bg
	sb.set_border_width_all(width)
	sb.border_color = border
	sb.set_corner_radius_all(10)
	sb.set_content_margin_all(8)
	return sb
