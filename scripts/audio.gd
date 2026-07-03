extends Node
## Global audio manager. Handles all sound effects and ambient audio.
## Autoload: add to Project → Project Settings → Autoload as "SFX"

var _players: Dictionary = {}  # node_path -> AudioStreamPlayer

func _ready() -> void:
	add_to_group("audio")

## Play a sound effect. Creates a temporary AudioStreamPlayer if needed.
func play(sound_id: String, position: Vector3 = Vector3.ZERO, volume_db: float = 0.0) -> void:
	var player = AudioStreamPlayer3D.new() if position != Vector3.ZERO else AudioStreamPlayer.new()
	player.bus = "SFX"

	# Map sound_id to audio file paths (add these files to res://assets/audio/)
	var sound_paths = {
		"infect": "res://assets/audio/infect.ogg",
		"infected": "res://assets/audio/infected.ogg",
		"hide": "res://assets/audio/hide.ogg",
		"leave": "res://assets/audio/leave.ogg",
		"zombie_near": "res://assets/audio/zombie_near.ogg",
		"danger_pulse": "res://assets/audio/danger_pulse.ogg",
		"locker_open": "res://assets/audio/locker_open.ogg",
		"locker_close": "res://assets/audio/locker_close.ogg",
		"heartbeat": "res://assets/audio/heartbeat.ogg",
	}

	if sound_id in sound_paths:
		var path = sound_paths[sound_id]
		if ResourceLoader.exists(path):
			player.stream = load(path)
			player.volume_db = volume_db
			if player is AudioStreamPlayer3D:
				player.global_position = position
				player.max_distance = 50.0
			add_child(player)
			player.play()
			await player.finished
			player.queue_free()

## Stop all sounds (used when transitioning scenes)
func stop_all() -> void:
	for p in get_children():
		if p is AudioStreamPlayer or p is AudioStreamPlayer3D:
			p.stop()
			p.queue_free()
