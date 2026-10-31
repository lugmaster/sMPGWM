[CmdletBinding()]
param(
    [string]$GodotExe = 'C:\Users\lober\godot_engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe',
    [string]$PackagedExe,
    [string]$ExportPreset,
    [string]$LogPath = 'C:\Users\lober\Documents\Godot\sMPGWM\sMPGWM.Tests\bin\Debug\net8.0\logs\host.log',
    [switch]$SkipExport,
    [switch]$ExportOnly,
    [string[]]$GameArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'sMPGWM.Game'
$presetPath = Join-Path $projectPath 'export_presets.cfg'
if (-not (Test-Path -LiteralPath $presetPath -PathType Leaf)) { throw "Missing export preset: $presetPath" }
$sections = @{}
$section = ''
foreach ($line in Get-Content -LiteralPath $presetPath) {
    if ($line -match '^\[([^\]]+)\]$') {
        $section = $Matches[1]
        $sections[$section] = @{}
    }
    elseif ($line -match '^([^=]+)="(.*)"$' -and $section) {
        $sections[$section][$Matches[1].Trim('"')] = $Matches[2]
    }
}
if (-not $ExportPreset) {
    if (-not $sections.ContainsKey('runnable_presets') -or
        -not $sections['runnable_presets'].ContainsKey('Windows Desktop')) {
        throw 'Select a runnable Windows Desktop preset in export_presets.cfg, or pass -ExportPreset.'
    }
    $ExportPreset = $sections['runnable_presets']['Windows Desktop']
}
$selectedPresets = @($sections.Keys | Where-Object {
    $_ -match '^preset\.\d+$' -and $sections[$_]['name'] -eq $ExportPreset
})
if ($selectedPresets.Count -ne 1) { throw "Export preset not found or ambiguous: $ExportPreset" }
$selectedPreset = $sections[$selectedPresets[0]]
if ($selectedPreset['platform'] -ne 'Windows Desktop') { throw 'This launcher requires a Windows Desktop preset.' }
if (-not $PackagedExe) {
    $configuredPath = $selectedPreset['export_path']
    if (-not $configuredPath) { throw "Configure export_path for preset $ExportPreset." }
    if (-not [IO.Path]::IsPathRooted($configuredPath)) { $configuredPath = Join-Path $projectPath $configuredPath }
    $PackagedExe = $configuredPath -replace '\.exe$', '.console.exe'
}
$PackagedExe = [IO.Path]::GetFullPath($PackagedExe)
$LogPath = [IO.Path]::GetFullPath($LogPath)
$exportDirectory = Split-Path $PackagedExe -Parent

if (-not $SkipExport) {
    if (-not (Test-Path -LiteralPath $GodotExe -PathType Leaf)) { throw "Godot executable missing: $GodotExe" }
    # GodotSteam is now a GDExtension. Godot resolves the standard .NET
    # export template and packages the extension's declared dependencies.
    New-Item -ItemType Directory -Force -Path $exportDirectory | Out-Null
    $gameExe = $PackagedExe -replace '\.console\.exe$', '.exe'
    if ($gameExe -eq $PackagedExe) { throw 'PackagedExe must end in .console.exe.' }
    Write-Host "Exporting preset '$ExportPreset' (including C# publish)..."
    $exportArguments = @('--headless', '--path', ('"' + $projectPath + '"'), '--export-debug', ('"' + $ExportPreset + '"'), ('"' + $gameExe + '"'))
    $exportProcess = Start-Process -FilePath $GodotExe -ArgumentList $exportArguments -NoNewWindow -Wait -PassThru
    if ($exportProcess.ExitCode -ne 0) { throw "Godot export failed with exit code $($exportProcess.ExitCode). Game was not launched." }
    if (-not (Test-Path -LiteralPath $PackagedExe -PathType Leaf)) { throw "Export did not produce $PackagedExe" }
    Copy-Item -LiteralPath (Join-Path $projectPath 'steam_appid.txt') -Destination $exportDirectory -Force
    $steamDll = Join-Path $projectPath 'addons/godotsteam/win64/steam_api64.dll'
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
    $launchArguments = @('--log-file', ('"' + $LogPath + '"')) + @($GameArguments | ForEach-Object { '"' + $_ + '"' })
    $gameProcess = Start-Process -FilePath $PackagedExe -ArgumentList $launchArguments -NoNewWindow -Wait -PassThru
    if ($gameProcess.ExitCode -ne 0) { throw "Game exited with code $($gameProcess.ExitCode). See $LogPath" }
}
finally { Pop-Location }

