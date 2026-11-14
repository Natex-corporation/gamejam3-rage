[CmdletBinding()]
param(
    [ValidateSet('Smoke','Playthrough','SaveProbe','Demo')][string]$Suite = 'Smoke',
    [string]$GameData = (Split-Path $PSScriptRoot -Parent),
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Profile = ('incremental-' + $Suite.ToLowerInvariant()),
    [int]$TimeoutSeconds = 900,
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$build = Join-Path $repo 'build'
$GameData = (Resolve-Path -LiteralPath $GameData).Path
$player = Split-Path $GameData -Parent
$fixture = Join-Path $build ('qa-' + $Suite.ToLowerInvariant())
$data = Join-Path $fixture 'GameJam3_Rage_Data'
New-Item -ItemType Directory -Path $data -Force | Out-Null
foreach ($name in 'GameJam3_Rage.exe','UnityPlayer.dll','dstorage.dll','dstoragecore.dll') {
    $source = Join-Path $player $name
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $fixture -Force }
}
$mono = Join-Path $player 'MonoBleedingEdge'
if (!(Test-Path -LiteralPath (Join-Path $fixture 'MonoBleedingEdge'))) { Copy-Item -LiteralPath $mono -Destination $fixture -Recurse }
foreach ($name in 'Managed','Plugins','Resources','StreamingAssets','UnitySubsystems') {
    $source = Join-Path $GameData $name
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $data -Recurse -Force }
}
Get-ChildItem -LiteralPath $GameData -File | Where-Object {
    $_.Name -match '^(level\d+|globalgamemanagers|boot\.config|app\.info)$' -or
    $_.Extension -in '.assets','.resS','.resource','.json'
} | Copy-Item -Destination $data -Force
Copy-Item -LiteralPath (Join-Path $build 'Assembly-CSharp.dll'),(Join-Path $build 'RageRefinement.dll') -Destination (Join-Path $data 'Managed') -Force
Copy-Item -LiteralPath (Join-Path $build 'RuntimeInitializeOnLoads.json'),(Join-Path $build 'ScriptingAssemblies.json') -Destination $data -Force
New-Item -ItemType Directory -Path (Join-Path $data 'Refinement\Assets') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $build 'Refinement\Assets\clay-ui.png') -Destination (Join-Path $data 'Refinement\Assets') -Force
Copy-Item -LiteralPath (Join-Path $build 'Refinement\Assets\kiln-king.png') -Destination (Join-Path $data 'Refinement\Assets') -Force

$names = 'mscorlib','netstandard','System','System.Core','UnityEngine.CoreModule','UnityEngine.ScriptingModule',
    'UnityEngine.MathematicsModule','UnityEngine.UIElementsModule','UnityEngine.TextRenderingModule',
    'UnityEngine.TextCoreTextEngineModule','UnityEngine.Physics2DModule','UnityEngine.AnimationModule',
    'UnityEngine.ScreenCaptureModule','UnityEngine.JSONSerializeModule','Unity.InputSystem','Assembly-CSharp','RageRefinement'
$references = $names | ForEach-Object { '/r:' + (Join-Path $data "Managed\$_.dll") }
$sources = if ($Suite -eq 'Playthrough') { @('FullPlaythrough.cs') }
    elseif ($Suite -eq 'SaveProbe') { @('SaveProbe.cs') }
    elseif ($Suite -eq 'Demo') { @('DemoChecks.cs') }
    else { @('VisualSmoke.cs','ProjectileChecks.cs','ProgressionChecks.cs') }
$sources = @($sources | ForEach-Object { Join-Path $PSScriptRoot $_ })
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /noconfig /nostdlib+ /target:library "/out:$data\Managed\RageBench.dll" @references @sources
if ($LASTEXITCODE) { throw 'QA runner compilation failed.' }
$runtimePath = Join-Path $data 'RuntimeInitializeOnLoads.json'
$runtime = Get-Content -LiteralPath $runtimePath -Raw | ConvertFrom-Json
$runtime.root = @($runtime.root | Where-Object assemblyName -ne 'RageBench') + @(
    [ordered]@{ assemblyName='RageBench'; nameSpace='RageBench'; className='Runner'; methodName='StartBenchmark'; loadTypes=0; isUnityClass=$false }
)
$assembliesPath = Join-Path $data 'ScriptingAssemblies.json'
$assemblies = Get-Content -LiteralPath $assembliesPath -Raw | ConvertFrom-Json
if ($assemblies.names -notcontains 'RageBench.dll') {
    $assemblies.names = @($assemblies.names) + 'RageBench.dll'
    $assemblies.types = @($assemblies.types) + 16
}
[IO.File]::WriteAllText($runtimePath, ($runtime | ConvertTo-Json -Depth 8 -Compress))
[IO.File]::WriteAllText($assembliesPath, ($assemblies | ConvertTo-Json -Depth 8 -Compress))
$log = Join-Path $fixture 'qa.log'
$windowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
$process = Start-Process -FilePath (Join-Path $fixture 'GameJam3_Rage.exe') -WorkingDirectory $fixture -WindowStyle $windowStyle -PassThru -ArgumentList @(
    '-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
    '-rage-qa-profile',$Profile,'-logFile',('"{0}"' -f $log)
)
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while (!$process.WaitForExit(30000)) {
    if ([DateTime]::UtcNow -gt $deadline) { $process.Kill(); throw "QA timeout: $log" }
    Get-Content -LiteralPath $log -Tail 3
}
Select-String -LiteralPath $log -Pattern 'RUN COMPLETE|RUN FAIL|RUN DEFEAT|RUN BOUGHT|SMOKE|PROJECTILE|ECONOMY|SAVE PROBE|DEMO|Exception' -CaseSensitive | ForEach-Object Line
if ($process.ExitCode -ne 0) { throw "QA failed with exit $($process.ExitCode). Log: $log" }
Write-Output "QA passed. Log and screenshots: $fixture"
