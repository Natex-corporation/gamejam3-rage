# Incremental runs

The game loop is **fight → earn clay → defeat or victory → workshop → stronger next run**. The village story plays on the first journey and is marked seen when it naturally reaches the course. Later runs start in the deepest unlocked district without repeating the conversations.

## Starting strength and rewards

- Player health starts at 3; maximum upgraded health is 15.
- Enemy health is 10, 18, 26, 36, 48, and 60 across the six districts; the Kiln King has 280.
- Every actual enemy health point removed earns 1 clay. Overkill earns no extra, and healing cannot reset an enemy's reward budget.
- An enemy defeat awards another 8 + 4 × the zero-based district index in clay by stage, once per enemy.
- Clearing a stage awards 25 × the one-based district number in clay.
- A failed run awards 4 consolation clay after combat, or after at least five seconds of play. Merely opening and restarting a run awards nothing.

Clay is saved as it is earned. A failed attempt can contribute toward the next purchase even if the player did not defeat an enemy.

![Upgraded clay heart HUD](images/max-hearts.png)

## Workshop

| Buff | Base price | Rank cap | Each rank adds |
| --- | --- | --- | --- |
| Stronger kicks | 20 | 8 | +1 damage on hits against enemies |
| More hearts | 24 | 6 | +2 maximum health |
| Clay shell | 32 | 4 | One blocked hit per run |
| Second wind | 40 | 3 | Heal one heart every 3 / 2 / 1 defeats |
| Clay dash | 45 | 3 | Invulnerable 0.18s burst; cooldown 3.25 / 2.5 / 1.75s |
| Ricochet | 60 | 2 | One / two terrain bounces |
| Forked shots | 80 | 2 | One / two extra pellets, capped at five |
| Rhythm punch | 70 | 3 | Double damage every 6 / 5 / 4 valid hits |

At current rank `r`, the next price is `base + 8*r + 4*r*r`. A purchase is rejected if it is unaffordable, outside the eight upgrade slots, or already capped. Each run snapshots the bought ranks at its start, so buying during a pause cannot refill health or shells mid-fight.

## Completing a run

The exit checks all active living enemies in that stage. Defeating only a few enemies and running past the rest no longer finishes the game. Each stage has its own clear reward, and the final clear opens victory results. Defeat and falling out of the level open results instead of immediately reloading. Both results lead to the workshop or a new run.

The upper shelf is a healing rest point, once per stage. It does not preserve stage position after a failed run. The HUD points toward the nearest remaining enemy, and enemies knocked below the world are retired so an unreachable enemy cannot keep an exit locked.

## Save behavior

Progression is stored in one versioned JSON value under Unity PlayerPrefs key `RageRefinement.Progression.v1`, with the last valid value kept as a backup. It contains clay, eight upgrade ranks, deepest unlocked district, attempt and victory counts, and whether the village story has been seen. Rank and wallet bounds are checked on load. Ordinary options and keyboard bindings use their existing separate preference keys.

Integration runs pass `-rage-qa-profile <name>` to use a different save key. The smoke suite deliberately resets only its named QA profile; it never resets the player's normal progression.

Older four-rank saves migrate by preserving their original ranks and adding four zero-rank tricks. Each cleared district permanently unlocks the next. Choose district can restart from any unlocked district; after victory the normal Play action starts a full expedition again. Boss damage uses the same finite reward budget, so healing cannot be farmed for unlimited clay.
