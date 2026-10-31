# sMPGWM — Project Overview

## Description

**sMPGWM** stands for **some Multiplayer Game With Modding** and is the project's working title.

sMPGWM is a **2D top-down coop multiplayer space shooter** developed in **Godot 4.6 with C#**. The engine is self-compiled with **GodotSteam**, and networking uses **GodotSteam MultiplayerPeer**.

## Development Stack

| Component | Technology |
| --- | --- |
| Game engine | Godot 4.6 (self-compiled) |
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

## Runtime and Networking

- **Frame rate:** Locked at 60 FPS.
- **Network tick interval:** 30 ms.
- **Authority model:** Host-authoritative multiplayer.
- **Session scale:** Co-op, typically 5 players and at most 10.
- **Performance:** Keep the 30 ms network interval stable at this scale. Avoid unnecessary recurring allocations, reuse storage where useful, and do not explicitly force garbage collection. Measure timing and GC impact; an absolute absence of automatic GC is not required.

### Multiplayer Terminology

- **Host:** The authoritative main player.
- **Client:** A non-host player, when discussed in their own local context.
- **Host dummy:** The representation of the host in another player's local context.
- **Client dummy:** The representation of a client in another player's local context.

These conventions should be used consistently in architecture discussions, implementation plans, and code examples.

## Tests

Run `dotnet test sMPGWM.Tests` from the repository root. The NUnit project targets .NET 8 to match the game assembly. Set `GODOT_BIN` to a Godot 4.6 .NET console executable if it is not available as `godot` on PATH (the local `godot_engine/Godot_v4.6.2_mono_steam` installation is also detected).

The command runs standalone tests directly and runs the `Godot` category through `GodotIntegrationTests` in a separate headless engine process. These cases use the real static registry, packaged YAML, and imported icon atlas. A temporary project and user-data directory keep test runs separate from gameplay, Steam, and player configuration. The engine runs tests on its main thread and writes individual NUnit results to `godot-results.xml`, attached to the orchestration test. Engine failures, skipped cases, or a timeout fail the orchestration test. Run the orchestration test in Rider to execute this category; directly launching an individual engine-dependent case through the ordinary .NET test host does not supply Godot bindings.

The equipment compatibility test checks every category/slot combination in one matrix test with fresh inventory and equipment owners per combination. Stack tests derive bulk quantities from loaded limits rather than assuming test-only unlimited stacks. The existing explicit stress benchmark remains an opt-in performance run.
