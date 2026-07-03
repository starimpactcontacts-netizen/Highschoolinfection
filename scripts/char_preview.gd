extends Control
class_name CharPreview
## Draws a stylized anime schoolgirl entirely from code (no art assets), tinted
## and reshaped live from the current Cosmetics selection. This is the preview
## doll on the customization screen — call refresh() after any selection change.
## Proves we can drive a rich, data-bound character view purely from UI state.

func refresh() -> void:
	queue_redraw()

func _draw() -> void:
	var w := size.x
	var h := size.y
	var cx := w * 0.5

	var skin := Cosmetics.get_color("skin")
	var hair := Cosmetics.get_color("hair_color")
	var eyes := Cosmetics.get_color("eyes")
	var uniform := Cosmetics.get_color("uniform")
	var style := Cosmetics.get_style()
	var acc := Cosmetics.get_accessory()
	var collar := Color(0.95, 0.96, 0.99)

	# Vertical anchors scaled to the box.
	var head_cy := h * 0.30
	var head_r := h * 0.15
	var head_rx := head_r * 0.82

	# ---- Back hair (behind everything) ----
	_draw_back_hair(cx, head_cy, head_rx, head_r, h, style, hair)

	# ---- Skirt ----
	var skirt_top := h * 0.66
	var skirt_bot := h * 0.90
	var skirt := PackedVector2Array([
		Vector2(cx - w * 0.11, skirt_top),
		Vector2(cx + w * 0.11, skirt_top),
		Vector2(cx + w * 0.20, skirt_bot),
		Vector2(cx - w * 0.20, skirt_bot),
	])
	draw_colored_polygon(skirt, uniform.darkened(0.12))
	# Skirt pleats
	for i in range(-2, 3):
		var fx := cx + i * w * 0.055
		draw_line(Vector2(fx, skirt_top + 2), Vector2(fx + i * w * 0.02, skirt_bot),
			uniform.darkened(0.3), 1.5)

	# ---- Torso / sailor uniform ----
	var torso_top := h * 0.46
	var torso := PackedVector2Array([
		Vector2(cx - w * 0.12, torso_top),
		Vector2(cx + w * 0.12, torso_top),
		Vector2(cx + w * 0.135, skirt_top),
		Vector2(cx - w * 0.135, skirt_top),
	])
	draw_colored_polygon(torso, uniform)

	# ---- Neck ----
	draw_rect(Rect2(cx - w * 0.035, head_cy + head_r * 0.72, w * 0.07, h * 0.06), skin)

	# ---- Sailor collar ----
	var collar_pts := PackedVector2Array([
		Vector2(cx - w * 0.12, torso_top),
		Vector2(cx + w * 0.12, torso_top),
		Vector2(cx + w * 0.05, torso_top + h * 0.055),
		Vector2(cx, torso_top + h * 0.11),
		Vector2(cx - w * 0.05, torso_top + h * 0.055),
	])
	draw_colored_polygon(collar_pts, collar)
	# Neckerchief
	draw_colored_polygon(PackedVector2Array([
		Vector2(cx, torso_top + h * 0.05),
		Vector2(cx - w * 0.02, torso_top + h * 0.12),
		Vector2(cx + w * 0.02, torso_top + h * 0.12),
	]), Color(0.85, 0.20, 0.30))

	# ---- Head ----
	_draw_ellipse(Vector2(cx, head_cy), head_rx, head_r, skin)
	# Cheek blush
	draw_circle(Vector2(cx - head_rx * 0.55, head_cy + head_r * 0.25), head_r * 0.13,
		Color(1.0, 0.6, 0.65, 0.5))
	draw_circle(Vector2(cx + head_rx * 0.55, head_cy + head_r * 0.25), head_r * 0.13,
		Color(1.0, 0.6, 0.65, 0.5))

	# ---- Eyes ----
	var eye_dy := head_cy + head_r * 0.05
	var eye_dx := head_rx * 0.45
	for s in [-1.0, 1.0]:
		var ec := Vector2(cx + s * eye_dx, eye_dy)
		_draw_ellipse(ec, head_rx * 0.22, head_r * 0.28, Color(0.99, 0.99, 1.0))
		_draw_ellipse(ec, head_rx * 0.18, head_r * 0.24, eyes)          # iris
		draw_circle(ec, head_rx * 0.09, Color(0.08, 0.05, 0.08))        # pupil
		draw_circle(ec + Vector2(-head_rx * 0.05, -head_r * 0.08),
			head_rx * 0.05, Color(1, 1, 1, 0.9))                        # highlight
	# Mouth
	draw_line(Vector2(cx - head_rx * 0.12, head_cy + head_r * 0.55),
		Vector2(cx + head_rx * 0.12, head_cy + head_r * 0.55),
		Color(0.7, 0.35, 0.4), 2.0)

	# ---- Front bangs ----
	_draw_bangs(cx, head_cy, head_rx, head_r, hair)

	# ---- Accessory ----
	_draw_accessory(cx, head_cy, head_rx, head_r, eye_dx, eye_dy, acc)

func _draw_back_hair(cx: float, head_cy: float, head_rx: float, head_r: float,
		h: float, style: String, hair: Color) -> void:
	match style:
		"long":
			draw_colored_polygon(PackedVector2Array([
				Vector2(cx - head_rx * 1.15, head_cy - head_r * 0.3),
				Vector2(cx + head_rx * 1.15, head_cy - head_r * 0.3),
				Vector2(cx + head_rx * 1.05, head_cy + h * 0.30),
				Vector2(cx - head_rx * 1.05, head_cy + h * 0.30),
			]), hair)
		"short":
			_draw_ellipse(Vector2(cx, head_cy - head_r * 0.05),
				head_rx * 1.12, head_r * 1.1, hair)
		"twin":
			_draw_ellipse(Vector2(cx, head_cy), head_rx * 1.05, head_r * 1.05, hair)
			for s in [-1.0, 1.0]:
				draw_colored_polygon(PackedVector2Array([
					Vector2(cx + s * head_rx * 0.9, head_cy - head_r * 0.2),
					Vector2(cx + s * head_rx * 1.6, head_cy + head_r * 0.1),
					Vector2(cx + s * head_rx * 1.4, head_cy + h * 0.26),
					Vector2(cx + s * head_rx * 0.85, head_cy + h * 0.24),
				]), hair)
		"pony":
			_draw_ellipse(Vector2(cx, head_cy), head_rx * 1.05, head_r * 1.05, hair)
			draw_colored_polygon(PackedVector2Array([
				Vector2(cx + head_rx * 0.8, head_cy - head_r * 0.4),
				Vector2(cx + head_rx * 1.7, head_cy + head_r * 0.4),
				Vector2(cx + head_rx * 1.4, head_cy + h * 0.24),
				Vector2(cx + head_rx * 0.7, head_cy + head_r * 0.2),
			]), hair)

func _draw_bangs(cx: float, head_cy: float, head_rx: float, head_r: float,
		hair: Color) -> void:
	# Crown cap
	_draw_ellipse(Vector2(cx, head_cy - head_r * 0.32), head_rx * 1.02,
		head_r * 0.78, hair)
	# Three spiky bang tufts framing the forehead
	draw_colored_polygon(PackedVector2Array([
		Vector2(cx - head_rx * 0.95, head_cy - head_r * 0.5),
		Vector2(cx - head_rx * 0.2, head_cy - head_r * 0.55),
		Vector2(cx - head_rx * 0.45, head_cy + head_r * 0.15),
	]), hair)
	draw_colored_polygon(PackedVector2Array([
		Vector2(cx - head_rx * 0.4, head_cy - head_r * 0.55),
		Vector2(cx + head_rx * 0.4, head_cy - head_r * 0.55),
		Vector2(cx, head_cy + head_r * 0.1),
	]), hair.lightened(0.05))
	draw_colored_polygon(PackedVector2Array([
		Vector2(cx + head_rx * 0.95, head_cy - head_r * 0.5),
		Vector2(cx + head_rx * 0.2, head_cy - head_r * 0.55),
		Vector2(cx + head_rx * 0.45, head_cy + head_r * 0.15),
	]), hair)

func _draw_accessory(cx: float, head_cy: float, head_rx: float, head_r: float,
		eye_dx: float, eye_dy: float, acc: String) -> void:
	match acc:
		"bow":
			var by := head_cy - head_r * 0.78
			var bx := cx + head_rx * 0.55
			var bow := Color(0.90, 0.25, 0.45)
			draw_colored_polygon(PackedVector2Array([
				Vector2(bx, by), Vector2(bx - head_rx * 0.4, by - head_r * 0.2),
				Vector2(bx - head_rx * 0.4, by + head_r * 0.2)]), bow)
			draw_colored_polygon(PackedVector2Array([
				Vector2(bx, by), Vector2(bx + head_rx * 0.4, by - head_r * 0.2),
				Vector2(bx + head_rx * 0.4, by + head_r * 0.2)]), bow)
			draw_circle(Vector2(bx, by), head_rx * 0.1, bow.darkened(0.2))
		"glasses":
			for s in [-1.0, 1.0]:
				var ec := Vector2(cx + s * eye_dx, eye_dy)
				draw_arc(ec, head_rx * 0.3, 0, TAU, 20, Color(0.15, 0.15, 0.18), 2.5)
			draw_line(Vector2(cx - eye_dx + head_rx * 0.28, eye_dy),
				Vector2(cx + eye_dx - head_rx * 0.28, eye_dy),
				Color(0.15, 0.15, 0.18), 2.5)
		"headphones":
			draw_arc(Vector2(cx, head_cy - head_r * 0.15), head_rx * 1.15,
				PI * 0.85, PI * 0.15, 24, Color(0.12, 0.12, 0.15), 6.0)
			for s in [-1.0, 1.0]:
				draw_circle(Vector2(cx + s * head_rx * 1.1, head_cy + head_r * 0.1),
					head_rx * 0.22, Color(0.85, 0.2, 0.4))
		"mask":
			draw_rect(Rect2(cx - head_rx * 0.6, head_cy + head_r * 0.35,
				head_rx * 1.2, head_r * 0.55), Color(0.90, 0.92, 0.96))

func _draw_ellipse(center: Vector2, rx: float, ry: float, color: Color) -> void:
	var pts := PackedVector2Array()
	for i in range(24):
		var a := TAU * float(i) / 24.0
		pts.append(center + Vector2(cos(a) * rx, sin(a) * ry))
	draw_colored_polygon(pts, color)
