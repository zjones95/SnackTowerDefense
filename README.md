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
  scales `1.13^(wave-1)` per wave and speed `+2%` per wave.
- **Economy**
  | | |
  |---|---|
  | Starting money | `$100` |
  | Build a tower | `$25` (flat, never rises) |
  | Merge two towers | `$10` |
  | Kill reward | `$3` |
  | Round bonus | `15 + 5 x wave` |
  | Starting lives | `20` |
- **Difficulty** — chosen before a single-player run, or by the host in a
  multiplayer lobby: **Easy** (-25% mob health), **Normal**, **Hard** (+25%),
  **Insane** (+50%).
- **Building** — you can only build **random tier-1** towers.
- **Merging** — select a tower, press **Merge**, then click another tower of the
  **same tier** (any type). You pay `$10` and get a **tier + 1** tower of a random
  type. Tiers go **1 → 3**, designed to be extended.
- Mobs that reach the end drain lives; reaching 0 ends the run.

## Towers

7 types, 3 tiers each. A merge picks the resulting type at random.

| Type | Name | Attack |
|---|---|---|
| SingleShot | Popcorn Bucket | Fires popping kernels — fast single-target DPS |
| Splash | Soda Cup | Lobs a liquid blob that bursts for area damage |
| Slow | Gumball Machine | Volleys 2/4/6 gumballs that slow on impact |
| Sniper | Sour Straw | Long-range neon bolt, big single hits |
| Chain | Sour Belt | Bolt arcs to 2/3/4 more enemies (25% falloff per hop) |
| Pierce | Skewer | Throws a slow metal rod that skewers 3/4/5 enemies |
| Poison | Spicy Chips | Fires a tortilla chip that applies damage over time |

Every tower fires twice as often for half the damage vs. the original numbers,
so DPS is similar but the board is far busier.

Stats live in `Assets/Scripts/TowerCatalog.cs`.

## Mobs

**35 waves of fruit & vegetables** — one type per wave (never mixed), with a
standalone **boss every 5th wave**: Watermelon, Pumpkin, Pineapple, Durian,
Coconut, Dragonfruit and the **Granola Mom** finale.

Archetypes are `Basic` · `Fast` · `Tank` · `Swarm` · `Boss`, and bosses carry
traits (regeneration, armour, slow-immunity, enrage, dashes).

Defined in `Assets/Scripts/MobCatalog.cs`; models live in
`Assets/Resources/Snack/Mobs/`. The full wave-by-wave table is in
[`docs/MobRoster.md`](docs/MobRoster.md).

## Controls

| Input | Action |
|---|---|
| **Left-click** | Build a tower (`$25`) on an empty play-mat tile, or select a tower |
| **Merge button** | Then click another same-tier tower to combine (`$10`) |
| **Right-click** | Deselect / cancel a merge |
| **W / A / S / D** (or arrows) | Pan the camera |
| **Middle-mouse drag** | Orbit / rotate |
| **Scroll wheel** | Zoom |
| **1–8** / **0** or **H** | In a match: jump to another player's board / back to yours |
| **M** | Mute / unmute the music |
| **Esc** | Back to the main menu |

## Music & sound

Everything is synthesised at runtime — there are no audio assets. Alongside the
per-tower shot sounds there's a looping cinematic track (driving string ostinato,
sub-bass drone, swelling pad, taiko hits and a riser, with reverb), generated
into an exact-period loop so it repeats seamlessly. Press **`M`** to mute/unmute.

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
- The **skip-wave key was removed**; waves will advance when the last player finishes.
- Esc/Back leaves the lobby or match and returns to the menu.

**Phase 2 (done): per-player boards + shared waves.**

- One board per player, laid out side by side. Other boards show a nameplate with
  the player's name, lives and status (their current wave / `cleared` / `out`).
- The **host owns the wave clock**: every board gets a 10s prep, then the wave goes
  live; the next wave begins once the **last** non-eliminated board clears.
- Money and lives are **per-player**. At 0 lives you're **out** — your board stops
  and the match continues; victory is shared when the survivors clear all 35
  waves, and defeat when every board is out.
- The skip-wave key was removed.

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
  tier glows, and projectiles, smoothly interpolated.
- The match ends with a **scoreboard** (wave, lives, money per player); the host
  can start another match from the lobby.

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
    Towers/*.glb          7 tower models
    Mobs/*.glb            34 fruit & veg models (Granola Mom is procedural)
    Projectiles/*.glb     5 projectile models
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

## Models

Tower and mob models were authored in Blender as actual food objects and exported
as glTF. Import is handled by [glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@latest).
Because the source files sit at different Blender origins, `SnackModels.CenterOn`
re-centers each model on its pedestal at spawn time.
