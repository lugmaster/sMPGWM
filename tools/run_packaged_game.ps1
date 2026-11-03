Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$godotExe = 'C:\Users\lober\godot_engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe'
$repositoryPath = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repositoryPath 'sMPGWM.Game'
$exportsPath = 'C:\Users\lober\Documents\Godot\Exports'
$destinationPath = 'G:\Andere Computer\Mein Laptop\flatEarthUniverse\game_exports'

if (-not (Test-Path -LiteralPath $godotExe -PathType Leaf)) {
    throw "Godot executable missing: $godotExe"
}

$commitId = git -C $repositoryPath rev-parse --verify HEAD
if ($LASTEXITCODE -ne 0) { throw 'Could not read the current Git commit.' }
$shortCommitId = $commitId.Trim().Substring(0, 9)
$archiveName = "sMPGWM_$shortCommitId.zip"

# A fresh export folder prevents old files from entering the archive.
$exportPath = Join-Path $exportsPath ("sMPGWM_" + $shortCommitId + "_" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $exportPath -Force | Out-Null
$gameExe = Join-Path $exportPath 'sMPGWM.exe'

# Use Godot's built-in exporter with the project's Windows Desktop preset.
$exportArguments = @('--headless', '--path', ('"' + $projectPath + '"'),
    '--export-debug', '"Windows Desktop"', ('"' + $gameExe + '"'))
$exportProcess = Start-Process -FilePath $godotExe -ArgumentList $exportArguments -NoNewWindow -Wait -PassThru
if ($exportProcess.ExitCode -ne 0) {
    throw "Godot export failed with exit code $($exportProcess.ExitCode)."
}
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    throw "Export did not produce $gameExe"
}

# ZipFile includes hidden files and all generated subdirectories.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$temporaryZip = Join-Path $exportsPath ([guid]::NewGuid().ToString('N') + '.zip')
[IO.Compression.ZipFile]::CreateFromDirectory($exportPath, $temporaryZip)
$zipPath = Join-Path $exportsPath $archiveName
Move-Item -LiteralPath $temporaryZip -Destination $zipPath -Force

New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
Copy-Item -LiteralPath $zipPath -Destination (Join-Path $destinationPath $archiveName) -Force
Write-Host "ZIP saved: $zipPath"
Write-Host "ZIP copied: $(Join-Path $destinationPath $archiveName)"
