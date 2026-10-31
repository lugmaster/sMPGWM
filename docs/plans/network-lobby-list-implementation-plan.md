# Create the lobby list

Status: proposed, 2026-10-31. Planning only; no implementation in this split.

Shared ownership, compatibility, initialization, protocol and cleanup contracts: [network plan](network-lobby-implementation-plan.md). Target code: `sMPGWM.Game/Scripts/Network`.

## Scope and ownership

Discover running Steam games and pass a selected lobby ID to the client plan. Browsing never creates a game peer or joins a lobby. LAN discovery remains outside scope; LAN uses an address input.

`SteamServerBrowser` is an ordinary class producing immutable snapshots. It uses the persistent ordinary `SteamLobbyService` for serialized native operations and callback draining. Existing menu/entry Controls are Godot Nodes because they render and receive input. They subscribe only while active: hidden history screens may remain inside the tree, so `_ExitTree` alone is insufficient. Browser lifetime does not own the lobby service lifetime.

## Steps

1. Define the snapshot and metadata contract against host publication: stable lobby ID, name, host/persona fallback, mode, version/build/protocol, state, admitted count and capacity. Counts are advisory.
2. Implement NET-04 request serialization, cancellation/draining and the rules below. Verify installed native callback correlation before allowing overlapping refreshes.
3. Implement browser portions of NET-05 using the actual join menu and entry scenes. Return a selected ID through the client request boundary.
4. Exercise UI with fabricated snapshots before Steam hosting is ready; verify real discovery after host publication exists.

## Behavior

Use running games/servers in UI language, with Join/host/players. Auto-request on activation; provide Refresh/loading/empty/error states. Reapply exact game_id/version plus Worldwide each request; filter protocol/build for this contract. Read only needed metadata, persona names with fallback, and exclude invalid/self/not-ready entries locally.

Serialize refresh and stop publication/listeners on deactivation while the lobby service drains unresolved work. Preserve discovery order by default. Search name/host and sort locally with stable-ID tie-breaks and bounded result count; do not issue new Steam calls for local filtering.

Replace samples through existing entry scenes/StartMenuProvider. Exactly one keyboard-accessible selection; Join requires visible valid selected ID and no pending search/connect. Refresh/filter removal clears selection; sorting preserves ID. Preserve inventory toolbar paths/options. Back cancels setup before navigating.

Membership counts are not admitted players. Publish separate host-owned admitted-player/capacity metadata for display and update on join/leave; treat it as advisory. Actual pending/admitted host state decides capacity when players race for the last slot. Stale/full entries fail cleanly with refresh/retry.

## Acceptance

Test loading/empty/error/inactive states, exact filters on every refresh, invalid/self/not-ready entries, persona fallback, stale/full results and late responses after Back. Search/sort must not issue discovery calls. Selection follows stable ID across sorting and clears when filtering/refresh removes it. Double Join/Refresh cannot produce duplicate operations. Preserve keyboard access and shared inventory toolbar behavior. Do not show connected-peer RTT as an unconnected lobby's ping.

## Dependencies and handoff

Own NET-04 and browser portion of NET-05. Consume shared lobby-service operation ownership from NET-01/02 and metadata from the host plan. Hand only a validated selected target to the client plan, which revalidates on join. Participate in NET-12 with stale listings and host shutdown during browsing.
