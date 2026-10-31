# Create game as host

Status: proposed, 2026-10-31. Planning only; no implementation in this split.

Shared ownership, compatibility, initialization, protocol and cleanup contracts: [network plan](network-lobby-implementation-plan.md). Target code: `sMPGWM.Game/Scripts/Network`.

Tick requirement: the fixed-rate physics loop drives the local session ticker. Host setup owns its start/stop through the session owner; it does not create a timer per player. Proposed ticker wiring and the unresolved frequency/send-cadence distinction are in the [refinement discussion](network-vertical-slice-refinement-plan.md#tick-loop-discussion).

## Scope and ownership

Final Host action through a running, locally admitted host and published Steam lobby (or listening LAN server). Host continues while clients join/leave. No waiting room or real player spawning in this increment.

`NetworkManager` is the persistent Node owner. Host setup is an ordinary `HostSessionFlow` owned by it; it does not need a scene or `_Ready`. `SteamLobbyService` and transport adapters are ordinary classes with explicit subscription/disposal. `SessionProtocolNode` is the shared RPC Node. Existing `GameFlowManager` and `GamePlayManager` retain scene/navigation and initialization responsibilities.

## Steps

1. Complete NET-00 custom-build verification and NET-01 common ownership/cleanup before relying on native auth or channels. Replace legacy `MpManager`, static completion events and `IActiveNetwork.GetNode` in the first buildable slice; do not retain two session managers.
2. Implement ENet hosting first (NET-03). Validate capacity including host, occupied port and host-only configuration. Use immutable host settings, cancellation and an explicit setup result.
3. Implement the Steam sequence below (host portion of NET-02). Keep creation and membership callbacks distinct.
4. Complete effective gameplay fingerprint and explicit initialization (NET-06/07). Admit the local host through the world-join/spawn logging seam (NET-13/10), without self-RPC or waiting for clients.
5. Publish only after these gates succeed; hand incoming peers to the admission contract described in the client plan. Wire health through the heartbeat plan.

Hosting:

1. Final Host submits a validated request. Reject duplicates and retain the menu during setup.
2. Create an initially private lobby, record membership immediately, and make it unjoinable while preparing. Avoid publishing an incomplete public entry; revalidate every join regardless of visibility.
3. Set name/game_id/mode/version, protocol/build, pinned host_steam_id, session_id and state=preparing. Check writes. Pin the actual owner at creation.
4. Create/validate server peer on virtual port 0, configure API/protocol, assign peer. Automatic creator LobbyJoined is bookkeeping, never another completion/client connect.
5. Complete setup once usable, load/initialize the scene, finalize gameplay fingerprint and admit local host through the logging stub.
6. Publish state=ready/public/joinable. Publication failure releases session with a reason. Keep the running game open while host can accept players.

## Acceptance

Host setup completes once; creator-join callback cannot prematurely complete it. Cancel, occupied port, native peer construction, initialization, checksum and publication failures release all owned resources. Repeated host/exit/retry leaves no membership or callbacks behind. Host world/session remains alive after client refusal, timeout, leave and rejoin. MaxPlayers includes the host; pending remote peers count against capacity. Verify ENet locally and Steam with the exact custom engine; build success alone does not verify Steam.

## Dependencies and handoff

Own NET-00/01 foundations, NET-03 host path, NET-02 host publication, NET-06/07 shared initialization and local-host NET-10. Client plan owns remote admission behavior and consumes these shared foundations. Browser consumes the publication metadata; heartbeat consumes connected peer/session lifecycle. Implement common foundations once.
