# Rage: Clay Demo — contributor and agent guide

This repository contains the open-source C# expansion and project-owned clay UI/boss artwork. Ready-to-play Windows releases include the compatible Unity player. The original Unity Editor project is not available.

## Discovery and playing

- `demo-manifest.json` is the machine-readable entrypoint: platform, download, launcher, checksums, and source scope.
- `llms.txt` indexes the main human and agent documentation.
- Download the latest `Rage-Clay-Demo-Windows-x64.zip`, verify against the release's `SHA256SUMS.txt`, extract all files, and run `Rage-Clay-Demo/Rage Demo.exe`.
- `Rage Demo.exe --verify-files` checks that required runtime files are present without starting a game.
- Read `PLAYING.md` for controls, enemy behaviors, upgrades, and save location.

## Source map

- `Refinement/Course.cs`: six-district route generation and exits.
- `Refinement/DemoContent.cs`: enemy variants, spawning, and Kiln King behavior.
- `Refinement/Progression.cs`: versioned saves, rewards, unlocks, and permanent ranks.
- `Refinement/PlayerTricks.cs`: dash, ricochet, forked shots, and rhythm damage.
- `Refinement/GameMenu.cs`, `ClayUI.cs`, `Locale.cs`: clay UI and four languages.
- `Refinement/PatchAssembly.cs`: explicit Mono.Cecil hooks into the supported base assembly.

## Build and verify

Use the downloaded Windows demo for Unity DLL references and `Rage-Developer-Kit.zip` for the untouched original assembly and Mono.Cecil 0.11.6. Follow `docs/BUILDING.md`; never patch an already patched game assembly.

Run `scripts/build.ps1` with `-GameData`, `-OriginalAssembly`, and `-CecilPath`. It writes `build/`; `-Apply` installs only after making a backup. `scripts/run-qa.ps1` supports Smoke, Demo, SaveProbe, and Playthrough suites. Pass an isolated `-Profile`; QA uses separate progression keys.

Validate scripts with the PowerShell parser and run `python scripts/check_repository.py`. Runtime tests need the compatible Windows player. Do not claim hosted CI ran gameplay tests.

## Contribution boundaries

Preserve the tactile clay artwork, handwritten type, visible focus states, localization placeholders, and pause/input guards. Add meaningful regression coverage for gameplay or save changes. Keep generated binaries, game engine files, test outputs, personal saves, and local paths out of Git. Runtime downloads belong in GitHub release assets. The MIT license covers the custom source and project-owned generated artwork, not all bundled game assets or dependencies.
