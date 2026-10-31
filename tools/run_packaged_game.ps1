[CmdletBinding()]
param(
    [string]$GodotExe = 'C:\Users\lober\godot_engine\Godot_v4.6.2_mono_steam\editor\godot.windows.editor.x86_64.mono.console.exe',
    [string]$PackagedExe = 'C:\Users\lober\Documents\Godot\Exports\sMPGWM_Exports\sMPGWM.console.exe',
    [string]$LogPath = 'C:\Users\lober\Documents\Godot\sMPGWM\sMPGWM.Tests\bin\Debug\net8.0\logs\host.log',
    [switch]$SkipExport,
    [switch]$ExportOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'sMPGWM.Game'
$PackagedExe = [IO.Path]::GetFullPath($PackagedExe)
$LogPath = [IO.Path]::GetFullPath($LogPath)
$exportDirectory = Split-Path $PackagedExe -Parent

if (-not $SkipExport) {
    if (-not (Test-Path -LiteralPath $GodotExe -PathType Leaf)) { throw "Godot executable missing: $GodotExe" }
    $presetPath = Join-Path $projectPath 'export_presets.cfg'
    if (-not (Test-Path -LiteralPath $presetPath)) { throw "Missing export preset: $presetPath" }
    $preset = Get-Content -LiteralPath $presetPath -Raw
    if ($preset -notmatch 'custom_template/debug="([^"]+)"' -or
        -not (Test-Path -LiteralPath $Matches[1] -PathType Leaf)) {
        throw 'Configure Windows Desktop custom_template/debug with the custom GodotSteam .NET template before exporting.'
    }
    $templatePath = $Matches[1]
    New-Item -ItemType Directory -Force -Path $exportDirectory | Out-Null
    $gameExe = $PackagedExe -replace '\.console\.exe$', '.exe'
    if ($gameExe -eq $PackagedExe) { throw 'PackagedExe must end in .console.exe.' }
    Write-Host 'Exporting a fresh Windows Desktop debug build (including C# publish)...'
    & $GodotExe --headless --path $projectPath --export-debug 'Windows Desktop' $gameExe
    if ($LASTEXITCODE -ne 0) { throw "Godot export failed with exit code $LASTEXITCODE. Game was not launched." }
    if (-not (Test-Path -LiteralPath $PackagedExe -PathType Leaf)) { throw "Export did not produce $PackagedExe" }
    Copy-Item -LiteralPath (Join-Path $projectPath 'steam_appid.txt') -Destination $exportDirectory -Force
    $steamDll = Join-Path (Split-Path $templatePath -Parent) 'steam_api64.dll'
    if (-not (Test-Path -LiteralPath $steamDll)) { throw "Steam runtime missing: $steamDll" }
    Copy-Item -LiteralPath $steamDll -Destination $exportDirectory -Force
    Write-Host "Export complete: $gameExe"
}
if ($ExportOnly) { return }
if (-not (Test-Path -LiteralPath $PackagedExe -PathType Leaf)) { throw "Packaged executable missing: $PackagedExe" }

New-Item -ItemType Directory -Force -Path (Split-Path $LogPath -Parent) | Out-Null
if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
    $oldLog = Get-Item -LiteralPath $LogPath
    $stamp = $oldLog.CreationTime.ToString('yyyyMMdd_HHmmss_fffffff')
    $archiveBase = Join-Path $oldLog.DirectoryName ($oldLog.BaseName + '_' + $stamp)
    $archivePath = $archiveBase + $oldLog.Extension
    $suffix = 1
    while (Test-Path -LiteralPath $archivePath) {
        $archivePath = $archiveBase + '_' + $suffix + $oldLog.Extension
        $suffix++
    }
    Move-Item -LiteralPath $LogPath -Destination $archivePath
    Write-Host "Previous log archived: $archivePath"
}
Write-Host "Game log: $LogPath"
Push-Location $exportDirectory
try {
    & $PackagedExe --log-file $LogPath
    if ($LASTEXITCODE -ne 0) { throw "Game exited with code $LASTEXITCODE. See $LogPath" }
}
finally { Pop-Location }

