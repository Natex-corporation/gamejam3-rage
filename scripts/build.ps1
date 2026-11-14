[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameData,
    [Parameter(Mandatory = $true)][string]$OriginalAssembly,
    [Parameter(Mandatory = $true)][string]$CecilPath,
    [string]$Output = (Join-Path $PSScriptRoot '..\build'),
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$GameData = (Resolve-Path -LiteralPath $GameData).Path
$OriginalAssembly = (Resolve-Path -LiteralPath $OriginalAssembly).Path
$CecilPath = (Resolve-Path -LiteralPath $CecilPath).Path
$Output = [IO.Path]::GetFullPath($Output)
$expected = '763B23D3013908AA3197A77E320852DC2261180A97ABA7DDD08978B2E9B80B74'
if ((Get-FileHash -LiteralPath $OriginalAssembly -Algorithm SHA256).Hash -ne $expected) {
    throw 'Unsupported original assembly. Use the untouched Assembly-CSharp.dll from the supported game build; see docs/BUILDING.md.'
}
if ((Split-Path $OriginalAssembly -Parent) -eq $Output -or $Output -eq (Join-Path $GameData 'Managed')) {
    throw 'Output must be separate from the original assembly and installed Managed directory.'
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Install the .NET Framework 4.x development tools (csc.exe is required).' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$names = 'mscorlib','netstandard','System','System.Core','UnityEngine.CoreModule','UnityEngine.ScriptingModule',
    'UnityEngine.UIElementsModule','UnityEngine.TextRenderingModule','UnityEngine.TextCoreTextEngineModule','UnityEngine.ImageConversionModule',
    'UnityEngine.InputLegacyModule','UnityEngine.AudioModule','UnityEngine.Physics2DModule','UnityEngine.AnimationModule','UnityEngine.MathematicsModule',
    'Unity.InputSystem','Unity.RenderPipelines.Core.Runtime','Unity.RenderPipelines.Universal.Runtime','Unity.RenderPipelines.GPUDriven.Runtime'
$references = $names | ForEach-Object {
    $path = Join-Path $GameData "Managed\$_.dll"
    if (!(Test-Path -LiteralPath $path)) { throw "Missing game reference: $path" }
    "/r:$path"
}
$references += "/r:$OriginalAssembly"
$sources = Get-ChildItem -LiteralPath (Join-Path $repo 'Refinement') -Filter '*.cs' |
    Where-Object Name -ne 'PatchAssembly.cs' | ForEach-Object FullName
& $compiler /nologo /noconfig /nostdlib+ /target:library "/out:$Output\RageRefinement.dll" @references @sources
if ($LASTEXITCODE) { throw 'Refinement compilation failed.' }
Copy-Item -LiteralPath $CecilPath -Destination (Join-Path $Output 'Mono.Cecil.dll') -Force
& $compiler /nologo /target:exe "/r:$CecilPath" "/out:$Output\PatchAssembly.exe" (Join-Path $repo 'Refinement\PatchAssembly.cs')
if ($LASTEXITCODE) { throw 'Patcher compilation failed.' }
& (Join-Path $Output 'PatchAssembly.exe') $OriginalAssembly (Join-Path $Output 'RageRefinement.dll') (Join-Path $Output 'Assembly-CSharp.dll')
if ($LASTEXITCODE) { throw 'Assembly patching failed.' }
$artwork = Join-Path $Output 'Refinement\Assets'
New-Item -ItemType Directory -Path $artwork -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'assets\clay-ui.png') -Destination $artwork -Force

# Keep existing player registrations and register this layer only once.
$assemblies = Get-Content -LiteralPath (Join-Path $GameData 'ScriptingAssemblies.json') -Raw | ConvertFrom-Json
if ($assemblies.names -notcontains 'RageRefinement.dll') {
    $assemblies.names = @($assemblies.names) + 'RageRefinement.dll'
    $assemblies.types = @($assemblies.types) + 16
}
$runtime = Get-Content -LiteralPath (Join-Path $GameData 'RuntimeInitializeOnLoads.json') -Raw | ConvertFrom-Json
$runtime.root = @($runtime.root | Where-Object assemblyName -ne 'RageRefinement') + @(
    [ordered]@{ assemblyName='RageRefinement'; nameSpace='RageRefinement'; className='GameMenu'; methodName='Install'; loadTypes=0; isUnityClass=$false }
)
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $Output 'ScriptingAssemblies.json'), ($assemblies | ConvertTo-Json -Depth 8 -Compress), $utf8)
[IO.File]::WriteAllText((Join-Path $Output 'RuntimeInitializeOnLoads.json'), ($runtime | ConvertTo-Json -Depth 8 -Compress), $utf8)
if ($Apply) {
    if (Get-Process -Name GameJam3_Rage -ErrorAction SilentlyContinue) { throw 'Close the game before applying. Build output is ready.' }
    $backup = Join-Path $GameData ('Refinement\Backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    foreach ($file in 'Assembly-CSharp.dll','RageRefinement.dll') {
        $installed = Join-Path $GameData "Managed\$file"
        if (Test-Path -LiteralPath $installed) { Copy-Item -LiteralPath $installed -Destination $backup }
        Copy-Item -LiteralPath (Join-Path $Output $file) -Destination $installed -Force
    }
    foreach ($file in 'ScriptingAssemblies.json','RuntimeInitializeOnLoads.json') {
        Copy-Item -LiteralPath (Join-Path $GameData $file) -Destination $backup
        Copy-Item -LiteralPath (Join-Path $Output $file) -Destination (Join-Path $GameData $file) -Force
    }
    $installedArt = Join-Path $GameData 'Refinement\Assets'
    $previousArt = Join-Path $installedArt 'clay-ui.png'
    if (Test-Path -LiteralPath $previousArt) { Copy-Item -LiteralPath $previousArt -Destination (Join-Path $backup 'clay-ui.png') }
    New-Item -ItemType Directory -Path $installedArt -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $artwork 'clay-ui.png') -Destination $installedArt -Force
    Write-Host "Installed. Previous files: $backup"
}
Write-Host "Build ready: $Output"
