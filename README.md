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
- **5 waves**, health scaling `x1.55` per wave and speed `+4%` per wave.
- **Economy**
  | | |
  |---|---|
  | Starting money | `$100` |
  | Build a tower | `$25` (flat, never rises) |
  | Merge two towers | `$10` |
  | Kill reward | `$3` |
  | Round bonus | `15 + 5 x wave` |
  | Starting lives | `20` |
- **Building** — you can only build **random tier-1** towers.
- **Merging** — select a tower, press **Merge**, then click another tower of the
  **same tier** (any type). You pay `$10` and get a **tier + 1** tower of a random
  type. Tiers go **1 → 3**, designed to be extended.
- Mobs that reach the end drain lives; reaching 0 ends the run.

## Towers

8 types, 3 tiers each. A merge picks the resulting type at random.

| Type | Name | Role |
|---|---|---|
| SingleShot | Popcorn Popper | Cheap single-target DPS (8/16/30 dmg) |
| Splash | Soda Mortar | Area damage (1.6/2.1/2.7 radius) |
| Slow | Gum Snare | No damage; slows 45/55/65% for 1.2-1.6s |
| Sniper | Pretzel Sniper | Long range, big hits (25/45/80 dmg, 9-13 range) |
| Chain | Sour Static Belt | Damage jumps to 2/3/4 extra targets |
| Pierce | Skewer | Hits 3/4/5 enemies along a line |
| Bounce | Bouncy Ball | Projectile hops to 2/3/4 more targets |
| Poison | Wasabi | Small hit + 6/11/18 dmg/sec over time |

Stats live in `Assets/Scripts/TowerCatalog.cs`.

## Mobs

| Mob | Health | Speed | Leak damage |
|---|---|---|---|
| Cracker (Basic) | 30 | 1.7 | 1 |
| Gummy (Fast) | 20 | 3.1 | 1 |
| Donut (Tank) | 95 | 1.15 | 2 |
| Cake Boss (Boss) | 340 | 0.95 | 5 |

Defined in `Assets/Scripts/MobCatalog.cs`.

## Controls

| Input | Action |
|---|---|
| **Left-click** | Build a tower (`$25`) on an empty play-mat tile, or select a tower |
| **Merge button** | Then click another same-tier tower to combine (`$10`) |
| **Right-click** | Deselect / cancel a merge |
| **W / A / S / D** (or arrows) | Pan the camera |
| **Middle-mouse drag** | Orbit / rotate |
| **Scroll wheel** | Zoom |
| **Space** | Start the next wave immediately |
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

**Still to come**
- **Phase 2:** one board per player, shared wave timing (next wave once the last
  player clears), per-player economy/lives, elimination to spectator.
- **Phase 3:** cross-board spectating with live mobs/towers, camera switching, and
  an end-of-match scoreboard.

Built on **Netcode for GameObjects**, host-authoritative with owner-simulated boards.

**Local testing** — the project includes [ParrelSync](https://github.com/VeriorPies/ParrelSync)
(editor-only, no runtime cost). Open its window from the top-level **ParrelSync →
Clones Manager** menu, create a clone, then host in one editor and join
`127.0.0.1:7777` from the other.

## Project layout

```
Assets/
  Editor/
    CICompileCheck.cs     Build entry point used by CI
    SnackPreview.cs       Renders tower/mob preview PNGs (editor only)
    TDArenaPreview.cs     Renders the map + room preview PNG (editor only)
  Resources/Snack/
    Towers/*.glb          Blender-authored tower models
    Mobs/Cracker.glb      Basic mob model
  Scenes/Boot.unity       Empty scene; the game bootstraps itself
  Scripts/
    TDGameManager.cs      Game state machine, waves, economy, input, HUD, lighting
    Tower.cs              Tower behaviour + model/turret composition
    TowerCatalog.cs       Tower types and per-tier stats
    Mob.cs / MobCatalog.cs
    TDMap.cs              Layout -> grid, waypoint route, BFS helpers
    TDRoom.cs             Kid's-room environment
    TDTextures.cs         Procedural play-mat / toy-track textures
    TDVisuals.cs          Primitive + material helpers
    SnackModels.cs        glTF loading + centering
    TDAudio.cs / TDSynth.cs   Procedural audio
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
