"""Run the opt-in normal-startup ENet shield verification with two late clients.""" # TEST-SUPPORT: SHIELD-PROCESS-001 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
from pathlib import Path # TEST-SUPPORT: SHIELD-PROCESS-002 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import os # TEST-SUPPORT: SHIELD-PROCESS-003 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import argparse # TEST-SUPPORT: SHIELD-PROCESS-004 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import subprocess # TEST-SUPPORT: SHIELD-PROCESS-005 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import tempfile # TEST-SUPPORT: SHIELD-PROCESS-006 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import time # TEST-SUPPORT: SHIELD-PROCESS-007 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import socket # TEST-SUPPORT: SHIELD-PROCESS-008 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import selectors # TEST-SUPPORT: SHIELD-PROCESS-009 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import threading # TEST-SUPPORT: SHIELD-PROCESS-010 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import random # TEST-SUPPORT: SHIELD-PROCESS-011 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
import heapq # TEST-SUPPORT: SHIELD-PROCESS-012 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup

root = Path(__file__).resolve().parents[1] # TEST-SUPPORT: SHIELD-PROCESS-013 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
engine = os.environ.get('GODOT_BIN', str(Path.home() / 'godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe')) # TEST-SUPPORT: SHIELD-PROCESS-014 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
logs = Path(tempfile.mkdtemp(prefix='shield-slice-')) # TEST-SUPPORT: SHIELD-PROCESS-015 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
parser = argparse.ArgumentParser() # TEST-SUPPORT: SHIELD-PROCESS-016 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
parser.add_argument("--players", type=int, default=3, choices=range(3, 11)) # TEST-SUPPORT: SHIELD-PROCESS-017 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
parser.add_argument("--impaired", action="store_true") # TEST-SUPPORT: SHIELD-PROCESS-018 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
options = parser.parse_args() # TEST-SUPPORT: SHIELD-PROCESS-019 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
players = options.players # TEST-SUPPORT: SHIELD-PROCESS-020 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
processes = [] # TEST-SUPPORT: SHIELD-PROCESS-021 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
stop_proxy = threading.Event() # TEST-SUPPORT: SHIELD-PROCESS-022 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
def proxy(): # TEST-SUPPORT: SHIELD-PROCESS-023 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    randomizer = random.Random(1042) # TEST-SUPPORT: SHIELD-PROCESS-024 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    selector = selectors.DefaultSelector() # TEST-SUPPORT: SHIELD-PROCESS-025 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: SHIELD-PROCESS-026 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    listener.bind(('127.0.0.1', 55672)) # TEST-SUPPORT: SHIELD-PROCESS-027 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    listener.setblocking(False) # TEST-SUPPORT: SHIELD-PROCESS-028 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    selector.register(listener, selectors.EVENT_READ, None) # TEST-SUPPORT: SHIELD-PROCESS-029 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    peers, pending = {}, [] # TEST-SUPPORT: SHIELD-PROCESS-030 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    serial = 0 # TEST-SUPPORT: SHIELD-PROCESS-031 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    try: # TEST-SUPPORT: SHIELD-PROCESS-032 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        while not stop_proxy.is_set(): # TEST-SUPPORT: SHIELD-PROCESS-033 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            for key, _ in selector.select(.002): # TEST-SUPPORT: SHIELD-PROCESS-034 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                try: # TEST-SUPPORT: SHIELD-PROCESS-035 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    data, sender = key.fileobj.recvfrom(65535) # TEST-SUPPORT: SHIELD-PROCESS-036 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                except (ConnectionResetError, BlockingIOError): # TEST-SUPPORT: SHIELD-PROCESS-037 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    continue # TEST-SUPPORT: SHIELD-PROCESS-038 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                if key.fileobj is listener: # TEST-SUPPORT: SHIELD-PROCESS-039 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    if sender not in peers: # TEST-SUPPORT: SHIELD-PROCESS-040 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        outgoing = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: SHIELD-PROCESS-041 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        outgoing.bind(('127.0.0.1', 0)) # TEST-SUPPORT: SHIELD-PROCESS-042 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        outgoing.setblocking(False) # TEST-SUPPORT: SHIELD-PROCESS-043 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        selector.register(outgoing, selectors.EVENT_READ, sender) # TEST-SUPPORT: SHIELD-PROCESS-044 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                        peers[sender] = outgoing # TEST-SUPPORT: SHIELD-PROCESS-045 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    destination, address = peers[sender], ('127.0.0.1', 55671) # TEST-SUPPORT: SHIELD-PROCESS-046 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                else: # TEST-SUPPORT: SHIELD-PROCESS-047 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    destination, address = listener, key.data # TEST-SUPPORT: SHIELD-PROCESS-048 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                if randomizer.random() < .01: # TEST-SUPPORT: SHIELD-PROCESS-049 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    continue # TEST-SUPPORT: SHIELD-PROCESS-050 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                serial += 1 # TEST-SUPPORT: SHIELD-PROCESS-051 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                heapq.heappush(pending, (time.monotonic() + randomizer.uniform(.02, .06), serial, destination, address, data)) # TEST-SUPPORT: SHIELD-PROCESS-052 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            now = time.monotonic() # TEST-SUPPORT: SHIELD-PROCESS-053 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            while pending and pending[0][0] <= now: # TEST-SUPPORT: SHIELD-PROCESS-054 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                _, _, destination, address, data = heapq.heappop(pending) # TEST-SUPPORT: SHIELD-PROCESS-055 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                destination.sendto(data, address) # TEST-SUPPORT: SHIELD-PROCESS-056 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    finally: # TEST-SUPPORT: SHIELD-PROCESS-057 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        for outgoing in peers.values(): outgoing.close() # TEST-SUPPORT: SHIELD-PROCESS-058 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        listener.close() # TEST-SUPPORT: SHIELD-PROCESS-059 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        selector.close() # TEST-SUPPORT: SHIELD-PROCESS-060 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
if options.impaired: # TEST-SUPPORT: SHIELD-PROCESS-061 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    threading.Thread(target=proxy, daemon=True).start() # TEST-SUPPORT: SHIELD-PROCESS-062 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    print('Impairment: seed 1042, 20-60 ms delay each way, 1% packet loss, delay-induced reordering', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-063 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
started = time.monotonic() # TEST-SUPPORT: SHIELD-PROCESS-064 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
try: # TEST-SUPPORT: SHIELD-PROCESS-065 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    schedule = [('host', 0, 'host')] + [(f'client-{i}', 3 + i * .5 if i < players - 2 else 12, 'client') for i in range(players - 1)] # TEST-SUPPORT: SHIELD-PROCESS-066 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    for name, delay, role in schedule: # TEST-SUPPORT: SHIELD-PROCESS-067 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        time.sleep(max(0, started + delay - time.monotonic())) # TEST-SUPPORT: SHIELD-PROCESS-068 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        stream = (logs / (name + '.log')).open('w', encoding='utf-8') # TEST-SUPPORT: SHIELD-PROCESS-069 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        environment = dict(os.environ, APPDATA=str(logs / name / 'userdata')) # TEST-SUPPORT: SHIELD-PROCESS-070 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        process = subprocess.Popen([engine, '--headless', '--path', str(root / 'sMPGWM.Game'), '--', '--shield-check-' + role] + (['--shield-check-proxy'] if options.impaired and role == 'client' else []), stdout=stream, stderr=subprocess.STDOUT, env=environment) # TEST-SUPPORT: SHIELD-PROCESS-071 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        processes.append((name, process, stream)) # TEST-SUPPORT: SHIELD-PROCESS-072 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        print(f'{name}: started at {time.monotonic() - started:.1f}s', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-073 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    success = True # TEST-SUPPORT: SHIELD-PROCESS-074 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    for name, process, stream in processes: # TEST-SUPPORT: SHIELD-PROCESS-075 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        code = process.wait(timeout=105) # TEST-SUPPORT: SHIELD-PROCESS-076 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        stream.close() # TEST-SUPPORT: SHIELD-PROCESS-077 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        print(f'{name}: exit={code}', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-078 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        content = (logs / (name + ".log")).read_text(encoding="utf-8") # TEST-SUPPORT: SHIELD-PROCESS-079 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        success &= code == 0 and "ERROR:" not in content # TEST-SUPPORT: SHIELD-PROCESS-080 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    evidence = root / ('docs/plans/evidence/shield-slice-enet-' + str(players) + ('-impaired' if options.impaired else '') + '.txt') # TEST-SUPPORT: SHIELD-PROCESS-081 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    evidence.parent.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: SHIELD-PROCESS-082 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    with evidence.open('w', encoding='utf-8') as report: # TEST-SUPPORT: SHIELD-PROCESS-083 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        for name, _, _ in processes: # TEST-SUPPORT: SHIELD-PROCESS-084 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            report.write(name + '\n') # TEST-SUPPORT: SHIELD-PROCESS-085 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            for line in (logs / (name + '.log')).read_text(encoding='utf-8').splitlines(): # TEST-SUPPORT: SHIELD-PROCESS-086 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                if 'SHIELD_CHECK' in line or 'ERROR:' in line or 'NET join_ready' in line: # TEST-SUPPORT: SHIELD-PROCESS-087 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
                    report.write(line + '\n') # TEST-SUPPORT: SHIELD-PROCESS-088 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    print(f'Raw logs: {logs}', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-089 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    raise SystemExit(0 if success else 1) # TEST-SUPPORT: SHIELD-PROCESS-090 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
finally: # TEST-SUPPORT: SHIELD-PROCESS-091 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    stop_proxy.set() # TEST-SUPPORT: SHIELD-PROCESS-092 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    for _, process, stream in processes: # TEST-SUPPORT: SHIELD-PROCESS-093 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        if process.poll() is None: # TEST-SUPPORT: SHIELD-PROCESS-094 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            process.terminate() # TEST-SUPPORT: SHIELD-PROCESS-095 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            process.wait(timeout=10) # TEST-SUPPORT: SHIELD-PROCESS-096 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
        stream.close() # TEST-SUPPORT: SHIELD-PROCESS-097 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
