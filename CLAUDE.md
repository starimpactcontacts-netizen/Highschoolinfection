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
  character into a *sibling* GameObject with the outline shader — the character's own material assignments
  are never touched. Currently only wired to `StudentChan` in `GameBootstrap.EnsurePlayer`; **there is no
  zombie model in this project** — if "zombie" gameplay gets requested, that's a new asset import, not
  something already here to reuse.
- `Assets/Shaders/WetSurface.shader` / `Assets/Shaders/RainWindow.shader` + `Assets/Scripts/WetSurfaceApplier.cs`
  — wet-ground/metal reflection (reflection-probe-driven Standard surface shader with high smoothness/metallic
  + a cheap panning-sine ripple normal, no real puddle normal maps) and rain-streaked windows (procedural
  scrolling rivulet + droplet noise via Emission, no textures). `WetSurfaceApplier.Apply` matches renderers by
  object/material name (`"floor"`, `"metal"`, `"window"`, etc. — plus `"green"` specifically because this
  map's actual ground plane material is literally named `"Paint - Metallic (Green)"`, confirmed from
  `GameBootstrap_Diagnostics.txt`, not a general-purpose rule) and copies each material's existing
  `_MainTex`/`_Color` over so nothing needs re-texturing. Called from `GameBootstrap.BuildMap` right after the
  map instantiates.

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
