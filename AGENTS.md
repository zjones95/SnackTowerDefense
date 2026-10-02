# AGENTS.md — SnackTowerDefense

Guidance for AI agents working in this repo.

**Read these before non-trivial changes:**
- `docs/HANDOFF.md` — current state, what is verified vs not, next steps
- `docs/ARCHITECTURE.md` — codebase map, conventions, verification loop, gotchas
- `docs/MobRoster.md` — the 50-wave fruit & vegetable roster and boss concepts
- `docs/MobModelling.md` — how mobs are modelled, rigged, walk-animated and verified (read before making or changing a mob model)

**Open work is tracked in GitHub issues** (`github.com/zjones95/SnackTowerDefense/issues`)
— check there before starting something non-trivial, and file new work as an issue.
When fixing a filed issue, close it with a comment naming the commit and stating
what was and was not verified.

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

Editor helpers (write PNGs/reports to `%TEMP%\opencode\`): `SnackPreview.Render`
(towers + mobs), `TDArenaPreview.Render` (map + room), `TDMusicCheck.Verify`,
`MobWalkCheck.Verify` (all mob walk rigs), `LateWaveCheck.Verify` (wave table,
model loading, boss HP), `TropicalMapCheck.Verify` (map layout). New features
should add a `-executeMethod` check like these rather than relying on a bare compile.

> **Do not build the WebGL player unless the user explicitly asks.** A batch
> *compile* (or, when asked, the Windows player) is the normal verification; the
> WebGL build + Pages publish is heavy. When a WebGL build *is* requested, use the
> `webgl-build` skill (or `tools\publish-webgl.ps1`) — the licence-free deploy to
> the `webgl` branch.

- **Never claim a behavioural change works** from a compile alone.
- **After every moderate or major change, build the Windows x64 player** with
  `CI_BUILD_TARGET=StandaloneWindows64` and `CICompileCheck.Build`. Commit and
  push first so the player version stamp contains the new commit SHA. Confirm
  `CICompileCheck: BUILD OK` in the log and that
  `build/StandaloneWindows64/SnackTowerDefense.exe` exists. A batch compile
  alone is enough for small, low-impact edits such as docs or comments.
- **Play mode and networking cannot be tested by an agent** — the editor pauses
  when unfocused. Ask the user to play-test and report back.

## Conventions

- Materials: `TDVisuals.Mat(color, metallic, smooth)` (opaque, cached),
  `TDVisuals.TransparentMat(color, alpha)` (alpha-blended), `TexturedMat` (tiled).
- Models load by path `Snack/<folder>/<name>` and **fall back to procedural art**
  when missing: towers `Snack/Towers/<TowerName>`, mobs `Snack/Mobs/<MobDef.id>`,
  projectiles `Snack/Projectiles/<TowerName>`.
- Models must be authored ~1 unit tall, sitting on **z = 0**, centred in X/Y.
- `TDGameManager` is a **partial class** spread across `TDGameManager.cs` plus
  `Multiplayer`, `TowerViewer`, `MobViewer`, `Settings`, `Controls`, `Playground`
  and `DamageTest`; share state freely.
- Adding a **model** to an existing mob needs no code: drop `<id>.glb` into
  `Assets/Resources/Snack/Mobs/` where `<id>` matches `MobDef.id`, and mirror it
  into `Assets/Models/Mobs/`. Adding a **new mob or wave** does need code:
  a `MobCatalog.All` entry **and** a `TDBalance.Waves` row, with
  `TDBalance.TotalWaves` kept equal to `Waves.Length`.

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
   The Blender app must be running with the MCP add-on server started, or the
   Blender tools fail with "Could not connect to Blender".
7. New mob GLBs must import with **Legacy** animation (`importSettings.animationMethod`
   = `GLTFast.AnimationMethod.Legacy`); `MobWalkCheck.VerifyLateWaveArt` sets this.
   Unity writes trailing spaces into generated `.glb.meta` files, which makes
   `git diff --check` fail — strip them before committing.
8. `TDBalance.HealthMult` pins its slow-growth taper to the original 45-wave run
   (`Clamp01((i - 9) / 34f)`), so extending the wave count does **not** rebase HP on
   earlier waves. Add new late waves at ~5%/wave instead of changing that constant.
9. Multiplayer named messages dispatch only if listed in
   `NetworkSession.matchNames` — a new channel (e.g. `td.audio`) that is registered
   but missing there is silently never delivered. Cosmetic channels
   (`FxSync`, `BoardAudioSync`) stay separate from `BoardSnapshot`; a **snapshot
   wire-format change needs `NetConfig.GameVersion` bumped** so join still gates.
10. `ProjectSettings/ProjectSettings.asset` normally carries a local
    `bundleVersion` stamp from the last build. Leave it alone; do not commit it.

## Workflow

1. Read the docs above, then the files you intend to change.
2. Make the change; keep it consistent with surrounding style.
3. Verify by compiling in batch mode (and rendering, if visual). For generated art,
   follow `docs/MobModelling.md`: GenAI concept first, **get approval**, then model.
4. Commit and push to `main`; **then always kick off a Windows x64 build for
   moderate or major changes** (a player's version stamp is `HEAD`'s sha, so
   commit *before* building). Ask the user to play-test behaviour.
5. **Only build WebGL when explicitly asked** (use the `webgl-build` skill); commit
   does not imply a WebGL publish. For a matched multiplayer test, build Windows too
   — both players must run the same `0.5.0+<sha>` version.
