# Mob Roster — Fruits & Vegetables

Mobs are now fruits and vegetables. **32 waves**, with a **boss on every 5th
wave** (5, 10, 15, 20, 25, 30). Boss waves contain **only the boss** — no
supporting mobs.

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
| 30 | **BOSS — Dragonfruit** | Boss | ✔ | Final boss |
| 31 | Avocado | Tank | ✔ | Armoured, slow |
| 32 | Final gauntlet | Mixed | – | Reuses earlier mobs in one big push |

**Archetypes used:** Basic, Fast, Tank, Swarm, Boss.
The game currently only has **Basic / Fast / Tank / Boss** — **Swarm** would be
new (fast, low HP, spawned in large numbers).

---

## Bosses (standalone, one per boss wave)

Each boss is a single, large mob with one distinctive gimmick.

| Wave | Boss | Concept | Gimmick |
|---:|---|---|---|
| 5 | Watermelon | Enormous, lazy | Huge HP pool, very slow — a pure damage check |
| 10 | Pumpkin | Jack-o'-lantern | **Regenerates** health slowly, so chip damage isn't enough |
| 15 | Pineapple | Spiky exterior | **Armoured** — flat damage reduction per hit |
| 20 | Durian | Foul and furious | **Enrages** — gets faster as its health drops |
| 25 | Coconut | Hard shell | **Immune to slow**, extremely high HP, very slow |
| 30 | Dragonfruit | Exotic final boss | **Phases** — dashes forward periodically |

### Stretch ideas (pick from these if you want more variety)
- **Artichoke** — layered armour: damage reduction drops in stages as you peel it.
- **Ghost Pepper** — leaves a burning trail that damages towers it passes.
- **Pomegranate** — splits into seeds on death (note: this breaks the
  "no supporting mobs" rule, so it'd need to be an exception).
- **Jackfruit** — the largest mob in the game; takes two lanes of the path.
- **Wasabi Root** — periodically becomes untargetable (goes "underground").

---

## Things this implies (not yet done)

1. **Health scaling must change.** The current curve is `1.55^(wave-1)`; by
   wave 32 that's ~10^6 × base health. A 32-wave run needs a much gentler
   curve (e.g. `1.18^(wave-1)`, or a piecewise ramp with a step per boss).
2. **New archetypes.** `Swarm` is new; `Regen`, `Armour`, `Immune-to-slow` and
   `Phases` are boss-only behaviours that need code.
3. **32 mob models.** One Blender model per entry above (24 regular + 6 bosses
   + the final gauntlet reusing existing ones).
4. **Boss waves** are single-spawn waves — the wave table needs a flag, or the
   boss wave is expressed as a one-entry spawn group.
