# Snack Tower Defense

A small 3D tower-defense game built in **Unity 6 (6000.6.0f1)**. A kid's bedroom
gets overrun by snack-shaped monsters, and you stop them by building snack towers
on the play-mat floor.

Everything is generated procedurally at runtime (map, room, textures, UI), and the
tower/mob characters are modelled in Blender and imported as glTF (`.glb`).

---

## Gameplay

- **Grid map** — a 9x10 ASCII layout (`Layout` in `TDGameManager`). Mobs follow a
  fixed ordered waypoint `Route` from the start tile to the end tile.
- **35 waves**, one mob type per wave, with a **boss every 5th wave**. Health
  scales `1.13^(wave-1)` per wave (plus a flat `×1.25` global health scale) and
  speed `+2%` per wave.
- **Economy**
  | | |
  |---|---|
  | Starting money | `$100` |
  | Build a tower | `$25` (flat, never rises) |
  | Merge two towers | `$10` (tiers 1-5, random result) |
  | Fuse two T6 towers | `$200` (random Tier 7) |
  | Ascend T4 → T5 | `$150` (single tower, no second consumed) |
  | Ascend T5 → T6 | `$300` (single tower, no second consumed) |
  | Kill reward | `$1` |
  | Round bonus | `$50` (flat) |
  | Starting lives | `10` |
- **Difficulty** — chosen before a single-player run, or by the host in a
  multiplayer lobby: **Easy** (-25% mob health), **Normal**, **Hard** (+25%),
  **Insane** (+50%).
- **Building** — press **B** (or the **Build (B)** HUD button) to enter build
  mode. A **"?" ghost** follows the mouse over the board and turns **green** when
  the tile is free and the `$25` is affordable, **red** otherwise. It uses a real
  3D question-mark model (`Snack/Ghost/QuestionMark`) when one is present, and
  otherwise the procedural fallback; both are tinted with opaque materials.
  Left-click to place a **random tier-1** tower; stay in the mode for repeated
  placement, and cancel with right-click or **Esc**.
- **Gold tower** — press **G** (or the **Gold (G) n/4** HUD button) to enter
  Gold-placement mode: the same green/red "?" ghost, but the placed tower is
  always **Gold Coin** (`$25`). A board may hold at most **4**; the button shows
  the count and is disabled at the cap, and placement past 4 is refused with a
  message. Gold deals no damage — it pays **`$1/$2/$3/$5` per confirmed hit by
  tier**; rate also improves (`5.0 → 4.0 → 3.2 → 2.0s`). Cash-upgrade T1→2 **$50** /
  T2→3 **$100** / T3→4 **$200** with **U**.
- **Selection** — left-click a tower to select it: its tile is outlined in
  **yellow** (the same frame build mode uses for hover). Left-click it again
  (or right-click / click empty ground) to deselect. Selecting a tower and
  entering build mode are mutually exclusive.
- **Merging** — select a tower, press **E** (or the **Merge (E)** button), then
  click another tower of the **same tier** (any type). You pay `$10` and get a
  **tier + 1** tower of a random type. Merging works at **every tier up to T5**
  (`T4 + T4 → T5`, `T5 + T5 → T6`), and **T6 + T6 → a random Tier 7** is the `$200`
  fusion (Tier 7 is terminal).
- **Ascending** — select a **tier 4 or 5** tower, press **U** (or the
  **Ascend (U)** button). You pay cash (`$150` for T4 → T5, `$300` for T5 → T6)
  and the tower is rebuilt **in place at tier + 1, same type**, still selected.
  **No second tower is consumed.** Tiers 5 and 6 each add a unique modifier; tier
  4 is a stats-only step. See [`docs/TierPlan.md`](docs/TierPlan.md).
- **Re-rolling** — select a tower of **tier 2+**, press **R** (or the
  **Re-roll (R)** button), then click a tower **exactly one tier below** it. The
  lower tower is consumed and the selected tower becomes a **different random
  type** at the **same tier, cell and selection**. There is **no money cost** —
  the spent tower is the price. Max-tier towers can re-roll too.
- Mobs that reach the end drain lives; reaching 0 ends the run.

## Board themes

The gameplay grid (`Layout` / `Route` in `TDGameManager`) never changes — a **theme**
only reskins the play-mat tiles and the surrounding room, so balance and routing are
identical across boards. Pick one with `TDGameManager.ActiveTheme`:

| Theme | Path | Tower plots | Surroundings |
|---|---|---|---|
| `Bedroom` (default) | Toy train track | Coloured play-mat squares | Kid's room: bed, shelf, chest, lamp |
| `ArcticOutpost` | Packed snowmobile track | Frosted ice tiles | Ice cliff rim, crate, sled, antenna, pines |
| `VolcanicCaldera` | Glowing lava channel | Dark basalt slabs | Ash plain, crater rim, obsidian spires, lava pools |
| `SpaceStation` | Lit mag-rail lane | Gunmetal deck plates | Hull walls with glowing portholes, hazard trim, crates, dish, robot arm |

`TDBoardBuilder.BuildTiles(...)` and `TDRoom.Build(...)` take the theme; both are also
used by remote boards and the editor previews, so every board in a match looks the same.

**Picking a board** — the **single-player difficulty screen** has a **map picker** beside
the difficulty buttons: the map name sits above a box holding a slowly rotating preview of
the board, with **`<` / `>`** arrows (or the **left/right arrow keys**) to cycle. The
choice is saved (`PlayerPrefs` key `td.theme`) and re-applied at boot. Multiplayer does not
offer the picker yet.

## Towers

8 types (7 random-build + the economy-only Gold Coin). The 7 random types have
**6 tiers each** — tiers 1-4 by merging, tiers 5-6 by cash ascension — while Gold
has 4 (it is economy-only). A merge picks the resulting type at random (never
Gold).

| Type | Name | Attack |
|---|---|---|
| SingleShot | Popcorn Bucket | Fires popping kernels — fast single-target DPS |
| Splash | Soda Cup | Lobs a liquid blob that bursts for area damage |
| Slow | Gumball Machine | Volleys 2/4/6 gumballs that slow on impact |
| Sniper | Sour Straw | Long-range neon bolt, big single hits |
| Chain | Sour Belt | Bolt arcs to 2/3/4 more enemies (25% falloff per hop) |
| Pierce | Skewer | Throws a slow metal rod that skewers 3/4/5 enemies |
| Poison | Spicy Chips | Fires a tortilla chip that applies damage over time |
| Gold | Gold Coin | Economy: no damage, pays $1/$2/$3/$5 per hit by tier (max 4 per board) |

Every tower fires twice as often for half the damage vs. the original numbers,
so DPS is similar but the board is far busier. A further balance pass cut all
tower damage by **20%** (exact multiplier **0.80**) across every tier.

Stats live in `Assets/Scripts/TowerCatalog.cs`.

- **Targeting** — each tower can pick its target mode from the selection panel:
  Default (furthest along the path), Nearest, Farthest, Random, Highest health,
  Lowest health.
- **Tier 5/6 modifiers** are implemented for every type (crit/Deadeye, Twin Lash +
  stun, Sticky Sour, Candy Shell, Sticky Tar, burn stacks + death explosion,
  Ricochet Pop, Kettle Burst, Fizz Ricochet, Sticky Soda, Wide Skewer, Boomerang),
  and **T6 keeps its T5 trait**. See [`docs/TierPlan.md`](docs/TierPlan.md).
- **Status icons** — burned mobs show a flame with a stack count, slowed mobs an ice
  cube, and tarred mobs a syrup icon, on both the floating bars and the boss bar.
- **Metrics** — each tower reports **Damage done** in the selection panel, and the HUD
  tracks total **Gold Generated**.

### Tier 7 fusion towers

Fuse **two T6 towers** with the normal **Merge (E)** key for **`$200`** → a random one
of four unique **Tier 7** towers (T7 is terminal — no further merge/ascend/re-roll):

| Type | Name | Attack |
|---|---|---|
| FondueFountain | Fondue Fountain | Heavy molten-chocolate **beam** that stacks a **"Dipped" damage-taken debuff** on the target |
| IceCreamTruck | Ice Cream Truck | Lobs ice-cream cones that **splash**, with a 20% chance to **stun** every mob caught |
| BobaBlaster | Boba Blaster | Single-target DPS that **ramps its fire rate** while it holds one target |
| PizzaOven | Pizza Oven | Landing hit + a **5 s translucent damaging zone** |

Their Blender models and the model authoring script live in `tools/blender/`; see
[`docs/TierPlan.md`](docs/TierPlan.md) and issue #12.

## Mobs

**35 waves of fruit & vegetables** — one type per wave (never mixed), with a
standalone **boss every 5th wave**: Watermelon, Pumpkin, Pineapple, Durian,
Coconut, Dragonfruit and the **Granola Mom** finale.

Archetypes are `Basic` · `Fast` · `Tank` · `Swarm` · `Boss`, and bosses carry
traits (regeneration, armour, slow-resistance, enrage, dashes).

Defined in `Assets/Scripts/MobCatalog.cs`; models live in
`Assets/Resources/Snack/Mobs/`. The full wave-by-wave table is in
[`docs/MobRoster.md`](docs/MobRoster.md).

## Controls

| Input | Action |
|---|---|
| **B** / **Build (B)** button | Enter/leave build mode; the ghost shows where a random tower would go |
| **G** / **Gold (G) n/4** button | Enter/leave Gold mode; the ghost places a Gold Coin (`$25`, max 4 per board) |
| **E** / **Merge (E)** button | Pick another tower of the **same tier** to combine into tier + 1 (`$10`, random type) — works at any tier up to T5; **T6 + T6** fuses to a random Tier 7 for `$200` |
| **U** / **Ascend (U)** button | With a **tier 4/5** tower selected: rebuild it in place at tier + 1, same type, for `$150` / `$300` — no second tower consumed |
| **R** / **Re-roll (R)** button | With a tier-2+ tower selected: pick a tower **exactly one tier below** to consume and re-type the selected tower (no money) |
| **Left-click** | In a build mode: place a tower (`$25`) on a green tile. Otherwise: select / deselect a tower (selected tile outlined in yellow) |
| **Right-click** | Cancel build / Gold / merge / re-roll mode, or deselect |
| **W / A / S / D** (or arrows) | Pan the camera |
| **Middle-mouse drag** | Orbit / rotate |
| **Scroll wheel** | Zoom |
| **1–8** / **0** or **H** | In a match: jump to another player's board / back to yours |
| **M** | Mute / unmute the music |
| **Esc** | Cancel the active build/Gold/merge/re-roll mode; otherwise open the pause menu (Settings / Quit to Main Menu / Quit Game) |

## Menus & settings

The main menu floats over a slowly orbiting view of a real game board, so you can
see what you're about to defend. Menu buttons are drawn on a procedural
construction-paper texture.

- **Settings** — reachable from the main menu and from the in-game pause menu.
  It has **Music** and **SFX** volume sliders (shown as a percentage), both
  persisted between sessions (`PlayerPrefs`), plus a Back button (Esc closes it).
- **Pause menu** — press **Esc** in a run to freeze the game and choose
  **Settings**, **Quit to Main Menu** or **Quit Game**. Quitting a multiplayer
  match leaves the session cleanly.

## Music & sound

Everything is synthesised at runtime — there are no audio assets. Alongside the
per-tower shot sounds there's a looping cinematic track (driving string ostinato,
sub-bass drone, swelling pad, taiko hits and a riser, with reverb), generated
into an exact-period loop so it repeats seamlessly. Press **`M`** to mute/unmute,
or set the level with the **Music** / **SFX** sliders in Settings.

Generation is pure DSP in `TDSynth`; `TDMusicCheck.Verify` prints its length, peak
and RMS for a quick sanity check.

## Multiplayer (in progress)

The main menu now splits into **Single Player** and **Multiplayer**.

**Phase 1 (done): menu + connect + lobby.**

- **Host Game** — creates a lobby. With Unity Gaming Services linked it produces a
  6-character **Relay join code**; when UGS isn't configured it silently falls back
  to a **LAN address** (`ip:port`) so it still works on one machine or a local network.
- **Join Game** — accepts either a join code or an `ip:port` address.
- **Lobby** — lists up to 8 players (host included), shows the code/address with a
  **Copy** button, and only the host can press **Start**. The host may start at any
  time, with fewer than 8 players.
- The **skip-wave key was removed**; waves are now **independent per board** (see Phase 2).
- Esc/Back leaves the lobby; in a match, Esc opens the pause menu, where **Quit to
  Main Menu** leaves the session.

**Phase 2 (done): per-player boards + independent waves.**

- One board per player, laid out side by side. Other boards show a nameplate with
  the player's name, lives and status (their current wave / `cleared` / `out`).
- Waves are **independent per board** (#7): each board gets a 10s prep on its
  opening wave, then clearing your wave starts your next one immediately — you never
  wait for another player.
- Money and lives are **per-player**. At 0 lives you're **out** — your board stops
  and the match continues; the match is won the moment **any** board clears all 35
  waves (shared victory), and defeat is called when every board is out.
- A **collapsible scoreboard** (top-right) shows each player's wave, lives, gold
  generated and tower value; a **relay chat** (**T** to open) lets the lobby talk.
- A **version gate** refuses mismatched builds (`<scheme>+<git sha>`) before joining.

**Phase 3 (done): live synchronised boards.**

- Every peer streams a compact snapshot of its board (mobs, towers, projectiles)
  to the host, which fans it out to all other clients, so **every board is live
  for everyone** — zoom out and watch the whole match at once.
- Snapshots go over **unreliable sequenced** delivery (each is a self-contained
  full state), so a dropped packet costs one stale frame instead of a
  retransmit stall.
- Press **1-8** to jump the camera to a board, **0** (or **H**) to return to your
  own. While spectating you are read-only — build/merge only works on your board.
- Remote boards show the other player's mobs (health bars included), towers with
  tier labels, and projectiles, smoothly interpolated.
- A **collapsible scoreboard** (top-right) tracks wave, lives, gold generated and
  tower value per player; a **relay chat** (**T**) is available during a match, and a
  **version gate** refuses mismatched builds before joining.
- The match ends with a shared **scoreboard**; the host can start another match from
  the lobby.

Built on **Netcode for GameObjects**, host-authoritative with owner-simulated boards.

**Local testing** — the project includes [ParrelSync](https://github.com/VeriorPies/ParrelSync)
(editor-only, no runtime cost). Open its window from the top-level **ParrelSync →
Clones Manager** menu, create a clone, then host in one editor and join
`127.0.0.1:7777` from the other.

## Documentation

| Doc | Contents |
|---|---|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Codebase map, conventions, how to verify changes, Blender pipeline, gotchas |
| [`docs/HANDOFF.md`](docs/HANDOFF.md) | Current state, what is verified, suggested next steps |
| [`docs/MobRoster.md`](docs/MobRoster.md) | The 35-wave fruit & vegetable roster and boss concepts |
| [`docs/TierPlan.md`](docs/TierPlan.md) | **Approved** tier 4-6 plan: cash ascension, per-tier stats, per-tower modifiers |

## Project layout

```
Assets/
  Editor/                 Editor-only tooling (never shipped)
    CICompileCheck.cs     Build entry point used by CI
    SnackPreview.cs       Renders towers + mobs
    TDArenaPreview.cs     Renders the map + room
    TDMultiBoardPreview.cs / TDSpectatePreview.cs / TDPlacementPreview.cs
    TDMusicCheck.cs       Prints music clip stats
  Resources/Snack/        Models loaded at runtime by path
    Towers/*.glb          7 tower models (Gold uses the procedural coin stack)
    Mobs/*.glb            34 fruit & veg models (Granola Mom is procedural)
    Projectiles/*.glb     5 projectile models
    Ghost/QuestionMark.glb  optional build "?" ghost (procedural fallback)
  Scenes/Boot.unity       Empty scene; the game bootstraps itself
  Scripts/
    TDGameManager*.cs     State machine, world, waves, HUD + the two viewers
    TDBalance.cs          Economy, difficulty, the 35-wave table, curves
    TowerCatalog.cs / Tower.cs / Projectile.cs / PierceProjectile.cs / SplashFX.cs
    MobCatalog.cs / Mob.cs / ChildModel.cs / SnackArt.cs
    TDMap.cs / TDBoardBuilder.cs / TDRoom.cs / TDTextures.cs
    TDVisuals.cs / SnackModels.cs / SnackVisuals.cs
    TDAudio.cs / TDSynth.cs
    TowerViewer.cs / MobViewer.cs
    Net/                  Multiplayer (NetworkSession, MatchSync, SpectateSync, …)
    RemoteBoard.cs
  Models/                 Original glTF sources (mirrored into Resources)
tools/
  publish-webgl.ps1       Build WebGL locally and publish it to the `webgl` branch
```

**No authored scene is required.** The game boots itself with
`[RuntimeInitializeOnLoadMethod]`, so `Assets/Scenes/Boot.unity` is intentionally
empty and only exists so builds have a scene to start from.

## Running locally

1. Install **Unity 6000.6.0f1** (Unity 6.6) with the platform modules you want.
2. Open the project folder in Unity Hub.
3. Press **Play**.

## Continuous integration

`.github/workflows/unity-ci.yml` builds a **StandaloneLinux64** player with
[GameCI](https://game.ci/) on every push / PR to `main`.

It needs a Unity license, supplied as repository secrets (Settings → Secrets and
variables → Actions):

| Secret | Notes |
|---|---|
| `UNITY_LICENSE` | Contents of your Unity `.ulf` license file (recommended) |
| `UNITY_EMAIL` | Optional fallback for license activation |
| `UNITY_PASSWORD` | Optional fallback for license activation |

See the [GameCI activation guide](https://game.ci/docs/github/activation) for how
to obtain these. **Without them the build step will fail.**

To reproduce the CI build locally:

```powershell
Unity -batchmode -quit -projectPath . -executeMethod CICompileCheck.Build -logFile -
```

## Web preview (shareable link)

**Play it:** <https://zjones95.github.io/SnackTowerDefense/>

The player is built **locally** — where a Unity licence already exists — and
published to **GitHub Pages** from the **`webgl`** branch, so the deploy needs
**no Unity licence in CI**:

```powershell
powershell -ExecutionPolicy Bypass -File tools\publish-webgl.ps1
```

That builds WebGL (`WebGLBuild.Build`, output in `build/WebGL/`) and force-pushes
a single-commit `webgl` branch. GitHub Pages is configured as **Deploy from a
branch → `webgl` / (root)** and republishes automatically; add `-SkipBuild` to
publish the existing `build/WebGL` without rebuilding.

- The project uses gzip + the JS decompression fallback, so the output runs on any
  static host with no special server headers.
- Browser builds are **single-player only** (Unity Netcode needs UDP sockets).
- To host elsewhere instead (itch.io, Netlify, Cloudflare Pages), upload the
  contents of `build/WebGL/` — or the shareable zip at
  `C:\Users\Desktop\SnackTowerDefense-WebGL.zip`.

> **Only build/publish the WebGL player when explicitly asked** — it's a heavy full
> build. Route it through the `webgl-build` skill (or `tools\publish-webgl.ps1`).

## Models

Tower and mob models were authored in Blender and exported as glTF. They live in
`Assets/Resources/Snack/{Towers,Mobs,Projectiles}` (runtime, loaded by path) and are
mirrored to `Assets/Models/**` (source). The **Soda Cup** (Splash tower) is a painted
straw-cannon; its authoring script is `tools/blender/soda_cup_concept.py`. Import is
handled by [glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@latest).
Because the source files sit at different Blender origins, `SnackModels.CenterOn`
re-centers each model on its pedestal at spawn time (a model whose base is named
`pedestal` anchors on that, so the tower stays on its tile while the head rotates to aim).
