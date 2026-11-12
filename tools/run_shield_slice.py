"""Run the opt-in normal-startup ENet shield verification with two late clients.""" # TEST-SUPPORT: SHIELD-PROCESS-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
from pathlib import Path # TEST-SUPPORT: SHIELD-PROCESS-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import os # TEST-SUPPORT: SHIELD-PROCESS-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import argparse # TEST-SUPPORT: SHIELD-PROCESS-004 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import subprocess # TEST-SUPPORT: SHIELD-PROCESS-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import tempfile # TEST-SUPPORT: SHIELD-PROCESS-006 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import time # TEST-SUPPORT: SHIELD-PROCESS-007 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import socket # TEST-SUPPORT: SHIELD-PROCESS-008 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import selectors # TEST-SUPPORT: SHIELD-PROCESS-009 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import threading # TEST-SUPPORT: SHIELD-PROCESS-010 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import random # TEST-SUPPORT: SHIELD-PROCESS-011 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import heapq # TEST-SUPPORT: SHIELD-PROCESS-012 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import shutil # TEST-SUPPORT: ABILITY-PROCESS-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import re # TEST-SUPPORT: ABILITY-PROCESS-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge

root = Path(__file__).resolve().parents[1] # TEST-SUPPORT: SHIELD-PROCESS-013 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
engine = os.environ.get('GODOT_BIN', str(Path.home() / 'godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe')) # TEST-SUPPORT: SHIELD-PROCESS-014 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
logs = Path(tempfile.mkdtemp(prefix='shield-slice-')) # TEST-SUPPORT: SHIELD-PROCESS-015 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser = argparse.ArgumentParser() # TEST-SUPPORT: SHIELD-PROCESS-016 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--players", type=int, default=3, choices=range(3, 11)) # TEST-SUPPORT: SHIELD-PROCESS-017 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--impaired", action="store_true") # TEST-SUPPORT: SHIELD-PROCESS-018 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--variants", action="store_true") # TEST-SUPPORT: ABILITY-PROCESS-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--movement", action="store_true") # TEST-SUPPORT: RESOURCE-MOTION-OPTION-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--baseline", action="store_true") # TEST-SUPPORT: SIM-BASELINE-OPTION-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--steam-smoke", action="store_true") # TEST-SUPPORT: STEAM-RUNNER-OPTION-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--comparison-workload", action="store_true") # TEST-SUPPORT: COMPARE-WORKLOAD-OPTION | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
parser.add_argument("--detached-latejoin", action="store_true") # TEST-SUPPORT: DETACHED-LATEJOIN-OPTION | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
options = parser.parse_args() # TEST-SUPPORT: SHIELD-PROCESS-019 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
players = options.players # TEST-SUPPORT: SHIELD-PROCESS-020 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
project = logs / 'project' # TEST-SUPPORT: EQUIP-STAGE-001 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
shutil.copytree(root / 'sMPGWM.Game', project, ignore=shutil.ignore_patterns('shader_cache', 'editor', 'obj')) # TEST-SUPPORT: EQUIP-STAGE-002 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
if options.baseline: # TEST-SUPPORT: SIM-BASELINE-STAGE-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    import io # TEST-SUPPORT: SIM-BASELINE-STAGE-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    import zipfile # TEST-SUPPORT: SIM-BASELINE-STAGE-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    archive = subprocess.check_output(['git', 'archive', '--format=zip', 'HEAD:sMPGWM.Game'], cwd=root) # TEST-SUPPORT: SIM-BASELINE-STAGE-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    with zipfile.ZipFile(io.BytesIO(archive)) as baseline: # TEST-SUPPORT: SIM-BASELINE-STAGE-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for relative in baseline.namelist(): # TEST-SUPPORT: SIM-BASELINE-STAGE-5 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if relative.endswith('/'): # TEST-SUPPORT: SIM-BASELINE-STAGE-6 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                continue # TEST-SUPPORT: SIM-BASELINE-STAGE-7 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if baseline.read(relative).startswith(b'version https://git-lfs.github.com/spec/v1'): continue # TEST-SUPPORT: BASELINE-MATERIALIZED-ASSETS | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            destination = project / relative # TEST-SUPPORT: SIM-BASELINE-STAGE-8 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            destination.parent.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: SIM-BASELINE-STAGE-9 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if destination.exists(): destination.chmod(0o666) # TEST-SUPPORT: BASELINE-WRITABLE-COPY | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if destination.exists(): destination.unlink() # TEST-SUPPORT: BASELINE-REPLACE-STAGED | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            destination.write_bytes(baseline.read(relative)) # TEST-SUPPORT: SIM-BASELINE-STAGE-10 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    tracked = set(subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', 'HEAD:sMPGWM.Game'], cwd=root).decode().splitlines()) # TEST-SUPPORT: SIM-BASELINE-STAGE-11 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for file in (project / 'Scripts').rglob('*.cs'): # TEST-SUPPORT: SIM-BASELINE-STAGE-12 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if file.relative_to(project).as_posix() not in tracked: # TEST-SUPPORT: SIM-BASELINE-STAGE-13 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            file.unlink() # TEST-SUPPORT: SIM-BASELINE-STAGE-14 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
fixture = project / 'Scripts/Verification/EquipmentAbilityVerification.cs' # TEST-SUPPORT: EQUIP-STAGE-003 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
fixture.parent.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: EQUIP-STAGE-004 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
shutil.copyfile(root / 'tools/fixtures/EquipmentAbilityVerification.cs.txt', fixture) # TEST-SUPPORT: EQUIP-STAGE-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
if options.baseline: # TEST-SUPPORT: SIM-BASELINE-FIXTURE-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    fixture.write_bytes(subprocess.check_output(['git', 'show', 'HEAD:tools/fixtures/EquipmentAbilityVerification.cs.txt'], cwd=root)) # TEST-SUPPORT: SIM-BASELINE-FIXTURE-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
if options.baseline: # TEST-SUPPORT: BASELINE-TARGET-MARGIN-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    fixture.write_text(fixture.read_text(encoding='utf-8').replace('<= 600 * 600)', '<= 300 * 300)'), encoding='utf-8') # TEST-SUPPORT: BASELINE-TARGET-MARGIN-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
settings = project / 'project.godot' # TEST-SUPPORT: EQUIP-STAGE-006 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
settings.write_text(settings.read_text(encoding='utf-8').replace('[autoload]', '[autoload]\nEquipmentAbilityVerification="*res://Scripts/Verification/EquipmentAbilityVerification.cs"'), encoding='utf-8') # TEST-SUPPORT: EQUIP-STAGE-007 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
coordinator = project / 'Scripts/NetworkSystem/SynchronizationCoordinator.cs' # TEST-SUPPORT: RESOURCE-METRIC-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
source = coordinator.read_text(encoding='utf-8') # TEST-SUPPORT: RESOURCE-METRIC-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
anchors = ['    public PlayerController Player', '        if (!_active) return;', '            if (tick % 2 == 0) Network.PublishMovement(Baseline());'] # TEST-SUPPORT: SIM-METRIC-ANCHOR-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
for anchor in anchors: # TEST-SUPPORT: SIM-METRIC-ANCHOR-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    if source.count(anchor) != 1: # TEST-SUPPORT: SIM-METRIC-ANCHOR-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        raise RuntimeError('Instrumentation anchor missing or ambiguous: ' + anchor) # TEST-SUPPORT: SIM-METRIC-ANCHOR-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
cs_marker = ' // TEST-SUPPORT: RESOURCE-HOST-METRIC-' # TEST-SUPPORT: RESOURCE-METRIC-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
metric_fields = ['public long VerificationStepTicks;', 'public long VerificationMaxStepTicks;', 'public long VerificationSteps;', 'public long VerificationPublicationTicks;', 'public long VerificationPublications;', 'public long VerificationLastPublication;', 'public int VerificationMaxBytes;'] # TEST-SUPPORT: RESOURCE-METRIC-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
source = source.replace('    public PlayerController Player', '\n'.join('    ' + line + cs_marker + str(i) + ' | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge' for i, line in enumerate(metric_fields)) + '\n    public PlayerController Player') # TEST-SUPPORT: RESOURCE-METRIC-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
source = source.replace('        if (!_active) return;', '        var verificationStarted = System.Diagnostics.Stopwatch.GetTimestamp();' + cs_marker + '100 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge\n        if (!_active) return;') # TEST-SUPPORT: RESOURCE-METRIC-5 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
metric_lines = ['if (tick % 2 == 0)', '{', 'var verificationBytes = Baseline();', 'Network.PublishMovement(verificationBytes);', 'VerificationMaxBytes = Math.Max(VerificationMaxBytes, verificationBytes.Length);', 'var verificationNow = System.Diagnostics.Stopwatch.GetTimestamp();', 'if (VerificationLastPublication > 0) { VerificationPublicationTicks += verificationNow - VerificationLastPublication; VerificationPublications++; }', 'VerificationLastPublication = verificationNow;', '}', 'var verificationElapsed = System.Diagnostics.Stopwatch.GetTimestamp() - verificationStarted;', 'VerificationStepTicks += verificationElapsed;', 'VerificationMaxStepTicks = Math.Max(VerificationMaxStepTicks, verificationElapsed);', 'VerificationSteps++;'] # TEST-SUPPORT: RESOURCE-METRIC-6 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
source = source.replace('            if (tick % 2 == 0) Network.PublishMovement(Baseline());', '\n'.join('            ' + line + cs_marker + str(200+i) + ' | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge' for i, line in enumerate(metric_lines))) # TEST-SUPPORT: RESOURCE-METRIC-7 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
coordinator.write_text(source, encoding='utf-8') # TEST-SUPPORT: RESOURCE-METRIC-8 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
import hashlib # TEST-SUPPORT: SCENARIO-SOURCE-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
staged_digest = hashlib.sha256() # TEST-SUPPORT: SCENARIO-SOURCE-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
for staged_file in sorted((project / 'Scripts').rglob('*.cs')): # TEST-SUPPORT: SCENARIO-SOURCE-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    staged_digest.update(staged_file.relative_to(project).as_posix().encode()) # TEST-SUPPORT: SCENARIO-SOURCE-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    staged_digest.update(staged_file.read_bytes()) # TEST-SUPPORT: SCENARIO-SOURCE-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
staged_metadata = f"sourceSha256={staged_digest.hexdigest()} detachedLateJoin={options.detached_latejoin} baseline={options.baseline} comparison={options.comparison_workload} players={players} impaired={options.impaired} movement={options.movement} stagedUtc={time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}\n" # TEST-SUPPORT: SCENARIO-SOURCE-5 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
subprocess.run(['dotnet', 'build', str(project / 'sMPGWM.csproj')], check=True, stdout=subprocess.DEVNULL) # TEST-SUPPORT: EQUIP-STAGE-008 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
if options.variants: # TEST-SUPPORT: ABILITY-PROCESS-005 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    import yaml # TEST-SUPPORT: ABILITY-PROCESS-006 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    import copy # TEST-SUPPORT: ABILITY-PROCESS-007 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for name in ('Ability', 'Effect', 'Behaviour'): # TEST-SUPPORT: ABILITY-PROCESS-010 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        file = project / 'assets/game_data' / (name + 'Definitions.yml') # TEST-SUPPORT: ABILITY-PROCESS-011 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        document = yaml.safe_load(file.read_text(encoding='utf-8-sig')) # TEST-SUPPORT: ABILITY-PROCESS-012 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        rows = document[name + 'Definitions'] # TEST-SUPPORT: ABILITY-PROCESS-013 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        variant = copy.deepcopy(next(row for row in rows if row['Id'] == 'shieldCapacityBoost')) # TEST-SUPPORT: ABILITY-PROCESS-014 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        variant['Id'] = 'shieldVariant' # TEST-SUPPORT: ABILITY-PROCESS-015 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if name == 'Ability': variant['EffectId'] = 'shieldVariant' # TEST-SUPPORT: ABILITY-PROCESS-016 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        elif name == 'Effect': variant['BehaviourId'] = 'shieldVariant' # TEST-SUPPORT: ABILITY-PROCESS-017 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        else: # TEST-SUPPORT: ABILITY-PROCESS-018 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            variant['DisplayName'] = 'Shield Variant' # TEST-SUPPORT: ABILITY-PROCESS-019 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            variant['Lifetime']['DurationMs'] = 20000 # TEST-SUPPORT: ABILITY-PROCESS-020 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            variant['Modifiers'][0]['Value'] = 25 # TEST-SUPPORT: ABILITY-PROCESS-021 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        rows.append(variant) # TEST-SUPPORT: ABILITY-PROCESS-022 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        file.write_text(yaml.safe_dump(document, sort_keys=False), encoding='utf-8') # TEST-SUPPORT: ABILITY-PROCESS-023 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    item_file = project / 'assets/game_data/ItemDefinitions.yml' # TEST-SUPPORT: EQUIP-STAGE-009 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    item_data = yaml.safe_load(item_file.read_text()) # TEST-SUPPORT: EQUIP-STAGE-010 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for item in item_data['Items']: # TEST-SUPPORT: EQUIP-STAGE-011 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if item['Id'] == 'mock_advanced_shield_generator': item['AbilityIds'] = ['shieldVariant'] # TEST-SUPPORT: EQUIP-STAGE-012 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    item_file.write_text(yaml.safe_dump(item_data, sort_keys=False), encoding='utf-8') # TEST-SUPPORT: EQUIP-STAGE-013 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
processes = [] # TEST-SUPPORT: SHIELD-PROCESS-021 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
stop_proxy = threading.Event() # TEST-SUPPORT: SHIELD-PROCESS-022 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
def proxy(): # TEST-SUPPORT: SHIELD-PROCESS-023 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    randomizer = random.Random(1042) # TEST-SUPPORT: SHIELD-PROCESS-024 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    selector = selectors.DefaultSelector() # TEST-SUPPORT: SHIELD-PROCESS-025 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: SHIELD-PROCESS-026 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    listener.bind(('127.0.0.1', 55672)) # TEST-SUPPORT: SHIELD-PROCESS-027 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    listener.setblocking(False) # TEST-SUPPORT: SHIELD-PROCESS-028 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    selector.register(listener, selectors.EVENT_READ, None) # TEST-SUPPORT: SHIELD-PROCESS-029 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    peers, pending = {}, [] # TEST-SUPPORT: SHIELD-PROCESS-030 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    serial = 0 # TEST-SUPPORT: SHIELD-PROCESS-031 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    try: # TEST-SUPPORT: SHIELD-PROCESS-032 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        while not stop_proxy.is_set(): # TEST-SUPPORT: SHIELD-PROCESS-033 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            for key, _ in selector.select(.002): # TEST-SUPPORT: SHIELD-PROCESS-034 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                try: # TEST-SUPPORT: SHIELD-PROCESS-035 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    data, sender = key.fileobj.recvfrom(65535) # TEST-SUPPORT: SHIELD-PROCESS-036 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                except (ConnectionResetError, BlockingIOError): # TEST-SUPPORT: SHIELD-PROCESS-037 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    continue # TEST-SUPPORT: SHIELD-PROCESS-038 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                if key.fileobj is listener: # TEST-SUPPORT: SHIELD-PROCESS-039 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    if sender not in peers: # TEST-SUPPORT: SHIELD-PROCESS-040 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        outgoing = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # TEST-SUPPORT: SHIELD-PROCESS-041 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        outgoing.bind(('127.0.0.1', 0)) # TEST-SUPPORT: SHIELD-PROCESS-042 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        outgoing.setblocking(False) # TEST-SUPPORT: SHIELD-PROCESS-043 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        selector.register(outgoing, selectors.EVENT_READ, sender) # TEST-SUPPORT: SHIELD-PROCESS-044 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                        peers[sender] = outgoing # TEST-SUPPORT: SHIELD-PROCESS-045 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    destination, address = peers[sender], ('127.0.0.1', 55671) # TEST-SUPPORT: SHIELD-PROCESS-046 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                else: # TEST-SUPPORT: SHIELD-PROCESS-047 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    destination, address = listener, key.data # TEST-SUPPORT: SHIELD-PROCESS-048 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                if randomizer.random() < .01: # TEST-SUPPORT: SHIELD-PROCESS-049 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    continue # TEST-SUPPORT: SHIELD-PROCESS-050 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                serial += 1 # TEST-SUPPORT: SHIELD-PROCESS-051 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                heapq.heappush(pending, (time.monotonic() + randomizer.uniform(.02, .06), serial, destination, address, data)) # TEST-SUPPORT: SHIELD-PROCESS-052 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            now = time.monotonic() # TEST-SUPPORT: SHIELD-PROCESS-053 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            while pending and pending[0][0] <= now: # TEST-SUPPORT: SHIELD-PROCESS-054 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                _, _, destination, address, data = heapq.heappop(pending) # TEST-SUPPORT: SHIELD-PROCESS-055 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                destination.sendto(data, address) # TEST-SUPPORT: SHIELD-PROCESS-056 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    finally: # TEST-SUPPORT: SHIELD-PROCESS-057 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for outgoing in peers.values(): outgoing.close() # TEST-SUPPORT: SHIELD-PROCESS-058 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        listener.close() # TEST-SUPPORT: SHIELD-PROCESS-059 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        selector.close() # TEST-SUPPORT: SHIELD-PROCESS-060 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
if options.impaired: # TEST-SUPPORT: SHIELD-PROCESS-061 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    threading.Thread(target=proxy, daemon=True).start() # TEST-SUPPORT: SHIELD-PROCESS-062 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    print('Impairment: seed 1042, 20-60 ms delay each way, 1% packet loss, delay-induced reordering', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-063 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
started = time.monotonic() # TEST-SUPPORT: SHIELD-PROCESS-064 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
try: # TEST-SUPPORT: SHIELD-PROCESS-065 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    schedule = [('host', 0, 'host')] + [(f'client-{i}', 3 + i * .5 if i < players - 2 else 12, 'client') for i in range(players - 1)] # TEST-SUPPORT: SHIELD-PROCESS-066 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    if options.steam_smoke: schedule = [('host', 0, 'host')] # TEST-SUPPORT: STEAM-RUNNER-SCHEDULE-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for name, delay, role in schedule: # TEST-SUPPORT: SHIELD-PROCESS-067 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        time.sleep(max(0, started + delay - time.monotonic())) # TEST-SUPPORT: SHIELD-PROCESS-068 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        stream = (logs / (name + '.log')).open('w', encoding='utf-8') # TEST-SUPPORT: SHIELD-PROCESS-069 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        environment = dict(os.environ, APPDATA=str(logs / name / 'userdata')) # TEST-SUPPORT: SHIELD-PROCESS-070 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        process = subprocess.Popen([engine, '--headless', '--path', str(project), '--', '--shield-check-' + role] + (['--detached-latejoin'] if options.detached_latejoin else []) + (['--comparison-workload'] if options.comparison_workload else []) + (['--steam-smoke'] if options.steam_smoke else []) + (['--exercise-movement'] if options.movement else []) + (['--ability-variants'] if options.variants else []) + (['--shield-check-proxy'] if options.impaired and role == 'client' else []), stdout=stream, stderr=subprocess.STDOUT, env=environment) # TEST-SUPPORT: ABILITY-PROCESS-026 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        processes.append((name, process, stream)) # TEST-SUPPORT: SHIELD-PROCESS-072 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        print(f'{name}: started at {time.monotonic() - started:.1f}s', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-073 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    success = True # TEST-SUPPORT: SHIELD-PROCESS-074 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for name, process, stream in processes: # TEST-SUPPORT: SHIELD-PROCESS-075 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        code = process.wait(timeout=105) # TEST-SUPPORT: SHIELD-PROCESS-076 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        stream.close() # TEST-SUPPORT: SHIELD-PROCESS-077 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        print(f'{name}: exit={code}', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-078 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        content = (logs / (name + ".log")).read_text(encoding="utf-8") # TEST-SUPPORT: SHIELD-PROCESS-079 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        success &= code == 0 and "ERROR:" not in content # TEST-SUPPORT: SHIELD-PROCESS-080 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    if options.steam_smoke: # TEST-SUPPORT: STEAM-RUNNER-RESULT-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        content = (logs / 'host.log').read_text(encoding='utf-8') # TEST-SUPPORT: STEAM-RUNNER-RESULT-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        evidence = root / 'docs/plans/evidence/cleanup-steam-host.txt' # TEST-SUPPORT: STEAM-RUNNER-RESULT-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        evidence.parent.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: STEAM-RUNNER-RESULT-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        evidence.write_text(content, encoding='utf-8') # TEST-SUPPORT: STEAM-RUNNER-RESULT-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        success &= 'SHIELD_CHECK steam success=True' in content # TEST-SUPPORT: STEAM-RUNNER-RESULT-5 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        print(f'Steam host lifecycle: success={success}; logs={logs}', flush=True) # TEST-SUPPORT: STEAM-RUNNER-RESULT-6 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        raise SystemExit(0 if success else 1) # TEST-SUPPORT: STEAM-RUNNER-RESULT-7 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    evidence = root / ('docs/plans/evidence/' + ('baseline-' if options.baseline else 'comparison-' if options.comparison_workload else 'cleanup-detached-latejoin-' if options.detached_latejoin else 'cleanup-') + ('equipment-resources-variants' if options.variants else 'equipment-resources') + '-enet-' + str(players) + ('-impaired' if options.impaired else '') + ('-moving' if options.movement else '') + '.txt') # TEST-SUPPORT: ABILITY-PROCESS-027 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    evidence.parent.mkdir(parents=True, exist_ok=True) # TEST-SUPPORT: SHIELD-PROCESS-082 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    with evidence.open('w', encoding='utf-8') as report: # TEST-SUPPORT: SHIELD-PROCESS-083 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        report.write(staged_metadata) # TEST-SUPPORT: SCENARIO-SOURCE-REPORT | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for name, _, _ in processes: # TEST-SUPPORT: SHIELD-PROCESS-084 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            report.write(name + '\n') # TEST-SUPPORT: SHIELD-PROCESS-085 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            for line in (logs / (name + '.log')).read_text(encoding='utf-8').splitlines(): # TEST-SUPPORT: SHIELD-PROCESS-086 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                if 'SHIELD_CHECK' in line or 'ERROR:' in line or 'NET join_ready' in line: # TEST-SUPPORT: SHIELD-PROCESS-087 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                    report.write(line + '\n') # TEST-SUPPORT: SHIELD-PROCESS-088 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    host_content = (logs / 'host.log').read_text(encoding='utf-8') # TEST-SUPPORT: SIM-METRIC-EXECUTED-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    metric = re.search(r'performance steps=(\d+) meanStepMs=([^ ]+) maxStepMs=([^ ]+) publicationMeanMs=([^ ]+) maxMovementBytes=(\d+)', host_content) # TEST-SUPPORT: SIM-METRIC-EXECUTED-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    success &= metric is not None and int(metric[1]) > 100 and 0 < int(metric[5]) <= 1400 and float(metric[4]) > 0 # TEST-SUPPORT: SIM-METRIC-EXECUTED-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for name, _, _ in processes: # TEST-SUPPORT: SIM-METRIC-EXECUTED-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        success &= 'SHIELD_CHECK complete success=True' in (logs / (name + '.log')).read_text(encoding='utf-8') # TEST-SUPPORT: SIM-METRIC-EXECUTED-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if options.detached_latejoin: # TEST-SUPPORT: DETACHED-LATEJOIN-ASSERT-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            baseline_log = (logs / (name + '.log')).read_text(encoding='utf-8') # TEST-SUPPORT: DETACHED-LATEJOIN-ASSERT-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            success &= all(marker in baseline_log for marker in ('SHIELD_CHECK detached_baseline', 'SHIELD_CHECK draining_baseline', 'SHIELD_CHECK restored_baseline')) # TEST-SUPPORT: DETACHED-LATEJOIN-ASSERT-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    observations = {} # TEST-SUPPORT: ABILITY-PROCESS-028 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    count = 0 # TEST-SUPPORT: ABILITY-PROCESS-029 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for name, _, _ in processes: # TEST-SUPPORT: ABILITY-PROCESS-030 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        content = (logs / (name + '.log')).read_text(encoding='utf-8') # TEST-SUPPORT: ABILITY-PROCESS-031 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for match in re.finditer(r'entity=(\w+) current=([^ ]+) max=([^ ]+) behaviours=([^ ]*) revision=(\d+)', content): # TEST-SUPPORT: ABILITY-PROCESS-032 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            entity, current, maximum, behaviours, revision = match.groups() # TEST-SUPPORT: ABILITY-PROCESS-033 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            key, state = (entity, revision), (current, maximum, behaviours) # TEST-SUPPORT: ABILITY-PROCESS-034 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            if key in observations and observations[key] != state: success = False # TEST-SUPPORT: ABILITY-PROCESS-035 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            observations[key] = state # TEST-SUPPORT: ABILITY-PROCESS-036 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            success &= 0 <= float(current.replace(',', '.')) <= float(maximum.replace(',', '.')) # TEST-SUPPORT: ABILITY-PROCESS-037 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            count += 1 # TEST-SUPPORT: ABILITY-PROCESS-038 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    if not options.baseline: # TEST-SUPPORT: COMPONENT-CONVERGENCE-0 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        component_observations = {} # TEST-SUPPORT: COMPONENT-CONVERGENCE-1 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        for name, _, _ in processes: # TEST-SUPPORT: COMPONENT-CONVERGENCE-2 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            content = (logs / (name + '.log')).read_text(encoding='utf-8') # TEST-SUPPORT: COMPONENT-CONVERGENCE-3 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            for match in re.finditer(r'entity=(\w+) .*?revision=(\d+) .*?components=(\d+)', content): # TEST-SUPPORT: COMPONENT-CONVERGENCE-4 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                entity, revision, components = match.groups() # TEST-SUPPORT: COMPONENT-CONVERGENCE-5 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                key = (entity, revision) # TEST-SUPPORT: COMPONENT-CONVERGENCE-6 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                if key in component_observations and component_observations[key] != components: success = False # TEST-SUPPORT: COMPONENT-CONVERGENCE-7 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
                component_observations[key] = components # TEST-SUPPORT: COMPONENT-CONVERGENCE-8 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        success &= options.comparison_workload or 'component stage=9' in host_content and 'ownership swap=1' in host_content # TEST-SUPPORT: COMPONENT-CONVERGENCE-9 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    success &= count > 0 # TEST-SUPPORT: ABILITY-PROCESS-039 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    print(f'Convergence: {count} observations, {len(observations)} entity revisions; success={success}', flush=True) # TEST-SUPPORT: ABILITY-PROCESS-040 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    print(f'Raw logs: {logs}', flush=True) # TEST-SUPPORT: SHIELD-PROCESS-089 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    raise SystemExit(0 if success else 1) # TEST-SUPPORT: SHIELD-PROCESS-090 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
finally: # TEST-SUPPORT: SHIELD-PROCESS-091 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    stop_proxy.set() # TEST-SUPPORT: SHIELD-PROCESS-092 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
    for _, process, stream in processes: # TEST-SUPPORT: SHIELD-PROCESS-093 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        if process.poll() is None: # TEST-SUPPORT: SHIELD-PROCESS-094 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            process.terminate() # TEST-SUPPORT: SHIELD-PROCESS-095 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
            process.wait(timeout=10) # TEST-SUPPORT: SHIELD-PROCESS-096 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
        stream.close() # TEST-SUPPORT: SHIELD-PROCESS-097 | Tests: sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge
