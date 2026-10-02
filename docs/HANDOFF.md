# Handoff — Snack Tower Defense

Written so a **fresh session** can pick this project up fast. Read this first, then
`docs/ARCHITECTURE.md`, then `docs/TierPlan.md` and `docs/MobRoster.md`.

> **The GitHub issue tracker is the live to-do list** —
> `github.com/zjones95/SnackTowerDefense/issues` (**#1–#32**; only **#8** and **#17**
> remain open, both design). This doc is the *state of the world*; the issues are the *work*.

---

## The project

- **Unity 6.6 (`6000.6.0f1`)**, built-in pipeline, **no authored scene** — the game
  bootstraps itself at runtime.
- **Path:** `C:\Users\Desktop\SnackTowerDefense`
- **Repo:** `github.com/zjones95/SnackTowerDefense` (private), branch `main`.
- A kid's bedroom is attacked by **fruit & vegetable** mobs; you defend it with
  **snack towers** (popcorn bucket, soda cup, gumball machine…).

## Where it stands

| Area | State |
|---|---|
| Single player | **35 waves**, one mob type per wave, a standalone boss every 5th (waves 5–35) |
| Towers | 12 random-build types × **6 tiers** + **Gold** (3 tiers) + **4 Tier 7 fusion types** (#12). The five added types are **Hot Sauce** (weak shots + damage aura), **Coffee Mug** (weak shots + attack-speed aura), **Pop-Tart Toaster** (3 small shots then a big splash), **Cookie Crumbler** (five-bit crumb cone) and **Sour Fizz** (flat-armour debuff). **T6+T6 → a random T7 for `$200`** on the merge key; T7 is terminal. 2:1 merge at any tier up to T5 (`$10`; `T4+T4 → T5`, `T5+T5 → T6`); cash **ascension** **T4→T5 `$150` / T5→T6 `$300`** with **U**. **T5/T6 modifiers implemented** and **T6 is cumulative with its T5 trait**. |
| Models | Every tower + the 4 T7 fusions have Blender `.glb` models; the **Soda Cup (Splash)** was re-authored as a painted straw-cannon. The five newest towers came from the approved v3 concepts in `tools/blender/new_snack_towers_v3.py`. Sources in `Assets/Models/**`, runtime in `Resources/Snack/**`; authoring scripts in `tools/blender/`. |
| Targeting | Per-tower mode in the selection panel: Default / Nearest / Farthest / Random / Highest health / Lowest health (`TowerTargeting`) |
| Mobs | 35 fruits/veg; **34 modelled**, Granola Mom is the procedural humanoid |
| Status FX | Floating bars **and** the boss bar show **burn (flame + `xN`)** and **slow (ice cube)** and **tar** icons (`MobStatusIcons`) |
| Metrics | Per-tower **Damage done** (selection panel; DoT + detonations are attributed) and per-Gold-tower **Gold made**; collective **Gold Generated** (HUD top-right) |
| Gold tower | Hotkey **G**, cap **4**, pays **$1/$2/$3/$5** per hit, cash-upgrade T1→2 **$50** / T2→3 **$100** / T3→4 **$200**, never merges, excluded from the random pool |
| Difficulty | Easy/Normal/Hard/Insane (−25% → +50% mob HP), single player + lobby — **untested** |
| Viewers | **Tower Viewer** and **Mob Viewer** on the main menu |
| Audio | fully procedural SFX + a **deep-house** looping track (122 BPM, 8 bars); `M` toggles mute |
| Menus | Orbiting board backdrop, construction-paper buttons, **Settings** (music/SFX), in-game **pause** menu |
| Multiplayer | Lobby (8 players), one board each, **Unity Relay (UGS)**; **independent per-board waves** (#7), **per-player board theme picked in the lobby** (roster-visible, remote boards render it), live board sync (~20 Hz) with remote projectiles + FX, collapsible scoreboard, relay chat (**T**), version gate (#30) |
| Board themes | `BoardTheme` (`Bedroom` / `ArcticOutpost` / `VolcanicCaldera` / `SpaceStation` / `DesertHighway` / `CandyShop` / `SewerSubway` / `MedievalCastle` / `FactoryFloor` / `SunkenReef` / `ZenGarden` / `ClassroomDesk`) reskins tiles + room via `TDBoardBuilder` + `TDRoom`; **`Layout`/`Route` are never changed by a theme** — only visuals. Each theme also carries a light/ambient row in `TDGameManager.ApplyThemeLighting` (mirrored in `TDArenaPreview`). `TDGameManager.SetTheme` swaps at runtime and persists to `PlayerPrefs("td.theme")`. Picked from the **single-player difficulty screen** (`<`/`>` + rotating preview) and from the **multiplayer lobby** (per-player; see the Multiplayer row). No `GameVersion` bump on its own — but adding it to the lobby roster did bump to `0.10.0`. |
| WebGL | `WebGLBuild.Build` → `build/WebGL`; live at <https://zjones95.github.io/SnackTowerDefense/>; publish with `tools\publish-webgl.ps1` → `webgl` branch (**licence-free**); share zip `C:\Users\Desktop\SnackTowerDefense-WebGL.zip` |
| CI | `unity-ci.yml` now **passes** (the Unity credentials were corrected); the WebGL deploy is licence-free (see below) |

## Verified vs not

**Verified by the agent** (batch mode): compiles clean; the **StandaloneWindows64**
and **WebGL** players link; models check out in editor renders (`SnackPreview` shows the
new Soda Cup in the tower grid); music loop checked (`len 15.74 s, peak 0.92, rms 0.21, bad 0`).

**NOT verified — needs a human play-test:**
1. **Balance** across 35 waves — the biggest unknown, incl. the new T7 towers.
2. **T7 fusion towers** — Fondue (beam + stacking Dipped), Ice Cream Truck (splash stun),
   Boba (rate ramp), Pizza Oven (translucent 5 s zone). Implemented + modelled, never played.
3. **Boss behaviours** (`regen`, `armour`, `slowResist`, `enrage`, `dash`).
4. **Multiplayer end-to-end** — independent waves, chat, scoreboard, version gate, remote
   projectiles + the FX channel (Relay over the internet). Agents **cannot** test networking.
5. **Difficulty selector**, **Damage Test** (#9), **sell** (#18) and **merge-at-any-tier** (#10).
6. **The new Soda Cup model** — check its aim/scale against the other towers.

## Open work — GitHub issues

The tracker is at **#1–#32**. **Everything is implemented and closed except two design
issues waiting on a decision:**

- **#8 [Feature] Roguelike upgrades** — pick 1 of 3 after each boss wave. A full spec is on
  the issue (pool, rarity weights 60/30/10, per-player in MP, carries into the Damage Test).
- **#17 [Design] Cooperative map** — larger, non-symmetrical shared board; a phased plan is
  on the issue (author the layout single-player first, then host-authoritative shared-board
  netcode).

Recent ships (all closed): #1–#7, #9, #11, #13–#16, #18–#32 — including the **Tier 7** tier
(#20) + four fusion towers (#21–#24), Spicy Chips → **Burn** (#29), multiplayer
**independent waves** (#7), spectate perf (#15), **version gate** (#30), collapsible
**scoreboard** (#32), relay **chat** (#27), **sell** (#18), **merge at any tier** (#10), the
**Damage Test** (#9), and the T7-tower + Soda-Cup Blender models.

## Suggested next steps

1. **Play-test single player** and tune balance (highest value) — especially the four T7
   fusion towers and the new Soda Cup model.
   - tower stats: `TowerCatalog` (all rows, incl. the `--- Tier 7 fusion towers ---` block)
   - health curve `TDBalance.HealthMult`; global HP `TDBalance.MobHealthScale` (1.25)
   - income `TDBalance.KillReward` (1) / `RoundBonus` (flat 50)
   - merge/ascend costs in `TDBalance`; sell/merge rules in `TDGameManager`
2. **Decide #8 and #17** (proposals are on the issues), then implement.
3. **Multiplayer play-test** (ParrelSync, two editors): independent waves, chat (**T**),
   scoreboard, version gate, remote projectiles + FX.
4. Re-author more towers in the soda-cup style if you like — `tools/blender/` has the script.

## CI / licence / deploy

- **WebGL → GitHub Pages is settled and needs no Unity licence** (it is unaffected by
  the CI problem below). The site <https://zjones95.github.io/SnackTowerDefense/> is
  served straight from the **`webgl`** branch (**Settings → Pages → Deploy from a
  branch → `webgl` / root**). Refresh it after gameplay/art changes:

  ```powershell
  powershell -ExecutionPolicy Bypass -File tools\publish-webgl.ps1
  ```

  It builds WebGL locally (`WebGLBuild.Build` → `build/WebGL`) and force-pushes a
  single-commit `webgl` branch; pass `-SkipBuild` to publish an existing build. No
  licence, no Actions minutes, no `UNITY_*` secrets.
- **Only publish WebGL when the user explicitly asks** — it is a heavy full build plus
  a branch push. Routine verification is a batch compile (or a Windows player build).
  The `webgl-build` project skill (`.opencode/skills/webgl-build/`) wraps the publish.
- `.github/workflows/unity-ci.yml` (StandaloneLinux64) uses **GameCI** and now
  **passes** (the Unity credentials were corrected). It previously failed on
  activation with **HTTP 401**:

  ```
  UnityConnectLoginRequest: Failed to login ... HTTP error code 401
  [Licensing::Module] Error: Failed to activate ULF license
  ```

  That is Unity rejecting the stored `UNITY_EMAIL`/`UNITY_PASSWORD` — stale
  credentials or **2FA** — not a code/config problem.
- Secrets present: `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE` (a valid,
  **unbound** Unity Personal ULF); no `UNITY_SERIAL`.
- Fixes (all require the Unity account holder): correct the credentials / disable
  2FA; **or** delete the two credential secrets so the ULF is used alone; **or** add
  `UNITY_SERIAL` (paid seat).
- Local build commands (editor **closed**): compile `-executeMethod
  CICompileCheck.EnsureBootScene`; Windows player `-executeMethod
  CICompileCheck.Build` with env `CI_BUILD_TARGET=StandaloneWindows64`; WebGL
  `-executeMethod WebGLBuild.Build`.

## Multiplayer specifics (for a fresh context)

- **UGS is linked** (`ProjectSettings.asset` `cloudProjectId`); **Relay + Anonymous auth
  enabled** in the dashboard (anonymous needs no identity provider — verified against
  the REST API).
- `NetworkSession.TryRelayHost` / `JoinViaRelay` build `RelayServerData` from the
  **endpoint matching the connection type** — desktop uses `dtls`, WebGL uses `wss`
  (the old code passed the raw UDP port and failed).
- `Application.runInBackground = true` + `Time.maximumDeltaTime = 1f` (a backgrounded
  host keeps simulating; WebGL background tabs are still browser-throttled).
- `MatchSync` is a per-board **state relay** (not a wave clock): each peer advances
  its own wave on clear (independent waves, #7). Any board clearing all 35 waves is a
  shared victory.
- `RemoteBoard` rebuilds a tower when its **type or tier** changes (merge/re-roll).
- `BoardSnapshot` carries mobs (`Status`/`Stacks`), towers, and projectiles (`Type` = the
  firing tower, so the remote picks the right model). **Changing the wire format requires
  bumping `NetConfig.GameVersion`** — both peers must match. The join handshake refuses
  mismatches (#30); the version is `<scheme>+<git sha>` stamped by `Assets/Editor/BuildVersion.cs`.
- Spectate snapshots stream at **~20 Hz** (#15); `RemoteBoard` interpolates mobs/projectiles
  and slerps turret aim, so remote boards render smoothly at the display frame rate.
- Cosmetic FX (splash bursts, Sniper/Chain tracer bolts, the Fondue beam) ride their **own
  best-effort channel** — `Net/FxSync.cs` (`td.fx`/`td.fxall`), separate from the board
  snapshot, so cosmetics can change/drop without touching the sim wire format.
- Chat relay lives in `Net/ChatSync.cs`; in-match overlays (collapsible scoreboard at
  top-right, chat log lower-left) are drawn from `TDGameManager.Multiplayer.cs`.

## How to work here

1. **Read** `README.md` → `docs/ARCHITECTURE.md` → `docs/TierPlan.md` / `docs/MobRoster.md`.
2. **Make changes** — no scenes/prefabs; everything is built in code.
3. **Verify** with batch mode (the editor **must be closed**) — see ARCHITECTURE's
   verification section — and/or the editor render helpers.
4. **Ask the user to play-test** anything behavioural (the agent cannot run play mode
   or networking).
5. **Commit and push** to `main`. For a **player build, commit first** — the player's
   version stamp is `<scheme>+<git sha>` of `HEAD`, so building uncommitted work bakes the
   previous commit. And **only build WebGL when the user explicitly asks** (see the
   `webgl-build` skill); routine verification is a batch compile / Windows build.
6. `gh` is **not logged in** by default; a token can be pulled from the git credential
   store when filing issues/PRs.

## Useful facts / gotchas

- **Transparent Standard materials are stripped from player builds** (they render
  opaque). For anything that must be translucent/additive at runtime, use the shader at
  `Assets/Resources/Snack/Fx.shader` (unlit alpha-blend), loaded via
  `Resources.Load<Shader>("Snack/Fx")`. This bit the tier glow, the Soda explosion and
  the ghost; `SplashFX` and the status icons use it now.
- Menu orbit camera uses its own `menuYaw/menuPitch/menuDist`; the game camera resets to
  `45/42/32` on `StartRun`.
- **Adding a mob needs no code**: drop `<id>.glb` into `Assets/Resources/Snack/Mobs/`
  where `<id>` matches `MobDef.id`, and mirror it into `Assets/Models/Mobs/`.
- Mobs and towers sit on **y = 0** (the play-mat top face; tiles are centred at y = -0.05).
- TextMesh reads from its **−Z** face — billboard by copying `Camera.main` rotation.
- The Blender MCP runs code at module scope — use `print`, no top-level `return`.
- Leftovers: `Cracker.glb` is unused; `Swarm` scale `0.42` makes small swarms nearly
  invisible (bump toward ~`0.7`).
