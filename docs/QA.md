# Demo verification

Verified on 2026-11-14 with Unity 6000.6.6f1 on Windows. Runtime suites use isolated PlayerPrefs profiles. Hosted CI checks documentation, metadata, source boundaries, and PowerShell syntax; it does not run the game.

## Fresh input-driven expedition

The runner completed the village conversations and all six districts, defeated the two-phase Kiln King, cleared its reinforcements, and reached victory using Input System keyboard events. It moved, jumped, attacked, dashed, entered exits, and bought upgrades through the workshop UI. It did not grant currency, teleport, change enemy health, or force completion.

Recorded result: **347 seconds, three defeats, 58 enemies defeated, 53 total damage received**. The first attempt ended after three kills with 58 clay; the player bought damage and health. Later attempts bought body buffs and tricks and resumed unlocked districts. This is an automated regression result, not a measured human playtime or a promise of demo length.

This final expedition ran after the boss draw-layer/collider and generated-exit fixes, against release source commit `9e74ec6896811ae95598dacec0a1cddf0e6af7ac`.

## Regression coverage

- Actions, controls, languages, body workshop, tricks, and district map in Czech, English, Slovak, and German at 1280×720, 800×600, and 1024×480; result screens and a 15-heart HUD.
- Currency, prices, insufficient funds, caps, duplicate reward prevention, next-run snapshots, shells, healing, defeat results, six district gates, and fallen-enemy retirement.
- Projectile cleanup on all four camera edges, partial visibility, camera movement, onscreen damage, and rejection of offscreen damage.
- Four-rank save migration, trick purchases, directional shield damage, mender healing, and immediate splitter-child exit accounting.
- Actual Shift dash movement/invulnerability/cooldown, forked attack cloning, a real projectile ricochet, rhythm sixth-hit damage, 280-health boss, phase-two reinforcements, telegraphs, and emitted attacks.
- Thirty generated-layout samples verify exits remain on supported ground before the end wall; patched stay callbacks advance all six district gates after the final enemy dies. A fresh replay exposed the old randomized exit crossing the wall, which is now constrained.
- Boss artwork uses the original enemy draw layer and one fitted collision body; visual review caught and corrected background occlusion and a duplicate collider that made the sculpture float.
- Standalone launcher and required-file verification; QA helpers are excluded from release registration.

Run the suites using [BUILDING.md](BUILDING.md). Use the same profile for Smoke followed by SaveProbe to verify persistence across processes. `-Visible` enables reliable captures where hidden-window rendering is suppressed. Targeted suites use controlled scene transitions and damage; they are separate from the input-driven expedition.

Controller navigation and input bindings are supported, but this verification used keyboard/mouse rather than a physical gamepad. Long-term human balance and broad hardware compatibility remain unmeasured.
