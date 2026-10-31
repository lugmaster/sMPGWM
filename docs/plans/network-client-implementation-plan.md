# Create game as client

Status: proposed, 2026-10-31. Planning only; no implementation in this split.

Shared ownership, compatibility, initialization, protocol and cleanup contracts: [network plan](network-lobby-implementation-plan.md). Target code: `sMPGWM.Game/Scripts/Network`.

Tick requirement: the client's local fixed-rate physics loop drives its ticker independently of incoming host tick packets. Host tick observations remain separate from the local counter and do not trigger another simulation step. Proposed wiring is in the [refinement discussion](network-vertical-slice-refinement-plan.md#tick-loop-discussion).

## Scope and ownership

Input is an already selected Steam lobby ID, or a validated LAN endpoint. Searching, displaying or choosing lobbies belongs exclusively to the lobby-list plan. A selected ID is a target, not proof that the lobby is still valid.

Use `GameConfig.ConnectionTimeout` in milliseconds for the connection attempt (from `[general] connection_timeout`, currently `60000`); capture the value per attempt. Keep diagnostic, gameplay-phase and health deadlines independent. Follow the shared host/client error-exit contract in the refinement plan.

`ClientSessionFlow` is an ordinary class owned by `NetworkManager`. Reuse the host plan's lobby service and adapters. `PeerAdmission`, immutable requests/results, `PlayerJoinData` and fingerprint values are ordinary C# types. Only `SessionProtocolNode` receives RPCs; existing scene Nodes provide explicit initialization results. World-join and spawn coordinators are plain classes for the logging-only increment; future instantiated players will be scene Nodes.

## Steps

1. Accept one immutable join request and cancellation token; reject duplicate starts. Keep navigation under GameFlowManager and distinguish membership, transport connection and gameplay admission.
2. Implement the sequence below, using the host plan's NET-00/01 foundation and NET-03/02 adapters.
3. Implement NET-08/09 remote admission on both host and client using the shared initialization/admission section: HostReady -> ClientReady -> world-join result -> AdmissionAccepted. This plan owns both ends so message definitions and validation cannot diverge.
4. Capture RPC sender before awaiting. Require session/token/epoch/content revision, bounded payloads, host capacity and readiness. Keep host-issued epochs separate from local scene generation. Start health after preflight, including during loading.
5. Await NET-13's explicitly named no-replication world-join stub. Host invokes NET-10 logging spawn once; matching acceptance enables client activation once. Real world synchronization and player instantiation remain future work.
6. Bound setup, initialization, waiting for HostReady and admission independently. Pongs cannot extend these deadlines. Failure releases only the joining peer on the host and returns the affected client to its menu.

Joining:

1. Join selected ID, record membership, revalidate required metadata/readiness/game/version, pin host/session. Initially require host membership/current ownership. Reject self-join.
2. Create client peer to pinned host on virtual port 0. Membership/CreateClient Ok are intermediate, not connection success.
3. Await actual connection/short preflight, then load/initialize, compare gameplay, synchronize current world, and activate on host acceptance.
4. Revalidate host/session on metadata/membership changes. Steam ownership may transfer when owner leaves; this does not migrate the authoritative world. Pinned host departure, owner/session inconsistency or server loss ends the client session. Never reconnect to a new owner automatically.
5. Departure releases only that client locally and its slot/transfers on host. Rejoining the same running game repeats admission with fresh tokens and current world/content state.

## Acceptance

Test refused connection, immediate CreateClient error, cancel at each stage, late successful membership, host departure/owner transfer, stale selection and full lobby. Test checksum mismatch, failed/stale initialization, malformed/wrong-sender/token/revision messages, duplicates and timeout. Duplicate ClientReady may resend acceptance but never respawn; duplicate acceptance never reactivates. Rejoin uses fresh identity/token and current world revision. Three-peer integration must show one failed client does not interrupt host or another admitted client. Record that the world-join seam does not replicate a running world yet.

## Dependencies and handoff

Own NET-08/09/13 and remote NET-10; consume shared NET-06/07 and host foundations. Cover client portions of NET-02/03 and menu connection portions of NET-05. Join can be exercised using a supplied lobby ID before the browser exists. Contribute repeated join/leave/rejoin cases to NET-12.
