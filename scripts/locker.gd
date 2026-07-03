extends StaticBody3D
## A red hallway locker a human can hide inside for a few seconds.
## Occupancy is decided by the server; this script just tracks the flag
## and animates the door. Every locker is in the "locker" group so players
## and the game manager can find it by its node name.

@onready var door: Node3D = $Door
@onready var hide_point: Marker3D = $HidePoint

var occupied := false

func _ready() -> void:
	add_to_group("locker")

## Swing the door open or closed (purely cosmetic).
func set_open(open: bool) -> void:
	if door:
		door.rotation.y = deg_to_rad(-100.0) if open else 0.0

## Where a hidden player is placed (inside the locker, facing out).
func get_hide_transform() -> Transform3D:
	return hide_point.global_transform if hide_point else global_transform
