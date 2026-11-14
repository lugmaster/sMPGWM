# Weapons | Syncing Firing Action over Host

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 5 of 7  
Dependencies: Story 4; uses group authority from story 3.

## Outcome

Participating players request firing through the host. All peers see host-confirmed muzzle firing, streaks, and impacts, with no damage yet.

## Scope

- Extend existing 30 Hz input with two held-fire bits and configuration revision. Validate ownership/session/control generation; retain redundancy, expiry, and release-on-focus/UI behavior.
- Host alone admits shots at 60 Hz and resolves hits. Clear held input on disconnect/unbind/recovery and require fresh released input after baseline/control transfer.
- Implement bounded separate unreliable shot batches and deduplication keyed by session, item identity, and shot sequence. Use authoritative origins/endpoints and the epic ?9 event contract, timeline, late-event, and recovery-cutoff rules.
- Synchronize configuration and current cadence/sequence state needed for join, recovery, and restoration. Do not invent ammunition state before story 6.
- Gate readiness on dependencies; do not replay old effects on joins or movement reconciliation. Use positional arrays/scalars and numeric references; bump protocol and verify codec ceilings.
- Validate both ENet and GodotSteam MultiplayerPeer and record actual coverage. Pool exhaustion or event loss affects cosmetics only.

## Acceptance

- Host and clients can fire either/both groups; every peer sees corresponding firing, streaks, and hit impacts under normal delivery.
- Wrong-peer, stale binding/session, malformed, oversized, duplicate, and reordered messages are handled safely. Missing input expires without stuck firing.
- Join/recovery/restoration gives current state without historical visual playback; packet loss permits missed cosmetics, never duplicate authoritative shots.
- Measure five/ten players and up to nine weapons each, including synchronized bursts and bounded queues. No health damage or supply consumption occurs.

## Excluded

Energy-per-shot, reload/ammo mechanics and UI, damage/death replication, client prediction, and historical rewind.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
