[CmdletBinding()]
param(
    [string]$GameData = (Split-Path $PSScriptRoot -Parent),
    [Parameter(Mandatory=$true)][string]$OriginalAssembly,
    [Parameter(Mandatory=$true)][string]$CecilPath,
    [string]$Version = '0.2.0'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$GameData = (Resolve-Path -LiteralPath $GameData).Path
$OriginalAssembly = (Resolve-Path -LiteralPath $OriginalAssembly).Path
$CecilPath = (Resolve-Path -LiteralPath $CecilPath).Path
$player = Split-Path $GameData -Parent
$output = Join-Path $repo 'build'
$package = Join-Path $output 'release\Rage-Clay-Demo'
$data = Join-Path $package 'GameJam3_Rage_Data'
New-Item -ItemType Directory -Path $data -Force | Out-Null
foreach ($name in 'GameJam3_Rage.exe','UnityPlayer.dll','dstorage.dll','dstoragecore.dll') {
    $source = Join-Path $player $name
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $package -Force }
}
Copy-Item -LiteralPath (Join-Path $player 'MonoBleedingEdge') -Destination $package -Recurse -Force
foreach ($name in 'Managed','Plugins','Resources','StreamingAssets','UnitySubsystems') {
    $source = Join-Path $GameData $name
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $data -Recurse -Force }
}
Get-ChildItem -LiteralPath $GameData -File | Where-Object {
    $_.Name -match '^(level\d+|globalgamemanagers|boot\.config|app\.info)$' -or $_.Extension -in '.assets','.resS','.resource','.json'
} | Copy-Item -Destination $data -Force
Copy-Item -LiteralPath (Join-Path $output 'Assembly-CSharp.dll'),(Join-Path $output 'RageRefinement.dll') -Destination (Join-Path $data 'Managed') -Force
Copy-Item -LiteralPath (Join-Path $output 'RuntimeInitializeOnLoads.json'),(Join-Path $output 'ScriptingAssemblies.json') -Destination $data -Force
New-Item -ItemType Directory -Path (Join-Path $data 'Refinement\Assets') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'assets\clay-ui.png'),(Join-Path $repo 'assets\kiln-king.png') -Destination (Join-Path $data 'Refinement\Assets') -Force
$assemblies = Get-Content -LiteralPath (Join-Path $data 'ScriptingAssemblies.json') -Raw | ConvertFrom-Json
if ($assemblies.names -contains 'RageBench.dll') { throw 'QA registrations must not be shipped.' }
$testDll = Join-Path $data 'Managed\RageBench.dll'
if (Test-Path -LiteralPath $testDll) { Remove-Item -LiteralPath $testDll -Force }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /r:System.Windows.Forms.dll "/out:$package\Rage Demo.exe" (Join-Path $PSScriptRoot 'DemoLauncher.cs')
if ($LASTEXITCODE) { throw 'Launcher compilation failed.' }
Copy-Item -LiteralPath (Join-Path $repo 'PLAYING.md') -Destination (Join-Path $package 'START-HERE.txt') -Force
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE'),(Join-Path $repo 'THIRD_PARTY_NOTICES.md') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $repo 'demo-manifest.json') -Destination $package -Force
$verify = Start-Process -FilePath (Join-Path $package 'Rage Demo.exe') -ArgumentList '--verify-files' -Wait -PassThru -WindowStyle Hidden
if ($verify.ExitCode) { throw 'The standalone demo is missing required files.' }
$kit = Join-Path $output 'release\Rage-Developer-Kit'
New-Item -ItemType Directory -Path (Join-Path $kit 'Original') -Force | Out-Null
Copy-Item -LiteralPath $OriginalAssembly -Destination (Join-Path $kit 'Original\Assembly-CSharp.dll') -Force
Copy-Item -LiteralPath $CecilPath -Destination (Join-Path $kit 'Mono.Cecil.dll') -Force
$license = Join-Path $output 'Mono.Cecil.LICENSE.txt'
if (!(Test-Path -LiteralPath $license)) { Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/jbevain/cecil/0.11.6/LICENSE.txt' -OutFile $license }
Copy-Item -LiteralPath $license -Destination $kit -Force
Copy-Item -LiteralPath (Join-Path $repo 'docs\BUILDING.md') -Destination (Join-Path $kit 'BUILDING.md') -Force
$artifacts = Join-Path $output 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$zip = Join-Path $artifacts 'Rage-Clay-Demo-Windows-x64.zip'
$devzip = Join-Path $artifacts 'Rage-Developer-Kit.zip'
Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal -Force
Compress-Archive -LiteralPath $kit -DestinationPath $devzip -CompressionLevel Optimal -Force
$hashes = foreach ($file in $zip,$devzip) { (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path $file -Leaf) }
[IO.File]::WriteAllLines((Join-Path $artifacts 'SHA256SUMS.txt'),$hashes)
$manifest = Get-Content -LiteralPath (Join-Path $repo 'demo-manifest.json') -Raw | ConvertFrom-Json
$manifest.version = $Version
$manifest | Add-Member -NotePropertyName source_commit -NotePropertyValue (git -C $repo rev-parse HEAD) -Force
$manifest | Add-Member -NotePropertyName sha256 -NotePropertyValue (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() -Force
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $artifacts 'demo-manifest.json') -Encoding utf8
Get-ChildItem -LiteralPath $artifacts | Select-Object Name,Length
