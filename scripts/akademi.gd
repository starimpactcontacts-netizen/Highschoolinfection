class_name Akademi
## Builds the entire school campus procedurally — geometry, materials, lights,
## sky, and props are all created in code from the layout constants below.
## Call Akademi.build(root) once; it returns refs the game scene needs
## (environment for sanity grading, the entrance clock label, glow shells).
##
## Layout (top-down, +Z is south toward the main gate):
##   - Perimeter wall + iron main gate at z=+46
##   - Sakura-lined stone path from the gate to a fountain plaza
##   - Hollow-square school building: outer 56x40, open-air inner courtyard
##   - Ground floor is walkable: entrance foyer, ring hallway, classrooms,
##     occult club, labeled club/office doors; three facade storeys above
##   - Incinerator area tucked behind the school

const FLOOR_H := 3.5          # ground floor ceiling height
const TOP_H := 14.0           # total building height (4 storeys)

static var _mats := {}

# ---------------------------------------------------------------- materials

static func _mat(key: String, color: Color, rough := 0.85, metal := 0.0,
		tex: Texture2D = null, tex_scale := Vector3.ONE, emission := Color.BLACK) -> StandardMaterial3D:
	if _mats.has(key):
		return _mats[key]
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metal
	if tex:
		m.albedo_texture = tex
		m.uv1_triplanar = true
		m.uv1_scale = tex_scale
		m.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	if emission != Color.BLACK:
		m.emission_enabled = true
		m.emission = emission
	_mats[key] = m
	return m

static func _glass() -> StandardMaterial3D:
	if _mats.has("glass"):
		return _mats["glass"]
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(0.70, 0.82, 0.92, 0.22)
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.roughness = 0.05
	m.metallic = 0.4
	m.cull_mode = BaseMaterial3D.CULL_DISABLED
	_mats["glass"] = m
	return m

static func _glow(color: Color) -> StandardMaterial3D:
	var key := "glow_" + color.to_html()
	if _mats.has(key):
		return _mats[key]
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(color.r, color.g, color.b, 0.45)
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.emission_enabled = true
	m.emission = color
	m.emission_energy_multiplier = 2.0
	m.no_depth_test = true
	_mats[key] = m
	return m

# ---------------------------------------------------------------- primitives

## Box mesh; if collide, wrapped in a StaticBody3D (raycast-interactable via
## the "iname" meta). Returns the root node placed at `pos`.
static func box(parent: Node, pos: Vector3, size: Vector3, mat: Material,
		collide := true, iname := "") -> Node3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	if mat:
		mi.material_override = mat
	if collide:
		var body := StaticBody3D.new()
		body.position = pos
		var cs := CollisionShape3D.new()
		var sh := BoxShape3D.new()
		sh.size = size
		cs.shape = sh
		body.add_child(cs)
		body.add_child(mi)
		if iname != "":
			body.set_meta("iname", iname)
		parent.add_child(body)
		return body
	mi.position = pos
	parent.add_child(mi)
	return mi

static func cylinder(parent: Node, pos: Vector3, radius: float, height: float,
		mat: Material, collide := true, iname := "") -> Node3D:
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	if mat:
		mi.material_override = mat
	if collide:
		var body := StaticBody3D.new()
		body.position = pos
		var cs := CollisionShape3D.new()
		var sh := CylinderShape3D.new()
		sh.radius = radius
		sh.height = height
		cs.shape = sh
		body.add_child(cs)
		body.add_child(mi)
		if iname != "":
			body.set_meta("iname", iname)
		parent.add_child(body)
		return body
	mi.position = pos
	parent.add_child(mi)
	return mi

static func sphere(parent: Node, pos: Vector3, radius: float, mat: Material) -> MeshInstance3D:
	var mesh := SphereMesh.new()
	mesh.radius = radius
	mesh.height = radius * 2.0
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	parent.add_child(mi)
	return mi

static func sign3d(parent: Node, pos: Vector3, text: String, yaw_deg: float,
		size := 40, color := Color(0.15, 0.17, 0.30)) -> Label3D:
	var l := Label3D.new()
	l.text = text
	l.font_size = size
	l.modulate = color
	l.outline_size = 0
	l.position = pos
	l.rotation_degrees = Vector3(0, yaw_deg, 0)
	parent.add_child(l)
	return l

## Wall with door/window openings. Runs along X when `horizontal`, else along Z.
## openings: [{a, b, h, sill (default 0), glass (default false)}]
static func wall(parent: Node, horizontal: bool, c0: float, c1: float, fixed: float,
		mat: Material, openings: Array = [], y0 := 0.0, h := FLOOR_H, th := 0.3) -> void:
	var marks := openings.duplicate()
	marks.sort_custom(func(p, q): return p["a"] < q["a"])
	var cur := c0
	for o in marks:
		if o["a"] > cur:
			_wall_seg(parent, horizontal, cur, o["a"], fixed, y0, h, th, mat)
		cur = maxf(cur, o["b"])
	if cur < c1:
		_wall_seg(parent, horizontal, cur, c1, fixed, y0, h, th, mat)
	for o in marks:
		var sill: float = o.get("sill", 0.0)
		var oh: float = o["h"]
		if sill > 0.0:
			_wall_seg(parent, horizontal, o["a"], o["b"], fixed, y0, sill, th, mat)
		if sill + oh < h:
			_wall_seg(parent, horizontal, o["a"], o["b"], fixed, y0 + sill + oh, h - sill - oh, th, mat)
		if o.get("glass", false):
			_wall_seg(parent, horizontal, o["a"], o["b"], fixed, y0 + sill, oh, 0.06, _glass(), false)

static func _wall_seg(parent: Node, horizontal: bool, a: float, b: float, fixed: float,
		y0: float, h: float, th: float, mat: Material, collide := true) -> void:
	if b - a < 0.01 or h < 0.01:
		return
	var pos: Vector3
	var size: Vector3
	if horizontal:
		pos = Vector3((a + b) * 0.5, y0 + h * 0.5, fixed)
		size = Vector3(b - a, h, th)
	else:
		pos = Vector3(fixed, y0 + h * 0.5, (a + b) * 0.5)
		size = Vector3(th, h, b - a)
	box(parent, pos, size, mat, collide)

# ================================================================== BUILD

static func build(root: Node3D) -> Dictionary:
	_mats.clear()
	var refs := {"glow": [], "clock": null, "env": null}

	var env := _build_sky_and_light(root)
	refs["env"] = env

	_build_grounds(root)
	_build_gate_and_path(root)
	_build_fountain(root, Vector3(0, 0, 30))
	_build_building_shell(root)
	_build_foyer(root, refs)
	_build_hallways(root, refs)
	_build_classroom(root, "1 - A", -26.0, -14.0)
	_build_classroom(root, "1 - B", -12.0, 0.0)
	_build_occult_club(root)
	_build_inner_courtyard(root)
	_build_incinerator(root, refs)
	_scatter_sakura(root)

	refs["clock"] = _build_entrance_clock(root)
	return refs

# ------------------------------------------------------------ sky & light

static func _build_sky_and_light(root: Node3D) -> Environment:
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color(0.35, 0.58, 0.92)
	sky_mat.sky_horizon_color = Color(0.82, 0.88, 0.96)
	sky_mat.ground_horizon_color = Color(0.82, 0.88, 0.96)
	sky_mat.ground_bottom_color = Color(0.45, 0.48, 0.52)
	var sky := Sky.new()
	sky.sky_material = sky_mat

	var env := Environment.new()
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	env.ambient_light_energy = 1.0
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.glow_enabled = true
	env.glow_intensity = 0.4
	env.ssao_enabled = true
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.1

	var we := WorldEnvironment.new()
	we.environment = env
	root.add_child(we)

	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-52, -32, 0)
	sun.light_color = Color(1.0, 0.96, 0.88)
	sun.light_energy = 1.25
	sun.shadow_enabled = true
	root.add_child(sun)
	return env

# ------------------------------------------------------------ grounds

static func _build_grounds(root: Node3D) -> void:
	var grass := _mat("grass", Color.WHITE, 0.95, 0.0, TexGen.grass(), Vector3(0.5, 0.5, 0.5))
	box(root, Vector3(0, -0.1, 10), Vector3(160, 0.2, 130), grass)

	# Perimeter wall (three sides; the gate side is built separately)
	var wallmat := _mat("perim", Color(0.80, 0.79, 0.75), 0.9)
	box(root, Vector3(-70, 1.1, 10), Vector3(0.4, 2.2, 130), wallmat)
	box(root, Vector3(70, 1.1, 10), Vector3(0.4, 2.2, 130), wallmat)
	box(root, Vector3(0, 1.1, -55), Vector3(140, 2.2, 0.4), wallmat)

static func _build_gate_and_path(root: Node3D) -> void:
	var stone := _mat("stone", Color.WHITE, 0.55, 0.0, TexGen.stone(), Vector3(0.5, 0.5, 0.5))
	var dark := _mat("iron", Color(0.16, 0.17, 0.20), 0.4, 0.8)
	var wallmat := _mat("perim", Color(0.80, 0.79, 0.75), 0.9)

	# Stone path: gate → plaza → building entrance
	box(root, Vector3(0, 0.02, 33), Vector3(8, 0.08, 27), stone)
	box(root, Vector3(0, 0.02, 24), Vector3(20, 0.08, 14), stone)   # fountain plaza
	box(root, Vector3(0, 0.02, 18.5), Vector3(6, 0.08, 5), stone)   # plaza → doors

	# Perimeter along the gate side, with the gate itself open
	box(root, Vector3(-37.75, 1.1, 46.5), Vector3(64.5, 2.2, 0.4), wallmat)
	box(root, Vector3(37.75, 1.1, 46.5), Vector3(64.5, 2.2, 0.4), wallmat)

	# Gate pillars + iron bars (swung-open panels)
	for sx in [-1.0, 1.0]:
		box(root, Vector3(sx * 5.5, 1.6, 46.5), Vector3(0.9, 3.2, 0.9), wallmat)
		sphere(root, Vector3(sx * 5.5, 3.4, 46.5), 0.35, dark)
		var panel := Node3D.new()
		panel.position = Vector3(sx * 5.0, 0, 46.5)
		panel.rotation_degrees = Vector3(0, sx * -55.0, 0)
		root.add_child(panel)
		for i in range(9):
			box(panel, Vector3(sx * -0.25 * float(i + 1) * 2.0, 1.3, 0), Vector3(0.08, 2.6, 0.08), dark, false)
		box(panel, Vector3(sx * -2.3, 2.5, 0), Vector3(4.6, 0.10, 0.10), dark, false)
		box(panel, Vector3(sx * -2.3, 0.4, 0), Vector3(4.6, 0.10, 0.10), dark, false)

	sign3d(root, Vector3(0, 3.9, 46.3), "AKADEMI HIGH SCHOOL", 180, 52, Color(0.92, 0.30, 0.55))

static func _build_fountain(root: Node3D, pos: Vector3) -> void:
	var stone := _mat("fstone", Color(0.78, 0.77, 0.75), 0.5)
	var water := _mat("water", Color(0.35, 0.62, 0.85, 0.7), 0.05, 0.3)
	water.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	water.emission_enabled = true
	water.emission = Color(0.10, 0.22, 0.35)

	cylinder(root, pos + Vector3(0, 0.3, 0), 3.2, 0.6, stone, true, "Make a wish")
	cylinder(root, pos + Vector3(0, 0.55, 0), 2.9, 0.15, water, false)
	cylinder(root, pos + Vector3(0, 0.9, 0), 0.4, 1.8, stone, false)
	cylinder(root, pos + Vector3(0, 1.85, 0), 1.1, 0.25, stone, false)
	cylinder(root, pos + Vector3(0, 1.98, 0), 0.95, 0.10, water, false)

	# Spray particles
	var p := GPUParticles3D.new()
	p.position = pos + Vector3(0, 2.1, 0)
	p.amount = 60
	p.lifetime = 0.9
	var pm := ParticleProcessMaterial.new()
	pm.direction = Vector3(0, 1, 0)
	pm.spread = 14.0
	pm.initial_velocity_min = 2.4
	pm.initial_velocity_max = 3.2
	pm.gravity = Vector3(0, -9.8, 0)
	pm.scale_min = 0.5
	pm.scale_max = 1.0
	p.process_material = pm
	var quad := QuadMesh.new()
	quad.size = Vector2(0.07, 0.07)
	var wm := StandardMaterial3D.new()
	wm.albedo_color = Color(0.85, 0.93, 1.0, 0.7)
	wm.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	wm.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	wm.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES
	quad.material = wm
	p.draw_pass_1 = quad
	root.add_child(p)

	# Benches around the plaza
	var wood := _mat("bench", Color(0.55, 0.40, 0.26), 0.7)
	for bx in [-7.0, 7.0]:
		box(root, Vector3(bx, 0.25, 24), Vector3(0.5, 0.5, 2.4), stone)
		box(root, Vector3(bx, 0.55, 24), Vector3(0.7, 0.10, 2.6), wood, true, "A campus bench")

# ------------------------------------------------------------ building shell

static func _build_building_shell(root: Node3D) -> void:
	var wallmat := _mat("wall", Color(0.93, 0.92, 0.90), 0.85)
	var facade := _mat("facade", Color.WHITE, 0.8, 0.0, TexGen.facade(), Vector3(0.125, 1.0 / FLOOR_H, 0.125))
	var rooftrim := _mat("trim", Color(0.75, 0.74, 0.71), 0.8)
	var lino := _mat("lino", Color.WHITE, 0.12, 0.0, TexGen.linoleum(), Vector3(0.5, 0.5, 0.5))
	var ceil_mat := _mat("ceil", Color(0.96, 0.96, 0.97), 0.9)

	# --- Ground-floor outer walls (x -28..28, z -20..20) with windows/doors ---
	# South face: entrance opening + windows
	wall(root, true, -28, 28, 20, wallmat, [
		{"a": -2.2, "b": 2.2, "h": 2.6},                                  # entrance
		{"a": -24, "b": -18, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": -14, "b": -8, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": 8, "b": 14, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": 18, "b": 24, "h": 1.6, "sill": 0.9, "glass": true},
	])
	# North face: classroom windows
	wall(root, true, -28, 28, -20, wallmat, [
		{"a": -25, "b": -16, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": -11, "b": -2, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": 2, "b": 11, "h": 1.6, "sill": 0.9, "glass": true},
		{"a": 16, "b": 25, "h": 1.6, "sill": 0.9, "glass": true},
	])
	# East / west faces
	for sx in [-28.0, 28.0]:
		wall(root, false, -20, 20, sx, wallmat, [
			{"a": -14, "b": -6, "h": 1.6, "sill": 0.9, "glass": true},
			{"a": 6, "b": 14, "h": 1.6, "sill": 0.9, "glass": true},
		])

	# --- Upper storeys: facade ring from FLOOR_H to TOP_H ---
	# Bands cover everything except the open inner courtyard (x -16..16, z -8..8)
	var up_h := TOP_H - FLOOR_H
	var up_y := FLOOR_H + up_h * 0.5
	box(root, Vector3(0, up_y, -14), Vector3(56, up_h, 12), facade, false)   # north band
	box(root, Vector3(0, up_y, 14), Vector3(56, up_h, 12), facade, false)    # south band
	box(root, Vector3(-22, up_y, 0), Vector3(12, up_h, 16), facade, false)   # west band
	box(root, Vector3(22, up_y, 0), Vector3(12, up_h, 16), facade, false)    # east band
	# Roof trim ring
	box(root, Vector3(0, TOP_H + 0.25, -14), Vector3(57, 0.5, 13), rooftrim, false)
	box(root, Vector3(0, TOP_H + 0.25, 14), Vector3(57, 0.5, 13), rooftrim, false)
	box(root, Vector3(-22, TOP_H + 0.25, 0), Vector3(13, 0.5, 17), rooftrim, false)
	box(root, Vector3(22, TOP_H + 0.25, 0), Vector3(13, 0.5, 17), rooftrim, false)

	# --- Ground-floor interior floor + ceilings over the ring (courtyard open) ---
	box(root, Vector3(0, 0.01, 0), Vector3(56, 0.12, 40), lino)
	box(root, Vector3(0, FLOOR_H + 0.1, -14), Vector3(56, 0.2, 12), ceil_mat, false)
	box(root, Vector3(0, FLOOR_H + 0.1, 14), Vector3(56, 0.2, 12), ceil_mat, false)
	box(root, Vector3(-22, FLOOR_H + 0.1, 0), Vector3(12, 0.2, 16), ceil_mat, false)
	box(root, Vector3(22, FLOOR_H + 0.1, 0), Vector3(12, 0.2, 16), ceil_mat, false)

	# Entrance canopy + school name over the doors
	box(root, Vector3(0, 2.9, 21.4), Vector3(7, 0.25, 3), rooftrim, false)
	for sx in [-3.0, 3.0]:
		box(root, Vector3(sx, 1.45, 22.6), Vector3(0.2, 2.9, 0.2), rooftrim)
	sign3d(root, Vector3(0, 3.7, 20.4), "AKADEMI  HIGH  SCHOOL", 0, 38, Color(0.95, 0.90, 0.95))

# ------------------------------------------------------------ foyer

static func _build_foyer(root: Node3D, refs: Dictionary) -> void:
	var wallmat := _mat("wall", Color(0.93, 0.92, 0.90), 0.85)
	var lockers := _mat("lockers", Color.WHITE, 0.6, 0.0, TexGen.lockers(), Vector3(0.25, 0.5, 0.25))

	# Foyer side walls (x = -8 and 8, from z=12..20) with door openings into the wings
	wall(root, false, 12, 20, -8, wallmat, [{"a": 14.5, "b": 16.0, "h": 2.2}])
	wall(root, false, 12, 20, 8, wallmat, [{"a": 14.5, "b": 16.0, "h": 2.2}])
	# Wall between foyer and south corridor, wide opening
	wall(root, true, -8, 8, 12, wallmat, [{"a": -3.0, "b": 3.0, "h": 2.6}])

	# Shoe locker banks
	for lx in [-5.5, -2.0, 2.0, 5.5]:
		box(root, Vector3(lx, 0.9, 18.6), Vector3(3.0, 1.8, 0.55),
			lockers, true, "Your shoe locker")

	# Welcome mat + sign
	var mat_dark := _mat("mat", Color(0.35, 0.20, 0.24), 0.95)
	box(root, Vector3(0, 0.08, 19.2), Vector3(4.4, 0.04, 1.4), mat_dark, false)
	sign3d(root, Vector3(0, 2.85, 12.3), "ENTRANCE  HALL", 0, 34, Color(0.35, 0.37, 0.50))

	# Doors out of the foyer: nurse (west) faculty (east)
	_club_door(root, Vector3(-7.85, 0, 15.25), 90, "NURSE'S OFFICE")
	var fac := _club_door(root, Vector3(7.85, 0, 15.25), -90, "FACULTY ROOM")
	# Faculty door glows yellow in Yandere Vision (high-alert)
	refs["glow"].append(_glow_shell(fac, Vector3(0.3, 2.4, 1.6), Color(1.0, 0.85, 0.2)))

# ------------------------------------------------------------ hallway ring

static func _build_hallways(root: Node3D, refs: Dictionary) -> void:
	var wallmat := _mat("wall", Color(0.93, 0.92, 0.90), 0.85)

	# --- Inner courtyard walls (hallway side), lined with big windows ---
	# South inner wall (z=8) with a door into the courtyard
	wall(root, true, -16, 16, 8, wallmat, [
		{"a": -1.0, "b": 1.0, "h": 2.4},
		{"a": -13, "b": -3, "h": 1.8, "sill": 0.9, "glass": true},
		{"a": 3, "b": 13, "h": 1.8, "sill": 0.9, "glass": true},
	])
	wall(root, true, -16, 16, -8, wallmat, [
		{"a": -13, "b": -3, "h": 1.8, "sill": 0.9, "glass": true},
		{"a": 3, "b": 13, "h": 1.8, "sill": 0.9, "glass": true},
	])
	wall(root, false, -8, 8, -16, wallmat, [
		{"a": -5, "b": 5, "h": 1.8, "sill": 0.9, "glass": true},
	])
	wall(root, false, -8, 8, 16, wallmat, [
		{"a": -5, "b": 5, "h": 1.8, "sill": 0.9, "glass": true},
	])

	# --- North wing: classroom wall (z=-12) with doors for 1-A .. 1-D ---
	wall(root, true, -28, 28, -12, wallmat, [
		{"a": -21.0, "b": -19.6, "h": 2.2},   # 1-A
		{"a": -7.0, "b": -5.6, "h": 2.2},     # 1-B
		{"a": 5.6, "b": 7.0, "h": 2.2},       # 1-C (decorative)
		{"a": 19.6, "b": 21.0, "h": 2.2},     # 1-D (decorative)
	])
	_room_sign(root, Vector3(-20.3, 2.5, -11.8), 0, "1 - A")
	_room_sign(root, Vector3(-6.3, 2.5, -11.8), 0, "1 - B")
	_room_sign(root, Vector3(6.3, 2.5, -11.8), 0, "1 - C")
	_room_sign(root, Vector3(20.3, 2.5, -11.8), 0, "1 - D")
	_slid_door(root, Vector3(-18.8, 0, -11.8), 0)   # 1-A door slid open
	_slid_door(root, Vector3(-4.8, 0, -11.8), 0)    # 1-B door slid open
	_slid_door(root, Vector3(6.3, 0, -11.8), 0)     # 1-C closed
	_slid_door(root, Vector3(20.3, 0, -11.8), 0)    # 1-D closed

	# --- South wing rooms flanking foyer already walled by foyer; corridor wall z=12 ---
	wall(root, true, -28, -8, 12, wallmat)
	wall(root, true, 8, 28, 12, wallmat)

	# --- West wing: occult club + art club (wall x=-20, between the wings) ---
	wall(root, false, -12, 12, -20, wallmat, [
		{"a": -1.0, "b": 0.4, "h": 2.2},      # occult club door
	])
	_room_sign(root, Vector3(-19.8, 2.5, -1.8), 90, "OCCULT CLUB")
	_club_door(root, Vector3(-19.85, 0, 5.0), 90, "ART CLUB")

	# --- East wing: science + cooking (wall x=20, between the wings) ---
	wall(root, false, -12, 12, 20, wallmat)
	_club_door(root, Vector3(19.85, 0, -4.0), -90, "SCIENCE LAB")
	_club_door(root, Vector3(19.85, 0, 4.0), -90, "COOKING CLUB")

	# --- Hallway dressing: lights, extinguishers, bulletin boards, vending ---
	_ceiling_lights(root)
	var red := _mat("fire", Color(0.80, 0.12, 0.14), 0.4)
	box(root, Vector3(-27.7, 1.2, 6), Vector3(0.25, 0.55, 0.35), red, true, "Fire extinguisher")
	box(root, Vector3(27.7, 1.2, -6), Vector3(0.25, 0.55, 0.35), red, true, "Fire extinguisher")

	var bb := _mat("bb", Color.WHITE, 0.9, 0.0, TexGen.bulletin(), Vector3(0.4, 0.9, 0.4))
	box(root, Vector3(0, 1.7, -11.8), Vector3(3.2, 1.4, 0.08), bb, true, "Club recruitment posters")
	box(root, Vector3(-14, 1.7, 11.8), Vector3(3.2, 1.4, 0.08), bb, true, "Exam schedule")

	var vend_b := _mat("vendb", Color.WHITE, 0.5, 0.0, TexGen.vending(Color(0.20, 0.42, 0.80)), Vector3(1.0, 0.55, 1.0))
	var vend_r := _mat("vendr", Color.WHITE, 0.5, 0.0, TexGen.vending(Color(0.80, 0.22, 0.28)), Vector3(1.0, 0.55, 1.0))
	box(root, Vector3(-10.5, 0.95, 11.5), Vector3(1.2, 1.9, 0.8), vend_b, true, "Buy a drink")
	box(root, Vector3(-12.0, 0.95, 11.5), Vector3(1.2, 1.9, 0.8), vend_r, true, "Buy a snack")

	# First-aid box (glows green in vision)
	var fa := _mat("fa", Color.WHITE, 0.7, 0.0, TexGen.firstaid(), Vector3(1.5, 1.5, 1.5))
	var fabox := box(root, Vector3(-19.8, 1.5, 10), Vector3(0.12, 0.5, 0.4), fa, true, "First-aid kit")
	refs["glow"].append(_glow_shell(fabox, Vector3(0.25, 0.6, 0.5), Color(0.2, 1.0, 0.4)))

	# Janitor corner: mop + bucket (glow green — clean-up tools)
	var wood := _mat("mop", Color(0.72, 0.58, 0.36), 0.8)
	var grey := _mat("bucket", Color(0.55, 0.57, 0.60), 0.4, 0.6)
	var mop := cylinder(root, Vector3(19.6, 0.75, 11.2), 0.03, 1.5, wood, true, "A trusty mop")
	mop.rotation_degrees = Vector3(0, 0, 12)
	cylinder(root, Vector3(19.45, 0.08, 11.2), 0.14, 0.16, grey, false)
	var bucket := cylinder(root, Vector3(18.9, 0.18, 11.3), 0.22, 0.36, grey, true, "Mop bucket")
	refs["glow"].append(_glow_shell(mop, Vector3(0.3, 1.7, 0.3), Color(0.2, 1.0, 0.4)))
	refs["glow"].append(_glow_shell(bucket, Vector3(0.55, 0.5, 0.55), Color(0.2, 1.0, 0.4)))

static func _ceiling_lights(root: Node3D) -> void:
	var lightmat := _mat("striplight", Color.WHITE, 0.3, 0.0, null, Vector3.ONE, Color(1.0, 0.98, 0.92))
	lightmat.emission_energy_multiplier = 2.2
	var spots: Array = []
	for x in [-24.0, -18.0, -12.0, -6.0, 0.0, 6.0, 12.0, 18.0, 24.0]:
		spots.append(Vector3(x, 0, -10))
		spots.append(Vector3(x, 0, 10))
	for z in [-4.0, 0.0, 4.0]:
		spots.append(Vector3(-18, 0, z))
		spots.append(Vector3(18, 0, z))
	spots.append(Vector3(0, 0, 17.5))
	spots.append(Vector3(-4, 0, 17.5))
	spots.append(Vector3(4, 0, 17.5))
	for s in spots:
		box(root, Vector3(s.x, FLOOR_H - 0.06, s.z), Vector3(1.6, 0.08, 0.5), lightmat, false)
		var l := OmniLight3D.new()
		l.position = Vector3(s.x, FLOOR_H - 0.5, s.z)
		l.light_color = Color(1.0, 0.97, 0.9)
		l.light_energy = 0.7
		l.omni_range = 5.5
		l.shadow_enabled = false
		root.add_child(l)

## Unit vector a wall-mounted thing at `yaw` degrees should face (out of the wall).
static func _face_dir(yaw: float) -> Vector3:
	var r := deg_to_rad(yaw)
	return Vector3(sin(r), 0.0, cos(r))

static func _room_sign(root: Node3D, pos: Vector3, yaw: float, text: String) -> void:
	var plate := _mat("plate", Color(0.90, 0.91, 0.94), 0.4)
	var vertical := absf(yaw) > 45.0
	var plate_size := Vector3(0.06, 0.4, 1.3) if vertical else Vector3(1.3, 0.4, 0.06)
	box(root, pos, plate_size, plate, false)
	sign3d(root, pos + _face_dir(yaw) * 0.05, text, yaw, 26)

static func _slid_door(root: Node3D, pos: Vector3, yaw: float) -> void:
	var wood := _mat("door", Color(0.58, 0.42, 0.26), 0.7)
	var dir := _face_dir(yaw)
	var d := box(root, pos + Vector3(0, 1.1, 0) + dir * 0.20, Vector3(1.4, 2.2, 0.08), wood)
	d.rotation_degrees = Vector3(0, yaw, 0)
	var w := box(root, pos + Vector3(0, 1.55, 0) + dir * 0.26, Vector3(0.5, 0.5, 0.02), _glass(), false)
	w.rotation_degrees = Vector3(0, yaw, 0)

## A labeled decorative door (locked); returns the door node for glow shells.
static func _club_door(root: Node3D, pos: Vector3, yaw: float, label: String) -> Node3D:
	var wood := _mat("door", Color(0.58, 0.42, 0.26), 0.7)
	var dir := _face_dir(yaw)
	var vertical := absf(yaw) > 45.0
	var size := Vector3(0.08, 2.2, 1.4) if vertical else Vector3(1.4, 2.2, 0.08)
	var d := box(root, pos + Vector3(0, 1.1, 0) + dir * 0.07, size, wood, true, label + " (locked)")
	sign3d(root, pos + Vector3(0, 2.45, 0) + dir * 0.14, label, yaw, 22)
	return d

## Transparent emissive shell used by Yandere Vision (visible through walls).
## Parented to the target body, centered on it.
static func _glow_shell(target: Node3D, size: Vector3, color: Color) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	mi.material_override = _glow(color)
	mi.position = Vector3.ZERO
	mi.visible = false
	target.add_child(mi)
	return mi

# ------------------------------------------------------------ classrooms

static func _build_classroom(root: Node3D, label: String, x0: float, x1: float) -> void:
	var wallmat := _mat("wall", Color(0.93, 0.92, 0.90), 0.85)
	var planks := _mat("planks", Color.WHITE, 0.5, 0.0, TexGen.planks(), Vector3(0.5, 0.5, 0.5))
	var deskwood := _mat("desk", Color(0.70, 0.54, 0.34), 0.6)
	var leg := _mat("leg", Color(0.35, 0.36, 0.40), 0.4, 0.7)
	var board := _mat("board", Color.WHITE, 0.8, 0.0, TexGen.chalkboard(), Vector3(0.35, 0.9, 0.35))

	# Partition walls between rooms
	wall(root, false, -20, -12, x0, wallmat)
	wall(root, false, -20, -12, x1, wallmat)

	# Wood floor
	box(root, Vector3((x0 + x1) * 0.5, 0.08, -16), Vector3(x1 - x0 - 0.2, 0.06, 7.6), planks, false)

	# Chalkboard on the west partition + podium
	box(root, Vector3(x0 + 0.25, 1.7, -16), Vector3(0.1, 1.2, 4.5), board, false)
	box(root, Vector3(x0 + 0.25, 1.02, -16), Vector3(0.16, 0.06, 4.6), deskwood, false)
	box(root, Vector3(x0 + 1.6, 0.55, -16), Vector3(0.9, 1.1, 1.4), deskwood, true, "Teacher's podium")

	# Desk grid: 4 x 4 facing the board
	for row in range(4):
		for col in range(4):
			var dx := x0 + 3.4 + float(row) * 2.1
			var dz := -18.6 + float(col) * 1.8
			box(root, Vector3(dx, 0.68, dz), Vector3(0.9, 0.05, 0.6), deskwood, true, "A student desk")
			box(root, Vector3(dx, 0.34, dz), Vector3(0.08, 0.66, 0.08), leg, false)
			box(root, Vector3(dx + 0.65, 0.45, dz), Vector3(0.45, 0.06, 0.45), deskwood, false)
			box(root, Vector3(dx + 0.65, 0.22, dz), Vector3(0.06, 0.44, 0.06), leg, false)

	# Room light
	var l := OmniLight3D.new()
	l.position = Vector3((x0 + x1) * 0.5, FLOOR_H - 0.4, -16)
	l.light_energy = 0.8
	l.omni_range = 7.0
	l.shadow_enabled = false
	root.add_child(l)

# ------------------------------------------------------------ occult club

static func _build_occult_club(root: Node3D) -> void:
	var wallmat := _mat("occwall", Color(0.30, 0.24, 0.38), 0.9)
	var carpet := _mat("ritual", Color.WHITE, 0.95, 0.0, TexGen.ritual_circle(), Vector3(0.125, 0.125, 0.125))
	var candle := _mat("candle", Color(0.92, 0.90, 0.84), 0.6)
	var altar := _mat("altar", Color(0.16, 0.12, 0.20), 0.7)

	# Room box: x -28..-20, z -6..4 — walls (the x=-20 wall with the door
	# opening is already built by the west wing)
	wall(root, true, -28, -20, -6, wallmat)
	wall(root, true, -28, -20, 4, wallmat)
	# Dark floor with the ritual circle centered
	box(root, Vector3(-24, 0.08, -1), Vector3(7.6, 0.06, 9.6), carpet, false)

	# Altar table + candles
	box(root, Vector3(-27.0, 0.5, -1), Vector3(1.0, 1.0, 2.2), altar, true, "An ominous altar")
	for i in range(5):
		var a := -PI / 2.0 + TAU * float(i) / 5.0
		var cp := Vector3(-24, 0, -1) + Vector3(cos(a) * 2.8, 0, sin(a) * 2.8)
		cylinder(root, cp + Vector3(0, 0.18, 0), 0.05, 0.36, candle, false)
		var flame := OmniLight3D.new()
		flame.position = cp + Vector3(0, 0.55, 0)
		flame.light_color = Color(1.0, 0.55, 0.25)
		flame.light_energy = 0.5
		flame.omni_range = 2.2
		flame.shadow_enabled = false
		root.add_child(flame)

	# Moody purple fill light
	var l := OmniLight3D.new()
	l.position = Vector3(-24, 2.6, -1)
	l.light_color = Color(0.6, 0.35, 0.9)
	l.light_energy = 0.6
	l.omni_range = 7.0
	l.shadow_enabled = false
	root.add_child(l)

# ------------------------------------------------------------ inner courtyard

static func _build_inner_courtyard(root: Node3D) -> void:
	var stone := _mat("stone", Color.WHITE, 0.55, 0.0, TexGen.stone(), Vector3(0.5, 0.5, 0.5))
	var grass := _mat("grass", Color.WHITE, 0.95, 0.0, TexGen.grass(), Vector3(0.5, 0.5, 0.5))
	var wood := _mat("bench", Color(0.55, 0.40, 0.26), 0.7)
	var stone_s := _mat("fstone", Color(0.78, 0.77, 0.75), 0.5)

	# Paving + grass patches
	box(root, Vector3(0, 0.02, 0), Vector3(31.6, 0.1, 15.6), stone, false)
	box(root, Vector3(-10, 0.07, -4), Vector3(8, 0.06, 5), grass, false)
	box(root, Vector3(10, 0.07, 4), Vector3(8, 0.06, 5), grass, false)

	# Center sakura tree
	_sakura_tree(root, Vector3(0, 0, 0), 1.2)

	# Benches
	for bz in [-5.0, 5.0]:
		box(root, Vector3(-5, 0.25, bz), Vector3(2.4, 0.5, 0.5), stone_s)
		box(root, Vector3(-5, 0.55, bz), Vector3(2.6, 0.10, 0.7), wood, true, "A quiet bench")
		box(root, Vector3(5, 0.25, bz), Vector3(2.4, 0.5, 0.5), stone_s)
		box(root, Vector3(5, 0.55, bz), Vector3(2.6, 0.10, 0.7), wood, true, "A quiet bench")

	# Petals drifting in the courtyard
	_petal_emitter(root, Vector3(0, 6, 0), Vector3(14, 1, 7))

# ------------------------------------------------------------ incinerator

static func _build_incinerator(root: Node3D, refs: Dictionary) -> void:
	var conc := _mat("conc", Color(0.62, 0.62, 0.60), 0.9)
	var metal := _mat("incin", Color(0.22, 0.23, 0.26), 0.45, 0.8)
	var ember := _mat("ember", Color(1.0, 0.4, 0.1), 0.5, 0.0, null, Vector3.ONE, Color(1.0, 0.35, 0.05))

	var base := Vector3(-20, 0, -26)
	box(root, base + Vector3(0, 0.1, 0), Vector3(10, 0.2, 8), conc)
	# Screening walls
	box(root, base + Vector3(-5, 1.25, 0), Vector3(0.3, 2.5, 8), conc)
	box(root, base + Vector3(5, 1.25, 0), Vector3(0.3, 2.5, 8), conc)
	box(root, base + Vector3(0, 1.25, -4), Vector3(10, 2.5, 0.3), conc)

	var body := box(root, base + Vector3(0, 1.3, -1.5), Vector3(2.6, 2.6, 2.6), metal, true, "The incinerator")
	box(root, base + Vector3(0, 0.7, -0.1), Vector3(1.0, 0.8, 0.1), ember, false)
	cylinder(root, base + Vector3(0.7, 3.6, -2.2), 0.25, 2.0, metal, false)
	sign3d(root, base + Vector3(0, 2.9, 0.2), "BURNABLE  WASTE  ONLY", 0, 20, Color(0.9, 0.35, 0.30))

	refs["glow"].append(_glow_shell(body, Vector3(2.9, 2.9, 2.9), Color(0.2, 1.0, 0.4)))

# ------------------------------------------------------------ sakura

static func _scatter_sakura(root: Node3D) -> void:
	# Trees lining the entrance path
	for z in [26.0, 32.0, 38.0, 44.0]:
		_sakura_tree(root, Vector3(-6.5, 0, z), 1.0)
		_sakura_tree(root, Vector3(6.5, 0, z), 1.0)
	# A few around the campus edges
	_sakura_tree(root, Vector3(-36, 0, 24), 1.15)
	_sakura_tree(root, Vector3(36, 0, 24), 1.15)
	_sakura_tree(root, Vector3(-38, 0, -10), 1.1)
	_sakura_tree(root, Vector3(38, 0, -10), 1.1)
	# Petals over the entrance path
	_petal_emitter(root, Vector3(0, 7, 34), Vector3(16, 1, 14))

static func _sakura_tree(root: Node3D, pos: Vector3, s: float) -> void:
	var trunk := _mat("trunk", Color(0.32, 0.22, 0.16), 0.9)
	var bloom_a := _mat("bloomA", Color(1.0, 0.72, 0.82), 0.9)
	var bloom_b := _mat("bloomB", Color(0.98, 0.62, 0.76), 0.9)
	cylinder(root, pos + Vector3(0, 1.5 * s, 0), 0.22 * s, 3.0 * s, trunk)
	sphere(root, pos + Vector3(0, 3.4 * s, 0), 1.5 * s, bloom_a)
	sphere(root, pos + Vector3(0.9 * s, 2.9 * s, 0.3 * s), 1.05 * s, bloom_b)
	sphere(root, pos + Vector3(-0.8 * s, 3.0 * s, -0.3 * s), 1.0 * s, bloom_b)
	sphere(root, pos + Vector3(0.1 * s, 3.1 * s, 0.9 * s), 0.9 * s, bloom_a)

static func _petal_emitter(root: Node3D, pos: Vector3, extents: Vector3) -> void:
	var p := GPUParticles3D.new()
	p.position = pos
	p.amount = 90
	p.lifetime = 7.0
	p.preprocess = 4.0
	p.visibility_aabb = AABB(Vector3(-extents.x - 4, -pos.y - 2, -extents.z - 4),
		Vector3(extents.x * 2 + 8, pos.y + 4, extents.z * 2 + 8))
	var pm := ParticleProcessMaterial.new()
	pm.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_BOX
	pm.emission_box_extents = extents
	pm.direction = Vector3(-0.3, -1, 0.1)
	pm.spread = 20.0
	pm.initial_velocity_min = 0.5
	pm.initial_velocity_max = 1.1
	pm.gravity = Vector3(0, -0.75, 0)
	pm.angular_velocity_min = -90.0
	pm.angular_velocity_max = 90.0
	pm.angle_min = 0.0
	pm.angle_max = 360.0
	p.process_material = pm
	var quad := QuadMesh.new()
	quad.size = Vector2(0.13, 0.13)
	var m := StandardMaterial3D.new()
	m.albedo_color = Color(1.0, 0.68, 0.80, 0.95)
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	quad.material = m
	p.draw_pass_1 = quad
	root.add_child(p)

# ------------------------------------------------------------ entrance clock

static func _build_entrance_clock(root: Node3D) -> Label3D:
	var l := Label3D.new()
	l.text = "07:00 AM"
	l.font_size = 64
	l.modulate = Color(0.95, 0.30, 0.55)
	l.outline_size = 10
	l.outline_modulate = Color(0.1, 0.05, 0.1)
	l.position = Vector3(0, 4.6, 20.35)
	root.add_child(l)
	return l
