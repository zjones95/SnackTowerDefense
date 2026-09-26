# Mob Roster — Fruits & Vegetables

Mobs are now fruits and vegetables. **35 waves**, with a **boss on every 5th
wave** (5, 10, 15, 20, 25, 30, 35). Boss waves contain **only the boss** — no
supporting mobs.

**One mob type per wave.** A wave never mixes mobs; it's a single fruit or
vegetable, possibly spawned in several staggered bursts.

This is a design draft for review; nothing here is implemented yet.

---

## Regular waves

| Wave | Mob | Archetype | New model | Notes |
|---:|---|---|---|---|
| 1 | Apple | Basic | ✔ | The plain intro mob |
| 2 | Carrot | Basic | ✔ | Slightly faster than the apple |
| 3 | Pear | Basic | ✔ | Standard |
| 4 | Banana | Fast | ✔ | First speed bump |
| 5 | **BOSS — Watermelon** | Boss | ✔ | See boss table |
| 6 | Cherry | Fast | ✔ | Small and quick |
| 7 | Potato | Tank | ✔ | Slow, chunky |
| 8 | Orange | Basic | ✔ | Standard |
| 9 | Grapes | Fast | ✔ | Arrives in tight clusters |
| 10 | **BOSS — Pumpkin** | Boss | ✔ | See boss table |
| 11 | Corn | Tank | ✔ | Armoured-ish, slow |
| 12 | Tomato | Basic | ✔ | Standard |
| 13 | Broccoli | Tank | ✔ | Tough |
| 14 | Strawberry | Fast | ✔ | Quick |
| 15 | **BOSS — Pineapple** | Boss | ✔ | See boss table |
| 16 | Peach | Basic | ✔ | Standard |
| 17 | Blueberry | Swarm | ✔ | Very fast, very weak, many |
| 18 | Cucumber | Tank | ✔ | Long, slow, tough |
| 19 | Plum | Basic | ✔ | Standard |
| 20 | **BOSS — Durian** | Boss | ✔ | See boss table |
| 21 | Onion | Basic | ✔ | Standard (maybe slows towers later) |
| 22 | Radish | Fast | ✔ | Quick |
| 23 | Eggplant | Tank | ✔ | Tough |
| 24 | Kiwi | Basic | ✔ | Standard |
| 25 | **BOSS — Coconut** | Boss | ✔ | See boss table |
| 26 | Mango | Basic | ✔ | Standard |
| 27 | Raspberry | Swarm | ✔ | Fast swarm |
| 28 | Cauliflower | Tank | ✔ | Tough |
| 29 | Beetroot | Tank | ✔ | Tough + slightly faster |
| 30 | **BOSS — Dragonfruit** | Boss | ✔ | See boss table |
| 31 | Avocado | Tank | ✔ | Armoured, slow |
| 32 | Lychee | Swarm | ✔ | Final swarm push |
| 33 | Turnip | Tank | ✔ | Very tough |
| 34 | Papaya | Tank | ✔ | Tough + faster |
| 35 | **BOSS — Granola Mom** | Boss | ✔ | Final boss |

**Archetypes used:** Basic, Fast, Tank, Swarm, Boss.
The game currently only has **Basic / Fast / Tank / Boss** — **Swarm** would be
new (fast, low HP, spawned in large numbers).

---

## Bosses (standalone, one per boss wave)

| Wave | Boss | Concept | Gimmick |
|---:|---|---|---|
| 5 | Watermelon | Enormous, lazy | Huge HP pool, very slow — a pure damage check |
| 10 | Pumpkin | Jack-o'-lantern | **Regenerates** health slowly, so chip damage isn't enough |
| 15 | Pineapple | Spiky exterior | **Armoured** — flat damage reduction per hit |
| 20 | Durian | Foul and furious | **Enrages** — gets faster as its health drops |
| 25 | Coconut | Hard shell | **Immune to slow**, extremely high HP, very slow |
| 30 | Dragonfruit | Exotic | **Phases** — dashes forward periodically |
| 35 | Granola Mom | Crunchy, organic, anti-snack | **Final boss.** **Immune to slow** — no processed sugar, no effect. **Enrages** — speeds up in her last third on green-smoothie energy. |

### Stretch ideas (swap in if you want more variety)
- **Artichoke** — layered armour: damage reduction drops in stages as you peel it.
- **Jackfruit** — the largest fruit in the world; a colossal mid-run boss.
- **Ghost Pepper** — leaves a burning trail that damages towers it passes.
- **Pomegranate** — splits into seeds on death (breaks the "no supporting mobs"
  rule, so it'd need to be an exception).
- **Wasabi Root** — periodically becomes untargetable (goes "underground").

---

## Things this implies (not yet done)

1. **Health scaling must change.** The current curve is `1.55^(wave-1)`; by
   wave 35 that's ~10^7 × base health. A 35-wave run needs a much gentler curve
   (e.g. `1.15^(wave-1)`, or a piecewise ramp with a step per boss).
2. **New archetypes.** `Swarm` is new; Regen / Armour / Slow-immunity / Enrage /
   Phases are boss-only behaviours that need code.
3. **35 mob models.** One Blender model per entry (28 regular + 7 bosses).
   Cheaper alternative: let a few earlier mobs return in later waves.
4. **Boss waves** are single-spawn waves — the wave table needs a flag, or the
   boss wave is expressed as a one-entry spawn group.
5. **`TDBalance.TotalWaves`** is currently `5`; it should move to `35` once the
   health curve is reworked.
