# Handoff — Snack Tower Defense

Written so a **fresh session** can pick this project up fast. Read this first, then
`docs/ARCHITECTURE.md`, then `docs/TierPlan.md` and `docs/MobRoster.md`.

> **The GitHub issue tracker is the live to-do list** —
> `github.com/zjones95/SnackTowerDefense/issues` (currently **#1–#19**). This doc is
> the *state of the world*; the issues are the *work*.

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
| Towers | 7 random-build types × **6 tiers** + **Gold** (3 tiers) + **4 Tier 7 fusion types** (#12). **T6+T6 → a random T7 for `$200`** on the merge key; T7 is terminal. 2:1 merge while source tier ≤ T3 (top merge **T3+T3 → T4**); cash **ascension** **T4→T5 `$150` / T5→T6 `$300`** with **U**. **T5/T6 modifiers implemented** and **T6 is cumulative with its T5 trait**. |
| Targeting | Per-tower mode in the selection panel: Default / Nearest / Farthest / Random / Highest health / Lowest health (`TowerTargeting`) |
| Mobs | 35 fruits/veg; **34 modelled**, Granola Mom is the procedural humanoid |
| Status FX | Floating bars **and** the boss bar show **burn (flame + `xN`)** and **slow (ice cube)** and **tar** icons (`MobStatusIcons`) |
| Metrics | Per-tower **Damage done** (selection panel; DoT + detonations are attributed) and per-Gold-tower **Gold made**; collective **Gold Generated** (HUD top-right) |
| Gold tower | Hotkey **G**, cap **4**, pays **$1/$2/$3** per hit, cash-upgrade T1→2 **$50** / T2→3 **$100**, never merges, excluded from the random pool |
| Difficulty | Easy/Normal/Hard/Insane (−25% → +50% mob HP), single player + lobby — **untested** |
| Viewers | **Tower Viewer** and **Mob Viewer** on the main menu |
| Audio | fully procedural SFX + a **deep-house** looping track (122 BPM, 8 bars); `M` toggles mute |
| Menus | Orbiting board backdrop, construction-paper buttons, **Settings** (music/SFX), in-game **pause** menu |
| Multiplayer | Lobby (8 players), one board each, **Unity Relay (UGS)**; **independent per-board waves** (#7), live board sync (~15 Hz), collapsible scoreboard, relay chat (**T**), version gate (#30) |
| WebGL | `WebGLBuild.Build` → `build/WebGL`; live at <https://zjones95.github.io/SnackTowerDefense/>; publish with `tools\publish-webgl.ps1` → `webgl` branch (**licence-free**); share zip `C:\Users\Desktop\SnackTowerDefense-WebGL.zip` |
| CI | `unity-ci.yml` now **passes** (the Unity credentials were corrected); the WebGL deploy is licence-free (see below) |

## Verified vs not

**Verified by the agent** (batch mode): the project compiles; `StandaloneWindows64`
and **WebGL** players link; models/levels checked with editor renders; music loop
checked (`len 15.74 s, peak 0.92, rms 0.21, bad 0`).

**NOT verified — needs a human play-test:**
1. **Balance** across 35 waves — the biggest unknown.
2. **Boss behaviours** (`regen`, `armour`, `slowImmune`, `enrage`, `dash`).
3. **Difficulty selector.**
4. **Multiplayer end-to-end** (tiers, status icons, Relay over the internet).
5. **T5/T6 modifier behaviours** — implemented, never played.

## Open work — GitHub issues

| # | Title |
|---|---|
| 1 | UI: Sour Belt selection panel content is cut off — enlarge the panel |
| 2 | UI: show full targeting-mode button names (needs #1) |
| 3 | Sour Straw (Sniper) tracer ray should be green |
| 4 | Bug: poison tower damage not counted in "Damage Done" |
| 5 | Balance: nerf the poison death explosion by 20% |
| 6 | Balance: bosses should never be immune to stuns *(superseded by #11)* |
| 7 | Multiplayer: independent board wave progression |
| 8 | Feature: roguelike upgrades (pick 1 of 3 after each boss wave) — design |
| 9 | Feature: "Damage Test" end-of-run scenario |
| 10 | Design: allow merging towers at any tier |
| 11 | Balance: stuns always work on bosses; Sour Belt stun 5% → 10% |
| 12 | Design: five new unique Tier 7 fusion towers — brainstorm for review |
| 13 | UI: bottom toolbar for build options (Tower / Gold Tower) |
| 14 | Balance: slow towers 50% effective on bosses instead of immune |
| 15 | Perf: optimize multiplayer board syncing; raise spectate FPS |
| 16 | UI: track gold earned per individual Gold tower |
| 17 | Design: cooperative map (larger, non-symmetrical) |
| 18 | Feature: sell towers for 50% of invested cost (hotkey X) |
| 19 | UI: tar (+damage-taken) status icon on mob/boss bars |

## Suggested next steps

1. **Play-test single player** and tune balance (highest value).
   - health curve: `TDBalance.HealthMult` (now tapers 13%→10%/wave)
   - global mob HP: `TDBalance.MobHealthScale` (1.25)
   - income: `TDBalance.KillReward` (1) / `RoundBonus` (flat 50)
   - tower stats: `TowerCatalog`; ascension costs in `TDBalance`
2. **Cheap wins:** #1–#3, #16, #19; balance tweaks #5, #11, #14.
3. **Multiplayer:** #15 (perf/spectate FPS), then decide #7 (independent waves).
4. **Design reviews** before code: #8 (roguelike), #12 (T7), #17 (co-op map).
5. **Fix CI licence** to unblock the Pages deploy (below).

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
- `.github/workflows/unity-ci.yml` (StandaloneLinux64) still uses **GameCI** and
  **fails**: Unity activation returns **HTTP 401**

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
- `BoardSnapshot.MobSnap` carries `Status` / `Stacks` (status icons). **Changing the
  wire format requires bumping `NetConfig.GameVersion`** — both peers must match. The
  join handshake refuses mismatches (#30); the version is `<scheme>+<git sha>` stamped
  by `Assets/Editor/BuildVersion.cs`.
- Spectate snapshots stream at ~15 Hz (#15); `RemoteBoard` interpolates frame-rate
  independently.
- Chat relay lives in `Net/ChatSync.cs`; in-match overlays (collapsible scoreboard at
  top-right, chat log lower-left) are drawn from `TDGameManager.Multiplayer.cs`.

## How to work here

1. **Read** `README.md` → `docs/ARCHITECTURE.md` → `docs/TierPlan.md` / `docs/MobRoster.md`.
2. **Make changes** — no scenes/prefabs; everything is built in code.
3. **Verify** with batch mode (the editor **must be closed**) — see ARCHITECTURE's
   verification section — and/or the editor render helpers.
4. **Ask the user to play-test** anything behavioural (the agent cannot run play mode
   or networking).
5. **Commit and push** to `main`.
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
