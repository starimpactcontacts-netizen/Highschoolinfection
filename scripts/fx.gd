class_name FxLayer
extends CanvasLayer
## Full-screen post effects, below the HUD layer so UI stays colored:
##  - Yandere Vision: desaturated high-contrast screen filter
##  - Sanity vignette: dark corners that close in as sanity drops
##  - Camera flash: white blink when snapping a photo

var _vision_rect: ColorRect
var _vision_mat: ShaderMaterial
var _vignette_rect: ColorRect
var _vignette_mat: ShaderMaterial
var _flash_rect: ColorRect
var _vision_amt := 0.0

const VISION_SHADER := """
shader_type canvas_item;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float amount : hint_range(0.0, 1.0) = 0.0;
void fragment() {
	vec4 scr = texture(screen_tex, SCREEN_UV);
	float g = dot(scr.rgb, vec3(0.299, 0.587, 0.114));
	vec3 grey = vec3(g) * 0.8;
	grey = (grey - 0.5) * 1.35 + 0.5;
	COLOR = vec4(mix(scr.rgb, grey, amount), 1.0);
}
"""

const VIGNETTE_SHADER := """
shader_type canvas_item;
uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform vec3 tint = vec3(0.0, 0.0, 0.0);
void fragment() {
	float d = distance(UV, vec2(0.5));
	float v = smoothstep(0.30, 0.72, d);
	COLOR = vec4(tint, v * strength);
}
"""

func _ready() -> void:
	layer = 1

	_vision_mat = _make_mat(VISION_SHADER)
	_vision_rect = _full_rect(_vision_mat)

	_vignette_mat = _make_mat(VIGNETTE_SHADER)
	_vignette_rect = _full_rect(_vignette_mat)

	_flash_rect = ColorRect.new()
	_flash_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_flash_rect.color = Color(1, 1, 1, 0)
	_flash_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_flash_rect)

func _make_mat(code: String) -> ShaderMaterial:
	var sh := Shader.new()
	sh.code = code
	var m := ShaderMaterial.new()
	m.shader = sh
	return m

func _full_rect(mat: ShaderMaterial) -> ColorRect:
	var r := ColorRect.new()
	r.set_anchors_preset(Control.PRESET_FULL_RECT)
	r.material = mat
	r.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(r)
	return r

func _process(delta: float) -> void:
	# Vision fades in/out quickly
	var target := 1.0 if State.vision else 0.0
	_vision_amt = move_toward(_vision_amt, target, delta * 5.0)
	_vision_mat.set_shader_parameter("amount", _vision_amt)

	# Vignette from low sanity (dark) — always slightly red-tinted at the bottom end
	var s := State.sanity / 100.0
	var strength := (1.0 - s) * 0.85
	_vignette_mat.set_shader_parameter("strength", strength)
	_vignette_mat.set_shader_parameter("tint", Vector3(0.05, 0.0, 0.02))

	# Flash decay
	if _flash_rect.color.a > 0.0:
		_flash_rect.color.a = maxf(0.0, _flash_rect.color.a - delta * 3.0)

func flash() -> void:
	_flash_rect.color = Color(1, 1, 1, 0.9)
