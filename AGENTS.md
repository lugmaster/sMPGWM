## Description

**sMPGWM** stands for **some Multiplayer Game With Modding** and is the project's working title.

sMPGWM is a **2D top-down coop multiplayer space shooter** developed in **Godot with C#**. The engine uses **GodotSteam**, and networking uses **GodotSteam MultiplayerPeer**.

## Development Stack

| Component | Technology |
| --- | --- |
| Game engine | Godot 4.7.2 |
| Programming language | C# |
| Steam integration | GodotSteam |
| Multiplayer transport | GodotSteam MultiplayerPeer | 
| Primary IDE | JetBrains Rider |
| Primary art tool | Aseprite |
| Genre / perspective | 2D top-down multiplayer space shooter |

## Coding Guidelines
- Keep comments minimal and useful; never include icons or emojis in code comments.
- Prefer `var` when type inference is possible and practical.
- Avoid LINQ operations; prefer straightforward loops.
- Prefer clear, efficient, allocation-conscious implementations and arrays where appropriate.
- Use one file per class, enum, or record. Exceptions: nested internal classes and `Icons.*`.
- Match namespaces to the directory structure and name files after their primary type.
- Keep the 60 FPS game loop and 30 Hz network tick fast: avoid unnecessary per-frame and per-tick allocations and reuse buffers where practical.
- Network payloads must contain only arrays and scalar values (numbers, booleans, and strings), using a predefined positional schema and numeric indices for game-object references; do not encode field names or key-value mappings.

## Traceability of test-support code

- Mark test-support code in `sMPGWM.Game` so reviewers can identify non-production code and review it for removal or exclusion before shipping. No markers are needed inside `sMPGWM.Tests`.
- Use `TEST-SUPPORT-CLASS` for an entirely test-only class, or `TEST-SUPPORT` above a test-support method, constructor, or isolated statement. 
- Give each marker a unique ID and list each consuming test class once, fully qualified. For script-based tests, use the script path instead.

```csharp
// TEST-SUPPORT: HOTBAR-SEED-STATE-*
// Tests: sMPGWM.Tests.ItemSystem.ItemHotbarTests
// Tests: sMPGWM.Tests.ItemSystem.SomeOtherTestClass
```

## Runtime and Networking

- **Frame rate:** Locked at 60 FPS.
- **Network tick interval:** Every second local physics step (30 Hz at 60 physics steps/second).
- **Authority model:** Host-authoritative multiplayer.
- **Session scale:** Co-op, typically 5 players and at most 10.
- **Performance:** Keep the 30 Hz network cadence stable at this scale. Avoid unnecessary recurring allocations
