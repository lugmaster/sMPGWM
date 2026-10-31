# Logging, export and native assertion review — 2026-10-31

Historical evidence snapshot. Its next-action and missing-auth statements are superseded by the current handoff in ../../network-epic-implementation-plan.md: Steam silent/malformed auth and host recovery passed; remaining NET-00 checks are deferred by user decision and do not block implementation.

## Logging

Logger's optional direct-file sink now opens existing files with ReadWrite, creates missing files with WriteRead, and seeks to the end before each entry. Previously WriteRead truncated every entry. FileLogging_CreatesThenPreservesEarlierMessages passed inside Godot, exercising creation, two retained entries and minimum-level filtering. The engine integration orchestration remains red because of the previously recorded inventory UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity failure; no clean full-suite result is claimed.

Normal Logger output still goes through Godot. The project now explicitly enables Godot file logging at user://game.log, matching the diagnostic overlay. Direct-file logging remains disabled by default; do not enable both writers against the same file. Export smoke evidence confirms normal startup logs are retained at the configured path. On a normal Windows launch this resolves to %APPDATA%/Godot/app_userdata/sMPGWM/game.log.

## Windows package

- Directory: artifacts/net00-windows (copy the whole directory, including data_sMPGWM_windows_x86_64 and both native DLLs).
- Godot: 4.7.2.stable.mono.official.ed1daf0bf, installed Windows x86_64 Mono debug template.
- Executable SHA256: 8A40D4BC82E0BFA71D2274E9A2E5D12B2635B33943F60114F35CA75FA152C33C.
- Full file hashes: artifacts/net00-windows-sha256.json.
- Export log: artifacts/net00-export.log. Smoke log: artifacts/net00-export-smoke.log.
- Export used the existing local Windows Desktop preset, whose include filter includes config/* and assets/game_data/*.yml. The preset is ignored by Git; this is a local artifact, not a reproducible checkout-only export pipeline.
- To avoid the open editor's hot-reload DLL collision, copied project source to an isolated temporary directory, excluding .godot, bin, obj and ~* files; copied the extension list, imported assets, built the C# project, then exported. Final export returned zero with no ERROR lines. Earlier attempts exposed a locked hot-reload copy and missing prebuilt editor assemblies; these were resolved by isolation and building before export.
- Launched the exported executable headlessly from its own directory with isolated APPDATA and a 120-frame exit. It returned zero, initialized Steam, loaded StartMenu and wrote the complete trace to user://game.log with no ERROR lines.

This proves local packaging/startup only, not rendered UI, remote exchange from the export, or deployment to a second machine. No new manual test is requested yet.

## Native assertion provenance

The old local-source-review.md describes the pre-migration compiled module and is not provenance for the current GDExtension. Current addon metadata reports GodotSteam 4.23.2; metadata alone is not binary/source attestation. Current Windows file hashes:

| File | SHA256 |
| --- | --- |
| libgodotsteam.windows.template_debug.x86_64.dll | 8EB26B667AD0E85C77FDA8FF7AA020496ACE6AFD58BCCC544365C782DF8F773E |
| libgodotsteam.windows.template_release.x86_64.dll | 6535AF56D325AF0FFF52DBB5DE3703D110BB430FDEF4E3026D7ECE617270E530 |
| steam_api64.dll | 8DE54D32508E216C9135B8BF025749243D44E404C1C22A8E5FE35ACECABE7A9C |

Valve's public [steamnetworkingsockets_internal.h](https://github.com/ValveSoftware/GameNetworkingSockets/blob/master/src/steamnetworkingsockets/steamnetworkingsockets_internal.h) contains the matching !IsLocked() assertion in ConfigValue<T>::Set, guarding mutation of a locked configuration value. This suggests configuration mutation, not by itself a mutex deadlock. The public branch is not established as the source of the user's loaded Steam runtime: line 769, the configuration key, native caller and exact binary revision remain unverified. No application change, suppression or claim of harmlessness is justified from this source match alone. Local application network/Steam code contains no direct Steam networking configuration setter call.

Next investigation needs a matching native source/build or a captured native call stack naming the configuration write. Do not replace the working runtime or disable authentication speculatively.

## Remaining capability checks

Prepare a bounded developer verification path before another UI-only two-account test. Keep ordinary diagnostics and admission separate from fault injection.

1. Steam malformed and silent auth: match rejection/timeout to peer identity; prove no diagnostic RPC eligibility, slot release, host survival and a subsequent valid exchange. Existing ENet coverage does not prove Steam negative paths. A normal menu cannot generate these cases.
2. Channels: enumerate the channels and transfer modes production will rely on, then test targeted correlated delivery, sender validation and cross-channel separation on both transports. Do not infer channel support from the current reliable exchange alone.
3. Abrupt loss: distinguish process termination/network loss from the already evidenced graceful host departure; record bounded cleanup and retry.
4. Export UI test: only after the preceding test controls are concrete, provide package identity, two-account prerequisites, numbered menu steps and both log paths. Retain existing successful auth/identity and lifecycle evidence.

NET-00 remains Needs verification. No later story is declared complete.
