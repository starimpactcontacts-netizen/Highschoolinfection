extends Control
## Character customization screen — the UI proof.
## Left: a live preview doll driven by the Cosmetics selection.
## Right: one section per category, each a row of rarity-bordered swatches.
## Everything is built in code so it renders identically no matter the editor.

const PINK := Color(1.0, 0.42, 0.72)
const PANEL_BG := Color(0.09, 0.05, 0.11, 0.92)

var _preview: CharPreview
var _swatches := {}       # category -> Array[Button]
var _readouts := {}       # category -> Label (selected item name)

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_background()
	add_child(preload("res://scripts/petals.gd").new())
	_build_title()
	_build_preview_panel()
	_build_catalog_panel()
	_build_bottom_bar()

# ---------- background ----------

func _build_background() -> void:
	var bg := ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.05, 0.03, 0.07)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)
	var glow := ColorRect.new()
	glow.set_anchors_preset(Control.PRESET_FULL_RECT)
	glow.anchor_top = 0.55
	glow.color = Color(0.13, 0.03, 0.10)
	glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(glow)

func _build_title() -> void:
	var t := Label.new()
	t.text = "CHARACTER"
	t.add_theme_font_size_override("font_size", 40)
	t.add_theme_color_override("font_color", PINK)
	t.add_theme_color_override("font_outline_color", Color.BLACK)
	t.add_theme_constant_override("outline_size", 8)
	t.position = Vector2(60, 34)
	add_child(t)
	var sub := Label.new()
	sub.text = "Make them yours. Rarer looks drop in the shop later."
	sub.add_theme_font_size_override("font_size", 15)
	sub.add_theme_color_override("font_color", Color(0.82, 0.8, 0.92))
	sub.position = Vector2(62, 82)
	add_child(sub)

# ---------- left: live preview doll ----------

func _build_preview_panel() -> void:
	var panel := Panel.new()
	panel.position = Vector2(60, 120)
	panel.size = Vector2(430, 520)
	panel.add_theme_stylebox_override("panel", _panel_style(PANEL_BG, PINK, 2, 18))
	add_child(panel)

	_preview = CharPreview.new()
	_preview.position = Vector2(20, 16)
	_preview.size = Vector2(390, 430)
	_preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_child(_preview)

	# Little floor shadow under the doll.
	var name_lbl := Label.new()
	name_lbl.text = "YOUR STUDENT"
	name_lbl.add_theme_font_size_override("font_size", 20)
	name_lbl.add_theme_color_override("font_color", Color(0.95, 0.95, 1.0))
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.position = Vector2(20, 462)
	name_lbl.size = Vector2(390, 26)
	panel.add_child(name_lbl)

# ---------- right: scrollable catalog ----------

func _build_catalog_panel() -> void:
	var scroll := ScrollContainer.new()
	scroll.position = Vector2(520, 120)
	scroll.size = Vector2(700, 500)
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(scroll)

	var col := VBoxContainer.new()
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	col.custom_minimum_size = Vector2(684, 0)
	col.add_theme_constant_override("separation", 18)
	scroll.add_child(col)

	for category in Cosmetics.CATEGORY_ORDER:
		col.add_child(_build_category_section(category))

func _build_category_section(category: String) -> Control:
	var section := VBoxContainer.new()
	section.add_theme_constant_override("separation", 8)

	# Header: CATEGORY  ............  Selected Item (rarity-colored)
	var header := HBoxContainer.new()
	var label := Label.new()
	label.text = Cosmetics.CATEGORY_LABEL[category]
	label.add_theme_font_size_override("font_size", 18)
	label.add_theme_color_override("font_color", PINK)
	header.add_child(label)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(spacer)
	var readout := Label.new()
	readout.add_theme_font_size_override("font_size", 16)
	header.add_child(readout)
	_readouts[category] = readout
	section.add_child(header)

	# Swatch row (wraps).
	var flow := HFlowContainer.new()
	flow.add_theme_constant_override("h_separation", 10)
	flow.add_theme_constant_override("v_separation", 10)
	var buttons: Array[Button] = []
	var items: Array = Cosmetics.catalog[category]
	for i in range(items.size()):
		var b := _make_swatch(category, i, items[i])
		buttons.append(b)
		flow.add_child(b)
	_swatches[category] = buttons
	section.add_child(flow)

	_refresh_category(category)
	return section

func _make_swatch(category: String, index: int, item: Dictionary) -> Button:
	var b := Button.new()
	b.custom_minimum_size = Vector2(72, 72)
	b.tooltip_text = "%s — %s" % [item["name"], item["rarity"]]
	b.focus_mode = Control.FOCUS_NONE
	# Color items show the color; style/accessory items show their name.
	if not item.has("color"):
		b.text = item["name"]
		b.add_theme_font_size_override("font_size", 11)
	b.pressed.connect(_on_swatch_pressed.bind(category, index))
	return b

# ---------- selection ----------

func _on_swatch_pressed(category: String, index: int) -> void:
	Cosmetics.selection[category] = index
	_refresh_category(category)
	_preview.refresh()

func _refresh_category(category: String) -> void:
	var items: Array = Cosmetics.catalog[category]
	var sel: int = Cosmetics.selection[category]
	var buttons: Array = _swatches[category]
	for i in range(buttons.size()):
		_style_swatch(buttons[i], items[i], i == sel)
	var item: Dictionary = items[sel]
	var r := Cosmetics.rarity_color(item["rarity"])
	var out: Label = _readouts[category]
	out.text = "%s · %s" % [item["name"], item["rarity"].to_upper()]
	out.add_theme_color_override("font_color", r)

func _style_swatch(b: Button, item: Dictionary, selected: bool) -> void:
	var rarity := Cosmetics.rarity_color(item["rarity"])
	var bg: Color = item.get("color", Color(0.13, 0.11, 0.16))
	var border := rarity if not selected else Color(1, 1, 1)
	var width := 4 if selected else 2
	for state in ["normal", "hover", "pressed", "focus"]:
		var use_bg := bg.lightened(0.08) if state == "hover" else bg
		b.add_theme_stylebox_override(state, _panel_style(use_bg, border, width, 10))
	# Text color for name-based swatches: readable over dark bg.
	if not item.has("color"):
		b.add_theme_color_override("font_color", Color(0.92, 0.92, 0.98))
		b.add_theme_color_override("font_hover_color", Color(1, 1, 1))
		b.add_theme_color_override("font_pressed_color", Color(1, 1, 1))

# ---------- bottom bar ----------

func _build_bottom_bar() -> void:
	var back := _make_button("← BACK", Vector2(60, 650), Vector2(150, 48))
	back.pressed.connect(func(): get_tree().change_scene_to_file("res://scenes/main_menu.tscn"))
	add_child(back)

	var rand := _make_button("🎲 RANDOMIZE", Vector2(230, 650), Vector2(190, 48))
	rand.pressed.connect(_on_randomize)
	add_child(rand)

	var done := _make_button("SAVE LOOK", Vector2(1050, 650), Vector2(170, 48))
	done.pressed.connect(func(): get_tree().change_scene_to_file("res://scenes/main_menu.tscn"))
	add_child(done)

func _on_randomize() -> void:
	for category in Cosmetics.CATEGORY_ORDER:
		var n: int = Cosmetics.catalog[category].size()
		Cosmetics.selection[category] = randi() % n
		_refresh_category(category)
	_preview.refresh()

func _make_button(text: String, pos: Vector2, sz: Vector2) -> Button:
	var b := Button.new()
	b.text = text
	b.position = pos
	b.size = sz
	b.add_theme_font_size_override("font_size", 18)
	b.add_theme_stylebox_override("normal", _panel_style(Color(0.15, 0.06, 0.14), PINK, 2, 10))
	b.add_theme_stylebox_override("hover", _panel_style(Color(0.28, 0.09, 0.22), PINK, 2, 10))
	b.add_theme_stylebox_override("pressed", _panel_style(Color(0.10, 0.04, 0.10), PINK, 2, 10))
	b.add_theme_color_override("font_color", Color(1, 0.85, 0.95))
	return b

func _panel_style(bg: Color, border: Color, width: int, radius: int) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = bg
	sb.set_border_width_all(width)
	sb.border_color = border
	sb.set_corner_radius_all(radius)
	sb.set_content_margin_all(6)
	return sb
