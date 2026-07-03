extends Control
## Falling sakura petals — the signature Yandere Simulator title ambiance.
## Fully procedural (no textures). Drop this Control over any screen; it ignores
## mouse input so buttons underneath still work.

const COUNT := 46

class Petal:
	var pos: Vector2
	var vel: Vector2
	var size: float
	var spin: float
	var angle: float
	var tint: Color

var _petals: Array[Petal] = []
var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_rng.randomize()
	set_anchors_preset(Control.PRESET_FULL_RECT)
	for i in range(COUNT):
		_petals.append(_new_petal(true))

func _new_petal(anywhere: bool) -> Petal:
	var vp := get_viewport_rect().size
	var p := Petal.new()
	p.pos = Vector2(_rng.randf() * vp.x, (_rng.randf() * vp.y) if anywhere else -20.0)
	p.vel = Vector2(_rng.randf_range(-22, -6), _rng.randf_range(28, 70))
	p.size = _rng.randf_range(5.0, 12.0)
	p.spin = _rng.randf_range(-2.2, 2.2)
	p.angle = _rng.randf() * TAU
	var shade := _rng.randf()
	p.tint = Color(1.0, 0.55 + shade * 0.25, 0.72 + shade * 0.12, _rng.randf_range(0.5, 0.9))
	return p

func _process(delta: float) -> void:
	var vp := get_viewport_rect().size
	for p in _petals:
		p.pos += p.vel * delta
		p.angle += p.spin * delta
		p.pos.x += sin(p.angle) * 14.0 * delta
		if p.pos.y > vp.y + 20.0 or p.pos.x < -20.0:
			var np := _new_petal(false)
			p.pos = np.pos
			p.vel = np.vel
			p.size = np.size
			p.spin = np.spin
			p.tint = np.tint
	queue_redraw()

func _draw() -> void:
	for p in _petals:
		var s := p.size
		# A soft 4-point petal (diamond squashed on one axis), rotated.
		var pts := PackedVector2Array([
			Vector2(0, -s), Vector2(s * 0.6, 0),
			Vector2(0, s * 0.85), Vector2(-s * 0.6, 0),
		])
		var rot := Transform2D(p.angle, p.pos)
		var world := PackedVector2Array()
		for v in pts:
			world.append(rot * v)
		draw_colored_polygon(world, p.tint)
