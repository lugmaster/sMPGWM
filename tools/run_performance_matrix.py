"""Isolated normal-startup local performance matrix; opt-in test driver.""" # TEST-SUPPORT: PERF-RUN-001 | Tests: sMPGWM.Tests.PerformanceProcessTests
from pathlib import Path # TEST-SUPPORT: PERF-RUN-002 | Tests: sMPGWM.Tests.PerformanceProcessTests
import argparse, hashlib, json, os, shutil, socket, subprocess, tempfile, time, zipfile, platform # TEST-SUPPORT: PERF-RUN-003 | Tests: sMPGWM.Tests.PerformanceProcessTests
ROOT = Path(__file__).resolve().parents[1] # TEST-SUPPORT: PERF-RUN-004 | Tests: sMPGWM.Tests.PerformanceProcessTests
import heapq, random, selectors, threading # TEST-SUPPORT: PERF-PROXY-001 | Tests: sMPGWM.Tests.PerformanceProcessTests

class DatagramProxy: # TEST-SUPPORT: PERF-PROXY-002 | Tests: sMPGWM.Tests.PerformanceProcessTests
    def __init__(self, host_port, profile): # TEST-SUPPORT: PERF-PROXY-003 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.profile = profile # TEST-SUPPORT: PERF-PROXY-004 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.minimum, self.maximum, self.loss = (0.02, 0.06, 0.01) if profile == "moderate" else (0.06, 0.14, 0.05) # TEST-SUPPORT: PERF-PROXY-005 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.random = random.Random(1042) # TEST-SUPPORT: PERF-PROXY-006 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.stop = threading.Event() # TEST-SUPPORT: PERF-PROXY-007 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: PERF-PROXY-008 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.listener.bind(("127.0.0.1", 0)) # TEST-SUPPORT: PERF-PROXY-009 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.listener.setblocking(False) # TEST-SUPPORT: PERF-PROXY-010 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.port = self.listener.getsockname()[1] # TEST-SUPPORT: PERF-PROXY-011 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.host = ("127.0.0.1", host_port) # TEST-SUPPORT: PERF-PROXY-012 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.selector = selectors.DefaultSelector() # TEST-SUPPORT: PERF-PROXY-013 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.selector.register(self.listener, selectors.EVENT_READ, None) # TEST-SUPPORT: PERF-PROXY-014 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.peers = {} # TEST-SUPPORT: PERF-PROXY-015 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.pending = [] # TEST-SUPPORT: PERF-PROXY-016 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.serial = 0 # TEST-SUPPORT: PERF-PROXY-017 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.error = None # TEST-SUPPORT: PERF-PROXY-018 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.maximum_pending = 0 # TEST-SUPPORT: PERF-PROXY-019 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.counts = {direction: dict(datagrams=0, bytes=0, dropped=0, forwarded=0, forwardedBytes=0, socketErrors=0) for direction in ("client_to_host", "host_to_client")} # TEST-SUPPORT: PERF-PROXY-020 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.thread = threading.Thread(target=self.run, name="performance-udp-proxy", daemon=True) # TEST-SUPPORT: PERF-PROXY-021 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.thread.start() # TEST-SUPPORT: PERF-PROXY-022 | Tests: sMPGWM.Tests.PerformanceProcessTests

    def run(self): # TEST-SUPPORT: PERF-PROXY-023 | Tests: sMPGWM.Tests.PerformanceProcessTests
        try: # TEST-SUPPORT: PERF-PROXY-024 | Tests: sMPGWM.Tests.PerformanceProcessTests
            while not self.stop.is_set(): # TEST-SUPPORT: PERF-PROXY-025 | Tests: sMPGWM.Tests.PerformanceProcessTests
                for key, _ in self.selector.select(0.002): # TEST-SUPPORT: PERF-PROXY-026 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    direction = "client_to_host" if key.fileobj is self.listener else "host_to_client" # TEST-SUPPORT: PERF-PROXY-027 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    counters = self.counts[direction] # TEST-SUPPORT: PERF-PROXY-028 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    try: # TEST-SUPPORT: PERF-PROXY-029 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        data, sender = key.fileobj.recvfrom(65535) # TEST-SUPPORT: PERF-PROXY-030 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    except BlockingIOError: # TEST-SUPPORT: PERF-PROXY-031 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        continue # TEST-SUPPORT: PERF-PROXY-032 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    except OSError: # TEST-SUPPORT: PERF-PROXY-033 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        counters["socketErrors"] += 1 # TEST-SUPPORT: PERF-PROXY-034 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        continue # TEST-SUPPORT: PERF-PROXY-035 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    counters["datagrams"] += 1 # TEST-SUPPORT: PERF-PROXY-036 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    counters["bytes"] += len(data) # TEST-SUPPORT: PERF-PROXY-037 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    if key.fileobj is self.listener: # TEST-SUPPORT: PERF-PROXY-038 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        if sender not in self.peers: # TEST-SUPPORT: PERF-PROXY-039 | Tests: sMPGWM.Tests.PerformanceProcessTests
                            outgoing = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: PERF-PROXY-040 | Tests: sMPGWM.Tests.PerformanceProcessTests
                            outgoing.bind(("127.0.0.1", 0)) # TEST-SUPPORT: PERF-PROXY-041 | Tests: sMPGWM.Tests.PerformanceProcessTests
                            outgoing.setblocking(False) # TEST-SUPPORT: PERF-PROXY-042 | Tests: sMPGWM.Tests.PerformanceProcessTests
                            self.selector.register(outgoing, selectors.EVENT_READ, sender) # TEST-SUPPORT: PERF-PROXY-043 | Tests: sMPGWM.Tests.PerformanceProcessTests
                            self.peers[sender] = outgoing # TEST-SUPPORT: PERF-PROXY-044 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        destination, address = self.peers[sender], self.host # TEST-SUPPORT: PERF-PROXY-045 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    else: # TEST-SUPPORT: PERF-PROXY-046 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        destination, address = self.listener, key.data # TEST-SUPPORT: PERF-PROXY-047 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    if self.random.random() < self.loss: # TEST-SUPPORT: PERF-PROXY-048 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        counters["dropped"] += 1 # TEST-SUPPORT: PERF-PROXY-049 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        continue # TEST-SUPPORT: PERF-PROXY-050 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    self.serial += 1 # TEST-SUPPORT: PERF-PROXY-051 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    due = time.monotonic() + self.random.uniform(self.minimum, self.maximum) # TEST-SUPPORT: PERF-PROXY-052 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    heapq.heappush(self.pending, (due, self.serial, destination, address, data, direction)) # TEST-SUPPORT: PERF-PROXY-053 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    self.maximum_pending = max(self.maximum_pending, len(self.pending)) # TEST-SUPPORT: PERF-PROXY-054 | Tests: sMPGWM.Tests.PerformanceProcessTests
                now = time.monotonic() # TEST-SUPPORT: PERF-PROXY-055 | Tests: sMPGWM.Tests.PerformanceProcessTests
                while self.pending and self.pending[0][0] <= now: # TEST-SUPPORT: PERF-PROXY-056 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    _, _, destination, address, data, direction = heapq.heappop(self.pending) # TEST-SUPPORT: PERF-PROXY-057 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    counters = self.counts[direction] # TEST-SUPPORT: PERF-PROXY-058 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    try: # TEST-SUPPORT: PERF-PROXY-059 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        sent = destination.sendto(data, address) # TEST-SUPPORT: PERF-PROXY-060 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        counters["forwarded"] += 1 # TEST-SUPPORT: PERF-PROXY-061 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        counters["forwardedBytes"] += sent # TEST-SUPPORT: PERF-PROXY-062 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    except OSError: # TEST-SUPPORT: PERF-PROXY-063 | Tests: sMPGWM.Tests.PerformanceProcessTests
                        counters["socketErrors"] += 1 # TEST-SUPPORT: PERF-PROXY-064 | Tests: sMPGWM.Tests.PerformanceProcessTests
        except Exception as error: # TEST-SUPPORT: PERF-PROXY-065 | Tests: sMPGWM.Tests.PerformanceProcessTests
            self.error = repr(error) # TEST-SUPPORT: PERF-PROXY-066 | Tests: sMPGWM.Tests.PerformanceProcessTests
        finally: # TEST-SUPPORT: PERF-PROXY-067 | Tests: sMPGWM.Tests.PerformanceProcessTests
            for outgoing in self.peers.values(): # TEST-SUPPORT: PERF-PROXY-068 | Tests: sMPGWM.Tests.PerformanceProcessTests
                outgoing.close() # TEST-SUPPORT: PERF-PROXY-069 | Tests: sMPGWM.Tests.PerformanceProcessTests
            self.listener.close() # TEST-SUPPORT: PERF-PROXY-070 | Tests: sMPGWM.Tests.PerformanceProcessTests
            self.selector.close() # TEST-SUPPORT: PERF-PROXY-071 | Tests: sMPGWM.Tests.PerformanceProcessTests

    def finish(self, path): # TEST-SUPPORT: PERF-PROXY-072 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.stop.set() # TEST-SUPPORT: PERF-PROXY-073 | Tests: sMPGWM.Tests.PerformanceProcessTests
        self.thread.join(timeout=5) # TEST-SUPPORT: PERF-PROXY-074 | Tests: sMPGWM.Tests.PerformanceProcessTests
        if self.thread.is_alive(): # TEST-SUPPORT: PERF-PROXY-075 | Tests: sMPGWM.Tests.PerformanceProcessTests
            raise RuntimeError("Impairment proxy did not stop.") # TEST-SUPPORT: PERF-PROXY-076 | Tests: sMPGWM.Tests.PerformanceProcessTests
        report = dict(profile=self.profile, seed=1042, delaySecondsEachDirection=[self.minimum,self.maximum], delayDistribution="uniform", lossProbabilityEachDirection=self.loss, mechanism="localhost UDP datagram proxy; delay-induced reordering; no duplication or bandwidth limit", listenerPort=self.port, hostPort=self.host[1], peers=len(self.peers), counters=self.counts, pendingAtStop=len(self.pending), maximumPending=self.maximum_pending, error=self.error) # TEST-SUPPORT: PERF-PROXY-077 | Tests: sMPGWM.Tests.PerformanceProcessTests
        path.write_text(json.dumps(report, indent=2)) # TEST-SUPPORT: PERF-PROXY-078 | Tests: sMPGWM.Tests.PerformanceProcessTests
        if self.error is not None: # TEST-SUPPORT: PERF-PROXY-FINISH-ERROR-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
            raise RuntimeError("Impairment proxy failed: " + self.error) # TEST-SUPPORT: PERF-PROXY-FINISH-ERROR-2 | Tests: sMPGWM.Tests.PerformanceProcessTests

TEST = "sMPGWM.Tests.PerformanceProcessTests" # TEST-SUPPORT: PERF-RUN-005 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser = argparse.ArgumentParser() # TEST-SUPPORT: PERF-RUN-006 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--output", required=True) # TEST-SUPPORT: PERF-RUN-007 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--players", type=int, nargs="+", default=[5,10]) # TEST-SUPPORT: PERF-RUN-008 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--repeats", type=int, default=3) # TEST-SUPPORT: PERF-RUN-009 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--seconds", type=float, default=630) # TEST-SUPPORT: PERF-RUN-010 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--scenario", default="matrix") # TEST-SUPPORT: PERF-RUN-011 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--weapons", type=int, choices=[0,9], default=0) # TEST-SUPPORT: PERF-FIRE-LOAD-001 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--weapon-rate", type=int, choices=[15,60], default=15) # TEST-SUPPORT: PERF-FIRE-LOAD-002 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--configuration", default="Release", choices=["Debug","Release"]) # TEST-SUPPORT: PERF-RUN-012 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--diagnostic", action="store_true") # TEST-SUPPORT: PERF-RUN-013 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--uninstrumented", action="store_true") # TEST-SUPPORT: PERF-RUN-014 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--latejoin", action="store_true") # TEST-SUPPORT: PERF-RUN-015 | Tests: sMPGWM.Tests.PerformanceProcessTests
parser.add_argument("--impairment", default="none", choices=["none","moderate","stress"]) # TEST-SUPPORT: PERF-PROXY-OPTION | Tests: sMPGWM.Tests.PerformanceProcessTests
args = parser.parse_args() # TEST-SUPPORT: PERF-RUN-016 | Tests: sMPGWM.Tests.PerformanceProcessTests
output = Path(args.output).resolve() # TEST-SUPPORT: PERF-RUN-017 | Tests: sMPGWM.Tests.PerformanceProcessTests
output.mkdir(parents=True, exist_ok=True)
if (output / "manifest.json").exists():
    raise RuntimeError("Refusing to overwrite existing measurement evidence: " + str(output)) # TEST-SUPPORT: PERF-RUN-018 | Tests: sMPGWM.Tests.PerformanceProcessTests
project = Path(tempfile.mkdtemp(prefix="smpgwm-performance-")) / "project" # TEST-SUPPORT: PERF-RUN-019 | Tests: sMPGWM.Tests.PerformanceProcessTests
engine = os.environ.get("GODOT_BIN", str(Path.home() / "godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe")) # TEST-SUPPORT: PERF-RUN-020 | Tests: sMPGWM.Tests.PerformanceProcessTests
shutil.copytree(ROOT / "sMPGWM.Game", project, ignore=shutil.ignore_patterns("shader_cache","editor","obj")) # TEST-SUPPORT: PERF-RUN-021 | Tests: sMPGWM.Tests.PerformanceProcessTests
verification = project / "Scripts/Verification" # TEST-SUPPORT: PERF-RUN-022 | Tests: sMPGWM.Tests.PerformanceProcessTests
verification.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: PERF-RUN-023 | Tests: sMPGWM.Tests.PerformanceProcessTests
for name in ("PerformanceVerification", "PerformanceRecorder"): # TEST-SUPPORT: PERF-RUN-024 | Tests: sMPGWM.Tests.PerformanceProcessTests
    shutil.copyfile(ROOT / "tools/fixtures" / (name + ".cs.txt"), verification / (name + ".cs")) # TEST-SUPPORT: PERF-RUN-025 | Tests: sMPGWM.Tests.PerformanceProcessTests
settings = project / "project.godot" # TEST-SUPPORT: PERF-RUN-026 | Tests: sMPGWM.Tests.PerformanceProcessTests
settings.write_text(settings.read_text(encoding="utf-8").replace("[autoload]", '[autoload]\nPerformanceVerification="*res://Scripts/Verification/PerformanceVerification.cs"'), encoding="utf-8") # TEST-SUPPORT: PERF-RUN-027 | Tests: sMPGWM.Tests.PerformanceProcessTests
config = project / "config/config.cfg" # TEST-SUPPORT: PERF-RUN-028 | Tests: sMPGWM.Tests.PerformanceProcessTests
if not args.diagnostic: # TEST-SUPPORT: PERF-RUN-029 | Tests: sMPGWM.Tests.PerformanceProcessTests
    config.write_text(config.read_text().replace('log_level_value = "Debug"', 'log_level_value = "Info"').replace("development = true","development = false")) # TEST-SUPPORT: PERF-RUN-030 | Tests: sMPGWM.Tests.PerformanceProcessTests
counter = 0 # TEST-SUPPORT: PERF-RUN-031 | Tests: sMPGWM.Tests.PerformanceProcessTests
def cs(line): # TEST-SUPPORT: PERF-RUN-032 | Tests: sMPGWM.Tests.PerformanceProcessTests
    global counter # TEST-SUPPORT: PERF-RUN-033 | Tests: sMPGWM.Tests.PerformanceProcessTests
    counter += 1 # TEST-SUPPORT: PERF-RUN-034 | Tests: sMPGWM.Tests.PerformanceProcessTests
    return line + " // TEST-SUPPORT: PERF-INJECT-" + str(counter) + " | Tests: " + TEST + "\n" # TEST-SUPPORT: PERF-RUN-035 | Tests: sMPGWM.Tests.PerformanceProcessTests
def replace(path, old, new): # TEST-SUPPORT: PERF-RUN-036 | Tests: sMPGWM.Tests.PerformanceProcessTests
    source = path.read_text(encoding="utf-8") # TEST-SUPPORT: PERF-RUN-037 | Tests: sMPGWM.Tests.PerformanceProcessTests
    if source.count(old) != 1: # TEST-SUPPORT: PERF-RUN-038 | Tests: sMPGWM.Tests.PerformanceProcessTests
        raise RuntimeError("Missing/ambiguous instrumentation anchor: " + old) # TEST-SUPPORT: PERF-RUN-039 | Tests: sMPGWM.Tests.PerformanceProcessTests
    path.write_text(source.replace(old,new), encoding="utf-8") # TEST-SUPPORT: PERF-RUN-040 | Tests: sMPGWM.Tests.PerformanceProcessTests
coordinator = project / "Scripts/NetworkSystem/SynchronizationCoordinator.cs" # TEST-SUPPORT: PERF-RUN-041 | Tests: sMPGWM.Tests.PerformanceProcessTests
if args.weapons == 9: # TEST-SUPPORT: PERF-FIRE-LOAD-003 | Tests: sMPGWM.Tests.PerformanceProcessTests
    groups = '[' + ', '.join('"weapon_' + str(index) + '"' for index in range(1,10)) + ']' # TEST-SUPPORT: PERF-FIRE-LOAD-004 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(project / "Scripts/GamePlaySystem/StartLoadout.cs", '        "weapon_1" => "vulcan_mk1",', cs('        var key when key.StartsWith("weapon_", StringComparison.Ordinal) => "vulcan_mk1",')) # TEST-SUPPORT: PERF-FIRE-LOAD-005 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(project / "Scripts/GamePlaySystem/StartLoadout.cs", '        if (equippedAny) equipment.ApplyWeaponGroupDefinition(["weapon_1"], []);', cs('        if (equippedAny) equipment.ApplyWeaponGroupDefinition(' + groups + ', ' + groups + ');')) # TEST-SUPPORT: PERF-FIRE-LOAD-006 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(project / "Scripts/GamePlaySystem/DevelopmentLoadout.cs", '        equipment.ApplyWeaponGroupDefinition(["weapon_1", "weapon_3"], ["weapon_2"]);', cs('        equipment.ApplyWeaponGroupDefinition(' + groups + ', ' + groups + ');')) # TEST-SUPPORT: PERF-FIRE-LOAD-007 | Tests: sMPGWM.Tests.PerformanceProcessTests
    reactor = project / "assets/game_data/items/reactor.yml" # TEST-SUPPORT: PERF-FIRE-LOAD-010 | Tests: sMPGWM.Tests.PerformanceProcessTests
    reactor.write_text(reactor.read_text().replace('EnergyProduction: 24', 'EnergyProduction: 1000')) # TEST-SUPPORT: PERF-FIRE-LOAD-011 | Tests: sMPGWM.Tests.PerformanceProcessTests
if args.scenario == "combat":
    shield = project / "assets/game_data/items/shield_generator.yml"
    shield.write_text(shield.read_text().replace("ShieldCapacity: 100", "ShieldCapacity: 1000000"))
if args.weapon_rate != 15: # TEST-SUPPORT: PERF-FIRE-LOAD-008 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(project / "assets/game_data/items/light_weapon.yml", '  AttackSpeed: 15', '  AttackSpeed: ' + str(args.weapon_rate)) # TEST-SUPPORT: PERF-FIRE-LOAD-009 | Tests: sMPGWM.Tests.PerformanceProcessTests
if not args.uninstrumented: # TEST-SUPPORT: PERF-RUN-042 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(coordinator, "    public override void _PhysicsProcess(double delta)\n    {", cs("    public override void _PhysicsProcess(double delta)") + cs("    {") + cs("        var perfStart = sMPGWM.Scripts.Verification.PerformanceRecorder.BeginStep();") + cs("        try {")) # TEST-SUPPORT: PERF-RUN-043 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(coordinator, "    public void TimingReply(long token, long hostTick)", cs("        finally { sMPGWM.Scripts.Verification.PerformanceRecorder.EndStep(perfStart, CompletedTick, Network.IsHost()); }") + cs("    }") + "    public void TimingReply(long token, long hostTick)") # TEST-SUPPORT: PERF-RUN-044 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(coordinator, "                Network.PublishMovement(Baseline());", cs("                var perfBytes = Baseline(); Network.PublishMovement(perfBytes); sMPGWM.Scripts.Verification.PerformanceRecorder.Publication(tick, perfBytes.Length);")) # TEST-SUPPORT: PERF-RUN-045 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(coordinator, "        CompletedTick = tick;\n        _clock.Observe", cs("        sMPGWM.Scripts.Verification.PerformanceRecorder.Arrival(tick);") + "        CompletedTick = tick;\n        _clock.Observe") # TEST-SUPPORT: PERF-RUN-046 | Tests: sMPGWM.Tests.PerformanceProcessTests
for relative, anchor, label in [("Scripts/GamePlaySystem/SessionInitialization.cs", "            prepare();", "definitions"), ("Scripts/GamePlaySystem/SessionInitialization.cs", "        construct();", "construction"), ("Scripts/GamePlaySystem/SessionInitialization.cs", "            bindAndActivate();", "binding"), ("Scripts/ShipSystem/ShipFactory.cs", "            ship = GamePlayProvider.CreateShip();", "ship_resource_instance"), ("Scripts/ShipSystem/ShipFactory.cs", "            ship.ConfigureLoadout(store, authoritative);", "loadout")]: # TEST-SUPPORT: PERF-PHASE-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
    replace(project / relative, anchor, cs('        sMPGWM.Scripts.Verification.PerformanceRecorder.Mark("phase_' + label + '_start");') + anchor + "\n" + cs('        sMPGWM.Scripts.Verification.PerformanceRecorder.Mark("phase_' + label + '_end");')) # TEST-SUPPORT: PERF-PHASE-2 | Tests: sMPGWM.Tests.PerformanceProcessTests
manifest = dict(revision=subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip(), arguments=vars(args), project=str(project), engine=engine, startedUtc=time.strftime("%Y-%m-%dT%H:%M:%SZ",time.gmtime()), placement="all headless peers on one machine", profiler="bounded in-process raw recorder" if not args.uninstrumented else "disabled recorder") # TEST-SUPPORT: PERF-RUN-047 | Tests: sMPGWM.Tests.PerformanceProcessTests
manifest["hashes"] = {str(p.relative_to(project)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(project.rglob("*")) if p.is_file() and (p.suffix in (".cs",".yml",".json",".cfg") and ".godot" not in p.parts)} # TEST-SUPPORT: PERF-RUN-048 | Tests: sMPGWM.Tests.PerformanceProcessTests
manifest["environment"] = dict(os=platform.platform(), logicalProcessors=os.cpu_count(), engineVersion=subprocess.check_output([engine, "--version"], text=True).strip(), dotnetVersion=subprocess.check_output(["dotnet", "--version"], text=True).strip())
manifest["sourceState"] = subprocess.check_output(["git", "status", "--short"], cwd=ROOT, text=True)
# Retain copied executable source, including untracked files and instrumentation.
with zipfile.ZipFile(output / "source.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(project.rglob("*")):
        if path.is_file() and ".godot" not in path.parts and (path.suffix in (".cs", ".csproj", ".yml", ".json", ".cfg", ".tscn") or path.name == "project.godot"):
            archive.write(path, str(path.relative_to(project)))
(output / "manifest.json").write_text(json.dumps(manifest,indent=2)) # TEST-SUPPORT: PERF-RUN-049 | Tests: sMPGWM.Tests.PerformanceProcessTests
(output / "initial.patch").write_bytes(subprocess.check_output(["git","diff","--binary"],cwd=ROOT)) # TEST-SUPPORT: PERF-RUN-050 | Tests: sMPGWM.Tests.PerformanceProcessTests
shutil.copyfile(config,output / "config.cfg") # TEST-SUPPORT: PERF-RUN-051 | Tests: sMPGWM.Tests.PerformanceProcessTests
shutil.copytree(project / "assets/game_data", output / "definitions", dirs_exist_ok=True) # TEST-SUPPORT: PERF-RUN-052 | Tests: sMPGWM.Tests.PerformanceProcessTests
with (output / "build.log").open("w") as log: # TEST-SUPPORT: PERF-RUN-053 | Tests: sMPGWM.Tests.PerformanceProcessTests
    subprocess.run(["dotnet","build",str(project / "sMPGWM.csproj"),"-c",args.configuration],check=True,stdout=log,stderr=subprocess.STDOUT) # TEST-SUPPORT: PERF-RUN-054 | Tests: sMPGWM.Tests.PerformanceProcessTests
if args.configuration == "Release": # TEST-SUPPORT: PERF-RUN-055 | Tests: sMPGWM.Tests.PerformanceProcessTests
    shutil.copytree(project / ".godot/mono/temp/bin/Release",project / ".godot/mono/temp/bin/Debug",dirs_exist_ok=True) # TEST-SUPPORT: PERF-RUN-056 | Tests: sMPGWM.Tests.PerformanceProcessTests
results = [] # TEST-SUPPORT: PERF-RUN-057 | Tests: sMPGWM.Tests.PerformanceProcessTests
for players in args.players: # TEST-SUPPORT: PERF-RUN-058 | Tests: sMPGWM.Tests.PerformanceProcessTests
    for repeat in range(1,args.repeats+1): # TEST-SUPPORT: PERF-RUN-059 | Tests: sMPGWM.Tests.PerformanceProcessTests
        run = output / (str(players) + "-players-run-" + str(repeat)) # TEST-SUPPORT: PERF-RUN-060 | Tests: sMPGWM.Tests.PerformanceProcessTests
        run.mkdir(exist_ok=True) # TEST-SUPPORT: PERF-RUN-061 | Tests: sMPGWM.Tests.PerformanceProcessTests
        with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as sock: # TEST-SUPPORT: PERF-RUN-062 | Tests: sMPGWM.Tests.PerformanceProcessTests
            sock.bind(("127.0.0.1",0)) # TEST-SUPPORT: PERF-RUN-063 | Tests: sMPGWM.Tests.PerformanceProcessTests
            port = sock.getsockname()[1] # TEST-SUPPORT: PERF-RUN-064 | Tests: sMPGWM.Tests.PerformanceProcessTests
        processes = [] # TEST-SUPPORT: PERF-RUN-065 | Tests: sMPGWM.Tests.PerformanceProcessTests
        proxy = DatagramProxy(port,args.impairment) if args.impairment != "none" else None # TEST-SUPPORT: PERF-PROXY-START | Tests: sMPGWM.Tests.PerformanceProcessTests
        started = time.monotonic() # TEST-SUPPORT: PERF-RUN-066 | Tests: sMPGWM.Tests.PerformanceProcessTests
        print("RUN",run.name,"port",port,flush=True) # TEST-SUPPORT: PERF-RUN-067 | Tests: sMPGWM.Tests.PerformanceProcessTests
        try: # TEST-SUPPORT: PERF-RUN-068 | Tests: sMPGWM.Tests.PerformanceProcessTests
            for index in range(players): # TEST-SUPPORT: PERF-RUN-069 | Tests: sMPGWM.Tests.PerformanceProcessTests
                name = "host" if index == 0 else "client-" + str(index) # TEST-SUPPORT: PERF-RUN-070 | Tests: sMPGWM.Tests.PerformanceProcessTests
                delay = 0 if index == 0 else (10 if args.scenario == "restored" else 3) + (index-1)*0.5 # TEST-SUPPORT: PERF-RUN-071 | Tests: sMPGWM.Tests.PerformanceProcessTests
                if args.latejoin and index == players-1: # TEST-SUPPORT: PERF-RUN-072 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    delay = 60 # TEST-SUPPORT: PERF-RUN-073 | Tests: sMPGWM.Tests.PerformanceProcessTests
                time.sleep(max(0,started+delay-time.monotonic())) # TEST-SUPPORT: PERF-RUN-074 | Tests: sMPGWM.Tests.PerformanceProcessTests
                peer = run / name # TEST-SUPPORT: PERF-RUN-075 | Tests: sMPGWM.Tests.PerformanceProcessTests
                peer.mkdir(exist_ok=True) # TEST-SUPPORT: PERF-RUN-076 | Tests: sMPGWM.Tests.PerformanceProcessTests
                stream = (peer / "engine.log").open("w",encoding="utf-8") # TEST-SUPPORT: PERF-RUN-077 | Tests: sMPGWM.Tests.PerformanceProcessTests
                seconds = args.seconds + (3 if index == 0 else 0) - delay # TEST-SUPPORT: PERF-RUN-078 | Tests: sMPGWM.Tests.PerformanceProcessTests
                env = dict(os.environ,APPDATA=str(peer / "userdata"),PERF_ROLE="host" if index == 0 else "client",PERF_PORT=str(port),PERF_SECONDS=str(seconds),PERF_SCENARIO=args.scenario,PERF_OUTPUT=str(peer),PERF_OFFSET=str(delay),PERF_PLAYERS=str(players)) # TEST-SUPPORT: PERF-RUN-079 | Tests: sMPGWM.Tests.PerformanceProcessTests
                env["PERF_COLLECT"] = "0" if args.uninstrumented else "1" # TEST-SUPPORT: PERF-COLLECT-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
                if proxy is not None and index != 0: # TEST-SUPPORT: PERF-PROXY-CLIENT-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    env["PERF_PORT"] = str(proxy.port) # TEST-SUPPORT: PERF-PROXY-CLIENT-2 | Tests: sMPGWM.Tests.PerformanceProcessTests
                process = subprocess.Popen([engine,"--headless","--max-fps","60","--path",str(project)],stdout=stream,stderr=subprocess.STDOUT,env=env) # TEST-SUPPORT: PERF-RUN-080 | Tests: sMPGWM.Tests.PerformanceProcessTests
                processes.append((name,process,stream)) # TEST-SUPPORT: PERF-RUN-081 | Tests: sMPGWM.Tests.PerformanceProcessTests
            while any(p.poll() is None for _,p,_ in processes): # TEST-SUPPORT: PERF-RUN-082 | Tests: sMPGWM.Tests.PerformanceProcessTests
                if proxy is not None and proxy.error is not None: # TEST-SUPPORT: PERF-PROXY-FAIL-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    raise RuntimeError("Impairment proxy failed: " + proxy.error) # TEST-SUPPORT: PERF-PROXY-FAIL-2 | Tests: sMPGWM.Tests.PerformanceProcessTests
                if time.monotonic()-started > args.seconds+60: # TEST-SUPPORT: PERF-RUN-083 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    raise TimeoutError("Performance run timed out") # TEST-SUPPORT: PERF-RUN-084 | Tests: sMPGWM.Tests.PerformanceProcessTests
                time.sleep(1) # TEST-SUPPORT: PERF-RUN-085 | Tests: sMPGWM.Tests.PerformanceProcessTests
            codes = {name:p.returncode for name,p,_ in processes} # TEST-SUPPORT: PERF-RUN-086 | Tests: sMPGWM.Tests.PerformanceProcessTests
            results.append(dict(run=run.name,exitCodes=codes,seconds=time.monotonic()-started)) # TEST-SUPPORT: PERF-RUN-087 | Tests: sMPGWM.Tests.PerformanceProcessTests
            (output / "runs.json").write_text(json.dumps(results,indent=2)) # TEST-SUPPORT: PERF-RUN-088 | Tests: sMPGWM.Tests.PerformanceProcessTests
            print("DONE",run.name,codes,flush=True) # TEST-SUPPORT: PERF-RUN-089 | Tests: sMPGWM.Tests.PerformanceProcessTests
            if any(codes.values()): # TEST-SUPPORT: PERF-RUN-090 | Tests: sMPGWM.Tests.PerformanceProcessTests
                raise RuntimeError("Peer failed: " + str(codes)) # TEST-SUPPORT: PERF-RUN-091 | Tests: sMPGWM.Tests.PerformanceProcessTests
        finally: # TEST-SUPPORT: PERF-RUN-092 | Tests: sMPGWM.Tests.PerformanceProcessTests
            for _,process,stream in processes: # TEST-SUPPORT: PERF-RUN-093 | Tests: sMPGWM.Tests.PerformanceProcessTests
                if process.poll() is None: # TEST-SUPPORT: PERF-RUN-094 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    process.terminate() # TEST-SUPPORT: PERF-RUN-095 | Tests: sMPGWM.Tests.PerformanceProcessTests
                    process.wait(timeout=15) # TEST-SUPPORT: PERF-RUN-096 | Tests: sMPGWM.Tests.PerformanceProcessTests
                stream.close() # TEST-SUPPORT: PERF-RUN-097 | Tests: sMPGWM.Tests.PerformanceProcessTests
            if proxy is not None: # TEST-SUPPORT: PERF-PROXY-STOP-1 | Tests: sMPGWM.Tests.PerformanceProcessTests
                proxy.finish(run / "impairment.json") # TEST-SUPPORT: PERF-PROXY-STOP-2 | Tests: sMPGWM.Tests.PerformanceProcessTests
print("COMPLETE",output,flush=True) # TEST-SUPPORT: PERF-RUN-098 | Tests: sMPGWM.Tests.PerformanceProcessTests

