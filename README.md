# AKADEMI — UI & Environment Demo

A single-player, walkable anime-high-school demo built in **Godot 4**, focused
entirely on **environment + UI**. No multiplayer, no game loop — this is the
visual proof-of-concept. Every texture, mesh, icon, and effect is **generated
in code**: the repo ships zero art assets.

## Run it

1. Open Godot 4, **Import** this folder's `project.godot`.
2. Press **F5**. Title screen → **START**.
3. You spawn just inside the main gate. Click the window once to capture the
   mouse and look around.

## What's in the world

- **Main gate** with iron doors, perimeter walls, and a sakura-lined stone path
- **Fountain plaza** with running spray and benches
- **The school**: white hollow-square building, 4-storey facade, glass windows
- **Ground floor** fully walkable: entrance hall with shoe lockers, a ring
  hallway around an **open-air inner courtyard** (big sakura tree, benches),
  classrooms **1-A / 1-B** furnished with desks + chalkboards, an **Occult
  Club** room (ritual circle, candles, purple light), labeled locked doors
  (Nurse, Faculty, Art, Science, Cooking), vending machines, bulletin boards,
  fire extinguishers, a janitor's mop corner
- **Incinerator area** hidden behind the school
- Drifting **sakura petals**, sun + sky, polished reflective floors

## The UI (the point of this demo)

| Element | Where | Notes |
| --- | --- | --- |
| Clock + day + phase | top right | big pink digits; game time actually advances |
| Heartbeat sanity meter | bottom right | pulsing heart; beats faster + turns dark red as sanity drops |
| Reputation bar | bottom center | dark-red → pink gradient with slider marker |
| Interaction prompt | center | walk up to props and press **E** for flavor text |
| **Smartphone pause menu** | **Enter** | app grid: Calendar, Camera, Schemes, Student Info, Settings |
| **Yandere Vision** | **V** | world goes grey; key items glow green / yellow through walls |
| Photo mode | phone → Camera | viewfinder brackets, REC tag, LMB snap flash, RMB exit |
| Sanity visual shift | press **1** / **2** | world desaturates, vignette closes in, camera shakes at low sanity |

The phone's **Settings app has live sliders** for sanity / reputation /
clock speed — drag them and close the phone to watch the world change.

## Controls

| Action | Key |
| --- | --- |
| Move / run | WASD / Shift |
| Look | Mouse (click to capture, Esc to release) |
| Interact | E |
| Smartphone menu | Enter |
| Yandere Vision | V |
| Sanity down / up (demo) | 1 / 2 |
| Reputation down / up (demo) | - / + |

## Code layout

| File | What it does |
| --- | --- |
| `scripts/state.gd` | Autoload: sanity, reputation, clock, UI flags, input map |
| `scripts/texgen.gd` | Procedural textures (linoleum, lockers, facade, vending…) |
| `scripts/akademi.gd` | Builds the whole campus in code |
| `scripts/player_fp.gd` | First-person controller + interaction raycast |
| `scripts/fx.gd` | Post FX: vision greyscale shader, sanity vignette, photo flash |
| `scripts/hud.gd` | Clock, heart meter, reputation bar, prompts, viewfinder |
| `scripts/phone.gd` | Smartphone pause menu + apps |
| `scripts/title.gd` | Title screen with drifting petals |
| `scripts/school.gd` | Scene orchestrator + global hotkeys |

## Next steps (when this look is approved)

- Rooftop access + more floors
- NPC students walking routines
- Real character models & animations
- Swap procedural textures for real art, room by room
