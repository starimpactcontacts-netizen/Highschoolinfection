# Campus Build

Generated procedurally by `Assets/Scripts/CampusGenerator.cs`, a runtime `MonoBehaviour`
that builds the whole level in `Awake()` — press Play and it's there, no menu clicks.
It also spawns `Player` (with `PrototypePlayerController` + `ThirdPersonCamera`) just
outside the front gate, and hides (not deletes) any pre-existing `School` /
`AnimeClassroom` objects so nothing overlaps.

This is a **blockout**: primitives (cubes, planes, cylinders, spheres) with flat colors,
not final art. Scale matches the original `SchoolGenerator` greybox (90 wide x 22 deep
main building, 4.5 units per floor).

## Hierarchy

Everything lives under a single `Campus` root:

```
Campus
├── Ground                 flat grass plane covering the whole property
├── Perimeter               solid walls (west/east/back) + fence (posts+rail, front) + Gate
├── Courtyard               path, benches, clocktower, cherry blossom trees
├── MainBuilding
│   ├── Floor_1              ground floor: Cafeteria, 3x Classroom, Stairwell shaft entry
│   ├── Floor_2              Library, 3x Classroom, Stairwell shaft entry
│   ├── Floor_3              5x Classroom, Stairwell shaft entry
│   ├── Roof                 rooftop deck (walkable) + RoofRailing
│   └── Stairwell            3 ramps + 2 landings, ground → roof
├── Annex                    west wing: 3 more classrooms off a short hallway
│   └── Breezeway            connects Annex to MainBuilding's west wall
└── Gym                      tall single-story hall behind MainBuilding, + bleachers
    └── Corridor             connects Gym to MainBuilding's back wall
```

## Where things are (world X/Z, Y = floor height)

- **Front gate**: `Z = -65` (walk-through gap in the front fence, flanked by two pillars).
  Player spawns just outside it at `Z = -73`, facing the school.
- **Courtyard**: `Z = -65` to `0`, centered path with cherry trees, benches, clocktower.
- **Main building**: `X = -45..45`, `Z = 0..22`, 3 floors (`Y = 0, 4.5, 9.0`), roof at `Y = 13.5`.
  - Segment 6 of every floor (`X = 30..45`) is the internal stairwell column — its floor
    slabs are intentionally left open there so the ramps can rise straight from ground to roof.
- **Annex**: `X = -88..-53`, same depth as main building, single floor, reached via the
  breezeway at `X = -53..-45`.
- **Gym**: `Z = 30..60`, `X = -17..17`, one tall room (`Y` up to `9`), reached via the
  corridor at `Z = 22..30`.
- **Perimeter**: rectangle from roughly `X = -96..55`, `Z = -65..68`.

## Known blockout limitations

- No NPCs or interactable objects (by design — this pass is collision/walkability only).
- Stairwell flights are single continuous ramps, not stepped stairs (much more reliable
  for `CharacterController` than boxed steps).
- Furniture (desks, tables, shelves, bleachers) is static dressing with default primitive
  colliders — nothing scripted or pickable.
