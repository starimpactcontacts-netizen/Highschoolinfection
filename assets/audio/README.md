# Audio Assets

Place the following `.ogg` or `.wav` audio files in this directory. The audio system will automatically play them when events occur.

## Required Sound Effects

| File | Purpose | Description |
|------|---------|-------------|
| `infect.ogg` | Zombie infection sound | Plays when a zombie successfully infects a human. Should be a wet, organic sound (~1-2 sec) |
| `infected.ogg` | Player transformation | Plays when local player becomes a zombie. Could be a scream or transformation sound (~1-2 sec) |
| `hide.ogg` | Player hides | Plays when entering a locker. Should be subtle and "safe" sounding (~0.5-1 sec) |
| `leave.ogg` | Player exits locker | Plays when exiting a locker. Should sound cautious (~0.5-1 sec) |
| `zombie_near.ogg` | Proximity warning | Plays when a zombie is very close. Could be a low groan or heartbeat (~0.5 sec, loops) |
| `danger_pulse.ogg` | Danger warning | Plays when threat level is high. Should be a pulse/beep sound (~0.3 sec, can repeat) |
| `locker_open.ogg` | Locker door opens | Plays when locker door opens (leaving). Mechanical creak sound (~0.8-1 sec) |
| `locker_close.ogg` | Locker door closes | Plays when locker door closes (hiding). Mechanical close sound (~0.8-1 sec) |
| `heartbeat.ogg` | Hiding heartbeat | Plays quietly when player is hiding. Could loop or play once (~1-2 sec) |

## Implementation Notes

- All files should be in `.ogg` format (Godot's preferred audio format)
- Alternatively, use `.wav` format (larger file size but better compatibility)
- Keep audio files relatively short (1-3 seconds) to avoid long load times
- Consider adding multiple variants for the same sound (e.g., `infect_1.ogg`, `infect_2.ogg`) to vary the audio feedback

## Current Status

🔧 **Audio system is wired up** but placeholder files are missing. The game will run but won't play sounds until these files are added.

## How to Add Audio

1. Create or download short audio clips for the sounds above
2. Save them as `.ogg` files in this directory
3. Reload the Godot editor
4. Test in-game—sounds will play automatically at key moments

## Quick Audio Generation

If you don't have audio files yet, you can:
- Use free online audio generators (e.g., Freesound, Pixabay, OpenGameArt)
- Generate simple beeps/tones programmatically in Godot (see `scripts/audio.gd`)
- Use placeholder sounds temporarily while developing gameplay
