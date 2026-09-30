# Mob Roster — Fruits & Vegetables

Mobs are now fruits and vegetables. **45 waves**, with a **boss on every 5th
wave** (5, 10, 15, 20, 25, 30, 35, 40, 45). Boss waves contain **only the boss** — no
supporting mobs.

**One mob type per wave.** A wave never mixes mobs; it's a single fruit or
vegetable, possibly spawned in several staggered bursts.

> **Status:** implemented. See *Implementation status* at the bottom for the
> handful of caveats (chiefly balance, which is untuned).

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
| 35 | **BOSS — Granola Mom** | Boss | ✔ | See boss table |
| 36 | Lemon | Fast | ✘ | Quick citrus |
| 37 | Lettuce | Swarm | ✘ | Leafy swarm |
| 38 | Zucchini | Tank | ✘ | Tough |
| 39 | Chili Pepper | Fast | ✘ | Hot and quick |
| 40 | **BOSS — Blackberry Bramble** | Boss | ✘ | See boss table |
| 41 | Button Mushroom | Swarm | ✘ | Dense swarm |
| 42 | Garlic | Tank | ✘ | Pungent and tough |
| 43 | Grapefruit | Basic | ✘ | Standard |
| 44 | Kale | Swarm | ✘ | Final swarm push |
| 45 | **BOSS — Rot King Cabbage** | Boss | ✘ | Final boss |

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
| 35 | Granola Mom | Crunchy, organic, anti-snack | Mid-run boss. **Resists slow** — no processed sugar, no effect. **Enrages** — speeds up in her last third on green-smoothie energy. **Dashes** periodically. Leak = instant loss. |
| 40 | Blackberry Bramble | Thorny tangle | **Enrages** hard (0.8) — gets much faster as its health drops. **Dashes** every 4s. |
| 45 | Rot King Cabbage | Rotting monarch | **Final boss.** **Armoured** (6 flat reduction). **Resists slow** (0.6). **Enrages** (0.55). **Dashes** every 5s. Leak = instant loss. Highest HP in the game (1100 base). |

### Stretch ideas (swap in if you want more variety)
- **Artichoke** — layered armour: damage reduction drops in stages as you peel it.
- **Jackfruit** — the largest fruit in the world; a colossal mid-run boss.
- **Ghost Pepper** — leaves a burning trail that damages towers it passes.
- **Pomegranate** — splits into seeds on death (breaks the "no supporting mobs"
  rule, so it'd need to be an exception).
- **Wasabi Root** — periodically becomes untargetable (goes "underground").

---

## Implementation status

- ✅ **`MobCatalog`** holds all 45 defs with archetype stats and boss traits.
- ✅ **`TDBalance.Waves`** — 45 waves, one mob each, boss every 5th; health curve
  is now `1.13^(wave-1)` and each wave is a single spawn group.
- ✅ **`Swarm` archetype** plus boss behaviours (regen / armour / slow-immunity /
  enrage / dash) are wired into `Mob`.
- ✅ **Models** — the original 35 built and walk-animated in Blender. **Granola Mom uses a
  dedicated 13-bone humanoid rig** with long hair, arm swing, and bending knees.
  Waves 36–45 are procedural fallback until their `.glb` art lands
  (`Lemon, Lettuce, Zucchini, Chili, Blackberry, Mushroom, Garlic, Grapefruit, Kale, RotKing`).
- ⚠️ **Balance is untuned.** The curve, tower DPS and the economy have never been
  play-tested across a full 45-wave run.
- ⚠️ **Swarm scale is `0.42`**, rendering Raspberry (0.32 tall) at ~0.2 units —
  nearly invisible next to a 2-unit tile. Bumping to ~`0.7` would fix it.
- ℹ️ `Cracker.glb` (the original cracker mob) is now unused and can be deleted.
