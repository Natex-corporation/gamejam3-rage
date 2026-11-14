# Verification notes

Verified on 2026-11-14 with the compatible Unity 6000.6.6f1 Windows player. All integration profiles were separate from the normal player save.

## Full keyboard-input playthrough

The final progression balance completed the village conversations and all three stages through actual Input System keyboard events. The runner moved, attacked, climbed, entered exits, and purchased upgrades through the workshop UI. It did not grant money, teleport the player, alter enemy health, or force victory.

- Fresh attempt: defeated three enemies, then died with 52 saved clay.
- Workshop: bought Stronger kicks rank 1 and Second wind rank 1; 12 clay remained.
- Next attempt: defeated every enemy in all three stages and reached the victory screen.
- Recorded result: 151 seconds, one defeat, 25 enemies defeated across both attempts, 15 total damage received.

The earlier route stalled because it tried to fight a platform enemy from the ground. Correcting the automated route to fight that enemy after climbing allowed a complete run; no enemy-placement change was needed for that issue.

## Regression suite

The final smoke suite passed:

- Actions, controls, languages, and workshop at 1280×720, 800×600, and 1024×480, across Czech, English, Slovak, and German.
- Result and workshop screens in all four languages, plus a fully upgraded 15-heart HUD.
- Fresh strength, saved partial-damage rewards, exact purchase costs, insufficient funds, duplicate defeat prevention, upgrade caps, and next-run-only effects.
- All four buff types, shell consumption, healing thresholds, defeat results, stage gates, and victory.
- Fallen enemies are retired and cannot keep an exit locked.
- Projectiles leave all four viewport edges, retain partial visibility, follow camera movement, and cannot deal offscreen damage.
- A separate `SaveProbe` process verifies the same saved wallet and buff ranks after restarting the game.

Hosted CI checks PowerShell syntax and repository documentation links. The runtime suites require the compatible local game and can be repeated with [the QA launcher](BUILDING.md#visual-and-gameplay-verification).
