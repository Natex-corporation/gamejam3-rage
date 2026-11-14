# Contributing

Thanks for helping improve Rage. Small, focused fixes are easy to review: a corrected translation, a clearer menu interaction, or a reproducible gameplay bug is a good starting point.

## Development

1. Fork the repository and create a branch for your change.
2. Follow [the build guide](docs/BUILDING.md) with your compatible local game installation.
3. Keep changes focused and preserve the existing handwritten, clay-and-paper art direction.
4. Rebuild and playtest the affected behavior.
5. Open a pull request describing the problem, resulting behavior, and what you tested. Include screenshots for visual changes.

## Before a pull request

- Try all four languages if you change UI or dialogue. Preserve placeholders such as `{0}` and `{1}`.
- Check 1280×720 and a compact viewport; include 1024×480 for menu layout changes.
- Check mouse hover and keyboard/controller focus.
- Verify that pause restores both gameplay and audio, with no accidental attack on dismissal.
- Recheck remapped controls and reset-to-default if you touch input.
- Do not commit compiled game files, engine libraries, local paths, backups, credentials, or game assets.

Report bugs with reproduction steps, expected and actual behavior, resolution, language, and input device. See the issue templates for a useful starting point.

By contributing, you agree that your original contributions are provided under the repository's MIT license. Only submit material you have permission to contribute.
