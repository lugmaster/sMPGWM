# Allocation audit of measured runtime paths

This is source evidence, not an allocation-profiler call-tree result. Counts below describe work the current implementation requests at nominal 60 Hz physics / 30 Hz publication and the scripted workload. They do not assign a measured GC pause or scheduling stall to a particular method. No production performance edits were made for this audit.

## First candidate: unchanged topology checks

`SynchronizationCoordinator._PhysicsProcess` calls `NetworkManager.PublishWorldTopology` on every host step. `PublishGameplay` calls it again on every even step. Therefore a stable idle host performs 90 topology captures per second, even when nothing changes. `PublishWorldTopology` only checks whether identity changed **after** `World.CaptureTopology` finishes.

Each `GameWorld.CaptureTopology` allocates the entity-key array, participant and entity arrays, one `ParticipantBinding` and one `EntitySpawnRecord` per participant/ship, a `WorldTopology` record, and the LINQ iterators. It also validates the captured topology with three fresh hash sets. `WorldTopology.Identity` sorts both arrays and formats every participant as a string, then creates joined identity strings. With ten controlled ships, this means 1,800 participant/entity record allocations and 900 formatted participant strings per second, before arrays, sets, iterator/enumerator objects, joined strings or native property access. Five players halve those two counts. During burst traffic each remote transfer reply calls `PublishWorldTopology` again, adding up to 45 captures/sec at ten players and the target five requests/sec/client.

Suggested first optimization: track a topology generation or dirty state at entity creation/removal and participant control-binding changes. Skip capture when generation is unchanged. Preserve complete validated snapshots for joining peers, control changes, restoration and explicit recovery, and preserve handling of queued/deleted entities. Moving the existing identity comparison alone cannot avoid the allocation; removing one duplicate call only saves one-third of the steady capture frequency. Do not cache moving spawn positions forever: when a join needs a full topology, capture current transforms.

Source: `NetworkSystem/SynchronizationCoordinator.cs:101`, `NetworkSystem/NetworkManager.Gameplay.cs:73`, `NetworkSystem/NetworkManager.cs:358`, `WorldSystem/GameWorld.cs:184`, `NetworkSystem/WorldTopology.cs:16`.

## Burst snapshots: empty inventory slots dominate object counts

`ItemSnapshotCodec.EncodeSlots` creates a `Godot.Collections.Array` for every slot, including empty slots. The ordinary inventory starts with 128 slots and equipment has nine slots. A full owner snapshot therefore creates 137 slot arrays and three outer arrays: 140 Godot array wrappers plus their native storage per snapshot. `ReplyItemTransfer` sends a complete owner snapshot before acknowledgement, including stale-revision rejection, as part of its recovery contract.

At the workload's target five requests/sec/client, four remote clients produce up to 20 replies/sec, or 2,800 encoded Godot array wrappers/sec. Nine remote clients produce up to 45 replies/sec, or 6,300 wrappers/sec. These are nominal source-derived rates, not a claim that all commands were admitted or all wrappers were collected during a particular run. The host's local transfers have no remote snapshot reply.

On each client, decoding additionally creates slot arrays, entity records for occupied slots, a uniqueness hash set, confirmation records and the batch array. Replica installation/preparation and UI snapshot creation add their own allocations. The existing immutable inventory/equipment snapshots clone slot arrays once on change; their getters are not re-cloning on every read. Transaction preparation copies only participating containers and updates incremental indexes; unrelated item storage is not scanned by the transfer commit.

Suggested follow-up: profile snapshot encoding and native Variant handling during the burst phase, then consider a sparse or bounded binary owner-state format that preserves container capacities, item identities and revisions. Keep the reliable snapshot-before-ack ordering and stale recovery semantics. This is a protocol change requiring compatibility/version and decode-boundary tests, not a final cleanup deletion. Do not pool mutable snapshot arrays until their ownership across callbacks and transport marshaling is explicit.

Source: `NetworkSystem/ItemSnapshotCodec.cs:14`, `NetworkSystem/NetworkManager.Items.cs:113`, `InventorySystem/InventoryController.cs:8`, `EquipmentSystem/EquipmentSlotDefinition.cs:8`, `InventorySystem/InventorySnapshot.cs:45`, `EquipmentSystem/EquipmentSnapshot.cs:11`, `ItemSystem/ItemStore.cs:285`.

## Movement codec: predictable recurring buffers

`MovementWire.Snapshot` builds a `MemoryStream` and `BinaryWriter`, allocates the stream backing buffer, allocates one 16-byte array per entity GUID and one for the session GUID, then copies the packet through `ToArray`. The packet is `25 + 111*N` bytes: 580 bytes at five players and 1,135 at ten. The two packet buffers plus GUID-array payload alone account for `30 * (2 * packetBytes + 16 * (N + 1))`: 37,680 bytes/sec at five players and 73,380 at ten. These numbers exclude object/array headers, writer/stream objects, RPC envelopes, marshaling and native transport buffers, and count allocated payload rather than retained memory.

Each receiving client constructs a reader/stream, a fresh sample list, two uniqueness sets, GUID-byte arrays and entity strings. The list grows from zero capacity. Every client also allocates `RecentCommands()` at roughly 30 Hz, command encode buffers/writer/stream and two GUID arrays. The host decodes each remote command packet into another list/reader/stream. Local prediction stores new steps in a `SortedDictionary`, allocating a tree node per new predicted tick; replay updates existing nodes. Read-only dictionary enumeration in hot paths can also box enumerators, but that is a smaller candidate than full packet/topology construction.

Suggested follow-up: write directly into one exactly sized packet buffer using spans, write GUIDs without temporary arrays, decode into bounded reusable storage, and use a bounded ring for the already bounded command history. Keep malformed-message bounds/identity/duplicate checks and monotonic ordering. Any reused buffer must remain valid until Godot finishes marshaling it; one immutable outgoing packet shared across peer sends is the current safe ownership boundary.

Source: `MovementSystem/MovementWire.cs:21`, `NetworkSystem/SnapshotEnvelope.cs:11`, `MovementSystem/LocalMovementPrediction.cs:12`, `MovementSystem/LocalMovementPrediction.cs:30`.

## Gameplay snapshots: conditional full-world work

Gameplay publication is revision-gated, so it should not be counted as an unconditional 30 Hz allocation. During recharge, ability effects, or equipment changes, a single dirty ship causes encoding of all ships. Each ship's encode allocates a 14-double array (112 payload bytes), row/condition/behaviour Godot arrays, and one nested row per active behaviour. At ten ships and 30 dirty publications/sec, stats arrays alone would allocate 33,600 payload bytes/sec on the host. Each receiver allocates decoded entries, uniqueness sets, stats buffers and per-entity condition/behaviour arrays. `ResolvedShipStats`, `BehaviourInstance`, `ResourceState` and movement sample/command values are structs: their `new` expressions are not individually heap-allocated class instances.

Session GUID formatting also repeats inside per-peer gameplay sends and receive validation. Caching the immutable session string can remove that bounded recurring string allocation. Higher-impact options include sending only changed gameplay entries after the complete initial baseline, or separating infrequent stat/configuration changes from resource updates. Preserve full-state initialization and revision continuity. A trace should establish actual dirty-publication frequency before assigning benefit.

Source: `NetworkSystem/NetworkManager.Gameplay.cs:70`, `NetworkSystem/GameplaySnapshotCodec.cs:14`, `NetworkSystem/GameplaySnapshotCodec.cs:32`, `ShipSystem/ShipRuntime.cs:214`.

## Things to retain

`HostJoinProtocol.Snapshot` caches membership until peers are added/removed; stable iteration is already reused. Item and gameplay registries cache the validated definitions and equipment precompilation; the steady loops do not reparse YAML. Resource-state accessors return structs. The indexed item store checks changed containers rather than scanning every item. These should not be rewritten based on a blanket assumption that getters, `new`, snapshots, or validation always imply avoidable recurring heap allocation.

To attribute a tail spike, correlate its raw timestamp with collector pause increments, CPU/scheduling data and workload/lifecycle events. One-second GC totals only establish collections within that sample interval; they cannot prove which individual frame was paused. A GC allocation trace or comparable isolated instrumentation is needed for exact allocation ownership and per-pause timing.
