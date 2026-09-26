# Architecture

Orientation for anyone (human or agent) working in this repo.

---

## Big picture

- **Unity 6.6 (`6000.6.0f1`)**, built-in render pipeline, **no authored scene**.
- The game **bootstraps itself**: `TDGameManager.Boot()` carries
  `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` and creates the manager
  GameObject. `Assets/Scenes/Boot.unity` is intentionally *empty* and exists
  only so builds have a scene to start from.
- **Everything is procedural** — map, room, textures, audio, and UI (IMGUI).
  There are no prefabs and no third-party UI assets.
- **Character art is Blender-authored glTF** (`.glb`), imported with glTFast and
  loaded from `Resources`.

## Boot & state flow

`TDGameManager.State` (`GameState`, in `TDCore.cs`):

```
MainMenu ─┬─ DifficultySelect ── StartingRun ── Playing ─┬─ GameOver
          │                                              └─ Victory
          ├─ TowerViewer
          ├─ MobViewer
          └─ MultiplayerMenu ── Lobby ── (match) ── Playing
```

`RoundState` is `Preparing` / `WaveActive`.

## Core files

| File | Role |
|---|---|
| `TDCore.cs` | `GameState` + `RoundState` enums |
| `TDGameManager.cs` | State machine, world build, waves, economy, input, HUD, lighting |
| `TDGameManager.Multiplayer.cs` | Multiplayer menu, join screen, lobby, match wiring |
| `TDGameManager.TowerViewer.cs` | Tower gallery screen |
| `TDGameManager.MobViewer.cs` | Mob gallery screen |
| `TDGameManager.Settings.cs` | Pause menu + music/SFX settings overlay, time-scale handling |
| `TDBalance.cs` | Money/lives/prep, difficulty, **35-wave table**, health & speed curves |
| `TowerCatalog.cs` | 8 tower types × 3 tiers (7 random-build + Gold) |
| `Tower.cs` | Targeting, firing, merging, muzzle, model composition |
| `Projectile.cs` | Homing projectile (also arc/hop + splash + poison + slow on hit) |
| `PierceProjectile.cs` | Straight-line travelling rod that skewers enemies |
| `SplashFX.cs` | Expanding burst + droplets for splash impacts |
| `MobCatalog.cs` | 35 mob defs: archetype, stats, colour, boss traits |
| `Mob.cs` | Movement, health, status effects, boss behaviours |
| `TDMap.cs` | ASCII layout → grid, cell centres, world↔cell, waypoints |
| `TDBoardBuilder.cs` | Builds tiles + room for a board at an arbitrary offset |
| `TDRoom.cs` | Kid's-room props (bed, shelf, chest, lamp…) |
| `TDTextures.cs` | Procedural play-mat / toy-track textures |
| `TDVisuals.cs` | Primitive + material helpers (`Mat`, `TransparentMat`, `Box`, …) |
| `TDTextures`/`TDRoom`/`TDVisuals` | Pure scene dressing; safe to ignore for gameplay work |
| `SnackModels.cs` | glTF loading, `CenterOn`, model path helpers |
| `SnackVisuals.cs` | `TowerVisual`, `MobVisual`, `BillboardLabel` |
| `SnackArt.cs` | Procedural fallback art when a `.glb` is missing |
| `ChildModel.cs` | Procedural humanoid with a walk cycle (used by Granola Mom) |
| `TDAudio.cs` / `TDSynth.cs` | Procedural SFX + the looping music track |
| `TowerViewer.cs` / `MobViewer.cs` | Off-screen turntables rendered to a RenderTexture |
| `Net/NetworkSession.cs` | NGO `NetworkManager`, host/join/leave, lobby roster |
| `Net/MatchSync.cs` | Host wave clock + per-board state; difficulty |
| `Net/SpectateSync.cs` | Board snapshot streaming (host fan-out) |
| `Net/BoardSnapshot.cs` | Compact quantised board state |
| `Net/BoardLayout.cs` | Where each player's board sits in the world |
| `RemoteBoard.cs` | Renders another player's board from snapshots |

## Systems

**Map & board** — `TDMap` parses a `string[]` layout (`s` spawn, `e` end,
`m` path, `t` buildable, `x` void) plus an explicit ordered `Route` of cells.
`TDBoardBuilder` builds the tiles and room at an offset, so the same code serves
the local board and every remote one. Tiles are centred at **y = -0.05** so
their top face is exactly **y = 0** — that is the plane towers and mobs sit on.

**Towers** — `Tower` picks the enemy **furthest along the path** within range,
then dispatches on type. Projectile towers spawn from `Muzzle()` (measured from
the model's bounds: half its height, nudged forward). Instant towers draw a
`Tracer` (a flat, camera-facing neon ribbon).

**Mobs** — one mob type per wave. `MobCatalog` derives stats from the archetype
(Basic/Fast/Tank/Swarm/Boss) and then applies per-boss traits: `regen`,
`armour` (flat damage reduction), `slowImmune`, `enrage` (speed rises as health
falls) and `dashEvery` (periodic burst).

**Waves** — `TDBalance.Waves` is the single source of truth: one mob id per
wave, 35 waves, a standalone boss every 5th. Health is
`1.13^(wave-1) × difficulty multiplier`.

**UI** — all IMGUI, drawn from `TDGameManager.OnGUI` and dispatched to the
partial-class screens. The main/difficulty menus build a real board
(`EnsureMenuWorld`) and orbit a camera over it (`UpdateMenuBackdrop`), softened by
a scrim (`TDTextures.MenuFade`); menu buttons use `TDTextures.Paper`.
Esc in a run opens the pause menu (`TDGameManager.Settings.cs`), which freezes
`Time.timeScale` and offers Settings / Quit to Main Menu / Quit Game.

**Multiplayer** — host-authoritative. Each peer simulates its **own** board and
streams a `BoardSnapshot` to the host; the host fans each board out to the other
clients over **unreliable sequenced** delivery. The host owns the wave clock
(`MatchSync`) and advances when every non-eliminated board reports `cleared`.

## Conventions

- **Materials**: `TDVisuals.Mat(color, metallic, smooth)` (cached) for opaque,
  `TDVisuals.TransparentMat(color, alpha, smooth)` for alpha-blended (it also
  fixes the Standard shader's blend state and render queue), and
  `TDVisuals.TexturedMat` for tiled textures.
- **Models** are loaded by path: `Snack/<folder>/<name>` — towers
  `Snack/Towers/<TowerName>`, mobs `Snack/Mobs/<MobDef.id>`, projectiles
  `Snack/Projectiles/<TowerName>`. Anything missing falls back to procedural art.
- **Model requirements**: authored ~1 unit tall, sitting on **z = 0**, centred
  in X/Y. `SnackModels.CenterOn` then aligns the base and centres it on the cell.
- **Partial classes**: `TDGameManager` is split across five files; state and
  helpers are shared.
- **No scene/prefab edits are ever needed** — if you find yourself opening Unity
  to wire something up, something is off.

## Verifying changes (the agent's main loop)

> ⚠️ **The Unity editor must be closed** for batch mode (it locks `Library/`).

```powershell
# compile only
Unity.exe -batchmode -projectPath <proj> -executeMethod CICompileCheck.EnsureBootScene -quit -logFile <log>
# compile + build a player
Unity.exe -batchmode -projectPath <proj> -executeMethod CICompileCheck.Build -quit -logFile <log>
```

Kill the right editor with:

```powershell
Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
  Where-Object { $_.CommandLine -match '<project>' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
```

Editor render helpers (write PNGs to `%TEMP%\opencode\`):

| Method | Renders |
|---|---|
| `SnackPreview.Render` | tower grid (3 tiers each) + a few mobs |
| `TDArenaPreview.Render` | map + room |
| `TDMultiBoardPreview.Render` | four player boards side by side |
| `TDSpectatePreview.Render` | a remote board fed a synthetic snapshot |
| `TDPlacementPreview.Render` | ground contact of towers/mobs |
| `TDMusicCheck.Verify` | prints the music clip's length/peak/RMS |

**Play mode and networking cannot be tested by an agent** — the editor pauses
when unfocused. Ask the user to play-test and report.

## Blender pipeline

Driven through the Blender MCP (`tools["blender"]`).

- Build geometry with `execute_blender_code`, export each object separately:
  `bpy.ops.export_scene.gltf(filepath=…, export_format='GLB', use_selection=True)`.
- **Axis conversion**: Blender `+Z` → Unity `+Y`; Blender `+Y` → Unity `−Z`.
- Models are mirrored to **both** `Assets/Resources/Snack/**` (runtime) and
  `Assets/Models/**` (source copies).
- For preview renders set `scene.view_settings.view_transform = 'Standard'` —
  the default AgX transform desaturates badly — and dial the key light to suit
  (the rig was tuned at ~65–95 W for Standard).

## Gotchas (learned the hard way)

1. **Bake transforms before shifting a mesh.** `bpy.ops.object.transform_apply`
   must run *before* you re-centre vertices, or the model lands offset by the
   object's origin.
2. **Don't move the play-mat tiles' Y** without moving towers/mobs too — they
   are placed on `y = 0`, which is the tile's top face.
3. **TextMesh reads from its −Z face.** Billboard labels by copying
   `Camera.main.transform.rotation`, *not* `LookRotation` toward the camera, or
   the text mirrors.
4. `SnackModels.CenterOn` anchors on a renderer named `pedestal` if one exists,
   otherwise the whole-model bounds.
5. Tower head-split: children whose names contain `pedestal`/`rim` stay fixed
   while everything else rotates to aim. The snack towers have no such nodes, so
   the **whole model rotates**.
6. **Adding a mob needs no code** — drop `<id>.glb` into
   `Assets/Resources/Snack/Mobs/` where `<id>` matches `MobDef.id`.
7. `Projectile.Bounces`/`Arc` remain from the retired Jelly Bean tower; `Arc` is
   currently unused.

## Repo & CI

- Branch `main`, remote `github.com/zjones95/SnackTowerDefense` (private).
- `.gitignore` excludes `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `build/`.
  `.gitattributes` keeps Unity YAML/binaries byte-stable (`core.autocrlf=true`).
- `.github/workflows/unity-ci.yml` builds StandaloneLinux64 via GameCI and
  **requires a Unity licence secret** (`UNITY_LICENSE`) — it fails without one.
