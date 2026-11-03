param([switch]$Impaired, [switch]$Lifecycle, [switch]$ReverseJoin, [ValidateRange(3,10)][int]$Players = 3)
$ErrorActionPreference = 'Stop'
$game = Join-Path $PSScriptRoot '../sMPGWM.Game'
$exe = 'C:/Users/lober/godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$logs = Join-Path $env:TEMP ('sMPGWM-synchronization-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $logs | Out-Null
Write-Output "Detailed logs: $logs"
$processes = @()
try {
    $roles = if ($ReverseJoin) { @('host','b','a') } else { @('host','a','b') }
    if ($Players -gt 3) { $roles += 3..($Players - 1) | ForEach-Object { 'client' + $_ } }
    foreach ($role in $roles) {
        $args = @('--headless','--path', $game, '--script','../sMPGWM.Tests/Network/synchronization_smoke.gd','--',$role)
        $args += 'players=' + $Players
        if ($Impaired) { $args += "impaired" }
        if ($Lifecycle) { $args += "lifecycle" }
        $processes += Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru -RedirectStandardOutput "$logs/$role.log" -RedirectStandardError "$logs/$role.err"
        Start-Sleep -Seconds 2
    }
    foreach ($process in $processes) {
        if (!$process.WaitForExit(30000)) { throw 'Smoke timed out' }
        $process.Refresh()
    }
    foreach ($role in $roles) {
        if (Select-String -Path "$logs/$role.err" -Pattern "^ERROR:|CRASH|Unhandled" -Quiet) { throw "$role runtime error; inspect $logs" }
        Get-Content "$logs/$role.log" | Select-String 'SYNC_|ERROR'
        if (!(Select-String -Path "$logs/$role.log" -Pattern 'SYNC_SMOKE passed' -Quiet)) { throw "$role failed" }

    }
} finally {
    foreach ($process in $processes) { if (!$process.HasExited) { $process.Kill() } }
}
