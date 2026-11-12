# Multiplayer measurements and cleanup, 2026-11-12

Status: partial; both workstreams remain open. These results establish correctness smoke coverage, not performance acceptance.

Revision: `a57352656b8ca7179bc81730772b6d477ea64022`, initially clean. The cleanup removes only the unused `FileLoader.AbortLoad` method using Rider safe-delete (no conflicts, one file). No runtime performance fix was applied. Environment, packaged definition hashes, project settings, configuration, TRX results and raw ENet logs are adjacent to this report. The SDK is 10.0.100; tests target .NET 8. Runs used Debug, no attached debugger, and shared this machine with the IDE. Test/build activity overlapped parts of the ENet run: timings must not be treated as isolated benchmark results.

## Executed checks

| Command | Outcome |
| --- | --- |
| `dotnet test sMPGWM.sln --no-restore --logger "trx;LogFileName=baseline.trx" --results-directory docs/measurements/2026-11-12-final/baseline` | 303 passed: 302 managed cases and one Godot wrapper. Wrapper separately reports 227 in-engine passes, zero skips. |
| `dotnet test sMPGWM.sln --no-restore --filter FullyQualifiedName=sMPGWM.Tests.AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge --logger "trx;LogFileName=enet-ten.trx" --results-directory docs/measurements/2026-11-12-final/enet` | Explicit case skipped; this invocation is not coverage. |
| `python tools/run_shield_slice.py --players 10 --impaired --variants` | All ten processes exited zero. Convergence succeeded: 6,610 observations, 2,534 entity revisions. |
| `dotnet test sMPGWM.sln --no-restore --logger "trx;LogFileName=cleanup.trx" --results-directory docs/measurements/2026-11-12-final/cleanup` | 303 passed after removal. |

ENet used the existing headless runner, isolated temporary project/user data and allocated ports. Seed 1042, 20–60 ms delay each direction, 1% packet loss, delay-induced reordering through the runner's UDP proxy. Host starts at zero, clients at 3–6.5 seconds, final client at 12 seconds. This is the existing short convergence workload, not three 180-second measurement intervals. Raw logs were copied from `shield-slice-xlc1_fck`; the staged project remains in the local temporary directory. Config changes made by the runner are reproducible from the runner at the recorded revision; packaged hashes are not a runtime compatibility fingerprint.

The existing tag benchmark was inspected but not executed: `RunBaselineStress` explicitly calls `GC.Collect` twice. It cannot meet this plan's no-forced-GC measurement requirement without revision. Existing compiler warnings concern `UiIcon.Size` and an identical-argument assertion in `AbilityArchitectureTests`.

## Cleanup ledger

| Candidate | Decision and evidence | Validation |
| --- | --- | --- |
| `FileLoader.AbortLoad` | Removed. No callers; Rider safe-delete confirms no conflicts. Failure presentation belongs to session initialization/coordinator and game flow, not file loading. | Full suite before and after. |
| `ItemRegistry.Data`, `ResetPreparation`, parser forwarding | Retain. Lazy data caches a successful definition set; reset permits retry after failed preparation. Item parsing also precompiles equipment stats, so forwarding is not uniformly redundant. | Registry and preparation cases in full suite. |
| `SessionCoordinator.State` versus `SessionInitialization.Stage` | Retain. Lifetime state and construction/binding progress differ; activation checks both. Stop closes the gate before callbacks during disposal. | Session cases in full suite. |
| `SessionScope.IsDisposed` and deferred cleanup | Retain. Acquisition can synchronously trigger cancellation; partially acquired resources still require release. | Session scope cases in full suite. |
| `GameSessionFactory` restored branch | Retain. It validates host ownership and restores supplied world/ability/hotbar state before networking. Fresh participant setup remains distinct. | RestoredSessionTests, RestoredWorldTests, ItemRestorationTests and RestoredRuntimeTests in suite. |
| `ItemStore.Prepare`/commit/publication | Retain. Preparation checks changed containers and reserves index capacity before commit; commit installs indexes/domain state before publication. No scan of unrelated items was introduced. Full callback audit remains open. | Item transfer/restoration cases in suite. |

The lifecycle remains StartMenu → Loading → initialization → Active → teardown. `SessionCoordinator.CompleteLoading` gates Active on initialization Ready. `GameSessionFactory` routes network readiness through binding verification; restored state does not replay the new-state defaults. Removal of an uncalled method does not alter timed runtime paths.

## Outstanding acceptance work

No frame/physics/publication percentile baseline, GC pauses, allocation rates, loading phase trace, retained-resource analysis, or collector-overhead comparison was collected. Do not infer 60 FPS or stable 30 Hz from convergence. Release measurement, healthy ENet first-pass baseline, five-player coverage, restored multiplayer workloads, equipment bursts, evolving-world joins, cancellation/retry measurements, moderate/stress profile comparison, and three 30-second warm-up/180-second measurement repetitions remain unexecuted. These need an extended bounded recorder/workload driver; the existing runner alone does not implement the full matrix.

Rendered ENet and Steam loading/first-active-frame checks remain unexecuted. Actual Steam multiplayer requires distinct authenticated accounts and peers; this run provides no Steam verification or networked machine placement. Steam integration version and runtime compatibility fingerprints still need capture. Full GameFlowManager, GamePlayManager, join-protocol, GameWorld and local-binding guard review remains open; no guards in those paths were removed without that evidence. Complete the missing baseline and review before claiming final cleanup acceptance.
