param([switch]$WaitForMatrix) # TEST-SUPPORT: PERF-FOLLOWUP-1 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
$root = Split-Path -Parent $PSScriptRoot # TEST-SUPPORT: PERF-FOLLOWUP-2 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Set-Location -LiteralPath $root # TEST-SUPPORT: PERF-FOLLOWUP-3 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
$destination = 'docs/measurements/2026-11-12-full' # TEST-SUPPORT: PERF-FOLLOWUP-4 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
if ($WaitForMatrix) { # TEST-SUPPORT: PERF-FOLLOWUP-5 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    $deadline = (Get-Date).AddHours(2) # TEST-SUPPORT: PERF-FOLLOWUP-6 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    while ($true) { # TEST-SUPPORT: PERF-FOLLOWUP-7 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if ((Get-Date) -gt $deadline) { throw 'Main matrix did not finish.' } # TEST-SUPPORT: PERF-FOLLOWUP-8 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        $manifest = Join-Path $destination 'release-matrix/runs.json' # TEST-SUPPORT: PERF-FOLLOWUP-9 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if (Test-Path -LiteralPath $manifest) { # TEST-SUPPORT: PERF-FOLLOWUP-10 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            try { $runs = @((Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json)) } catch { $runs = @() } # TEST-SUPPORT: PERF-FOLLOWUP-11 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            if ($runs.Count -eq 6) { break } # TEST-SUPPORT: PERF-FOLLOWUP-12 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        } # TEST-SUPPORT: PERF-FOLLOWUP-13 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        Start-Sleep -Seconds 5 # TEST-SUPPORT: PERF-FOLLOWUP-14 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    } # TEST-SUPPORT: PERF-FOLLOWUP-15 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
} # TEST-SUPPORT: PERF-FOLLOWUP-16 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
$results = @() # TEST-SUPPORT: PERF-FOLLOWUP-17 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
function Run-Measurement([string]$Name, [string[]]$Options) { # TEST-SUPPORT: PERF-FOLLOWUP-18 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    & python tools/run_performance_matrix.py --output "$destination/$Name" @Options # TEST-SUPPORT: PERF-FOLLOWUP-19 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    $script:results += [pscustomobject]@{ name = $Name; exitCode = $LASTEXITCODE; completedUtc = [DateTime]::UtcNow.ToString('o') } # TEST-SUPPORT: PERF-FOLLOWUP-20 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    $script:results | ConvertTo-Json | Set-Content -LiteralPath "$destination/followups.json" # TEST-SUPPORT: PERF-FOLLOWUP-21 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
} # TEST-SUPPORT: PERF-FOLLOWUP-22 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'restored' @('--players','5','10','--repeats','1','--seconds','210','--scenario','restored') # TEST-SUPPORT: PERF-FOLLOWUP-23 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'latejoin' @('--players','5','10','--repeats','1','--seconds','210','--scenario','active','--latejoin') # TEST-SUPPORT: PERF-FOLLOWUP-24 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'moderate' @('--players','10','--repeats','1','--seconds','210','--scenario','active','--impairment','moderate') # TEST-SUPPORT: PERF-FOLLOWUP-25 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'stress' @('--players','10','--repeats','1','--seconds','210','--scenario','active','--impairment','stress') # TEST-SUPPORT: PERF-FOLLOWUP-26 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'observer-on' @('--players','10','--repeats','2','--seconds','90','--scenario','active') # TEST-SUPPORT: PERF-FOLLOWUP-27 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'observer-off' @('--players','10','--repeats','2','--seconds','90','--scenario','active','--uninstrumented') # TEST-SUPPORT: PERF-FOLLOWUP-28 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
Run-Measurement 'debug-development' @('--players','10','--repeats','1','--seconds','210','--scenario','active','--configuration','Debug','--diagnostic') # TEST-SUPPORT: PERF-FOLLOWUP-29 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
if (@($results | Where-Object exitCode -ne 0).Count -gt 0) { exit 1 } # TEST-SUPPORT: PERF-FOLLOWUP-30 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

