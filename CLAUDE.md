# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo contains two unrelated game prototypes — know which one you're touching

- **`Assets/`, `ProjectSettings/`, `Packages/` — Unity 6 project (`6000.5.2f1`).** This is where active
  development is happening (see recent commit history: greybox generator, Fortnite-style camera, imported
  classroom art). There is no CLAUDE.md-worthy build/lint/test tooling here — it's a Unity Editor project,
  not a CLI-driven one. "Running" it means opening the folder in Unity Hub/Editor (version must match
  `ProjectSettings/ProjectVersion.txt`) and pressing Play.
- **`project.godot`, `scripts/*.gd` — legacy Godot 4 project**, described by the top-level `README.md`
  ("AKADEMI — UI & Environment Demo"). That README (procedural-only art, `scripts/state.gd`,
  `scripts/akademi.gd`, etc.) documents this Godot version, **not** the current Unity code. It was
  superseded by the Unity rewrite (commit `bd5c545` "Rebuild from scratch" onward) but left in the repo.
  Don't assume README instructions apply to `Assets/` — they don't.

When asked to add gameplay features, assume the Unity project unless the user is explicitly working with
`project.godot`/`scripts/*.gd`.

## Working with the Unity project

There's no command-line build/test/lint — everything happens through the Editor:

- Open the project root in Unity Hub with editor version `6000.5.2f1`. **This project uses the Built-in
  Render Pipeline, not URP** — confirmed via `Packages/manifest.json` (no `com.unity.render-pipelines.universal`
  entry) and `ProjectSettings/GraphicsSettings.asset` (`m_CustomRenderPipeline: {fileID: 0}`, i.e. none
  assigned). This matters for anything render-related: there is no Volume/Renderer Feature system available,
  so post-processing is done via the legacy `Camera.OnRenderImage` path (see `StormPostProcess.cs`), and
  custom materials use `Shader.Find("...")` names from the Built-in shader library (e.g. `"Legacy Shaders/..."`,
  `#pragma surface` surface shaders), not `"Universal Render Pipeline/..."` shader names.
- **Every earlier level attempt was torn out and deleted from disk** (SchoolGenerator's primitive greybox,
  CampusGenerator's primitive+prop campus, the AnimeClassroom OBJ, the "School Assets" prop pack, the
  Japanese School Corridor FBX + `HallwayBootstrap.cs`, and the BackroomsLikeAssetRe pack + level +
  `BackroomsBootstrap.cs`/`BackroomsSceneRegistration.cs`) — none of it held up visually or the user wanted a
  different map. If you see any of these referenced in old commit messages, they no longer exist; don't try
  to resurrect or reference them. Current level: `Assets/Models/Resources/YandereSimulatorMap/` (a single
  FBX + textures bundle, not a full pre-built scene like the Backrooms one was) — `GameBootstrap.BuildMap`
  loads it via `Resources.Load`, falling back to a bare temporary plane (`TemporaryGround`) only if that
  load fails.
- **`Assets/Models/Resources/StudentChan/` is the Player's character model and must not be re-extracted.**
  It's a Yandere-Simulator-derived base model whose bundled textures had cryptic filenames (`Untitled36...`,
  `Advgp-h04wu.png`, etc.) that Unity's automatic material search couldn't match — so after `ModelTexturePostprocessor`
  auto-extracted empty materials, each of the 4 `.mat` files under `StudentChan/Materials/` had its correct
  texture hand-wired in directly (by GUID) after visually inspecting what each texture actually depicted, and
  `_Color` reset from a grey 0.4 tint to white. If this FBX ever gets reimported/moved, the postprocessor will
  regenerate blank materials again and wipe that out — don't touch this file/folder without redoing the
  texture assignment (see git history on the `.mat` files for the exact GUIDs used, or re-derive by reading
  the texture images and matching them to `f02_face_00_h` / `f02_hair_00_h` / `f02_schoolwear_210_h_c` (color)
  / `f02_schoolwear_210_h_s` (shadow map) by content).
- Requires **zero manual Editor steps** — the user works purely by pressing Play, not by finding menu items:
  - `Assets/Scripts/GameBootstrap.cs` — a runtime `MonoBehaviour` that self-spawns via
    `[RuntimeInitializeOnLoadMethod]` the instant Play starts. Destroys any stale `Player`/`Map`/
    `TemporaryGround` left in the saved scene first (a leftover Player silently wins over building a fresh
    one — `EnsurePlayer` only builds the character when none exists yet; this exact bug happened once
    already). `BuildMap` loads `YandereSimulatorMap` from Resources, adds a `MeshCollider` to any renderer
    that doesn't already have one, and finds the spawn point via a ring search outward from the model's
    bounding-box center — raycasting down each candidate and confirming via `Physics.CheckCapsule` that the
    player's capsule actually fits there, since the geometric center isn't guaranteed to be open ground (it
    can land inside geometry, which is exactly what got a `CharacterController` stuck once already on a
    different map). Then spawns `Player` with the `StudentChan` model as its visual (Humanoid `Animator` +
    `SimpleHumanoidWalkAnimator` for a basic procedural walk) and attaches `ThirdPersonCamera` to
    `Camera.main` for a 3rd-person view. Writes a bounds breakdown to `GameBootstrap_Diagnostics.txt` in the
    project root every run — read that file directly rather than guessing at scale/placement problems.
  - **Window → Generate Demo Scene** (`SceneSetup.cs`) — older/simpler Editor-menu helper that just
    (re)creates `Player` + camera as a plain capsule; doesn't use `StudentChan`. Not required for normal use.
  - `Assets/Editor/ModelTexturePostprocessor.cs` — `AssetPostprocessor.OnPreprocessModel`, runs on any FBX
    under `Assets/Models/`: extracts materials to real external assets with textures auto-searched from
    sibling folders (the scripted equivalent of manually clicking "Extract Materials"), since Sketchfab-style
    FBX+textures bundles otherwise import pink/untextured. This is generally useful for the *next* map's FBX
    if it's a raw model+textures bundle — but see the `StudentChan` warning above about what it did to her.

## Dark storm atmosphere + shaders

All auto-run via `[RuntimeInitializeOnLoadMethod]`, no manual setup, same as everything else here:

- `Assets/Scripts/StormAtmosphere.cs` — the non-shader half of the storm look: finds (or creates) the scene's
  directional light and dims/greys it with shadows off (`LightShadows.None` — overcast light is diffuse, no
  crisp sun shadows), sets flat ambient lighting, points `Camera.main` at a solid dark grey-blue color (no
  cloud texture available, so a flat overcast sky stands in — realistically close to how a real overcast sky
  looks anyway) plus matching linear fog (`fogStartDistance`/`fogEndDistance` tuned so ~20m+ stays legible),
  builds a world-space rain `ParticleSystem` that follows the Player/camera every frame (`Update()`, since it
  can't assume load order against `GameBootstrap`), and runs a `LightningLoop` coroutine that flashes the
  directional light and fires a delayed thunder rumble on a random interval. **Both rain-hiss and
  thunder-rumble audio are procedurally generated** (`GenerateFilteredNoiseClip` — filtered white noise via
  `AudioClip.Create`/`SetData`) since there are no audio assets anywhere in this project; don't assume a real
  recording exists somewhere.
- `Assets/Shaders/StormColorGrade.shader` + `Assets/Scripts/StormPostProcess.cs` — the color-grade half:
  desaturation, contrast, cold/green tint, vignette, film grain, and a cheap depth-based defocus blur (small
  fixed-pattern blur weighted by distance from a focus plane via `_CameraDepthTexture` — **not** true bokeh
  DOF, that would need more than a Built-in RP `OnRenderImage` pass is worth here). `StormAtmosphere` attaches
  this to `Camera.main` automatically.
- `Assets/Shaders/ToonOutline.shader` + `Assets/Scripts/ToonOutlineApplier.cs` — inverted-hull black outline
  (extrude along normals, cull front faces, flat color). Applied by duplicating each renderer under a
  target into a *sibling* GameObject with the outline shader — the target's own material assignments are
  never touched. Wired to `StudentChan` in `GameBootstrap.EnsurePlayer` (width `0.0025`) and to the whole map
  in `GameBootstrap.BuildMap` (width `0.0008` — thinner, so building edges read as a subtle line rather than
  the character's heavier cartoon border). **There is no zombie model in this project** — if "zombie"
  gameplay gets requested, that's a new asset import, not something already here to reuse.
- `Assets/Shaders/WetSurface.shader` (opaque: ground/metal/walls) / `Assets/Shaders/RainWindow.shader`
  (transparent: glass) + `Assets/Scripts/WetSurfaceApplier.cs` — a custom cel-shaded/toon lighting model
  (`#pragma surface surf Toon`) with fake wet reflection via `Emission`. **`WetSurfaceApplier.Apply` is NOT
  called from `GameBootstrap` anymore** — it replaced every renderer's material on the map at Play time, and
  for any material where it couldn't resolve a real `_MainTex` (including anything manually assigned/swapped
  in the Editor afterward) it silently fell back to the shader's default flat white texture × tint, i.e. a
  solid color block. That directly fought against actually placing/texturing objects by hand, which is why it
  got pulled. The shader/applier files are still here in case this comes back, but don't wire the `Apply`
  call back in without also making it skip/ignore materials that already resolve a real texture, and without
  re-running it in a way that clobbers manual changes on a second Play session.

## Zombie/Human match MVP — NOT networked, Photon is not in this project

`Assets/Scripts/MatchManager.cs` is the full rules engine (lobby -> 3s countdown -> 5min playing ->
result -> back to lobby, first-registered-player-is-Zombie role assignment, detection radius, win
conditions), but **there is no Photon PUN2 (or any networking) in this
project yet.** **Touched Humans are converted to Zombies, not eliminated/removed** — `ConvertToZombie`
flips their `PlayerRole`, destroys their `HumanAbility`, and adds `ZombieAbility` so they immediately
start hunting too (matching "they become zombie" — this replaced an earlier deactivate-on-touch
design). The Zombie side grows over the course of a match; Zombies win once every Human has been
converted, Humans win if the timer runs out with at least one still Human. `HumansConverted` is
the "eaten" stat shown on the result screen. The owner has added "PUN 2 - FREE" to their Unity Asset Store account ("My Assets"),
but that's just the account entitlement — it still needs to be imported into this actual project via
Package Manager -> My Assets (or the Asset Store window) inside the Editor before anything under
`Assets/` reflects it; check `Packages/manifest.json`/`find Assets -iname "*photon*"` before assuming
it's present. Don't assume real multiplayer exists just because `MatchManager` talks about
"players" — every "player" in a running session right now is a local `Transform`, not a network peer.

- `Assets/Scripts/DummyHuman.cs` — since a solo Play session only ever has one real (keyboard-driven)
  player, and that player always registers first (always Zombie), a match with zero real Humans
  would resolve instantly with nothing to observe. `GameBootstrap.SpawnDummyHumans` spawns 5 of
  these (simple wander + periodic auto-hide, no pathfinding/NavMesh) purely so the Zombie has
  something to hunt locally, using the same `StudentChan` model as the real Player (via the shared
  `GameBootstrap.BuildCharacterVisual` helper) rather than bare capsules. **These are test
  scaffolding — delete them once Photon spawns real networked Humans**, don't mistake them for
  permanent NPCs/AI enemies. When one gets converted, `MatchManager.ConvertToZombie` explicitly
  stops its coroutines and disables the component — otherwise its `HideCycle` coroutine would keep
  calling `SetHiding` on a `HumanAbility` that conversion just destroyed.
- `Assets/Scripts/ZombieAbility.cs` / `Assets/Scripts/HumanAbility.cs` — attached to a player
  *after* `MatchManager.RegisterPlayer` returns their role (the role isn't known beforehand, so
  these can't be pre-attached). Zombie: 2x speed via `PrototypePlayerController.SpeedMultiplier`,
  green tint via *instanced* materials (`renderer.materials`, not `sharedMaterials` — otherwise
  every player sharing the StudentChan model would turn green). Human: crouch/hide toggle (`C` key,
  also callable via `SetHiding` for `DummyHuman`'s AI-driven toggling), shrinks `CharacterController`
  height while hiding.
- **Spec inconsistency, resolved one way, not silently**: the brief said hiding needs "50m+ radius"
  to be detected, which taken literally is a *larger* radius than the normal 15m (i.e. easier to
  detect) — contradicting "harder to detect" in the same sentence. Implemented as the sensible
  opposite: hiding shrinks `MatchManager`'s effective detection radius down to 3m. If that's not
  what was meant, this is the one place to revisit.
- `Assets/Scripts/GameHUD.cs` — procedural Canvas built at Play time (timer top-center, role badge
  top-left, player count top-right, "HIDING" bottom-left, a contextual hide-instruction prompt
  bottom-center — "Press [C] to hide" or "Press [Left Click] to hide" when a spot's in reach, "Press
  [C] to stop hiding" while crouched — and a full result-screen overlay), driven by
  `MatchManager.Instance` each frame. Deliberately uses legacy `UnityEngine.UI.Text` with Unity's
  built-in font, not TextMeshPro — TMP needs its "Essentials" resources imported once via a dialog
  before `TextMeshProUGUI` reliably renders anything, and nothing in this project guarantees that's
  happened, which matters for something built with zero manual setup steps.
- `MatchManager.requiredPlayers` defaults to `6` (5 dummies + 1 real player), not the spec's `11` —
  changing it to 11 today would need 10 more locally-spawned dummies just to fill the lobby, which
  is purely a local-testing knob. Set it to whatever the real Photon room size should be once that
  exists.
- `Assets/Scripts/HidingSpot.cs` — clickable hide-in-place props (lockers, cabinet stand-ins) with a
  hard 10s cap, distinct from `HumanAbility`'s crouch: entering freezes movement, disables the
  player's renderers (fully invisible, not just crouched), and calls `HumanAbility.SetHiding(true)`
  then **disables** the `HumanAbility` component itself so its own `C`-key toggle can't fight the
  locker's forced state while occupied. Ejects automatically at 10s or on a second click; not gated
  on facing the object, just proximity (`interactRange`) + left-click — a raycast/hover-click model
  doesn't really work here since `ThirdPersonCamera` locks the cursor to screen center during
  gameplay. `GameBootstrap.SpawnHidingSpots` places these using `FindClearPointsForProps` (the same
  ring-search-plus-capsule-check technique as the player's own spawn point, extended to collect
  several spaced-apart points instead of just the first). **There's no way to target "the hallways"
  specifically** — this map's submeshes have generic auto-generated names (`Body152` etc.), not the
  descriptive material names that made ground/wall categorization possible elsewhere — so spots land
  on confirmed-clear floor generally, which may need manual repositioning in the Editor.
- `Assets/Models/Resources/SchoolLocker/` — moved here from `Assets/Models/SchoolLocker/` so
  `Resources.Load` can find it at runtime (matches the `StudentChan`/`YandereSimulatorMap`
  convention). Its scale relative to the map is untested/unverified — see `lockerScale` in
  `GameBootstrap.SpawnHidingSpots` if lockers read as the wrong size next to the building.

## Unity runtime script architecture (`Assets/Scripts/`)

- `PrototypePlayerController.cs` — `CharacterController`-based third-person mover. Reads WASD, moves
  **relative to `cameraTransform`'s yaw** (not world space), smooth-turns the body to face the movement
  heading, applies simple gravity, and drives an optional Animator `"Speed"` float. It does not read the
  camera's rotation for anything but yaw, so it's agnostic to which camera script is attached.
- `ThirdPersonCamera.cs` — the camera currently paired with the player (Fortnite-style over-the-shoulder
  orbit): mouse-drives yaw/pitch, holds a shoulder offset, and spherecasts to pull the camera in front of
  geometry so it doesn't clip through walls indoors. Owns cursor lock/unlock (click to capture, Esc to
  release).
  - `SetTarget()` is the wiring point editor scripts use to attach it to a spawned `Player` at runtime.
- `IsometricFollowCamera.cs` — an older fixed-offset/SmoothDamp follow camera (no mouse input, no
  collision handling). `GameBootstrap.cs` actively strips this component off `Camera.main` if present
  and replaces it with `ThirdPersonCamera`, so treat it as superseded rather than in active use.

Both camera scripts and `PrototypePlayerController` are designed to be wired together only through
`EnsurePlayer` in `GameBootstrap.cs` (runtime, auto-run) or `GenerateScene` in `Assets/Editor/SceneSetup.cs`
(Editor menu, manual) — there are no scene-authored prefabs for `Player` or the camera rig; they're always
spawned/reconfigured in code. `GameBootstrap.EnsurePlayer` uses the `StudentChan` model as `BodyVisual`
(falling back to a plain capsule if that Resources load fails); `SceneSetup.cs` still just uses a capsule.
Whichever visual is used, it should be positioned to match `CharacterController.center` (capsule) or pivoted
at the feet (humanoid rig) — mismatching this makes the visible body float or sink relative to the actual
collider.

## Known repo quirks

- `.gitignore` is still Godot-only (`.godot/`, `.import/`, etc.) — it does **not** exclude Unity's generated
  `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or the `.meta` sidecar files, which is why those currently
  show as untracked. Be deliberate about what you stage from the repo root; the generated Unity folders are
  not meant to be committed.
