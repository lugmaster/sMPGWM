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
- Prefer arrays and efficient, allocation-conscious implementations where appropriate.
- Favor fast, clear implementations without unnecessary overhead.
- Use one file per class/enum/record except for internal classes that are embedded in another class. Exception: Icons.*
- Each file should reflect its namespace, so that it can be properly imported!

## Traceability of test-support code

Every line of executable code or declaration introduced solely to enable, drive, inspect, or verify tests must be marked with a unique traceability ID, followed by the fully qualified test method name(s) that use it. This includes test-only hooks, accessors, instrumentation, injected controls, and development helpers added to application code for automated or manual verification. Ordinary production code does not require a marker merely because tests exercise it.

Use this comment format on each affected line, or immediately above it when an inline comment is impractical:

```csharp
internal void SetShieldForTest(float value) // TEST-SUPPORT: <unique-id> | Tests: Namespace.Class.Method
{
    _shield = value; // TEST-SUPPORT: <different-unique-id> | Tests: Namespace.Class.Method
}
```

## Runtime and Networking

- **Frame rate:** Locked at 60 FPS.
- **Network tick interval:** Every second local physics step (30 Hz at 60 physics steps/second).
- **Authority model:** Host-authoritative multiplayer.
- **Session scale:** Co-op, typically 5 players and at most 10.
- **Performance:** Keep the 30 Hz network cadence stable at this scale. Avoid unnecessary recurring allocations, reuse storage where useful, and do not explicitly force garbage collection. Measure timing and GC impact; an absolute absence of automatic GC is not required.