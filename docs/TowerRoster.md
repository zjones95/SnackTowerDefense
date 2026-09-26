# Tower Roster — Snacks

The game launches with **7 tower types** and should reach **20 at launch**.
This doc proposes the **13 new snack towers**, each mapped to an existing
mechanic or a new `TowerType`, with tier scaling and model notes.

> **Status:** design only. Nothing here is implemented yet. Numbers are
> first-pass and sit inside the current magnitudes (tier-1 damage ~1–15, range
> ~4–9, fire interval ~0.25–1.0 s, tier-3 ≈ 2–4× tier-1). Everything still
> needs a play-test.

---

## Design goals

1. **Cover the gaps the current 7 leave open** — control, support, economy,
   armour counter, area denial, AoE DoT, spread/ricochet, sustained damage.
2. **Keep every tower mechanically distinct.** No two towers should answer the
   same wave in the same way; prefer a new *shape* of damage or utility over a
   bigger stat stick.
3. **Reuse before you invent.** The projectile pipeline already supports
   ricochet (`Bounces`/`BounceRange`), arcs (`Arc`), splash, slow and poison —
   several concepts are mostly data.
4. **Stay snacky and readable.** One clear visual idea per tower: if you can't
   guess what it does from the model, the model is wrong.
5. **Respect the economy.** Builds are a flat **$25**, merges **$10**, and a
   full run only affords **~18 towers**. Towers must be worth a slot, and any
   economy tower has to be tuned carefully (see *Prioritisation*).

### The existing 7

| Tower | Snack | Role | Current mechanic |
|---|---|---|---|
| SingleShot | Popcorn Bucket | Baseline DPS | Homing popcorn, one target, balanced rate |
| Splash | Soda Cup | AoE burst | Homing blob, `splashRadius` damage on impact |
| Slow | Gumball Machine | Control | 0 damage, fires `multiShot` gumballs that apply `slowFactor` |
| Sniper | Sour Straw | Burst / long range | Instant hit, highest damage, slowest rate, longest range |
| Chain | Sour Belt | Multi-hit | Damage arcs to `chainCount` nearby enemies, 25% falloff per jump |
| Pierce | Skewer | Line clear | Straight rod that skewers up to `pierceCount` enemies |
| Poison | Spicy Chips | Damage over time | Homing chip applies `poisonDps` for `poisonDuration` |

---

## The 13 new towers

### 1. Lollipop — *Repeater*

- **Snack:** a big swirl lollipop spinning on its stick.
- **Role / archetype:** rapid-fire single-target, anti-swarm finisher.
- **Core mechanic:** fires low-damage candy pellets at a fast rate. Unlike every
  other tower it targets the **lowest-health enemy in range** (not the one
  furthest along), so it mops up stragglers while the front towers hold the
  line. Same homing pipeline as Popcorn.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 3 | 5 | 9 |
  | range | 4.0 | 4.5 | 5.0 |
  | fireInterval | 0.30 | 0.28 | 0.25 |
  | projectileSpeed | 26 | 28 | 30 |

- **Differs from the 7:** Popcorn is the balanced generalist; Lollipop trades
  per-shot damage for rate and a different target rule, making it the dedicated
  swarm-cleanup tower rather than a second Popcorn.
- **Model notes:** lollipop head ~0.45 units wide on a 0.5-unit stick; total
  ~0.95 tall, base at z=0, centred in X/Y. The whole model can spin to aim, or
  give the stick a child named `pedestal` to keep the base planted.
- **Maps to:** new enum `Repeater` — **no new `Tower.cs` behaviour** beyond an
  optional targeting tweak (lowest-HP instead of furthest-along).

### 2. Jelly Bean — *Bounce*

- **Snack:** the retired Jelly Bean model, back from the cupboard.
- **Role / archetype:** ricochet, crowd damage.
- **Core mechanic:** a hopping bean that ricochets to nearby enemies up to
  `bounceCount` times, landing full damage on each hop. Reuses the
  **already-present but unused** `bounceCount` / `bounceRange` fields and the
  `Arc` parabola.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 5 | 9 | 15 |
  | range | 4.5 | 5.2 | 6.0 |
  | fireInterval | 0.75 | 0.65 | 0.55 |
  | projectileSpeed | 16 | 17 | 18 |
  | bounceCount | 2 | 3 | 4 |
  | bounceRange | 3.0 | 3.5 | 4.0 |

- **Differs from the 7:** Chain hits *instantly* along a line of mobs with no
  travel; Jelly Bean is a physical, arcing bounce that can whiff if the crowd
  scatters. Different feel, different counterplay.
- **Model notes:** bean ~0.22 long, ~0.5 total with a small pedestal base so it
  reads as a tower rather than a dropped sweet. `JellyBean.glb` already exists
  for both tower and projectile.
- **Maps to:** new enum `Bounce` — reuses existing `Projectile` bounce code;
  one line to set `p.Arc = true`. No other new behaviour.

### 3. Trail Mix — *Shotgun*

- **Snack:** a torn bag of mixed nuts, pretzels and candy that scatters.
- **Role / archetype:** spread / close-range burst.
- **Core mechanic:** fires `pelletCount` projectiles in a fan each volley. If
  enough enemies are in range, pellets prefer different targets; otherwise they
  spread outward. High point-blank burst, deliberately short range.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage (per pellet) | 2 | 3 | 5 |
  | range | 3.8 | 4.2 | 4.8 |
  | fireInterval | 0.70 | 0.60 | 0.50 |
  | projectileSpeed | 18 | 19 | 20 |
  | pelletCount | 3 | 4 | 5 |
  | spreadAngle | 15° | 18° | 22° |

- **Differs from the 7:** Soda splashes in a circle after one projectile;
  Trail Mix throws several weaker, independently-homing pellets. It rewards
  letting mobs get close and is poor against a lone tank.
- **Model notes:** a lumpy 1-unit mound of mixed pieces on a small tray/base;
  a cloth bag leaning against it works too. Whole model rotates to aim.
- **Maps to:** new enum `Shotgun` — **new `Tower.cs` branch** to spawn the fan.
  Add `pelletCount` and `spreadAngle` to `TowerTierStats`.

### 4. Marshmallow — *Mortar*

- **Snack:** a toasted marshmallow on a forked stick.
- **Role / archetype:** long-range lobbed AoE.
- **Core mechanic:** lobs one big gooey marshmallow in a slow **arc**
  (`Arc = true`) at the furthest enemy, landing a large `splashRadius`. Slow
  rate, long range, can undershoot fast movers — a positional answer to tight
  packs.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 8 | 15 | 26 |
  | range | 7.0 | 8.0 | 9.0 |
  | fireInterval | 1.00 | 0.90 | 0.80 |
  | projectileSpeed | 9 | 10 | 11 |
  | splashRadius | 2.2 | 2.8 | 3.4 |

- **Differs from the 7:** Soda is short-range and fast with a small splash;
  Marshmallow is the long-range artillery — bigger payload, slower delivery,
  and the first tower to use the arc path.
- **Model notes:** squat round marshmallow ~0.9 tall on a thin stick; base at
  z=0, centred. Whole model tilts/rotates to aim. Projectile `Marshmallow.glb`
  is a slightly browner toasted blob.
- **Maps to:** new enum `Mortar` — **one-line `Tower.cs` change** to set
  `p.Arc = true`; otherwise the Splash pipeline already fits.

### 5. Pop Rocks — *Nova*

- **Snack:** a pile of popping candy fizzing around a pedestal.
- **Role / archetype:** point-blank pulse / self-defence.
- **Core mechanic:** fires **no projectile**. Every interval it pops, damaging
  **every** enemy within a short radius around itself. It is the last line of
  defence for a leaky corner, not a ranged tower.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 5 | 9 | 15 |
  | splashRadius (pulse) | 3.0 | 3.5 | 4.0 |
  | fireInterval | 0.80 | 0.70 | 0.60 |

- **Differs from the 7:** every other damaging tower shoots outward; Pop Rocks
  only threatens what is already on top of it. Different risk/reward, good
  against swarms, useless as a front-line tower.
- **The pulse radius is deliberately below the normal 4–9 range band** — that
  short reach *is* the trade-off.
- **Model notes:** a low candy pile (<0.6 tall) that puffs upward each pulse,
  with a fixed base and a small popping cluster that scales with tier.
- **Maps to:** new enum `Nova` — **new `Tower.cs` branch** (radius query, no
  projectile). Reuses `splashRadius` for the pulse radius.

### 6. Candy Cane — *Beam*

- **Snack:** a striped candy cane that glows at the crook.
- **Role / archetype:** sustained single-target beam.
- **Core mechanic:** locks a continuous beam onto the most-advanced enemy in
  range with **no travel time**. Damage per second starts modest and **ramps**
  the longer it holds the *same* target; switching targets (or losing it)
  resets the ramp. Hits instantly, so it never misses.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage (DPS, base) | 6 | 10 | 17 |
  | beamRamp (extra DPS per second held) | 4 | 6 | 9 |
  | ramp cap | ×2.5 | ×2.5 | ×2.5 |
  | range | 4.5 | 5.2 | 6.0 |
  | tick interval | 0.25 | 0.25 | 0.25 |

- **Differs from the 7:** Sniper and Popcorn trade discrete shots; the beam is
  continuous and rewards focus-fire on one target, so it pairs with control
  (Slow/Freeze) rather than replacing raw DPS.
- **Model notes:** cane ~1 unit tall, base on z=0, curved hook ~0.35 wide; a
  small emissive tip marks the muzzle. Whole model rotates to aim.
- **Maps to:** new enum `Beam` — **new `Tower.cs` branch** plus a line/tracer
  visual. Add `beamRamp` to `TowerTierStats`.

### 7. Popsicle — *Freeze*

- **Snack:** a bright frozen ice pop.
- **Role / archetype:** hard control (brief stun).
- **Core mechanic:** hits a single target and **freezes it solid** for a short
  time — a full stop, implemented as `slowFactor = 1.0` through the existing
  `Mob.ApplySlow`. Low uptime and single-target, so it's a tempo tool, not a
  permanent snare. Bosses with `slowImmune` (Coconut, Granola Mom) ignore it.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 0 | 0 | 0 |
  | range | 4.0 | 4.6 | 5.2 |
  | fireInterval | 1.00 | 0.95 | 0.90 |
  | slowFactor | 1.0 | 1.0 | 1.0 |
  | slowDuration | 0.6 | 0.8 | 1.0 |
  | multiShot | 1 | 1 | 2 |

- **Differs from the 7:** Gumball is a *partial, multi-target, sustained* slow;
  Popsicle is a *total, single-target, brief* stop. They stack in a lane
  rather than compete for the same slot.
- **Model notes:** a twin-stick ice pop ~1 unit tall on a flat base; a frosted
  tip and a small drip. Whole model rotates to aim.
- **Maps to:** new enum `Freeze` — **new `Tower.cs` branch** that fires a slow
  projectile with `slowFactor = 1.0`; the `Mob` side already supports it
  (clamped to a full stop). No new mob system.

### 8. Ketchup Packet — *Mark*

- **Snack:** a little foil ketchup sachet with a red dollop.
- **Role / archetype:** debuff / damage amplification.
- **Core mechanic:** splats a target and **marks** it. Marked enemies take
  **+X% damage from every tower** for the duration (refreshes, does not stack).
  Very low direct damage — it exists to make your other towers hit harder.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 1 | 2 | 3 |
  | range | 4.5 | 5.0 | 5.5 |
  | fireInterval | 0.60 | 0.55 | 0.50 |
  | markBonus | +20% | +30% | +45% |
  | markDuration | 4s | 5s | 6s |

- **Differs from the 7:** no existing tower changes how much *other* towers
  hurt. This is a force multiplier, worthless alone and excellent in a
  well-placed cluster.
- **Model notes:** a flat foil sachet ~0.8 tall, tilted, with a red smear on a
  plate base. Whole model rotates to aim.
- **Maps to:** new enum `Mark` — **new `Tower.cs` branch** plus a **new
  mob-side system** (a damage-taken multiplier on `Mob`). Add `markBonus` and
  `markDuration` to `TowerTierStats`.

### 9. Rock Candy — *Armour Piercing*

- **Snack:** a cluster of sharp sugar crystals on a stick.
- **Role / archetype:** armour counter / anti-boss.
- **Core mechanic:** an instant crystal shard that **ignores flat armour** —
  it bypasses `Mob.Def.armour` entirely. Moderate damage and rate, medium-long
  range. It exists purely because armoured bosses (Pineapple, Avocado) blunt
  every other tower's hits.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 7 | 13 | 22 |
  | range | 6.0 | 7.0 | 8.0 |
  | fireInterval | 0.85 | 0.80 | 0.70 |
  | pierceArmour | yes | yes | yes |

- **Differs from the 7:** Sniper has bigger raw numbers but every hit is still
  reduced by armour; Rock Candy trades raw damage for consistently landing
  full value, so it scales *better* against armoured targets and *worse*
  against naked ones.
- **Model notes:** a jagged crystal cluster ~0.9 tall on a small disc base,
  a few translucent shards catching the light. Whole model rotates to aim.
- **Maps to:** new enum `ArmourPierce` — **new `Tower.cs` branch** plus a
  **new mob-side method** that deals damage without the armour subtraction.
  Add `pierceArmour` to `TowerTierStats`.

### 10. Maple Syrup — *Tar*

- **Snack:** a glass bottle of syrup pouring a sticky puddle.
- **Role / archetype:** area denial / persistent ground control.
- **Core mechanic:** periodically pours a **sticky puddle onto the path** (a
  fixed zone, not a projectile). Every enemy passing through is slowed by
  `slowFactor` while inside; the puddle lasts `zoneLifetime`. It doesn't aim
  at mobs — it controls *space*.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 0 | 0 | 0 |
  | range (pour distance) | 4.0 | 4.5 | 5.0 |
  | puddle radius (`splashRadius`) | 1.8 | 2.2 | 2.6 |
  | re-pour interval | 2.5s | 2.2s | 2.0s |
  | slowFactor | 0.55 | 0.65 | 0.75 |
  | slowDuration | 3.5s | 4.0s | 4.5s |

- **Differs from the 7:** Gumball slows individual enemies with travelling
  gumballs; Maple Syrup drops a *persistent zone* that catches everything
  crossing it, including future mobs. The long re-pour interval is deliberate —
  it's a placement decision, not a DPS tower.
- **Model notes:** a syrup bottle ~1 unit tall on a saucer, with a glossy
  puddle bead at the spout. Keep the base fixed (name the bottle's base
  `pedestal`) so only a small lid/spout node tilts toward the pour point.
- **Maps to:** new enum `Tar` — **new `Tower.cs` branch** plus a pooled ground
  puddle object. Reuses `splashRadius` and the slow fields; add `zoneLifetime`.

### 11. Wasabi Peas — *Toxic Splash*

- **Snack:** a tub of green wasabi-coated peas.
- **Role / archetype:** AoE damage over time.
- **Core mechanic:** throws a pea that splashes on impact and applies **poison to
  every enemy in the splash** — a burning crowd, not a single target. Small
  direct damage; the DoT does the work.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 1 | 2 | 3 |
  | range | 4.0 | 4.6 | 5.2 |
  | fireInterval | 0.70 | 0.62 | 0.55 |
  | splashRadius | 1.6 | 2.0 | 2.4 |
  | poisonDps | 4 | 7 | 11 |
  | poisonDuration | 3.0 | 3.5 | 4.0 |

- **Differs from the 7:** Spicy Chips poisons one target at a time; Wasabi
  Peas poisons a whole cluster. Soda splashes but leaves nothing ticking, so
  Wasabi is the answer to dense, slow pushes (swarms and tanks alike).
- **Model notes:** a small tub ~0.7 tall with a mound of green peas; base on
  z=0. Whole model rotates to aim. Projectile `WasabiPeas.glb` is a single pea.
- **Maps to:** new enum `ToxicSplash` — **small `Projectile.cs` change** to
  apply poison inside the `SplashRadius > 0` branch (it currently skips status
  effects on splash). Otherwise the Splash pipeline fits.

### 12. Honey Jar — *Buff Aura*

- **Snack:** a sticky honey pot with a wooden dipper.
- **Role / archetype:** support / force multiplier.
- **Core mechanic:** attacks nothing. Emits a constant sweet aura; **every
  tower within `buffRadius` gains `buffDamage` bonus damage**. Multiple Honey
  Jars do **not** stack (highest tier wins) to stop degenerate piles.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 0 | 0 | 0 |
  | buffRadius | 3.5 | 4.0 | 4.5 |
  | buffDamage | +15% | +22% | +30% |

- **Differs from the 7:** all seven are damage or control. Honey Jar is the
  first *buff* tower — it changes how the towers you already own perform, which
  is exactly the kind of decision a 20-tower roster needs.
- **Model notes:** a rounded honey pot ~0.8 tall, a drip down one side and a
  dipper resting in it, maybe a faint golden glow ring on the mat. Static model;
  no aiming needed.
- **Maps to:** new enum `BuffAura` — **new `Tower.cs` support branch** plus a
  buff recompute when towers are built/merged/destroyed. Add `buffDamage` and
  `buffRadius` to `TowerTierStats`.

### 13. Fortune Cookie — *Economy*

- **Snack:** a cracked fortune cookie with a paper slip.
- **Role / archetype:** economy.
- **Core mechanic:** deals no damage. At the **end of every wave** it pays out
  `income` (scaled by tier), feeding the merge economy. The slip pokes out and
  changes colour as the tier rises.
- **Scaling:**

  | Field | Tier 1 | Tier 2 | Tier 3 |
  |---|---:|---:|---:|
  | damage | 0 | 0 | 0 |
  | income per wave | $1 | $2 | $4 |

- **Differs from the 7:** no existing tower generates income. Because builds
  are a flat $25 and income is the real limiter, this is the riskiest tower to
  balance — a Tier-3 Fortune Cookie can snowball a run. Start low and tune.
- **Model notes:** a cracked cookie shell ~0.5 tall on a small plate, a curled
  paper slip rising ~0.9 total. Static model; no aiming needed.
- **Maps to:** new enum `Fortune` — **new economy hook**. Reuses the existing
  `EndWave()` path (where `RoundBonus` is applied) and adds `income` to
  `TowerTierStats`. Works for multiplayer only if the host payout is mirrored,
  so treat it as single-player-first.

---

## Enum mapping

`TowerType` is serialised to peers as a raw `(byte)t.Type` in
`BoardSnapshot`, so **always append new enum values at the end** — existing
ordinals must not shift.

| Concept | Enum value | Builds on | New C# behaviour? |
|---|---|---|---|
| Lollipop | `Repeater` | SingleShot projectile | Optional: lowest-HP targeting |
| Jelly Bean | `Bounce` | Unused `Bounces`/`Arc` | One line: `p.Arc = true` |
| Trail Mix | `Shotgun` | SingleShot projectile | Yes — fan spawn branch |
| Marshmallow | `Mortar` | Splash + `Arc` | One line: `p.Arc = true` |
| Pop Rocks | `Nova` | Splash query | Yes — self-radius pulse |
| Candy Cane | `Beam` | Sniper instant hit | Yes — continuous ramp beam |
| Popsicle | `Freeze` | Slow (`ApplySlow`) | Yes — full-stop projectile |
| Ketchup Packet | `Mark` | SingleShot projectile | Yes + new mob damage-taken multiplier |
| Rock Candy | `ArmourPierce` | Sniper instant hit | Yes + armour-ignoring damage call |
| Maple Syrup | `Tar` | Slow fields | Yes — persistent ground zone |
| Wasabi Peas | `ToxicSplash` | Splash + poison | Small `Projectile.cs` change |
| Honey Jar | `BuffAura` | — | Yes — support aura + buff recompute |
| Fortune Cookie | `Fortune` | — | Yes — per-wave income hook |

Every new enum also needs: a `TowerCatalog` def, a `SnackModels.TowerName`
case, a `SnackArt.BuildTower` fallback case, and a `TDGameManager.TowerViewer`
blurb/special-row entry (the last two are cosmetic and degrade gracefully).

---

## Mechanical gaps

| Gap | Why it matters | Covered by |
|---|---|---|
| No hard control | Slow is only a partial snare; nothing stops a dash | Popsicle (full stop) |
| No damage amplification | Support is impossible; every tower is standalone | Ketchup Packet (mark) |
| No support / buff tower | No reason to cluster or layer towers | Honey Jar (damage aura) |
| No economy tower | Income is fixed, so build order never varies | Fortune Cookie |
| Armour has no counter | Armoured bosses blunt *everything* | Rock Candy (armour pierce) |
| No area denial | Control can't be *placed* ahead of a wave | Maple Syrup (tar zone) |
| Poison is single-target | No answer to dense crowds over time | Wasabi Peas (toxic splash) |
| No ricochet / spread | `Bounces` and `Arc` sit unused | Jelly Bean, Trail Mix |
| No sustained-fire option | All damage is discrete shots | Candy Cane (ramp beam) |
| No point-blank option | Towers only threaten outward | Pop Rocks (nova) |
| No true anti-air | No flying mobs exist at all | **Deliberately not in launch 13** |

**Anti-air is intentionally deferred.** A flier-only tower is meaningless until
flying mobs exist, and that needs a path mode, a new targeting filter and
wave-design support on the **mob side**. Add air mobs and an anti-air tower
together, in a later content drop — don't ship an unusable tower at launch.

The launch 13 give the roster a coherent shape: **damage** (Lollipop, Jelly
Bean, Trail Mix, Marshmallow, Pop Rocks, Candy Cane), **control** (Popsicle,
Maple Syrup), **debuff** (Ketchup Packet, Rock Candy), **DoT** (Wasabi Peas)
and **support/economy** (Honey Jar, Fortune Cookie) — no two sharing a role.

---

## Prioritisation / suggested order

**Tier A — data reskins, cheapest (no new gameplay logic):**
1. **Jelly Bean** — the bounce fields and model already exist.
2. **Marshmallow** — Splash plus `Arc = true`.
3. **Wasabi Peas** — one line in `Projectile.Hit()`.
4. **Lollipop** — new stats; optional targeting tweak only.

These give the biggest roster growth for the least risk and are ideal first
models.

**Tier B — small new `Tower.cs` branches, no mob-side work:**
5. **Popsicle** — reuses `ApplySlow` at full strength.
6. **Pop Rocks** — radius query, no projectile.
7. **Trail Mix** — fan spawn; new `pelletCount`/`spreadAngle`.
8. **Maple Syrup** — ground puddle object, easy to scope.
9. **Candy Cane** — beam visual + ramp state.

**Tier C — need new mob-side or economy systems (most design risk):**
10. **Ketchup Packet** — needs a damage-taken multiplier on `Mob`.
11. **Rock Candy** — needs an armour-ignoring damage path on `Mob`.
12. **Honey Jar** — needs a support aura and buff recompute across towers.
13. **Fortune Cookie** — needs a wave-end income hook, and it can break the
    flat-$25 economy if overtuned. Ship it last, on its own, and play-test it.

### Two practical flags

- **Build/merge randomness.** `TryBuild` and `TryMerge` both call
  `TowerCatalog.RandomType()`. At 7 types that already feels random; at 20 it
  will feel like a slot machine and make deliberate strategies impossible.
  Either offer a small build palette of unlocked types, or weight the random
  roll toward the player's existing towers. This is worth doing **as part of**
  the roster expansion, not after.
- **Multiplayer.** Appending enum values is byte-safe for peers, but new
  mob-side state (Mark, armour-pierce, Fortune income) must be host-authoritative
  and reflected in `BoardSnapshot`/round payouts, or remote boards will
  disagree. Re-test online once the first new tower lands.
