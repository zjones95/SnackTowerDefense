# Handoff — Snack Tower Defense

Written to let a **fresh session** pick this project up with minimal ramp-up.
Read this first, then `docs/ARCHITECTURE.md`, then `docs/MobRoster.md`.

---

## The project

- **Unity 6.6 (`6000.6.0f1`)**, built-in pipeline, no authored scene — the game
  bootstraps itself at runtime.
- **Path:** `C:\Users\Desktop\SnackTowerDefense`
- **Repo:** `github.com/zjones95/SnackTowerDefense` (private), branch `main`
- A kid's bedroom is attacked by **fruit & vegetable** mobs; you defend it with
  **snack towers** (popcorn bucket, soda cup, gumball machine…).

## Where it stands

| Area | State |
|---|---|
| Single player | **35 waves**, one mob type per wave, boss every 5th |
| Towers | 7 random-build types × **6 tiers** + Gold (×3, procedural); merge two same-tier while source ≤ T3 (top merge T3+T3 → T4), then cash-ascend **T4→T5 `$150` / T5→T6 `$300`** with **U**. Tier 4 is stats-only; tiers 5/6 carry modifier **fields** whose behaviours are a follow-up pass (`docs/TierPlan.md`) |
| Mobs | 35 fruits/veg; **34 modelled**, Granola Mom is the procedural humanoid |
| Projectiles | modelled popcorn / soda blob / skewer rod / jelly bean / chip |
| Difficulty | Easy/Normal/Hard/Insane (−25% → +50% mob HP), single player + lobby |
| Viewers | **Tower Viewer** and **Mob Viewer** on the main menu |
| Audio | fully procedural SFX + a looping music track (`M` toggles mute) |
| Multiplayer | Phases 1–3 done: lobby (8 players, join code/IP), one board per player, host wave clock, live board sync, scoreboard |
| CI | GameCI workflow exists but **fails — missing Unity licence secret** |

## Verified vs not

**Verified by the agent** (batch mode): the project compiles, a
`StandaloneWindows64` build links, and every model/level look has been checked
with editor renders.

**NOT verified — needs a human play-test:**
1. **Balance.** The health curve `1.13^(wave-1)`, tower DPS (after the
   "2× rate, ½ damage" pass), and the economy are all **guesses** across 35
   waves. This is the biggest unknown.
2. **Boss behaviours** (`regen`, `armour`, `slowImmune`, `enrage`, `dash`) are
   implemented but never play-tested.
3. **Difficulty selector** — implemented, untested.
4. **Multiplayer with the new content.** Phases 1–3 worked with the old 5-wave
   roster; the mob/wave rework has not been re-tested online.
5. **Tier 4-6 + cash ascension.** Costs (`$150` / `$300`), the capped merge
   (source ≤ T3), the 6-tier scale/colours and the `U` panel are implemented and
   compile, but the balance of the new stat rows has never been played. The
   tier-5/6 **modifier behaviours** also do not exist yet — the fields are set in
   `TowerCatalog` but nothing reads them.
6. **Tier-4-6 modifier behaviours.** A follow-up pass implements crit + Deadeye,
   Twin Lash branching, Sticky Sour, Candy Shell / Sticky Tar, poisoning stacks
   + detonation, boomerang return, Kettle Burst, and Sticky Soda — see
   [`docs/TierPlan.md`](TierPlan.md).

## Suggested next steps (roughly in order)

1. **Implement the tier-5/6 modifier behaviours** (the field data is already in
   `TowerCatalog`; nothing reads it yet) — crit + Deadeye, Twin Lash / Sticky
   Sour, Candy Shell / Sticky Tar, poisoning stacks + detonation, boomerang
   return, Kettle Burst, Sticky Soda. Full brief: [`docs/TierPlan.md`](TierPlan.md).
2. **Play-test single player** and tune balance — most valuable thing right now.
   - curve: `TDBalance.HealthMult`
   - tower stats: `TowerCatalog` (now T1-6; ascension costs in `TDBalance`)
   - economy: `TDBalance.KillReward` / `RoundBonus`
   - new: `TDBalance.AscendCost4to5` / `AscendCost5to6`
3. **Bump the Swarm scale.** `Swarm` is `0.42`, so Raspberry (0.32 tall) renders
   ~0.2 units against a 2-unit tile — nearly invisible. ~`0.7` would fix it.
   (`MobCatalog.Make`, the `Swarm` case.)
4. **Delete the unused `Cracker.glb`** (and its `Assets/Models` mirror) — the
   original cracker mob is gone.
5. **Re-test multiplayer** now that waves/mobs changed (ParrelSync clone or two
   builds; host + join `127.0.0.1:7777`).
6. **Unity Gaming Services / Relay.** Join codes fall back to a LAN address
   today. Creating a UGS project, enabling Relay and setting the Project ID makes
   real 6-character codes work over the internet.
7. **Fix CI** — add the `UNITY_LICENSE` secret (see README).

## Open design questions

- **Boss overlap.** Durian *and* Granola Mom both enrage; Coconut *and* Mom are
  both slow-immune. If every boss should be unique, give Durian something else
  (e.g. spiky — reflects damage) and leave Coconut as "extremely tanky/slow".
- **Swarm archetype** is new and only used by Blueberry / Raspberry / Lychee.
- **Granola Mom** is the simple humanoid with long hair. She could use more
  character (kale-print tee, sunglasses, tote details) if she should land harder.

## How to work here

1. **Read** `README.md` → `docs/ARCHITECTURE.md` → `docs/MobRoster.md`.
2. **Make changes** — remember the project has no scenes/prefabs; everything is
   built in code.
3. **Verify** with batch mode (the editor **must be closed**):
   ```powershell
   Unity.exe -batchmode -projectPath C:\Users\Desktop\SnackTowerDefense `
             -executeMethod CICompileCheck.EnsureBootScene -quit -logFile <log>
   ```
   ...or render a preview (`SnackPreview.Render`, `TDArenaPreview.Render`, …).
4. **Ask the user to play-test** anything behavioural — the agent cannot run
   play mode or networking.
5. **Commit and push** to `main` (`git add -A; git commit; git push`).

## Useful facts

- Adding a mob model needs **no code**: drop `<id>.glb` into
  `Assets/Resources/Snack/Mobs/` where `<id>` matches `MobDef.id`, and mirror it
  into `Assets/Models/Mobs/`.
- Mobs and towers sit on **y = 0**, which is the play-mat's top face.
- Tools used: **Blender MCP** for modelling, **PowerShell batch mode** for
  verification, editor render helpers for visual checks.
- The Blender MCP runs `exec` at module scope — use `print`, no top-level
  `return`.
