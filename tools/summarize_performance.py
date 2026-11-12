"""Summarize retained performance CSVs without changing or discarding raw evidence.""" # TEST-SUPPORT: FPS-0001 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import argparse # TEST-SUPPORT: FPS-0002 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import csv # TEST-SUPPORT: FPS-0003 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import json # TEST-SUPPORT: FPS-0004 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
import math # TEST-SUPPORT: FPS-0005 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
from pathlib import Path # TEST-SUPPORT: FPS-0006 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def read_json(path, default=None): # TEST-SUPPORT: FPS-0007 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else default # TEST-SUPPORT: FPS-0008 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def read_csv(path): # TEST-SUPPORT: FPS-0009 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if not path.exists(): # TEST-SUPPORT: FPS-0010 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return [] # TEST-SUPPORT: FPS-0011 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    with path.open(encoding="utf-8-sig", newline="") as stream: # TEST-SUPPORT: FPS-0012 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return list(csv.DictReader(stream)) # TEST-SUPPORT: FPS-0013 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def distribution(values, budget=None): # TEST-SUPPORT: FPS-0014 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    ordered = sorted(values) # TEST-SUPPORT: FPS-0015 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if not ordered: # TEST-SUPPORT: FPS-0016 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return None # TEST-SUPPORT: FPS-0017 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    result = dict(count=len(ordered), mean=sum(ordered) / len(ordered), p50=ordered[math.ceil(len(ordered) * .50) - 1], # TEST-SUPPORT: FPS-0018 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  p95=ordered[math.ceil(len(ordered) * .95) - 1], p99=ordered[math.ceil(len(ordered) * .99) - 1], maximum=ordered[-1]) # TEST-SUPPORT: FPS-0019 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if budget is not None: # TEST-SUPPORT: FPS-0020 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        overruns = [value for value in ordered if value > budget] # TEST-SUPPORT: FPS-0021 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        result.update(budget=budget, overBudgetCount=len(overruns), overBudgetTimeMs=sum(overruns), excessTimeMs=sum(value - budget for value in overruns)) # TEST-SUPPORT: FPS-0022 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return result # TEST-SUPPORT: FPS-0023 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def numbers(rows, key): # TEST-SUPPORT: FPS-0024 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return [float(row[key]) for row in rows] # TEST-SUPPORT: FPS-0025 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def window(rows, start, end, offset, key="time_s"): # TEST-SUPPORT: FPS-0026 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return [row for row in rows if start <= float(row[key]) + offset < end] # TEST-SUPPORT: FPS-0027 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def process_summary(rows): # TEST-SUPPORT: FPS-0028 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if len(rows) < 2: # TEST-SUPPORT: FPS-0029 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return None # TEST-SUPPORT: FPS-0030 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    first, last = rows[0], rows[-1] # TEST-SUPPORT: FPS-0031 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    duration = float(last["time_s"]) - float(first["time_s"]) # TEST-SUPPORT: FPS-0032 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if duration <= 0: # TEST-SUPPORT: FPS-0033 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return None # TEST-SUPPORT: FPS-0034 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    allocated = int(last["allocated_bytes"]) - int(first["allocated_bytes"]) # TEST-SUPPORT: FPS-0035 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return dict(sampleDurationSeconds=duration, allocationBytesPerSecond=allocated / duration, allocatedBytes=allocated, # TEST-SUPPORT: FPS-0036 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                collections=[int(last[key]) - int(first[key]) for key in ("gen0", "gen1", "gen2")], # TEST-SUPPORT: FPS-0037 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                gcPauseMs=float(last["gc_pause_ms"]) - float(first["gc_pause_ms"]), # TEST-SUPPORT: FPS-0038 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                cpuPercentOneCore=(float(last["cpu_ms"]) - float(first["cpu_ms"])) / (duration * 10), # TEST-SUPPORT: FPS-0039 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                workingSetBytes=distribution(numbers(rows, "working_set_bytes")), # TEST-SUPPORT: FPS-0040 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                workingSetChangeBytes=int(last["working_set_bytes"]) - int(first["working_set_bytes"]), # TEST-SUPPORT: FPS-0041 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                heapBytes=distribution(numbers(rows, "heap_bytes")), committedBytes=distribution(numbers(rows, "committed_bytes")), # TEST-SUPPORT: FPS-0042 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                rttMs=distribution([value * 1000 for value in numbers(rows, "rtt_ms")]), jitterMs=distribution([value * 1000 for value in numbers(rows, "jitter_ms")])) # TEST-SUPPORT: FPS-0043 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def statistics(data): # TEST-SUPPORT: FPS-0044 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    steps, publications, arrivals, frames, samples = (data[key] for key in ("steps", "publications", "arrivals", "frames", "process")) # TEST-SUPPORT: FPS-0045 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    published = [row for row in steps if int(row["host"]) and int(row["tick"]) % 2 == 0] # TEST-SUPPORT: FPS-0046 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    unpublished = [row for row in steps if int(row["host"]) and int(row["tick"]) % 2 != 0] # TEST-SUPPORT: FPS-0047 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    intervals = numbers(publications, "interval_ms") # TEST-SUPPORT: FPS-0048 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    process = process_summary(samples) # TEST-SUPPORT: FPS-0049 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return dict(stepMs=distribution(numbers(steps, "work_ms"), 1000 / 60), # TEST-SUPPORT: FPS-0050 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationStepMs=distribution(numbers(published, "work_ms"), 1000 / 60), # TEST-SUPPORT: FPS-0051 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                nonPublicationStepMs=distribution(numbers(unpublished, "work_ms"), 1000 / 60), # TEST-SUPPORT: FPS-0052 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                stepAllocationBytes=distribution(numbers(steps, "allocated_bytes")), # TEST-SUPPORT: FPS-0053 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationIntervalMs=distribution(intervals, 1000 / 30), # TEST-SUPPORT: FPS-0054 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationIntervalsAbove40Ms=sum(value > 40 for value in intervals), # TEST-SUPPORT: FPS-0055 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationIntervalsAbove50Ms=sum(value > 50 for value in intervals), # TEST-SUPPORT: FPS-0056 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationDeviationMs=distribution([abs(value - 1000 / 30) for value in intervals]), # TEST-SUPPORT: FPS-0057 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                publicationBytes=distribution(numbers(publications, "bytes")), # TEST-SUPPORT: FPS-0058 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                arrivalIntervalMs=distribution(numbers(arrivals, "interval_ms"), 1000 / 30), # TEST-SUPPORT: FPS-0059 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                frameIntervalMs=distribution(numbers(frames, "delta_ms"), 1000 / 60), # TEST-SUPPORT: FPS-0060 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                wallFrameIntervalMs=distribution([float(row["wall_interval_ms"]) for row in frames if "wall_interval_ms" in row], 1000 / 60), # TEST-SUPPORT: FPS-0210 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                frameWorkMs=distribution(numbers(frames, "process_work_ms"), 1000 / 60), # TEST-SUPPORT: FPS-0061 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                physicsWorkMs=distribution(numbers(frames, "physics_work_ms"), 1000 / 60), process=process, # TEST-SUPPORT: FPS-0062 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                worstSteps=sorted(steps, key=lambda row: float(row["work_ms"]), reverse=True)[:10], # TEST-SUPPORT: FPS-0203 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                stepOverruns=[row for row in steps if float(row["work_ms"]) > 1000 / 60], # TEST-SUPPORT: FPS-0204 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                worstPublicationIntervals=sorted(publications, key=lambda row: float(row["interval_ms"]), reverse=True)[:10]) # TEST-SUPPORT: FPS-0205 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def offset_for(peer, arguments, workload): # TEST-SUPPORT: FPS-0063 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if "offset" in workload: # TEST-SUPPORT: FPS-0064 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return float(workload["offset"]) # TEST-SUPPORT: FPS-0065 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if peer == "host": # TEST-SUPPORT: FPS-0066 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return 0 # TEST-SUPPORT: FPS-0067 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    index = int(peer.split("-")[1]) # TEST-SUPPORT: FPS-0068 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if arguments.get("latejoin") and index == int(arguments["players"][-1]) - 1: # TEST-SUPPORT: FPS-0069 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return 60 # TEST-SUPPORT: FPS-0070 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return (10 if arguments.get("scenario") == "restored" else 3) + (index - 1) * .5 # TEST-SUPPORT: FPS-0071 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def phase_windows(arguments): # TEST-SUPPORT: FPS-0072 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    duration = float(arguments.get("seconds", 630)) # TEST-SUPPORT: FPS-0073 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if duration <= 30: # TEST-SUPPORT: FPS-0201 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        return [("startup_smoke", 0, duration, False)] # TEST-SUPPORT: FPS-0202 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    if arguments.get("scenario", "matrix") == "matrix": # TEST-SUPPORT: FPS-0074 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        phases = [(name, start, end) for name, start, end in (("idle", 30, 210), ("active", 240, 420), ("burst", 450, 630)) if duration > start] # TEST-SUPPORT: FPS-0075 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    else: # TEST-SUPPORT: FPS-0076 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        phases = [(arguments.get("scenario", "active"), 30, duration)] if duration > 30 else [] # TEST-SUPPORT: FPS-0077 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return [(name, start, min(end, duration), end - start >= 180 and duration >= end) for name, start, end in phases] # TEST-SUPPORT: FPS-0078 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def summarize_peer(peer_dir, manifest, root, players): # TEST-SUPPORT: FPS-0079 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    workload = read_json(peer_dir / "workload.json", {}) # TEST-SUPPORT: FPS-0080 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    recorder = read_json(peer_dir / "recorder.json", {}) # TEST-SUPPORT: FPS-0081 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    arguments = dict(manifest["arguments"]) # TEST-SUPPORT: FPS-0082 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    arguments["players"] = [players] # TEST-SUPPORT: FPS-0083 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    offset = offset_for(peer_dir.name, arguments, workload) # TEST-SUPPORT: FPS-0084 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    markers = read_csv(peer_dir / "markers.csv") # TEST-SUPPORT: FPS-0085 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    data = {key: read_csv(peer_dir / (key + ".csv")) for key in ("steps", "publications", "arrivals", "frames", "process")} # TEST-SUPPORT: FPS-0086 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for key in ("steps", "publications"): # TEST-SUPPORT: FPS-0230 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for row in data[key]: # TEST-SUPPORT: FPS-0231 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            row["sourcePeer"] = peer_dir.name # TEST-SUPPORT: FPS-0232 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for index in range(1, len(data["frames"])): # TEST-SUPPORT: FPS-0211 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        data["frames"][index]["wall_interval_ms"] = (float(data["frames"][index]["time_s"]) - float(data["frames"][index - 1]["time_s"])) * 1000 # TEST-SUPPORT: FPS-0212 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    phases = [] # TEST-SUPPORT: FPS-0087 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for name, start, end, complete in phase_windows(arguments): # TEST-SUPPORT: FPS-0088 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        selected = {key: window(rows, start, end, offset, "start_s" if key == "steps" else "time_s") for key, rows in data.items()} # TEST-SUPPORT: FPS-0089 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        stats = statistics(selected) # TEST-SUPPORT: FPS-0090 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        stats["publicationRateHz"] = len(selected["publications"]) / (end - start) if recorder.get("enabled") else None # TEST-SUPPORT: FPS-0213 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        stats["physicsStepRateHz"] = len(selected["steps"]) / (end - start) if recorder.get("enabled") else None # TEST-SUPPORT: FPS-0214 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for spike in stats["stepOverruns"]: # TEST-SUPPORT: FPS-0233 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            for index in range(1, len(data["process"])): # TEST-SUPPORT: FPS-0234 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                before, after = data["process"][index - 1], data["process"][index] # TEST-SUPPORT: FPS-0235 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                if float(before["time_s"]) <= float(spike["start_s"]) and float(after["time_s"]) >= float(spike["end_s"]): # TEST-SUPPORT: FPS-0236 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    spike["gcBucketCorrelation"] = dict(startSeconds=float(before["time_s"]), endSeconds=float(after["time_s"]), collections=[int(after[key]) - int(before[key]) for key in ("gen0", "gen1", "gen2")], pauseMs=float(after["gc_pause_ms"]) - float(before["gc_pause_ms"])) # TEST-SUPPORT: FPS-0237 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    break # TEST-SUPPORT: FPS-0238 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        phases.append(dict(name=name, startSeconds=start, endSeconds=end, nominalComplete180Seconds=complete, statistics=stats)) # TEST-SUPPORT: FPS-0091 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    marker_times = {} # TEST-SUPPORT: FPS-0092 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    pending_markers, phase_durations = {}, {} # TEST-SUPPORT: FPS-0220 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for row in markers: # TEST-SUPPORT: FPS-0093 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        marker_times.setdefault(row["name"], []).append(float(row["time_s"])) # TEST-SUPPORT: FPS-0094 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        label = row["name"] # TEST-SUPPORT: FPS-0221 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if label.endswith("_start"): # TEST-SUPPORT: FPS-0222 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            pending_markers.setdefault(label[:-6], []).append(float(row["time_s"])) # TEST-SUPPORT: FPS-0223 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        elif label.endswith("_end") and pending_markers.get(label[:-4]): # TEST-SUPPORT: FPS-0224 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            started = pending_markers[label[:-4]].pop() # TEST-SUPPORT: FPS-0225 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            phase_durations.setdefault(label[:-4], []).append((float(row["time_s"]) - started) * 1000) # TEST-SUPPORT: FPS-0226 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    loading = marker_times.get("loading_start", []) # TEST-SUPPORT: FPS-0095 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    active = marker_times.get("active", []) # TEST-SUPPORT: FPS-0096 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    loading_seconds = active[0] - loading[0] if loading and active else None # TEST-SUPPORT: FPS-0097 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return dict(path=str(peer_dir.relative_to(root)), peer=peer_dir.name, offsetSeconds=offset, workload=workload, # TEST-SUPPORT: FPS-0098 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                recorder=recorder, loadingToActiveSeconds=loading_seconds, markers=markers, phases=phases, phaseDurationsMs={name: distribution(values) for name, values in phase_durations.items()}), data # TEST-SUPPORT: FPS-0099 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def fmt(value): # TEST-SUPPORT: FPS-0100 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return "—" if value is None else f"{value:.3f}" # TEST-SUPPORT: FPS-0101 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def metric(stats, key, field): # TEST-SUPPORT: FPS-0102 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    value = stats.get(key) # TEST-SUPPORT: FPS-0103 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return value.get(field) if value else None # TEST-SUPPORT: FPS-0104 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def generate_markdown(report): # TEST-SUPPORT: FPS-0105 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    lines = ["# Local performance measurements", "", "Nearest-rank percentiles; milliseconds unless stated. Each steady phase excludes its first 30 seconds. Raw CSVs remain adjacent to each peer.", "", # TEST-SUPPORT: FPS-0106 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
             "Host results:", "", "| Dataset / run | Phase | Step p50 / p95 / p99 / max | Steps >16.67 ms | Publication p99 / max | Publications/s | Intervals >40 / >50 ms | Allocation MB/s | GC 0/1/2; pause ms | CPU % of one core |", # TEST-SUPPORT: FPS-0107 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
             "| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- | ---: |"] # TEST-SUPPORT: FPS-0108 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for run in report["runs"]: # TEST-SUPPORT: FPS-0109 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        host = next((peer for peer in run["peers"] if peer["peer"] == "host"), None) # TEST-SUPPORT: FPS-0110 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if not host: # TEST-SUPPORT: FPS-0111 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            continue # TEST-SUPPORT: FPS-0112 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for phase in host["phases"]: # TEST-SUPPORT: FPS-0113 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            stats = phase["statistics"] # TEST-SUPPORT: FPS-0114 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            step = stats["stepMs"] # TEST-SUPPORT: FPS-0115 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            process = stats["process"] # TEST-SUPPORT: FPS-0116 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            label = run["path"] # TEST-SUPPORT: FPS-0117 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            name = phase["name"] + ("" if phase["nominalComplete180Seconds"] else " (short)") # TEST-SUPPORT: FPS-0118 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            step_text = " / ".join(fmt(metric(stats, "stepMs", key)) for key in ("p50", "p95", "p99", "maximum")) # TEST-SUPPORT: FPS-0119 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            publication = " / ".join(fmt(metric(stats, "publicationIntervalMs", key)) for key in ("p99", "maximum")) # TEST-SUPPORT: FPS-0120 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            allocation = process["allocationBytesPerSecond"] / 1e6 if process else None # TEST-SUPPORT: FPS-0121 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            gc = "/".join(str(value) for value in process["collections"]) + "; " + fmt(process["gcPauseMs"]) if process else "—" # TEST-SUPPORT: FPS-0122 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            lines.append(f"| {label} | {name} | {step_text} | {step['overBudgetCount'] if step else '—'} | {publication} | {fmt(stats.get('publicationRateHz'))} | {stats['publicationIntervalsAbove40Ms']} / {stats['publicationIntervalsAbove50Ms']} | {fmt(allocation)} | {gc} | {fmt(process['cpuPercentOneCore'] if process else None)} |") # TEST-SUPPORT: FPS-0123 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    lines.extend(["", "Client samples pooled within each run/phase; process rates are summed across clients:", "", "| Dataset / run | Phase | Step p99 / max | Frame interval p99 / max | Arrival interval p99 / max | Allocation MB/s | Maximum RTT p99 |", "| --- | --- | --- | --- | --- | ---: | ---: |"]) # TEST-SUPPORT: FPS-0124 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for run in report["runs"]: # TEST-SUPPORT: FPS-0125 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for phase in run["clientAggregates"]: # TEST-SUPPORT: FPS-0126 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            stats = phase["statistics"] # TEST-SUPPORT: FPS-0127 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            step = " / ".join(fmt(metric(stats, "stepMs", key)) for key in ("p99", "maximum")) # TEST-SUPPORT: FPS-0128 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            frame = " / ".join(fmt(metric(stats, "wallFrameIntervalMs", key)) for key in ("p99", "maximum")) # TEST-SUPPORT: FPS-0129 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            arrival = " / ".join(fmt(metric(stats, "arrivalIntervalMs", key)) for key in ("p99", "maximum")) # TEST-SUPPORT: FPS-0130 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            lines.append(f"| {run['path']} | {phase['name']} | {step} | {frame} | {arrival} | {fmt(phase['allocationBytesPerSecondSum'] / 1e6)} | {fmt(phase['maximumRttP99Ms'])} |") # TEST-SUPPORT: FPS-0131 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    lines.extend(["", "Startup and workload outcomes:", "", "| Dataset / run | Peers complete / expected | Loading → active min / max seconds | Accepted abilities | Transfer attempts | Item rejections |", "| --- | ---: | --- | ---: | ---: | ---: |"]) # TEST-SUPPORT: FPS-0132 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for run in report["runs"]: # TEST-SUPPORT: FPS-0133 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        peers = run["peers"] # TEST-SUPPORT: FPS-0134 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        loading = [peer["loadingToActiveSeconds"] for peer in peers if peer["loadingToActiveSeconds"] is not None] # TEST-SUPPORT: FPS-0135 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        loading_text = fmt(min(loading)) + " / " + fmt(max(loading)) if loading else "—" # TEST-SUPPORT: FPS-0136 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        count = sum(bool(peer["workload"].get("success")) for peer in peers) # TEST-SUPPORT: FPS-0137 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        totals = [sum(peer["workload"].get(key, 0) for peer in peers) for key in ("acceptedAbilities", "transferAttempts", "itemRejections")] # TEST-SUPPORT: FPS-0138 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        lines.append(f"| {run['path']} | {count} / {run['players']} | {loading_text} | {totals[0]} | {totals[1]} | {totals[2]} |") # TEST-SUPPORT: FPS-0139 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    lines.extend(["", "Interpretation limits:", "", "- Engine frame/physics monitors are cached sampled values, not independent per-frame CPU observations; direct coordinator work timings are separate. Headless intervals do not measure rendered FPS or GPU work.", # TEST-SUPPORT: FPS-0140 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Publication timing is host emission; client arrival records accepted application snapshots after validation, not UDP arrivals. Nominal-budget counts include ordinary timer jitter; >40/>50 ms counts show larger deviations.", # TEST-SUPPORT: FPS-0141 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Process rates use the first/last 1 Hz samples inside each phase; their actual sample duration is retained in JSON. GC pauses are cumulative runtime pause time; no individual pause-event trace was collected.", # TEST-SUPPORT: FPS-0142 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Client process rates are summed, while latency percentiles pool raw observations. CPU percentages use one logical core as 100%. Heap size updates at GC.", # TEST-SUPPORT: FPS-0143 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Short/control runs are labeled and never establish the full 180-second steady workload. Endpoint recorder totals exclude CSV output but include startup; recorder-off controls have no raw hot-path samples.", # TEST-SUPPORT: FPS-0144 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Approximate shared phase time adds the requested peer launch offset to its local recorder clock; process startup scheduling introduces small alignment differences.", # TEST-SUPPORT: FPS-0145 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Frame interval tables use differences between recorder wall timestamps; JSON also retains Godot delta separately, which may be smoothed or clamped.", # TEST-SUPPORT: FPS-0215 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "- Raw process.csv rtt_ms/jitter_ms columns contain SECONDS from NetworkClock despite their headers; summaries multiply by 1000. RTT is a smoothed probe estimate; jitter is smoothed absolute RTT deviation, not packet-arrival jitter.", # TEST-SUPPORT: FPS-0216 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                  "", "Full per-peer distributions, over-budget counts/time/excess, process samples, lifecycle markers, endpoint totals, configuration arguments and workload outcomes are in summary.json.", ""]) # TEST-SUPPORT: FPS-0146 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    return "\n".join(lines) # TEST-SUPPORT: FPS-0147 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

def main(): # TEST-SUPPORT: FPS-0148 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    parser = argparse.ArgumentParser() # TEST-SUPPORT: FPS-0149 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    parser.add_argument("root", help="Directory containing one or more measurement manifests.") # TEST-SUPPORT: FPS-0150 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    args = parser.parse_args() # TEST-SUPPORT: FPS-0151 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    root = Path(args.root).resolve() # TEST-SUPPORT: FPS-0152 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    report = dict(method="nearest-rank", stepBudgetMs=1000 / 60, publicationBudgetMs=1000 / 30, runs=[]) # TEST-SUPPORT: FPS-0153 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    for path in sorted(root.rglob("manifest.json")): # TEST-SUPPORT: FPS-0154 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        manifest = read_json(path) # TEST-SUPPORT: FPS-0155 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        if not isinstance(manifest, dict) or "arguments" not in manifest: # TEST-SUPPORT: FPS-0156 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            continue # TEST-SUPPORT: FPS-0157 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        arguments = manifest["arguments"] # TEST-SUPPORT: FPS-0158 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
        for run_dir in sorted(path.parent.glob("*-players-run-*")): # TEST-SUPPORT: FPS-0159 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            players = int(run_dir.name.split("-")[0]) # TEST-SUPPORT: FPS-0160 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            run = dict(path=str(run_dir.relative_to(root)), players=players, configuration=arguments.get("configuration"), arguments=arguments, peers=[], clientAggregates=[]) # TEST-SUPPORT: FPS-0161 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            pooled = {name: {key: [] for key in ("steps", "publications", "arrivals", "frames", "process")} for name, _, _, _ in phase_windows(arguments)} # TEST-SUPPORT: FPS-0162 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            processes = {name: [] for name in pooled} # TEST-SUPPORT: FPS-0163 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            for peer_dir in sorted(run_dir.iterdir()): # TEST-SUPPORT: FPS-0164 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                if not peer_dir.is_dir() or not (peer_dir / "recorder.json").exists(): # TEST-SUPPORT: FPS-0165 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    continue # TEST-SUPPORT: FPS-0166 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                peer, data = summarize_peer(peer_dir, manifest, root, players) # TEST-SUPPORT: FPS-0167 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                run["peers"].append(peer) # TEST-SUPPORT: FPS-0168 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                if peer["peer"] == "host": # TEST-SUPPORT: FPS-0169 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    continue # TEST-SUPPORT: FPS-0170 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                for phase in peer["phases"]: # TEST-SUPPORT: FPS-0171 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    name = phase["name"] # TEST-SUPPORT: FPS-0172 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    for key, rows in data.items(): # TEST-SUPPORT: FPS-0173 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                        if key != "process": # TEST-SUPPORT: FPS-0174 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                            pooled[name][key].extend(window(rows, phase["startSeconds"], phase["endSeconds"], peer["offsetSeconds"], "start_s" if key == "steps" else "time_s")) # TEST-SUPPORT: FPS-0175 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                    if phase["statistics"]["process"]: # TEST-SUPPORT: FPS-0176 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                        processes[name].append(phase["statistics"]["process"]) # TEST-SUPPORT: FPS-0177 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            for name, data in pooled.items(): # TEST-SUPPORT: FPS-0178 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                process = processes[name] # TEST-SUPPORT: FPS-0179 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                rtts = [value["rttMs"]["p99"] for value in process if value["rttMs"]] # TEST-SUPPORT: FPS-0180 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                run["clientAggregates"].append(dict(name=name, statistics=statistics(data), allocationBytesPerSecondSum=sum(value["allocationBytesPerSecond"] for value in process), # TEST-SUPPORT: FPS-0181 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                                                   maximumRttP99Ms=max(rtts) if rtts else None, processSummaries=process)) # TEST-SUPPORT: FPS-0182 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
            if run["peers"]: # TEST-SUPPORT: FPS-0239 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
                report["runs"].append(run) # TEST-SUPPORT: FPS-0183 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    (root / "summary.json").write_text(json.dumps(report, indent=2), encoding="utf-8") # TEST-SUPPORT: FPS-0184 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    (root / "summary.md").write_text(generate_markdown(report), encoding="utf-8") # TEST-SUPPORT: FPS-0185 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    print(f"Summarized {len(report['runs'])} runs into {root / 'summary.md'}") # TEST-SUPPORT: FPS-0186 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

if __name__ == "__main__": # TEST-SUPPORT: FPS-0187 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix
    main() # TEST-SUPPORT: FPS-0188 | Tests: sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix

