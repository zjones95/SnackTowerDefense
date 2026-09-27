# Tier 4–6 Plan

**Tier 4 = stats only. Tier 5 = the old tier-4 modifiers. Tier 6 = the old tier-5
modifiers.** The original tier-6 ideas were scrapped.

> Status: **implemented.** The tier 4–6 stat rows *and* the T5/T6 modifier behaviours
> are all in (`TowerCatalog`, `Tower`, `Projectile`, `PierceProjectile`, `Mob`), and
> **a T6 tower keeps its T5 trait in addition** to its own modifier. Numbers still need
> a play-test. Exact current numbers live in `TowerCatalog.cs`; some differ from the
> first-pass table below (e.g. poison detonation and the Chain stun are tuned in code).

---

## Decisions

- **`MaxTier` 3 → 6.**
- **Tier 4** adds no new behaviour — a pure stat tier.
- **Tier 5 / Tier 6** each add a unique modifier per tower type (below), and **T6 is
  cumulative** — a T6 tower also keeps its T5 trait.
- **Economy — merging and ascension both exist.** 2:1 merging is allowed at any
  tier up to 5 (`$10`, random result: `T4+T4 → T5`, `T5+T5 → T6`); **T6+T6 → a
  random T7** is the `$200` fusion. **Cash ascension** remains the single-tower
  alternative at T4/T5 (`$150` / `$300`). *(Issue #10, option 3 — keep both.)*
- **Gold tower:** pays `$1/$2/$3` per hit by tier (rate `5.0/4.0/3.2 s`), capped at 4,
  never merges, cash-upgraded `$50` / `$100` for T1→2 / T2→3, and excluded from the
  random pool.
- **Income:** `KillReward = 1` and a flat `$50` `RoundBonus` per completed round;
  late-game scaling is meant to come from Gold towers.

---

## Gold tower rules

- Gold is capped at **3 tiers** (T1→T3). It must **never** reach tier 4+.
- Gold is **never mergeable** — `TryMerge` must reject any Gold tower, and the
  selection panel must not offer Merge on a Gold.
- Gold is **upgradeable for cash** instead (same action/button as ascension, no
  second tower): **T1→T2 `$50`, T2→T3 `$100`**. No upgrade is offered at T3.
- Gold is **excluded from the random pool** for random build, merge results and
  re-roll. Verify `TowerCatalog.RandomTypeExcluding` / the re-roll path can never
  return Gold (only `G` builds Gold).
- Gold build cap stays **4**.

---

## Visuals

**`TowerVisual.TierScale`** — keep T1–3, damp the ultimate steps so a ~1-unit
model doesn't clip neighbours on a 2-unit tile:

| Tier | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Scale | 0.78 | 1.00 | 1.22 | 1.34 | 1.44 | 1.54 |

**`TowerVisual.TierColours`** — currently only 3 entries (T4–6 clamp to tier-3
green). Append three (**gold is reserved for the Gold tower**):

| Tier | 4 | 5 | 6 |
|---|---|---|---|
| Colour | bronze `(0.85, 0.45, 0.25)` | magenta `(0.95, 0.30, 0.85)` | cyan `(0.25, 0.95, 1.0)` |

---

## Modifiers

`[data]` = stats only · `[small]` = a few lines · `[new]` = new branch/field or
mob-side system.

| Tower (snack) | Tier 5 (old T4) | Tier 6 (old T5, strengthened) |
|---|---|---|
| **SingleShot** (Popcorn Bucket) | **Ricochet Pop** — bounces to 2 further mobs at full damage (`bounceCount 2`, `bounceRange 2.5`) `[data]` | **Kettle Burst** — every impact mini-splashes (`splashRadius 1.3`) and the volley fires **3** pellets at different targets `[small]` |
| **Splash** (Soda Cup) | **Fizz Ricochet** — the blob bounces once after each detonation (`bounceCount 1`, `bounceRange 3.0`) `[data]` | **Sticky Soda** — splash also applies slow **0.45 / 2.0 s** to everything caught `[small]` |
| **Slow** (Gumball Machine) | **Candy Shell** — gumballs deal impact damage (currently hardcoded 0) `[small]` | **Sticky Tar** — mobs slowed by this tower take **+30%** damage from every source while slowed, lingering **1.5 s** after the slow `[new]` (mob-side) |
| **Sniper** (Sour Straw) | **Powdered Sour** — 30% crit for ×2.5, crits bypass `Mob.Def.armour` `[small]` | **Deadeye** — consecutive hits on the same target ramp **+15%/stack, cap ×1.8**; each stack also **+5% crit**; resets on switch `[new]` |
| **Chain** (Sour Belt) | **Twin Lash** — each node arcs to its 2 nearest neighbours (`chainBranches 2`, ~6 targets) `[new]` | **Sticky Sour** — removes the `0.75^i` falloff (full damage every jump), applies slow **0.40 / 2.0 s**, **one extra jump** `[small]` |
| **Pierce** (Skewer) | **Wide Skewer** — `pierceWidth 2.0`, `pierceCount 8` `[data]` | **Boomerang Skewer** — the rod returns and skewers everything again at **full damage**, steering toward the nearest mob `[new]` (`PierceProjectile`) |
| **Poison** (Spicy Chips) | **Extra Hot** — poison stacks up to **3×** `[new]` (mob-side) | **Ghost Pepper** — a poisoned mob that dies detonates: **full** current poison DPS in a **2.5** radius, re-applying poison (can chain) `[new]` |

---

## Per-tier stats

Damage values below are **already scaled ×0.8** to match the tier 1–3 nerf.
Fields not listed keep the usual step. Everything needs a play-test.

| Tower | T4 (stats only) | T5 (+ modifier) | T6 (+ modifier) |
|---|---|---|---|
| SingleShot | dmg 16, rng 7.5, fi 0.22, spd 27 | dmg 24, rng 8.5, fi 0.20, spd 28, bounce 2 / 2.5 | dmg 21 ×3 pellets, rng 9.5, fi 0.20, spd 29, splash 1.3 |
| Splash | dmg 11, rng 6.4, fi 0.40, spd 22, splash 3.3 | dmg 17, rng 7.2, fi 0.36, spd 24, splash 3.9, bounce 1 / 3.0 | dmg 24, rng 8.0, fi 0.32, spd 26, splash 4.4, slow 0.45 / 2.0 |
| Slow | rng 5.8, fi 0.48, slow 0.72 / 2.6, multi 7 | rng 6.6, fi 0.46, slow 0.78 / 2.8, multi 8, impact dmg 3 | rng 7.4, fi 0.44, slow 0.84 / 3.0, multi 10, impact dmg 6 + tar |
| Sniper | dmg 38, rng 15, fi 0.62 | dmg 54, rng 17, fi 0.56, crit 30% / ×2.5, pierce armour | dmg 77, rng 19, fi 0.50, + deadeye +15%/stack cap ×1.8 |
| Chain | dmg 11, rng 6.8, fi 0.36, chain 5 / 5.0 | dmg 16, rng 7.6, fi 0.34, chain 6 / 5.5, branches 2 | dmg 22, rng 8.4, fi 0.32, chain 7 / 6.0, no falloff, slow 0.40 / 2.0 |
| Pierce | dmg 22, rng 11.5, fi 0.50, pc 6, pw 1.7 | dmg 32, rng 13, fi 0.46, pc 8, pw 2.0 | dmg 45, rng 14.5, fi 0.42, pc 9, pw 2.2, boomerang |
| Poison | dmg 3, rng 6.8, fi 0.36, spd 23, dps 24 / 4.5 | dmg 4, rng 7.6, fi 0.34, spd 24, dps 33 / 5.0, stacks 3 | dmg 6, rng 8.4, fi 0.32, spd 25, dps 45 / 5.5, detonation |

---

## New `TowerTierStats` fields (schema)

| Field | Used by |
|---|---|
| `critChance`, `critMult`, `critPierceArmour` | Sniper T5/T6 |
| `deadeyeRamp`, `deadeyeCap`, `deadeyeCrit` | Sniper T6 |
| `poisonMaxStacks` | Poison T5 |
| `poisonDetonateRadius`, `poisonDetonateFraction` | Poison T6 |
| `chainBranches`, `chainFullDamage` | Chain T5/T6 |
| `tarDamageBonus`, `tarLinger` | Slow T6 |
| `boomerangReturn` (bool) | Pierce T6 |
| `multiShot` (existing) + `impactSplash` | SingleShot T6 |
| `splashSlowFactor`, `splashSlowDuration` | Splash T6 |

(Add only what the behaviour pass actually needs.)

---

## Behaviour work (separate pass)

- **Sniper** — crit + armour bypass; Deadeye ramp state in `Tower.cs`.
- **Chain** — Twin Lash branching; Sticky Sour full-damage + slow + extra jump.
- **Slow** — Candy Shell impact damage; Sticky Tar mob-side damage-taken debuff.
- **Poison** — stacking (`Mob.ApplyPoison`); death detonation (`Mob.Die`).
- **Pierce** — Boomerang return pass (`PierceProjectile`).
- **Splash** — Sticky Soda splash-slow (`Projectile.Hit` splash branch currently skips status).
- **SingleShot** — Kettle Burst multi-pellet + per-impact mini-splash.
- **Multiplayer:** mob-side state must stay host-authoritative / reflected in
  `BoardSnapshot`, or remote boards will diverge.
