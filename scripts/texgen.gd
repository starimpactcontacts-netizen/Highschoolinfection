class_name TexGen
## Procedural texture factory. Every texture in the demo is generated here at
## startup — no image assets on disk. Textures are drawn into an Image with
## simple fills/lines, mipmapped, and returned as an ImageTexture. Materials
## tile them with triplanar mapping so boxes of any size look right.

static var _cache := {}

static func _tex(key: String, w: int, h: int, painter: Callable) -> ImageTexture:
	if _cache.has(key):
		return _cache[key]
	var img := Image.create(w, h, false, Image.FORMAT_RGBA8)
	painter.call(img)
	img.generate_mipmaps()
	var tex := ImageTexture.create_from_image(img)
	_cache[key] = tex
	return tex

## Polished linoleum: big pale tiles with thin grout lines + faint speckle.
static func linoleum() -> ImageTexture:
	return _tex("lino", 128, 128, func(img: Image) -> void:
		var base := Color(0.87, 0.86, 0.82)
		img.fill(base)
		var rng := RandomNumberGenerator.new()
		rng.seed = 7
		for i in range(500):
			var x := rng.randi_range(0, 127)
			var y := rng.randi_range(0, 127)
			var d := rng.randf_range(-0.05, 0.05)
			img.set_pixel(x, y, Color(base.r + d, base.g + d, base.b + d))
		var grout := Color(0.68, 0.67, 0.64)
		for i in range(128):
			img.set_pixel(i, 0, grout)
			img.set_pixel(i, 64, grout)
			img.set_pixel(0, i, grout)
			img.set_pixel(64, i, grout)
	)

## Courtyard / plaza stone paving.
static func stone() -> ImageTexture:
	return _tex("stone", 128, 128, func(img: Image) -> void:
		var base := Color(0.72, 0.71, 0.69)
		img.fill(base)
		var rng := RandomNumberGenerator.new()
		rng.seed = 21
		for i in range(900):
			var x := rng.randi_range(0, 127)
			var y := rng.randi_range(0, 127)
			var d := rng.randf_range(-0.07, 0.07)
			img.set_pixel(x, y, Color(base.r + d, base.g + d, base.b + d))
		var line := Color(0.55, 0.54, 0.52)
		for i in range(128):
			img.set_pixel(i, 0, line)
			img.set_pixel(0, i, line)
	)

## Grass with mottled blades.
static func grass() -> ImageTexture:
	return _tex("grass", 128, 128, func(img: Image) -> void:
		img.fill(Color(0.32, 0.52, 0.26))
		var rng := RandomNumberGenerator.new()
		rng.seed = 33
		for i in range(2600):
			var x := rng.randi_range(0, 127)
			var y := rng.randi_range(0, 127)
			var g := rng.randf_range(0.40, 0.62)
			img.set_pixel(x, y, Color(0.22 + g * 0.2, g, 0.20))
	)

## Wood planks (classroom floors).
static func planks() -> ImageTexture:
	return _tex("planks", 128, 128, func(img: Image) -> void:
		var rng := RandomNumberGenerator.new()
		rng.seed = 5
		for row in range(4):
			var shade := rng.randf_range(-0.05, 0.05)
			var c := Color(0.62 + shade, 0.45 + shade, 0.28 + shade)
			img.fill_rect(Rect2i(0, row * 32, 128, 32), c)
			for i in range(240):
				var x := rng.randi_range(0, 127)
				var y := rng.randi_range(row * 32, row * 32 + 31)
				img.set_pixel(x, y, c.darkened(rng.randf_range(0.0, 0.12)))
		var seam := Color(0.38, 0.27, 0.16)
		for row in range(4):
			for i in range(128):
				img.set_pixel(i, row * 32, seam)
	)

## A bank of wooden lockers: door grid with dark seams and small vents.
static func lockers() -> ImageTexture:
	return _tex("lockers", 256, 128, func(img: Image) -> void:
		var wood := Color(0.55, 0.38, 0.22)
		img.fill(wood)
		var seam := Color(0.30, 0.20, 0.11)
		var vent := Color(0.40, 0.28, 0.15)
		# 8 doors wide, 2 tall
		for cx in range(8):
			for cy in range(2):
				var x0 := cx * 32
				var y0 := cy * 64
				for i in range(32):
					img.set_pixel(x0, y0 + mini(i * 2, 63), seam)
					img.set_pixel(x0 + i, y0, seam)
				# vents
				for v in range(3):
					img.fill_rect(Rect2i(x0 + 8, y0 + 10 + v * 6, 16, 2), vent)
				# handle
				img.fill_rect(Rect2i(x0 + 24, y0 + 34, 4, 8), Color(0.75, 0.72, 0.68))
	)

## Upper-floor facade band: one storey of windows on white wall.
static func facade() -> ImageTexture:
	return _tex("facade", 256, 128, func(img: Image) -> void:
		img.fill(Color(0.92, 0.91, 0.89))
		var glass := Color(0.45, 0.58, 0.72)
		var frame := Color(0.35, 0.36, 0.40)
		for w in range(4):
			var x0 := 16 + w * 64
			img.fill_rect(Rect2i(x0 - 2, 30, 36, 62), frame)
			img.fill_rect(Rect2i(x0, 32, 32, 58), glass)
			img.fill_rect(Rect2i(x0 + 14, 32, 3, 58), frame)
			img.fill_rect(Rect2i(x0, 58, 32, 3), frame)
		# floor divider line at bottom
		img.fill_rect(Rect2i(0, 122, 256, 6), Color(0.80, 0.79, 0.76))
	)

## Vending machine front: dark panel with rows of colorful drinks.
static func vending(main: Color) -> ImageTexture:
	var key := "vend_" + main.to_html()
	return _tex(key, 64, 128, func(img: Image) -> void:
		img.fill(main)
		img.fill_rect(Rect2i(6, 8, 52, 70), Color(0.08, 0.09, 0.12))
		var rng := RandomNumberGenerator.new()
		rng.seed = int(main.r * 255.0)
		for row in range(4):
			for col in range(6):
				var c := Color.from_hsv(rng.randf(), 0.7, 0.95)
				img.fill_rect(Rect2i(10 + col * 8, 12 + row * 16, 6, 12), c)
		# coin slot + tray
		img.fill_rect(Rect2i(44, 84, 10, 3), Color(0.85, 0.85, 0.88))
		img.fill_rect(Rect2i(10, 100, 44, 16), Color(0.05, 0.05, 0.07))
	)

## Bulletin board: cork with pinned colored papers.
static func bulletin() -> ImageTexture:
	return _tex("bulletin", 128, 64, func(img: Image) -> void:
		img.fill(Color(0.72, 0.55, 0.35))
		var rng := RandomNumberGenerator.new()
		rng.seed = 11
		var papers := [Color(0.95, 0.95, 0.92), Color(0.95, 0.85, 0.55),
			Color(0.75, 0.88, 0.95), Color(0.95, 0.75, 0.82)]
		for i in range(9):
			var w := rng.randi_range(14, 22)
			var h := rng.randi_range(16, 26)
			var x := rng.randi_range(4, 124 - w - 4)
			var y := rng.randi_range(4, 60 - h - 4)
			var col: Color = papers[rng.randi_range(0, papers.size() - 1)]
			img.fill_rect(Rect2i(x, y, w, h), col)
			for l in range(3):
				img.fill_rect(Rect2i(x + 3, y + 4 + l * 5, w - 6, 1), Color(0.5, 0.5, 0.55))
	)

## First-aid wall box: white with red cross.
static func firstaid() -> ImageTexture:
	return _tex("firstaid", 64, 64, func(img: Image) -> void:
		img.fill(Color(0.95, 0.96, 0.97))
		img.fill_rect(Rect2i(26, 12, 12, 40), Color(0.85, 0.12, 0.15))
		img.fill_rect(Rect2i(12, 26, 40, 12), Color(0.85, 0.12, 0.15))
	)

## Occult club floor: dark carpet with a chalk ritual circle.
static func ritual_circle() -> ImageTexture:
	return _tex("ritual", 256, 256, func(img: Image) -> void:
		img.fill(Color(0.13, 0.09, 0.17))
		var chalk := Color(0.85, 0.80, 0.95)
		var c := Vector2(128, 128)
		# two concentric circles
		for ring in [100.0, 88.0]:
			for i in range(720):
				var a := TAU * float(i) / 720.0
				var p := c + Vector2(cos(a), sin(a)) * ring
				img.set_pixel(int(p.x), int(p.y), chalk)
		# five-pointed star connecting every second vertex of a pentagon
		var pts: Array = []
		for i in range(5):
			var a := -PI / 2.0 + TAU * float(i) / 5.0
			pts.append(c + Vector2(cos(a), sin(a)) * 88.0)
		for i in range(5):
			var p0: Vector2 = pts[i]
			var p1: Vector2 = pts[(i + 2) % 5]
			for s in range(200):
				var p := p0.lerp(p1, float(s) / 200.0)
				img.set_pixel(int(p.x), int(p.y), chalk)
	)

## Chalkboard: deep green with faint chalk smudges.
static func chalkboard() -> ImageTexture:
	return _tex("chalk", 128, 64, func(img: Image) -> void:
		img.fill(Color(0.14, 0.28, 0.20))
		var rng := RandomNumberGenerator.new()
		rng.seed = 3
		for i in range(160):
			var x := rng.randi_range(2, 125)
			var y := rng.randi_range(2, 61)
			img.set_pixel(x, y, Color(0.9, 0.9, 0.88, rng.randf_range(0.03, 0.10)))
	)
