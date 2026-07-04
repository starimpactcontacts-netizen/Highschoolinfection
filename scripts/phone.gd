class_name PhoneLayer
extends CanvasLayer
## The pause menu, styled as an in-game smartphone. Opens with Enter.
## Home screen is an app grid; each app is a small self-contained panel:
##   Calendar — week strip + upcoming events
##   Camera   — closes the phone into photo mode
##   Schemes  — a checklist of (tongue-in-cheek) plans
##   Students — profile cards for the student body
##   Settings — live sliders wired to State (sanity/reputation demo!)

const PINK := Color(1.0, 0.42, 0.72)
const BG := Color(0.07, 0.03, 0.08)
const PANEL := Color(0.12, 0.06, 0.13)

# Original demo roster (name, club, personality)
const STUDENTS := [
	["Hana Mori", "Gardening", "Social Butterfly"],
	["Riko Tanabe", "Art", "Loner"],
	["Yui Katsu", "Cooking", "Teacher's Pet"],
	["Mei Aizawa", "Occult", "Coward"],
	["Sora Kimura", "Sports", "Hero"],
	["Nao Fujita", "Science", "Evil"],
	["Aya Shimizu", "Drama", "Dangerous"],
	["Ken Watari", "None", "Sleuth"],
]

var _dim: ColorRect
var _phone: PanelContainer
var _content: VBoxContainer
var _status_clock: Label
var _open := false

func _ready() -> void:
	layer = 3
	visible = false

	_dim = ColorRect.new()
	_dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	_dim.color = Color(0, 0, 0, 0.55)
	add_child(_dim)

	_phone = PanelContainer.new()
	_phone.set_anchors_preset(Control.PRESET_CENTER)
	_phone.custom_minimum_size = Vector2(400, 660)
	_phone.offset_left = -200
	_phone.offset_top = -330
	_phone.offset_right = 200
	_phone.offset_bottom = 330
	var sb := StyleBoxFlat.new()
	sb.bg_color = BG
	sb.set_border_width_all(3)
	sb.border_color = PINK
	sb.set_corner_radius_all(28)
	sb.set_content_margin_all(16)
	_phone.add_theme_stylebox_override("panel", sb)
	add_child(_phone)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 10)
	_phone.add_child(vbox)

	# Status bar
	var status := HBoxContainer.new()
	_status_clock = _mk_label("07:00 AM", 15, Color(0.9, 0.85, 0.95))
	status.add_child(_status_clock)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status.add_child(spacer)
	status.add_child(_mk_label("AKA-NET 5G  ▮▮▮▯  92%", 13, Color(0.75, 0.7, 0.85)))
	vbox.add_child(status)

	_content = VBoxContainer.new()
	_content.add_theme_constant_override("separation", 10)
	_content.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(_content)

	_show_home()

func _process(_delta: float) -> void:
	if _open:
		_status_clock.text = State.clock_text()

# ------------------------------------------------------------ open/close

func open() -> void:
	_open = true
	visible = true
	State.ui_open = true
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	_show_home()

func close(recapture := true) -> void:
	_open = false
	visible = false
	State.ui_open = false
	if recapture:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func is_open() -> bool:
	return _open

# ------------------------------------------------------------ home grid

func _clear() -> void:
	for c in _content.get_children():
		c.queue_free()

func _show_home() -> void:
	_clear()
	_content.add_child(_mk_label("M Y   P H O N E", 20, PINK, true))

	var grid := GridContainer.new()
	grid.columns = 3
	grid.add_theme_constant_override("h_separation", 14)
	grid.add_theme_constant_override("v_separation", 14)
	grid.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	_content.add_child(grid)

	_app_icon(grid, "📅", "Calendar", Color(0.85, 0.35, 0.45), _show_calendar)
	_app_icon(grid, "📷", "Camera", Color(0.30, 0.32, 0.40), _launch_camera)
	_app_icon(grid, "📋", "Schemes", Color(0.55, 0.20, 0.60), _show_schemes)
	_app_icon(grid, "👥", "Students", Color(0.25, 0.55, 0.80), _show_students)
	_app_icon(grid, "⚙", "Settings", Color(0.45, 0.47, 0.52), _show_settings)
	_app_icon(grid, "✖", "Close", Color(0.70, 0.15, 0.25), func(): close())

	var tip := _mk_label("The world is frozen while your phone is out.", 13, Color(0.7, 0.65, 0.8))
	tip.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_content.add_child(tip)

func _app_icon(grid: GridContainer, glyph: String, name_txt: String, col: Color, cb: Callable) -> void:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 4)
	var b := Button.new()
	b.text = glyph
	b.custom_minimum_size = Vector2(96, 96)
	b.add_theme_font_size_override("font_size", 40)
	b.focus_mode = Control.FOCUS_NONE
	var sb := StyleBoxFlat.new()
	sb.bg_color = col
	sb.set_corner_radius_all(22)
	b.add_theme_stylebox_override("normal", sb)
	var sbh := sb.duplicate()
	sbh.bg_color = col.lightened(0.15)
	b.add_theme_stylebox_override("hover", sbh)
	var sbp := sb.duplicate()
	sbp.bg_color = col.darkened(0.2)
	b.add_theme_stylebox_override("pressed", sbp)
	b.pressed.connect(cb)
	v.add_child(b)
	var l := _mk_label(name_txt, 14, Color(0.92, 0.9, 0.97), true)
	v.add_child(l)
	grid.add_child(v)

# ------------------------------------------------------------ apps

func _app_frame(title: String) -> VBoxContainer:
	_clear()
	var head := HBoxContainer.new()
	var back := Button.new()
	back.text = "←"
	back.add_theme_font_size_override("font_size", 22)
	back.focus_mode = Control.FOCUS_NONE
	back.pressed.connect(_show_home)
	head.add_child(back)
	var t := _mk_label(title, 20, PINK)
	t.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	t.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	head.add_child(t)
	var pad := Control.new()
	pad.custom_minimum_size = Vector2(40, 0)
	head.add_child(pad)
	_content.add_child(head)

	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_content.add_child(scroll)
	var body := VBoxContainer.new()
	body.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 8)
	scroll.add_child(body)
	return body

func _show_calendar() -> void:
	var body := _app_frame("CALENDAR — WEEK 1")
	var strip := HBoxContainer.new()
	strip.add_theme_constant_override("separation", 6)
	strip.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	for d in ["MON", "TUE", "WED", "THU", "FRI"]:
		var chip := PanelContainer.new()
		var sb := StyleBoxFlat.new()
		sb.bg_color = PINK if d == "MON" else PANEL
		sb.set_corner_radius_all(10)
		sb.set_content_margin_all(8)
		chip.add_theme_stylebox_override("panel", sb)
		var l := _mk_label(d, 15, Color(0.1, 0.04, 0.08) if d == "MON" else Color(0.85, 0.8, 0.9))
		chip.add_child(l)
		strip.add_child(chip)
	body.add_child(strip)

	body.add_child(_mk_label("UPCOMING", 14, Color(0.8, 0.6, 0.75)))
	_event_row(body, "WED", "Midterm exams begin", Color(0.85, 0.75, 0.4))
	_event_row(body, "THU", "Occult Club meeting — 5:30 PM", Color(0.6, 0.4, 0.85))
	_event_row(body, "FRI", "⚠ A confession under the cherry tree — 6:00 PM", Color(0.95, 0.3, 0.4))

func _event_row(body: VBoxContainer, day: String, text: String, col: Color) -> void:
	var p := PanelContainer.new()
	var sb := StyleBoxFlat.new()
	sb.bg_color = PANEL
	sb.set_corner_radius_all(10)
	sb.set_content_margin_all(10)
	sb.border_width_left = 4
	sb.border_color = col
	p.add_theme_stylebox_override("panel", sb)
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 12)
	var d := _mk_label(day, 15, col)
	h.add_child(d)
	var t := _mk_label(text, 14, Color(0.92, 0.9, 0.96))
	t.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	t.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	h.add_child(t)
	p.add_child(h)
	body.add_child(p)

func _launch_camera() -> void:
	close(true)
	State.camera_mode = true

func _show_schemes() -> void:
	var body := _app_frame("SCHEMES")
	body.add_child(_mk_label("Today's to-do list:", 14, Color(0.8, 0.6, 0.75)))
	for item in [
		"Arrive at school without being noticed",
		"Learn every student's daily routine",
		"Join a club to gain access to its room",
		"Find out what the Occult Club is hiding",
		"Be home before the streetlights come on",
	]:
		var cb := CheckBox.new()
		cb.text = item
		cb.focus_mode = Control.FOCUS_NONE
		cb.add_theme_font_size_override("font_size", 14)
		cb.add_theme_color_override("font_color", Color(0.92, 0.9, 0.96))
		body.add_child(cb)

func _show_students() -> void:
	var body := _app_frame("STUDENT INFO")
	for s in STUDENTS:
		var p := PanelContainer.new()
		var sb := StyleBoxFlat.new()
		sb.bg_color = PANEL
		sb.set_corner_radius_all(12)
		sb.set_content_margin_all(10)
		p.add_theme_stylebox_override("panel", sb)
		var h := HBoxContainer.new()
		h.add_theme_constant_override("separation", 12)

		var pic := PanelContainer.new()
		var psb := StyleBoxFlat.new()
		var name_str: String = s[0]
		psb.bg_color = Color.from_hsv(fmod(float(name_str.hash()) / 1000.0, 1.0), 0.45, 0.8)
		psb.set_corner_radius_all(24)
		psb.set_content_margin_all(4)
		pic.add_theme_stylebox_override("panel", psb)
		pic.custom_minimum_size = Vector2(48, 48)
		var initials := _mk_label(name_str.substr(0, 1), 22, Color(0.1, 0.05, 0.1), true)
		initials.size_flags_vertical = Control.SIZE_EXPAND_FILL
		initials.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		pic.add_child(initials)
		h.add_child(pic)

		var v := VBoxContainer.new()
		v.add_child(_mk_label(name_str, 16, Color(0.96, 0.92, 0.98)))
		v.add_child(_mk_label("Club: %s   ·   %s" % [s[1], s[2]], 13, Color(0.75, 0.68, 0.82)))
		h.add_child(v)
		p.add_child(h)
		body.add_child(p)

func _show_settings() -> void:
	var body := _app_frame("SETTINGS")
	body.add_child(_mk_label("DEMO CONTROLS — move these and close the phone", 13, Color(0.8, 0.6, 0.75)))

	_slider(body, "Sanity  (drains color from the world)", 0, 100, State.sanity,
		func(v: float): State.sanity = v)
	_slider(body, "Reputation", -100, 100, State.reputation,
		func(v: float): State.reputation = v)
	_slider(body, "Mouse sensitivity", 1, 10, State.mouse_sens * 2000.0,
		func(v: float): State.mouse_sens = v / 2000.0)
	_slider(body, "Clock speed (game-min / sec)", 0, 30, State.clock_speed,
		func(v: float): State.clock_speed = v)

	body.add_child(_mk_label("Akademi UI Demo — everything on screen is\ngenerated in code: no textures, no models, no assets.",
		12, Color(0.65, 0.6, 0.75)))

func _slider(body: VBoxContainer, label: String, mn: float, mx: float, val: float, cb: Callable) -> void:
	body.add_child(_mk_label(label, 14, Color(0.92, 0.9, 0.96)))
	var s := HSlider.new()
	s.min_value = mn
	s.max_value = mx
	s.value = val
	s.focus_mode = Control.FOCUS_NONE
	s.value_changed.connect(cb)
	body.add_child(s)

func _mk_label(txt: String, size: int, color: Color, center := false) -> Label:
	var l := Label.new()
	l.text = txt
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	if center:
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	return l
