# Building and installing

## Requirements

- Windows with the .NET Framework 4.x C# compiler at `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
- The compatible GameJam3_Rage Windows player, built with Unity **6000.6.6f1**.
- The **untouched original** `Assembly-CSharp.dll`, kept separately from the installed patched DLL.
- [Mono.Cecil 0.11.6](https://www.nuget.org/packages/Mono.Cecil/0.11.6). Download the package, extract it as a ZIP, and use `lib/net40/Mono.Cecil.dll`.

No Unity Editor or .NET SDK is required for this refinement build. Mono.Cecil and Unity libraries are not vendored in this repository.

## Get the reproducible inputs

Download both ZIPs from [the latest release](https://github.com/Natex-corporation/gamejam3-rage/releases/latest) and compare their hashes with `SHA256SUMS.txt`. Extract them completely. Use `Rage-Clay-Demo/GameJam3_Rage_Data` as `-GameData`, `Rage-Developer-Kit/Original/Assembly-CSharp.dll` as `-OriginalAssembly`, and `Rage-Developer-Kit/Mono.Cecil.dll` as `-CecilPath`. Clone this source repository separately. Playing only requires the Windows demo ZIP.

## Compatibility

The patcher targets specific methods and fields from one player build. The build script requires this SHA-256 for the original assembly:

```text
763B23D3013908AA3197A77E320852DC2261180A97ABA7DDD08978B2E9B80B74
```

Do not feed an already patched assembly into the patcher. A different build can have different IL and needs a deliberate port. The script rejects mismatches before changing installed files.

## Build

Run from the repository root:

```powershell
.\scripts\build.ps1 `
  -GameData 'C:\Games\Rage\GameJam3_Rage_Data' `
  -OriginalAssembly 'C:\Games\Rage\Original\Assembly-CSharp.dll' `
  -CecilPath 'C:\Tools\Mono.Cecil\lib\net40\Mono.Cecil.dll'
```

The `build/` directory contains the refinement DLL, patched game DLL, updated runtime registration manifests, local patcher tools, and both clay sprites under `Refinement/Assets/`. Existing entries in the game's manifests are preserved. No original game art or engine files are copied into the source repository.

## Install and undo

Close the game and repeat the command with `-Apply`. The script saves the current DLLs and both registration manifests to a timestamped `Refinement/Backups/` folder inside the supplied game data directory before replacing them.

To undo, close the game, copy the backed-up `Assembly-CSharp.dll` and `RageRefinement.dll` (if present) into `Managed/`, and restore both JSON files to the game data root. Restore both clay sprites to `Refinement/Assets/` if it exists in the backup. If the backup had no refinement DLL, the restored manifests no longer reference it, and you may remove that extra DLL.

Build a fresh source checkout against your own installation. The original game files, historical local backups, compiled DLLs, and generated outputs are ignored by Git.

## Visual and gameplay verification

Check the title, options, controls, languages, confirmations, pause, and course finish. Use all four languages at 1280×720, 800×600, and 1024×480. Verify keyboard focus, hover, controller navigation, rebinding, and pause/resume during conversation and combat.

After building, the reusable QA launcher makes a separate complete player copy, compiles and registers its test runner, copies the clay artwork, and launches with a separate progression profile:

```powershell
.\scripts\run-qa.ps1 -Suite Smoke -GameData 'C:\Games\Rage\GameJam3_Rage_Data' -Profile smoke
.\scripts\run-qa.ps1 -Suite SaveProbe -GameData 'C:\Games\Rage\GameJam3_Rage_Data' -Profile smoke
.\scripts\run-qa.ps1 -Suite Playthrough -GameData 'C:\Games\Rage\GameJam3_Rage_Data' -Profile fresh-playthrough
```

Add `-Visible` for screenshot captures on systems that suppress hidden-window rendering. The Demo suite checks the new enemies, save migration, all four tricks, and boss mechanics:

```powershell
.\scripts\run-qa.ps1 -Suite Demo -GameData 'C:\Games\Rage\GameJam3_Rage_Data' -Profile demo-check -Visible
```

The smoke suite checks six menu screens in four languages and three resolutions, result and workshop layouts, fifteen-heart wrapping, projectile cleanup and damage, purchase costs, insufficient funds, caps, saved currency, next-run effects, shells, healing, stage gates, and enemies falling below the level. `SaveProbe` launches a new process and compares the smoke profile with the value saved by the previous process. Use the same profile for those two suites.

The playthrough suite uses Input System keyboard events to finish the village conversations, fight enemies, jump between platforms, buy affordable buffs through the workshop UI after defeat, and reach the ending. It does not grant currency, teleport the player, change enemy health, or force stage completion. Use a new profile name for a genuinely fresh start. The smoke suite does use controlled damage and scene transitions to test specific regressions; it is separate from the playthrough.

Logs and screenshots are written below `build/qa-<suite>/`. Do not register these runners in your everyday game installation. The ordinary player's progression key is never used by this launcher.

The hosted repository check validates PowerShell syntax and repository documentation. Building and runtime verification require local game files and are not performed by hosted CI.

## Limits of this checkout

The source rebuilds the refinement layer and its game-code hooks. It does not reconstruct the original Unity project, serialized assets, or historical asset-level typography edits. The UI reuses the game's existing StrawberryMuffins font; if that asset is unavailable at runtime, it falls back to Unity's built-in font.

## Package a standalone release

After a successful build, run `scripts/package-demo.ps1` with the same `-GameData`, `-OriginalAssembly`, and `-CecilPath` arguments. It writes the complete Windows player ZIP, developer-kit ZIP with original assembly/Cecil license, checksums, and a source-commit manifest under `build/artifacts/`. It rejects QA registrations. Commit source before packaging so the manifest identifies the release commit. Upload all four artifacts together. Never commit player binaries to Git.
