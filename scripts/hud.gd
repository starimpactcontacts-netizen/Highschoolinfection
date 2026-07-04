class_name HudLayer
extends CanvasLayer
## The in-world HUD, styled after edgy-cute anime UI:
##  - Top-right: big pink clock, day of week, school-day phase, FPS
##  - Bottom-right: pulsing heart with sanity % (beats faster as sanity drops)
##  - Bottom-center: reputation gradient bar with slider marker
##  - Center-bottom: interaction prompt + one-shot toast messages
##  - Photo mode: viewfinder corners + hint
##  - "YANDERE VISION" tag while the vision overlay is active

const PINK := Color(1.0, 0.42, 0.72)

var _clock: Label
var _day: Label
var _phase: Label
var _fps: Label
var _heart: HeartWidget
var _rep: RepWidget
var _prompt: Label
var _toast: Label
var _toast_timer := 0.0
var _vision_tag: Label
var _viewfinder: Viewfinder
var _hint: Label

func _ready() -> void:
	layer = 2

	var root := Control.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)

	# ---- Top-right clock block ----
	_clock = _label(root, 44, PINK, 10)
	_clock.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	_clock.offset_left = -320
	_clock.offset_top = 14
	_clock.offset_right = -24
	_clock.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

	_day = _label(root, 22, Color(0.95, 0.75, 0.86), 7)
	_day.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	_day.offset_left = -320
	_day.offset_top = 66
	_day.offset_right = -24
	_day.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

	_phase = _label(root, 18, Color(0.85, 0.85, 0.95), 6)
	_phase.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	_phase.offset_left = -320
	_phase.offset_top = 94
	_phase.offset_right = -24
	_phase.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

	_fps = _label(root, 13, Color(0.7, 0.7, 0.8), 4)
	_fps.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	_fps.offset_left = -320
	_fps.offset_top = 120
	_fps.offset_right = -24
	_fps.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

	# ---- Bottom-right heart / sanity ----
	_heart = HeartWidget.new()
	_heart.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_heart.offset_left = -150
	_heart.offset_top = -170
	_heart.offset_right = -20
	_heart.offset_bottom = -30
	_heart.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(_heart)

	# ---- Bottom-center reputation ----
	_rep = RepWidget.new()
	_rep.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_rep.offset_left = -220
	_rep.offset_top = -78
	_rep.offset_right = 220
	_rep.offset_bottom = -22
	_rep.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(_rep)

	# ---- Prompt + toast ----
	_prompt = _label(root, 24, PINK, 8)
	_prompt.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_prompt.offset_left = -300
	_prompt.offset_top = -150
	_prompt.offset_right = 300
	_prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

	_toast = _label(root, 20, Color(0.95, 0.92, 0.98), 7)
	_toast.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_toast.offset_left = -400
	_toast.offset_top = -200
	_toast.offset_right = 400
	_toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_toast.modulate.a = 0.0

	# ---- Vision tag ----
	_vision_tag = _label(root, 26, PINK, 9)
	_vision_tag.text = "YANDERE  VISION"
	_vision_tag.set_anchors_preset(Control.PRESET_TOP_LEFT)
	_vision_tag.offset_left = 24
	_vision_tag.offset_top = 18
	_vision_tag.offset_right = 400
	_vision_tag.visible = false

	# ---- Photo-mode viewfinder ----
	_viewfinder = Viewfinder.new()
	_viewfinder.set_anchors_preset(Control.PRESET_FULL_RECT)
	_viewfinder.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_viewfinder.visible = false
	root.add_child(_viewfinder)

	# ---- Controls hint (bottom-left) ----
	_hint = _label(root, 13, Color(0.78, 0.76, 0.88), 4)
	_hint.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	_hint.offset_left = 20
	_hint.offset_top = -168
	_hint.offset_right = 360
	_hint.text = "WASD  move      SHIFT  run\nMOUSE  look  (click to capture)\nE  interact      V  yandere vision\nENTER  phone      ESC  release mouse\n1 / 2  sanity demo      - / +  reputation demo"

func _label(parent: Control, size: int, color: Color, outline: int) -> Label:
	var l := Label.new()
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.add_theme_color_override("font_outline_color", Color(0.08, 0.03, 0.07))
	l.add_theme_constant_override("outline_size", outline)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(l)
	return l

func _process(delta: float) -> void:
	_clock.text = State.clock_text()
	_day.text = State.DAY_NAME
	_phase.text = State.phase_text()
	_fps.text = "FPS: %d" % Engine.get_frames_per_second()
	_prompt.text = State.prompt
	_vision_tag.visible = State.vision
	_viewfinder.visible = State.camera_mode
	_hint.visible = not State.camera_mode

	if State.toast_request != "":
		_toast.text = State.toast_request
		State.toast_request = ""
		_toast_timer = 2.6
		_toast.modulate.a = 1.0
	if _toast_timer > 0.0:
		_toast_timer -= delta
		if _toast_timer < 0.8:
			_toast.modulate.a = maxf(0.0, _toast_timer / 0.8)

# ================================================================ widgets

## Pulsing heart with the sanity percentage. Beats faster and turns darker
## red as sanity drops.
class HeartWidget extends Control:
	var _beat_t := 0.0
	var _pulse := 0.0

	func _process(delta: float) -> void:
		var s := State.sanity / 100.0
		var interval := lerpf(0.38, 1.05, s)
		_beat_t += delta
		if _beat_t >= interval:
			_beat_t = 0.0
			_pulse = 1.0
		_pulse = maxf(0.0, _pulse - delta * 4.0)
		queue_redraw()

	func _draw() -> void:
		var s := State.sanity / 100.0
		var c := size * 0.5 + Vector2(0, -8)
		var scale_f := (0.85 + 0.18 * _pulse) * (size.x / 130.0)
		var col := Color(1.0, 0.42, 0.72).lerp(Color(0.62, 0.05, 0.12), 1.0 - s)

		var pts := PackedVector2Array()
		for i in range(48):
			var t := TAU * float(i) / 48.0
			var hx := 16.0 * pow(sin(t), 3.0)
			var hy := 13.0 * cos(t) - 5.0 * cos(2.0 * t) - 2.0 * cos(3.0 * t) - cos(4.0 * t)
			pts.append(c + Vector2(hx, -hy) * 2.4 * scale_f)
		# soft outline
		draw_colored_polygon(pts, col)
		draw_polyline(pts, Color(0.08, 0.03, 0.07), 3.0)

		# % text
		var f := get_theme_default_font()
		var txt := "%d%%" % int(State.sanity)
		draw_string(f, c + Vector2(-30, 64), txt, HORIZONTAL_ALIGNMENT_CENTER, 60,
			20, Color(1, 0.9, 0.96))
		draw_string(f, c + Vector2(-45, 84), "SANITY", HORIZONTAL_ALIGNMENT_CENTER, 90,
			13, Color(0.9, 0.62, 0.78))

## Reputation slider: thorny dark red on the left, sparkly pink on the right.
class RepWidget extends Control:
	func _process(_delta: float) -> void:
		queue_redraw()

	func _draw() -> void:
		var w := size.x
		var bar := Rect2(0, 10, w, 14)
		# gradient in slices
		var steps := 48
		for i in range(steps):
			var t := float(i) / float(steps - 1)
			var col := Color(0.45, 0.04, 0.10).lerp(Color(1.0, 0.55, 0.80), t)
			draw_rect(Rect2(bar.position.x + t * (w - w / steps), bar.position.y,
				w / steps + 1.0, bar.size.y), col)
		# border + center notch
		draw_rect(bar, Color(0.08, 0.03, 0.07), false, 2.0)
		draw_rect(Rect2(w * 0.5 - 1, 6, 2, 22), Color(0.95, 0.95, 1.0))
		# marker
		var t_val := (State.reputation + 100.0) / 200.0
		var mx := t_val * w
		var tri := PackedVector2Array([
			Vector2(mx, 28), Vector2(mx - 7, 40), Vector2(mx + 7, 40)])
		draw_colored_polygon(tri, Color(1, 1, 1))
		draw_polyline(tri, Color(0.08, 0.03, 0.07), 2.0)
		# labels
		var f := get_theme_default_font()
		draw_string(f, Vector2(0, 54), "REPUTATION  %+d" % int(State.reputation),
			HORIZONTAL_ALIGNMENT_CENTER, int(w), 14, Color(0.95, 0.8, 0.9))

## Photo-mode frame: corner brackets + focus box + hint text.
class Viewfinder extends Control:
	func _draw() -> void:
		var w := size.x
		var h := size.y
		var c := Color(1, 1, 1, 0.9)
		var len := 46.0
		var th := 4.0
		# corner brackets
		for corner in [Vector2(30, 30), Vector2(w - 30, 30), Vector2(30, h - 30), Vector2(w - 30, h - 30)]:
			var dx := 1.0 if corner.x < w * 0.5 else -1.0
			var dy := 1.0 if corner.y < h * 0.5 else -1.0
			draw_rect(Rect2(corner.x, corner.y, dx * len, dy * th).abs(), c)
			draw_rect(Rect2(corner.x, corner.y, dx * th, dy * len).abs(), c)
		# center focus box
		var fb := Rect2(w * 0.5 - 60, h * 0.5 - 45, 120, 90)
		draw_rect(fb, Color(1, 0.42, 0.72, 0.9), false, 2.0)
		var f := get_theme_default_font()
		draw_string(f, Vector2(w * 0.5 - 200, h - 46), "PHOTO MODE — LMB snap · RMB exit",
			HORIZONTAL_ALIGNMENT_CENTER, 400, 18, c)
		draw_string(f, Vector2(30, 96), "● REC", HORIZONTAL_ALIGNMENT_LEFT, 200, 18,
			Color(1.0, 0.25, 0.3))
