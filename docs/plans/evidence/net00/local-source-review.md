# NET-00 local source review — 2026-10-31

The clean working tree and retained native-host.txt were reviewed before advancing. Existing passing runtime evidence was reused; no runtime check was rerun for this documentation-only increment.

Local source root: `C:/Users/lober/godot_engine/godotsteam-build/`.

- Godot checkout HEAD: `001aa128b1cd80dc4e47e823c360bccf45ed6bad`, matching the short engine revision in the retained runtime trace.
- Clean GodotSteam checkout HEAD: `38a65c316acd7f9a1128e063f2b2a6700803c514`.
- The GodotSteam checkout and Godot's copied module contain identical `godotsteam_multiplayer_peer.cpp` bytes: SHA256 `F3B1EA28364A9958C1545CC10C714DC247B74B4BAE91CB7F59C80ED790CBA2DD`.
- This establishes local source provenance, not a reproducible binary-to-module attestation. Exact compiled module revision remains unverified.

In `godot/modules/godotsteam/godotsteam_multiplayer_peer.h`, lines 110–121 declare:

```cpp
Error create_host(int p_virtual_port = 0);
Error create_client(uint64_t p_host_steam_id, int p_virtual_port = 0);
uint64_t get_steam_id_for_peer_id(int p_peer_id);
int get_peer_id_for_steam_id(uint64_t p_steam_id);
```

The corresponding cpp bindings at lines 733–750 expose these names to Godot dynamic calls. Identity lookup implementation at lines 705–731 returns the local identity for self, searches connected peers for remote identities, and returns zero when absent. These are source findings; actual remote identity mapping and membership comparison still require runtime verification.

UI readiness review: `JoinSteamGameMenu.OnJoinSteamGameButtonPressed` supplies Client/Steam without a lobby target; `LobbyList` remains an empty scaffold. Therefore no actionable full-build UI-only remote test is available. Do not request launch flags or editor-driven tests from the user.

Next increment: implement the minimum selected-lobby UI path and private diagnostic session dependencies described in the plan, retaining the existing menu layout. Automatically record remote capability evidence in that flow before requesting a two-account build test. Native authentication remains unverified; no fallback protocol has been selected.
