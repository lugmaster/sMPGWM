import csv # TEST-SUPPORT: ARCH-SUMMARY-001 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import io # TEST-SUPPORT: ARCH-SUMMARY-002 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import json # TEST-SUPPORT: ARCH-SUMMARY-003 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import math # TEST-SUPPORT: ARCH-SUMMARY-004 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
from pathlib import Path # TEST-SUPPORT: ARCH-SUMMARY-005 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
from zipfile import ZipFile # TEST-SUPPORT: ARCH-SUMMARY-006 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

ROOT = Path(__file__).resolve().parents[1] # TEST-SUPPORT: ARCH-SUMMARY-008 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
OUTPUT = ROOT / "docs/measurements/2026-11-14-architecture" # TEST-SUPPORT: ARCH-SUMMARY-009 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
START, END = 15, 29 # TEST-SUPPORT: ARCH-SUMMARY-010 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def rows(text): # TEST-SUPPORT: ARCH-SUMMARY-012 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return list(csv.DictReader(io.StringIO(text))) # TEST-SUPPORT: ARCH-SUMMARY-013 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def percentiles(values): # TEST-SUPPORT: ARCH-SUMMARY-015 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    values = sorted(values) # TEST-SUPPORT: ARCH-SUMMARY-016 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return {name: values[math.ceil(len(values) * q) - 1] for name, q in (("p50", .5), ("p95", .95), ("p99", .99))} if values else None # TEST-SUPPORT: ARCH-SUMMARY-017 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def metrics(read, run): # TEST-SUPPORT: ARCH-SUMMARY-019 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    directory = run["run"] # TEST-SUPPORT: ARCH-SUMMARY-020 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    host = directory + "/host/" # TEST-SUPPORT: ARCH-SUMMARY-021 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    steps = [r for r in rows(read(host + "steps.csv")) if START <= float(r["start_s"]) < END] # TEST-SUPPORT: ARCH-SUMMARY-022 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    publications = [r for r in rows(read(host + "publications.csv")) if START <= float(r["time_s"]) < END] # TEST-SUPPORT: ARCH-SUMMARY-023 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    process = [r for r in rows(read(host + "process.csv")) if START <= float(r["time_s"]) < END] # TEST-SUPPORT: ARCH-SUMMARY-024 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    workloads = {} # TEST-SUPPORT: ARCH-SUMMARY-025 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    errors = {} # TEST-SUPPORT: ARCH-SUMMARY-026 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for peer in run["exitCodes"]: # TEST-SUPPORT: ARCH-SUMMARY-027 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        workloads[peer] = json.loads(read(directory + "/" + peer + "/workload.json")) # TEST-SUPPORT: ARCH-SUMMARY-028 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        counts = {} # TEST-SUPPORT: ARCH-SUMMARY-029 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for line in read(directory + "/" + peer + "/engine.log").splitlines(): # TEST-SUPPORT: ARCH-SUMMARY-030 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            if line.startswith("ERROR:") or "Unhandled exception" in line: # TEST-SUPPORT: ARCH-SUMMARY-031 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                counts[line] = counts.get(line, 0) + 1 # TEST-SUPPORT: ARCH-SUMMARY-032 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if counts: # TEST-SUPPORT: ARCH-SUMMARY-033 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            errors[peer] = counts # TEST-SUPPORT: ARCH-SUMMARY-034 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return dict(run=directory, exitCodes=run["exitCodes"], # TEST-SUPPORT: ARCH-SUMMARY-035 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        allPeersPassed=all(code == 0 for code in run["exitCodes"].values()), # TEST-SUPPORT: ARCH-SUMMARY-036 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        hostStepMs=percentiles([float(r["work_ms"]) for r in steps]), # TEST-SUPPORT: ARCH-SUMMARY-037 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        publicationIntervalMs=percentiles([float(r["interval_ms"]) for r in publications]), # TEST-SUPPORT: ARCH-SUMMARY-038 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        stepAllocationKiBPerSecond=sum(int(r["allocated_bytes"]) for r in steps) / (END - START) / 1024, # TEST-SUPPORT: ARCH-SUMMARY-039 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        gcPauseMs=float(process[-1]["gc_pause_ms"]) - float(process[0]["gc_pause_ms"]) if process else None, # TEST-SUPPORT: ARCH-SUMMARY-040 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        hostShots=workloads["host"]["confirmedShots"], localShots={p: w["localShots"] for p, w in workloads.items()}, # TEST-SUPPORT: ARCH-SUMMARY-041 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        maxPoolHighWater=max(w["poolHighWater"] for w in workloads.values()), # TEST-SUPPORT: ARCH-SUMMARY-042 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        maxPoolDrops=max(w["poolDrops"] for w in workloads.values()), engineErrors=errors) # TEST-SUPPORT: ARCH-SUMMARY-043 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

results = [] # TEST-SUPPORT: ARCH-SUMMARY-045 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
for rate in (15, 60): # TEST-SUPPORT: ARCH-SUMMARY-046 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    previous = ROOT / f"docs/measurements/2026-11-14-hitscan/smg{rate}-nine-powered.zip" # TEST-SUPPORT: ARCH-SUMMARY-047 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    current = OUTPUT / f"smg{rate}-nine-warmup" # TEST-SUPPORT: ARCH-SUMMARY-048 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if not (current / "runs.json").exists(): # TEST-SUPPORT: ARCH-SUMMARY-049 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        continue # TEST-SUPPORT: ARCH-SUMMARY-050 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    with ZipFile(previous) as archive: # TEST-SUPPORT: ARCH-SUMMARY-051 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        prefix = f"smg{rate}-nine-powered/" # TEST-SUPPORT: ARCH-SUMMARY-052 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        def baseline_read(name): # TEST-SUPPORT: ARCH-SUMMARY-053 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            return archive.read(prefix + name).decode("utf-8") # TEST-SUPPORT: ARCH-SUMMARY-054 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        before = {r["run"]: metrics(baseline_read, r) for r in json.loads(baseline_read("runs.json"))} # TEST-SUPPORT: ARCH-SUMMARY-055 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    def current_read(name): # TEST-SUPPORT: ARCH-SUMMARY-056 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return (current / name).read_text(encoding="utf-8") # TEST-SUPPORT: ARCH-SUMMARY-057 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for run in json.loads(current_read("runs.json")): # TEST-SUPPORT: ARCH-SUMMARY-058 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        after = metrics(current_read, run) # TEST-SUPPORT: ARCH-SUMMARY-059 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        baseline = before.get(run["run"]) # TEST-SUPPORT: ARCH-SUMMARY-060 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        results.append(dict(rate=rate, before=baseline, after=after, # TEST-SUPPORT: ARCH-SUMMARY-061 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            allocationReductionPercent=100 * (1 - after["stepAllocationKiBPerSecond"] / baseline["stepAllocationKiBPerSecond"]) if baseline else None)) # TEST-SUPPORT: ARCH-SUMMARY-062 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
report = dict(windowSeconds=[START, END], percentile="nearest-rank", results=results) # TEST-SUPPORT: ARCH-SUMMARY-063 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
(OUTPUT / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8") # TEST-SUPPORT: ARCH-SUMMARY-064 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
for result in results: # TEST-SUPPORT: ARCH-SUMMARY-065 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    print(json.dumps(result)) # TEST-SUPPORT: ARCH-SUMMARY-066 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
