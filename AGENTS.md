# AGENTS.md — SnackTowerDefense

Guidance for AI agents working in this repo.

**Read these before non-trivial changes:**
- `docs/HANDOFF.md` — current state, what is verified vs not, next steps
- `docs/ARCHITECTURE.md` — codebase map, conventions, verification loop, gotchas
- `docs/MobRoster.md` — the 35-wave fruit & vegetable roster and boss concepts
- `docs/MobModelling.md` — how mobs are modelled, rigged, walk-animated and verified (read before making or changing a mob model)

**Open work is tracked in GitHub issues** (`github.com/zjones95/SnackTowerDefense/issues`,
currently **#1–#19**) — check there before starting something non-trivial, and file
new work as an issue.

## The project

- **Unity 6.6 (`6000.6.0f1`)**, built-in render pipeline.
- **No scenes and no prefabs.** The game bootstraps itself via
  `[RuntimeInitializeOnLoadMethod]`; `Assets/Scenes/Boot.unity` is an empty stub
  that exists only so builds have a scene.
- Map, room, textures, audio and UI are all **procedural**; the UI is **IMGUI**.
- Character art is **Blender-authored glTF** in
  `Assets/Resources/Snack/{Towers,Mobs,Projectiles}`, mirrored into
  `Assets/Models/**`.

## Verify changes

The **Unity editor must be closed** for batch mode (it locks `Library/`).

```powershell
# compile
Unity.exe -batchmode -projectPath C:\Users\Desktop\SnackTowerDefense `
          -executeMethod CICompileCheck.EnsureBootScene -quit -logFile <log>
# compile + build a player
Unity.exe -batchmode -projectPath C:\Users\Desktop\SnackTowerDefense `
          -executeMethod CICompileCheck.Build -quit -logFile <log>
```

Editor render helpers (write PNGs to `%TEMP%\opencode\`): `SnackPreview.Render`
(towers + mobs), `TDArenaPreview.Render` (map + room), `TDMusicCheck.Verify`.

> **Do not build the WebGL player unless the user explicitly asks.** A batch
> *compile* (or, when asked, the Windows player) is the normal verification; the
> WebGL build + Pages publish is heavy. When a WebGL build *is* requested, use the
> `webgl-build` skill (or `tools\publish-webgl.ps1`) — the licence-free deploy to
> the `webgl` branch.

- **Never claim a behavioural change works** from a compile alone.
- **Play mode and networking cannot be tested by an agent** — the editor pauses
  when unfocused. Ask the user to play-test and report back.

## Conventions

- Materials: `TDVisuals.Mat(color, metallic, smooth)` (opaque, cached),
  `TDVisuals.TransparentMat(color, alpha)` (alpha-blended), `TexturedMat` (tiled).
- Models load by path `Snack/<folder>/<name>` and **fall back to procedural art**
  when missing: towers `Snack/Towers/<TowerName>`, mobs `Snack/Mobs/<MobDef.id>`,
  projectiles `Snack/Projectiles/<TowerName>`.
- Models must be authored ~1 unit tall, sitting on **z = 0**, centred in X/Y.
- `TDGameManager` is a **partial class** across four files; share state freely.
- Adding a mob needs **no code**: drop `<id>.glb` into `Assets/Resources/Snack/Mobs/`
  where `<id>` matches `MobDef.id`, and mirror it into `Assets/Models/Mobs/`.

## Gotchas

1. Bake transforms (`bpy.ops.object.transform_apply`) **before** re-centring a
   joined mesh in Blender, or the model lands offset.
2. Towers and mobs sit on **y = 0** — the play-mat tiles are centred at `y = -0.05`
   so their top face is exactly 0. Moving the tiles moves everything.
3. TextMesh reads from its **−Z** face: billboard by copying
   `Camera.main.transform.rotation`, never `LookRotation` toward the camera.
4. Tower head-split keeps children named `pedestal`/`rim` fixed and rotates the
   rest; the snack towers have neither, so the **whole model rotates** to aim.
5. Blender axes: `+Z` → Unity `+Y`, `+Y` → Unity `−Z`. For previews set
   `scene.view_settings.view_transform = 'Standard'` (AgX desaturates badly).
6. The Blender MCP runs code at module scope — use `print`, no top-level `return`.

## Workflow

1. Read the docs above, then the files you intend to change.
2. Make the change; keep it consistent with surrounding style.
3. Verify by compiling in batch mode (and rendering, if visual).
4. Commit and push to `main`; **then** build if needed (a player's version stamp is
   `HEAD`'s sha, so commit *before* building). Ask the user to play-test behaviour.
5. **Only build WebGL when explicitly asked** (use the `webgl-build` skill); commit
   does not imply a WebGL publish.
