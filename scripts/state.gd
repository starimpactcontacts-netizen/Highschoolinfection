extends Node
## Global game state. Autoloaded as "State".
## Holds the demo's simulated stats (sanity, reputation), the in-game clock,
## and UI mode flags. The HUD, post-fx, and environment all read from here
## every frame, so changing a value anywhere updates everything at once.

# --- Simulated stats (drive the HUD + visual mood) ---
var sanity := 100.0          # 0..100 — low sanity desaturates the world
var reputation := 0.0        # -100..+100

# --- In-game clock ---
const DAY_NAME := "MONDAY"
var time_minutes := 7.0 * 60.0     # 07:00 AM
var clock_speed := 6.0             # game-minutes per real second

# --- UI mode flags ---
var ui_open := false         # smartphone menu is up
var vision := false          # Yandere Vision overlay active
var camera_mode := false     # phone camera viewfinder active

# --- Cross-node message passing (HUD polls these) ---
var prompt := ""             # "[E] ..." interaction prompt
var toast_request := ""      # one-shot flavor message

# --- Settings ---
var mouse_sens := 0.0025

func _ready() -> void:
	_setup_input()

func _process(delta: float) -> void:
	# Clock runs while walking around; freezes inside the phone menu.
	if not ui_open:
		time_minutes = minf(time_minutes + clock_speed * delta, 18.0 * 60.0)

func reset() -> void:
	sanity = 100.0
	reputation = 0.0
	time_minutes = 7.0 * 60.0
	ui_open = false
	vision = false
	camera_mode = false
	prompt = ""
	toast_request = ""

func add_sanity(v: float) -> void:
	sanity = clampf(sanity + v, 0.0, 100.0)

func add_reputation(v: float) -> void:
	reputation = clampf(reputation + v, -100.0, 100.0)

## "07:03 AM" style clock text.
func clock_text() -> String:
	var total := int(time_minutes)
	var h := total / 60
	var m := total % 60
	var suffix := "AM" if h < 12 else "PM"
	var h12 := h % 12
	if h12 == 0:
		h12 = 12
	return "%02d:%02d %s" % [h12, m, suffix]

## What part of the school day we're in.
func phase_text() -> String:
	if time_minutes < 8.5 * 60.0:
		return "BEFORE SCHOOL"
	elif time_minutes < 13.0 * 60.0:
		return "CLASS TIME"
	elif time_minutes < 13.5 * 60.0:
		return "LUNCH TIME"
	elif time_minutes < 15.5 * 60.0:
		return "CLASS TIME"
	elif time_minutes < 18.0 * 60.0:
		return "AFTER SCHOOL"
	return "GO HOME"

func _setup_input() -> void:
	_bind("move_forward", KEY_W)
	_bind("move_back", KEY_S)
	_bind("move_left", KEY_A)
	_bind("move_right", KEY_D)
	_bind("run", KEY_SHIFT)
	_bind("interact", KEY_E)
	_bind("vision", KEY_V)
	_bind("phone", KEY_ENTER)
	_bind("phone", KEY_KP_ENTER)

func _bind(action: String, keycode: Key) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action)
	var ev := InputEventKey.new()
	ev.physical_keycode = keycode
	InputMap.action_add_event(action, ev)
