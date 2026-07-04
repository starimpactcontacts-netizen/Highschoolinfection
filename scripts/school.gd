extends Node3D
## The walkable school scene. Builds the campus, spawns the player, and wires
## up the UI layers (post-fx → HUD → phone). Also owns the global hotkeys:
##   Enter — smartphone menu     V — Yandere Vision
##   Esc   — close phone / release mouse
##   1 / 2 — sanity down/up      - / + — reputation down/up
##   LMB / RMB in photo mode — snap / exit

var _env: Environment
var _glow_shells: Array = []
var _entrance_clock: Label3D
var _player: PlayerFP
var _fx: FxLayer
var _hud: HudLayer
var _phone: PhoneLayer

func _ready() -> void:
	State.reset()

	var refs := Akademi.build(self)
	_env = refs["env"]
	_glow_shells = refs["glow"]
	_entrance_clock = refs["clock"]

	_player = PlayerFP.new()
	_player.position = Vector3(0, 0.2, 42)   # just inside the main gate
	add_child(_player)

	_fx = FxLayer.new()
	add_child(_fx)
	_hud = HudLayer.new()
	add_child(_hud)
	_phone = PhoneLayer.new()
	add_child(_phone)

func _process(_delta: float) -> void:
	# Sanity drives the world's color: vibrant at 100%, grey and dim at 0%.
	var s := State.sanity / 100.0
	if State.vision:
		_env.adjustment_saturation = 1.0   # the vision shader handles the look
	else:
		_env.adjustment_saturation = lerpf(0.35, 1.1, s)
	_env.adjustment_brightness = lerpf(0.88, 1.0, s)

	# Vision glow shells
	for shell in _glow_shells:
		if is_instance_valid(shell):
			shell.visible = State.vision

	# Entrance clock mirrors game time
	if _entrance_clock:
		_entrance_clock.text = State.clock_text()

func _unhandled_input(event: InputEvent) -> void:
	# --- Photo mode clicks ---
	if State.camera_mode and event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_fx.flash()
			State.toast_request = "Photo saved to AKA-GRAM. Probably."
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			State.camera_mode = false
		return

	if event.is_action_pressed("phone"):
		if State.camera_mode:
			State.camera_mode = false
		if _phone.is_open():
			_phone.close()
		else:
			_phone.open()
		return

	if event.is_action_pressed("ui_cancel"):
		if _phone.is_open():
			_phone.close()
		elif State.camera_mode:
			State.camera_mode = false
		elif Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		return

	if event.is_action_pressed("vision") and not State.ui_open:
		State.vision = not State.vision
		return

	# --- Demo stat hotkeys ---
	if event is InputEventKey and event.pressed and not event.echo and not State.ui_open:
		match event.physical_keycode:
			KEY_1:
				State.add_sanity(-10.0)
			KEY_2:
				State.add_sanity(10.0)
			KEY_MINUS:
				State.add_reputation(-10.0)
			KEY_EQUAL:
				State.add_reputation(10.0)
