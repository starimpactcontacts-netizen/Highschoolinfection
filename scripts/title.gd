extends Control
## Title screen: dark backdrop, drifting petals, pink menu.
## Menu items highlight with a ♥ marker on hover, anime-menu style.

const PINK := Color(1.0, 0.42, 0.72)

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	set_anchors_preset(Control.PRESET_FULL_RECT)

	var bg := ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.05, 0.02, 0.06)
	add_child(bg)

	var glow := ColorRect.new()
	glow.set_anchors_preset(Control.PRESET_FULL_RECT)
	glow.anchor_top = 0.6
	glow.color = Color(0.12, 0.03, 0.09)
	add_child(glow)

	add_child(Petals2D.new())

	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 12)
	box.custom_minimum_size = Vector2(460, 0)
	center.add_child(box)

	var title := Label.new()
	title.text = "A K A D E M I"
	title.add_theme_font_size_override("font_size", 64)
	title.add_theme_color_override("font_color", PINK)
	title.add_theme_color_override("font_outline_color", Color.BLACK)
	title.add_theme_constant_override("outline_size", 14)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(title)

	var sub := Label.new()
	sub.text = "UI  &  ENVIRONMENT  DEMO"
	sub.add_theme_font_size_override("font_size", 17)
	sub.add_theme_color_override("font_color", Color(0.85, 0.8, 0.92))
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(sub)

	var pad := Control.new()
	pad.custom_minimum_size = Vector2(0, 26)
	box.add_child(pad)

	_menu_button(box, "START", func(): get_tree().change_scene_to_file("res://scenes/school.tscn"))
	_menu_button(box, "EXIT", func(): get_tree().quit())

	var foot := Label.new()
	foot.text = "everything you will see is generated in code — no art assets"
	foot.add_theme_font_size_override("font_size", 13)
	foot.add_theme_color_override("font_color", Color(0.6, 0.55, 0.7))
	foot.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(foot)

func _menu_button(box: VBoxContainer, txt: String, cb: Callable) -> void:
	var b := Button.new()
	b.text = txt
	b.custom_minimum_size = Vector2(0, 52)
	b.add_theme_font_size_override("font_size", 24)
	b.focus_mode = Control.FOCUS_NONE
	b.flat = true
	b.add_theme_color_override("font_color", Color(0.92, 0.9, 0.96))
	b.add_theme_color_override("font_hover_color", PINK)
	b.add_theme_color_override("font_pressed_color", PINK.darkened(0.2))
	b.add_theme_color_override("font_outline_color", Color.BLACK)
	b.add_theme_constant_override("outline_size", 8)
	b.pressed.connect(cb)
	b.mouse_entered.connect(func(): b.text = "♥  " + txt)
	b.mouse_exited.connect(func(): b.text = txt)
	box.add_child(b)

## Procedural drifting petals for 2D screens.
class Petals2D extends Control:
	const COUNT := 40

	var _petals: Array = []
	var _rng := RandomNumberGenerator.new()

	func _ready() -> void:
		mouse_filter = Control.MOUSE_FILTER_IGNORE
		set_anchors_preset(Control.PRESET_FULL_RECT)
		_rng.randomize()
		for i in range(COUNT):
			_petals.append(_spawn(true))

	func _spawn(anywhere: bool) -> Dictionary:
		var vp := get_viewport_rect().size
		var shade := _rng.randf()
		return {
			"pos": Vector2(_rng.randf() * vp.x, (_rng.randf() * vp.y) if anywhere else -16.0),
			"vel": Vector2(_rng.randf_range(-26, -8), _rng.randf_range(30, 75)),
			"size": _rng.randf_range(5.0, 12.0),
			"spin": _rng.randf_range(-2.0, 2.0),
			"angle": _rng.randf() * TAU,
			"tint": Color(1.0, 0.55 + shade * 0.25, 0.72 + shade * 0.12, _rng.randf_range(0.5, 0.9)),
		}

	func _process(delta: float) -> void:
		var vp := get_viewport_rect().size
		for p in _petals:
			p["pos"] += p["vel"] * delta
			p["angle"] += p["spin"] * delta
			p["pos"].x += sin(p["angle"]) * 14.0 * delta
			if p["pos"].y > vp.y + 20.0 or p["pos"].x < -20.0:
				var np := _spawn(false)
				for k in np.keys():
					p[k] = np[k]
		queue_redraw()

	func _draw() -> void:
		for p in _petals:
			var s: float = p["size"]
			var pts := PackedVector2Array([
				Vector2(0, -s), Vector2(s * 0.6, 0),
				Vector2(0, s * 0.85), Vector2(-s * 0.6, 0)])
			var xf := Transform2D(p["angle"], p["pos"])
			var world := PackedVector2Array()
			for v in pts:
				world.append(xf * v)
			draw_colored_polygon(world, p["tint"])
