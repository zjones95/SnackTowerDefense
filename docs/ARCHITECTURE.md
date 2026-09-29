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
| `TowerCatalog.cs` | 12 types: 7 random-build ×6 tiers, Gold ×3, and 4 Tier 7 fusion types; tier stats; merge/ascend/fuse costs live in `TDBalance` |
| `Tower.cs` | Targeting, firing, merging, muzzle, model composition |
| `Projectile.cs` | Homing projectile (also arc/hop + splash + poison + slow on hit) |
| `PierceProjectile.cs` | Straight-line travelling rod that skewers enemies |
| `SplashFX.cs` | Expanding burst + droplets for splash impacts |
| `FxEvents.cs` | Queues/replays cosmetic FX (splash, tracer, beam) for remote boards |
| `MobCatalog.cs` | 35 mob defs: archetype, stats, colour, boss traits |
| `Mob.cs` | Movement, health, status effects, boss behaviours |
| `TDMap.cs` | ASCII layout → grid, cell centres, world↔cell, waypoints |
| `TDBoardBuilder.cs` | Builds tiles + room for a board at an arbitrary offset |
| `TDRoom.cs` | Kid's-room props (bed, shelf, chest, lamp…) |
| `TDTextures.cs` | Procedural play-mat / toy-track textures |
| `TDVisuals.cs` | Primitive + material helpers (`Mat`, `TransparentMat`, `Box`, …) |
| `TDTextures`/`TDRoom`/`TDVisuals` | Pure scene dressing; safe to ignore for gameplay work |
| `SnackModels.cs` | glTF loading, `CenterOn`, model path helpers |
| `SnackVisuals.cs` | `TowerVisual`, `MobVisual`, `MobStatusIcons` (burn/slow/tar icons), tier scale/colours |
| `SnackArt.cs` | Procedural fallback art when a `.glb` is missing |
| `ChildModel.cs` | Procedural humanoid with a walk cycle (used by Granola Mom) |
| `TDAudio.cs` / `TDSynth.cs` | Procedural SFX + the looping music track |
| `TowerViewer.cs` / `MobViewer.cs` | Off-screen turntables rendered to a RenderTexture |
| `Net/NetworkSession.cs` | NGO `NetworkManager`, host/join/leave, lobby roster |
| `Net/MatchSync.cs` | Per-board state relay + independent waves; difficulty |
| `Net/SpectateSync.cs` | Board snapshot streaming (host fan-out, ~20 Hz) |
| `Net/FxSync.cs` | Best-effort cosmetic FX channel (`td.fx` / `td.fxall`) |
| `Net/BoardSnapshot.cs` | Compact quantised board state |
| `Net/BoardLayout.cs` | Where each player's board sits in the world |
| `Net/ChatSync.cs` | Relay chat + typing flag |
| `RemoteBoard.cs` | Renders another player's board from snapshots |
| `PizzaZone.cs` | Pizza Oven's persistent ground hazard |
| `TDGameManager.DamageTest.cs` | Solo Damage Test scenario (invincible dummy) |
| `Assets/Editor/WebGLBuild.cs` | WebGL build entry point (`-executeMethod WebGLBuild.Build`) |
| `Assets/Editor/BuildVersion.cs` | Stamps `<scheme>+<git sha>` before builds (version gate) |
| `Assets/Plugins/WebGL/WebGLClipboard.jslib` | Browser clipboard bridge for the multiplayer **Copy** button |
| `Assets/Resources/Snack/Fx.shader` | Unlit alpha-blend shader for runtime FX/icons (build-safe) |

## Systems

**Map & board** — `TDMap` parses a `string[]` layout (`s` spawn, `e` end,
`m` path, `t` buildable, `x` void) plus an explicit ordered `Route` of cells.
`TDBoardBuilder` builds the tiles and room at an offset, so the same code serves
the local board and every remote one. Tiles are centred at **y = -0.05** so
their top face is exactly **y = 0** — that is the plane towers and mobs sit on.

**Towers** — `Tower` picks a target by its per-tower **targeting mode**
(`TowerTargeting`: Default = furthest along the path, Nearest, Farthest, Random,
Highest health, Lowest health; ties fall back to furthest-along), then dispatches on
type. Projectile towers spawn from `Muzzle()` (half the model's height, nudged
forward); instant towers draw a `Tracer` (flat camera-facing neon ribbon). Each tower
tracks `DamageDone` (shown in the selection panel). **Gold** is an economy tower
(`G`, cap 4, `$1/$2/$3/$5` per hit) that never merges and is excluded from the random pool.

Tiers: 2:1 **merging** (`E`) works at any source tier up to `TowerCatalog.MaxMergeTier`
(5) for `$10`; at **T6 the same action fuses two T6s into a random Tier 7 for `$200`**
(T7 is terminal). Tiers **4 → 5 → 6** also advance by **cash ascension** (`U`,
`TDBalance.AscendCost`) — a single tower rebuilt in place at tier + 1, same type, no
second tower consumed. `MaxTier` is 7. **All T5/T6 modifier behaviours are implemented**
(crit/Deadeye, Twin Lash + stun, Sticky Sour, Candy Shell, Sticky Tar, burn stacks +
Ghost Pepper detonation, Ricochet Pop, Kettle Burst, Fizz Ricochet, Sticky Soda, Wide
Skewer, Boomerang) and **T6 keeps its T5 trait**; the four T7 fusion types add their own
mechanics (Fondue beam + Dipped, Ice Cream splash-stun, Boba ramp, Pizza zone). See
[`docs/TierPlan.md`](TierPlan.md) and issue #12.

**Mobs** — one mob type per wave. `MobCatalog` derives stats from the archetype
(Basic/Fast/Tank/Swarm/Boss) and then applies per-boss traits: `regen`,
`armour` (flat damage reduction), `slowResist` (0.5 on Coconut/Granola Mom), `enrage`
(speed rises as health falls) and `dashEvery` (periodic burst).

**Waves** — `TDBalance.Waves` is the single source of truth: one mob id per wave,
35 waves, a standalone boss every 5th. Mob health is `TDBalance.HealthMult(wave)`
(per-wave growth tapers from 13% → 10% by the last wave, ~41× at wave 35) times the
difficulty multiplier and `TDBalance.MobHealthScale` (1.25). In single player the next
wave starts immediately; in multiplayer each board runs its **own** wave independently
(#7) — `MatchSync` relays per-board state rather than sharing a clock.

**UI** — all IMGUI, drawn from `TDGameManager.OnGUI` and dispatched to the
partial-class screens. The main/difficulty menus build a real board
(`EnsureMenuWorld`) and orbit a camera over it (`UpdateMenuBackdrop`), softened by
a scrim (`TDTextures.MenuFade`); menu buttons use `TDTextures.Paper`.
Esc in a run opens the pause menu (`TDGameManager.Settings.cs`), which freezes
`Time.timeScale` and offers Settings / Quit to Main Menu / Quit Game.

**Multiplayer** — host-authoritative state, per-board simulation. Each peer simulates
its **own** board and streams a `BoardSnapshot` to the host; the host fans each board
out to the others over **unreliable sequenced** delivery (~20 Hz). Waves are
**independent per board** (#7): clearing your wave starts your next immediately, and
`MatchSync` is a per-board state relay rather than a shared clock. Connections go
through **Unity Relay (UGS)** — `NetworkSession` picks the allocation endpoint by
connection type (`dtls` desktop, `wss` WebGL) and uses anonymous auth, and the join
handshake refuses mismatched `NetConfig.GameVersion`s (#30). Snapshots carry each mob's
**status** (`Status`/`Stacks`) so remote boards show the burn/slow/tar icons too;
changing the snapshot format means bumping `NetConfig.GameVersion`. In-match overlays
(collapsible scoreboard, relay chat) are drawn from `TDGameManager.Multiplayer.cs`
backed by `Net/ChatSync.cs` (chat) and `Net/FxSync.cs` (cosmetic FX).

## Network protocol (wire format)

All multiplayer traffic rides Netcode for GameObjects **named messages**. Board
positions are **board-local** and quantised to centimetres; each receiver adds its own
`BoardOffset`. Anything that is a self-contained full state or a transient cosmetic
uses **unreliable-sequenced**, so a dropped packet costs one stale frame or one missed
sparkle — never a stall or a desync.

| Message | Direction | Payload |
|---|---|---|
| `td.snap` | owner → host | `BoardSnapshot` (mobs, towers, projectiles), ~20 Hz |
| `td.relay` | host → clients | boardId + `BoardSnapshot` |
| `td.fx` | owner → host | FX batch (splash bursts, tracer bolts) |
| `td.fxall` | host → clients | boardId + FX batch |
| `td.state` | client → host | per-board state (name/wave/lives/money/gold/tower value/cleared/eliminated) |
| `td.boards` | host → all | full per-board state roster |
| `td.chat` | client → host | chat line |
| `td.chatall` | host → all | sender name + chat line |
| `td.hello` / `td.lobby` / `td.start` | session | handshake / lobby roster / match start |

**`BoardSnapshot` layout** (`Net/BoardSnapshot.cs`):
- `MobSnap { ushort Id; byte Type; short X, Z; byte Hp, Status, Stacks }` — `Status` bits: 0 slowed/stunned, 1 stunned, 2 tar.
- `TowerSnap { byte Type, Tier; short Cx, Cy; byte Yaw }` — `Yaw` is 0-255 mapped to 0-360°.
- `ProjSnap { int Id; byte Type; short X, Y, Z }` — `Type` is the firing `TowerType`, so the remote picks the right model.
- `short` fields are centimetres (`Enc`/`Dec`), clamped to ±320 m.

**Rules**
- **Any change to `BoardSnapshot` (or the lobby/match state) bumps `NetConfig.GameVersion`.**
- **Cosmetic FX lives on a separate message** (`FxSync`) with its own format — it can change or be dropped/throttled without touching the sim version or risking a desync.
- The join handshake compares `NetConfig.FullVersion` (`<scheme>+<git sha>`); peers on different builds/commits are refused at connection approval.

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
# WebGL (the shareable web build)
Unity.exe -batchmode -projectPath <proj> -executeMethod WebGLBuild.Build -quit -logFile <log>
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
| `SnackPreview.Render` | tower grid (6 tiers each) + a few mobs |
| `TDArenaPreview.Render` | map + room |
| `TDMultiBoardPreview.Render` | four player boards side by side |
| `TDSpectatePreview.Render` | a remote board fed a synthetic snapshot |
| `TDPlacementPreview.Render` | ground contact of towers/mobs |
| `TDMusicCheck.Verify` | prints the music clip's length/peak/RMS |

> **Player builds:** commit **before** building — the player's version stamp is
> `<scheme>+<git sha>` of `HEAD`, so building uncommitted work bakes the previous commit.
> **Only build WebGL when the user explicitly asks** (see the `webgl-build` skill);
> routine verification is a batch compile or a Windows player build.

**Play mode and networking cannot be tested by an agent** — the editor pauses
when unfocused. Ask the user to play-test and report.

## Blender pipeline

Driven through the Blender MCP (`tools["blender"]`).

- Build geometry with `execute_blender_code`, export each object separately:
  `bpy.ops.export_scene.gltf(filepath=…, export_format='GLB', use_selection=True)`.
- **Axis conversion**: Blender `+Z` → Unity `+Y`; Blender `+Y` → Unity `−Z`.
- Models are mirrored to **both** `Assets/Resources/Snack/**` (runtime) and
  `Assets/Models/**` (source copies). Authoring scripts live in `tools/blender/`
  (e.g. `soda_cup_concept.py`, which builds + exports the Soda Cup as `Soda.glb`).
- Name a model's base object **`pedestal`** when it has a fixed stand (the Soda Cup
  cannon does): `CenterOn` anchors on it, and the head-split keeps it from rotating.
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
   while everything else rotates to aim. Most snack towers have no such nodes, so
   the **whole model rotates**; the Soda Cup names its base `pedestal`, so the cup +
   straw cannon aim while the stand stays put.
6. **Adding a mob needs no code** — drop `<id>.glb` into
   `Assets/Resources/Snack/Mobs/` where `<id>` matches `MobDef.id`.
7. `Projectile.Bounces`/`Arc` remain from the retired Jelly Bean tower; `Arc` is
   currently unused.
8. **Transparent Standard materials are stripped from player builds** (they render
   opaque). For anything that must be translucent/additive at runtime use
   `Assets/Resources/Snack/Fx.shader` (unlit alpha-blend), loaded via
   `Resources.Load<Shader>("Snack/Fx")` — `SplashFX` and the status icons do this.

## Repo & CI

- Branch `main`, remote `github.com/zjones95/SnackTowerDefense` (public). A second,
  throwaway branch **`webgl`** holds the prebuilt WebGL player that GitHub Pages
  serves.
- `.gitignore` excludes `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `build/`.
  `.gitattributes` keeps Unity YAML/binaries byte-stable (`core.autocrlf=true`).
- `.github/workflows/unity-ci.yml` builds StandaloneLinux64 via GameCI. It
  **currently fails on Unity activation (HTTP 401)** — a credentials/2FA problem on
  the Unity account, see the *CI / licence* section in `docs/HANDOFF.md`.
- **The WebGL deploy is licence-free**: `tools/publish-webgl.ps1` builds locally and
  force-pushes a single-commit `webgl` branch, which **GitHub Pages** serves directly
  (Deploy from a branch). No Unity, no `UNITY_*` secrets, and no Actions in the loop.
