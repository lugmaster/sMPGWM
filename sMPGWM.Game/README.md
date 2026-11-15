# sMPGWM — Project Overview

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

## Runtime and Networking

- **Frame rate:** Locked at 60 FPS.
- **Network tick interval:** Every second local physics step (30 Hz at 60 physics steps/second).
- **Authority model:** Host-authoritative multiplayer.
- **Session scale:** Co-op, typically 5 players and at most 10.
- **Performance:** Keep the 30 Hz network cadence stable at this scale. Avoid unnecessary recurring allocations, reuse storage where useful, and do not explicitly force garbage collection. Measure timing and GC impact; an absolute absence of automatic GC is not required.

### Multiplayer Terminology

- **Host:** The authoritative main player.
- **Client:** A non-host player, when discussed in their own local context.
- **Host dummy:** The representation of the host in another player's local context.
- **Client dummy:** The representation of a client in another player's local context.

### Session Lifecycle

Each participant's local session has two main phases after pressing **Host** or **Join**:

| Phase | Lifecycle state name | Description |
| --- | --- | --- |
| **Session Initialization** | `Initializing` | Load and validate YAML gameplay definitions, establish networking, build or synchronize the world, and prepare the local player's ship, inventory, equipment, hotbar, and interface bindings. The loading screen remains visible and gameplay controls remain inactive. |
| **Gameplay** | `Playing` | Begin once initialization succeeds and the local player can interact. Hide the loading screen, enable gameplay controls, and continue normal gameplay and network synchronization. |

Use **`SessionReady`** as the name for the event marking the transition from Session Initialization to Gameplay. Readiness means all required local setup and, for a joining client, initial synchronization and host join confirmation have completed; a network connection alone is not sufficient.

These phases describe each participant's **local session lifecycle**, not a shared phase for the entire multiplayer session. A joining client can be `Initializing` while the host and existing players remain `Playing`.



## Current Features

### AbilitySystem

**Ownership:** Each ship holds its available abilities and any active ability effects. Equipment can grant abilities to the ship carrying it. Players choose abilities and targets for their controlled ship; the host has final authority over activation and its outcome. The session owns the shared ability runtime.

**Responsibility:** Decide whether a ship may use an ability on the selected target, then apply the supported effect. This includes checking the ability's source, the caster's condition, whether the target is a living ally, and targeting range. The supported activation path currently covers instant abilities with no resource cost or cooldown that apply temporary shield-capacity modifiers.

**Contribution to gameplay:** Shield Capacity Boost lets a player support their own ship or a living allied ship within 600 world units. It adds 50 maximum shield for 30 seconds. Normal shield recharge fills the added capacity; activation does not immediately refill shields. Reapplying the boost refreshes its duration rather than stacking additional capacity. When it expires, current shields are reduced to the remaining maximum if necessary. Players activate equipment abilities through the hotbar, select a ship, and can cancel targeting with right-click or Escape.

**Synced: Yes.** Clients send reliable activation requests to the host. The host validates and executes them, then reliably returns success or rejection to the requesting player. Resulting shield values and active effects are distributed through authoritative gameplay snapshots when state changes, on the 30 Hz network cadence. Target selection and the targeting cursor remain local to each player.

**Responsibilities during Initialization Phase:** Prepare supported abilities from the loaded gameplay definitions, create the session's ability runtime, and establish each ship's ability grants from its equipment. Connect hotbar activation, targeting, and player feedback. When restoring a session, restore ability-related state alongside the ships. Ability use waits until the required ship and multiplayer state is ready.

**Responsibilities during GamePlay Phase:** Handle ability selection and target validation, submit activation requests, and recheck ownership, equipment availability, caster condition, and target eligibility on the host before applying an effect. Report the outcome to the player. Temporary effects are tracked on the affected ship and expire through the gameplay simulation, with the resulting state synchronized to other players. Targeting is cancelled when the player loses control or the granting equipment is no longer available.

### CombatSystem

**Ownership:** The shared gameplay definitions own the damage-source catalogue. Each player's game loads its own copy. CombatSystem currently has no separate state owned by a player or ship.

**Responsibility:** Define the available damage-source categories and ensure that weapon items and damage-effect definitions refer to valid categories. The current catalogue contains Laser, Physical, and Plasma.

**Contribution to gameplay:** Supplies consistent damage-type labels for weapon and effect data. The implemented scope is currently limited to these definitions and their validation. Hit resolution, damage application, and different damage effectiveness against shields and hull are not implemented combat features.

**Synced: No.** The catalogue is loaded locally rather than sent from the host, and CombatSystem produces no runtime combat events or state to synchronize. Its data is included in the multiplayer compatibility check, which requires matching game data before players can join together.

**Responsibilities during Initialization Phase:** Load and validate the damage-source definitions alongside the other gameplay data. Check that referenced damage sources exist in weapon items and damage-effect definitions. Invalid references prevent definition preparation from completing. Include the loaded data in the session's compatibility check.

**Responsibilities during GamePlay Phase:** Keep the validated damage-source catalogue available for lookup. CombatSystem currently performs no per-frame or per-tick combat processing and does not apply damage to ships.

### EnergySystem

**Ownership:** Each ship owns its energy reserve and power state. The ship's installed equipment determines production, consumption, and storage capacity. The host calculates energy changes for all ships; clients receive the authoritative results.

**Responsibility:** Balance reactor production against equipment consumption, charge or drain the battery, and determine whether powered ship systems can operate. Stored energy stays between zero and the installed battery's capacity.

**Contribution to gameplay:** Reactor output supports the ship's continuous equipment demand. Surplus energy charges the battery, while a shortfall draws from stored energy. Afterburner adds 25% of the engine's normal energy consumption, making sustained boosting a loadout and energy-management choice. If demand exceeds production and the battery is empty, thrust, turning, braking, and shield protection become unavailable. The ship continues drifting, and its shields lose charge while unpowered. Releasing afterburner can restore power if production then covers demand. An empty battery alone does not disable systems when production meets demand. The HUD shows current energy, storage capacity, and net energy production per second.

**Synced: Yes.** The host updates energy during the 60 Hz gameplay simulation. Changes to stored energy, production, consumption, capacity, afterburner state, and depletion are sent through reliable gameplay snapshots on the 30 Hz network cadence. Clients use the confirmed state for their energy display and powered-system availability. Joining players receive the current state during initial synchronization.

**Responsibilities during Initialization Phase:** Establish each ship's power component and energy reserve, then derive its energy balance from the installed loadout and ship modifiers. Newly initialized ships start with empty energy storage and immediately evaluate whether production covers demand. Restored ships recover their saved energy reserve. Clients receive the host's initial resource state before gameplay becomes ready.

**Responsibilities during GamePlay Phase:** Update energy as production, equipment demand, and afterburner use change. Recalculate depletion and make the resulting power availability available to movement and shields. Equipment changes update the energy balance and storage limit; replacing or removing the battery clears the ship's stored charge. Keep the HUD and other players informed through the shared gameplay state.

### GameFlow

**Ownership:** Each running copy of the game owns its current phase, loading screen, and scene transitions. Its session coordinator owns the local session's startup and cleanup. The host controls admission to the multiplayer world; each client completes its own initialization before entering gameplay.

**Responsibility:** Guide the player from the start menu through the Initialization Phase into the GamePlay Phase, then handle leaving or ending the session. The visible flow is Start Menu → Loading → In Game → Disconnecting → Start Menu. Gameplay controls remain inactive until initialization succeeds.

**Contribution to gameplay:** Players see loading progress while the game prepares their session and synchronizes their starting state. They enter gameplay with their ship, inventory, equipment, hotbar, and controls connected and ready. Readiness is checked per player, so an existing game can continue while another player joins. Startup or session failures display an explanation and a way to return to the menu.

**Synced: No.** The current phase and loading progress are local. Entry into multiplayer gameplay is coordinated through reliable network messages: clients receive the host's world, movement, gameplay, and item state, acknowledge readiness, and wait for the host's join confirmation. Local readiness checks then complete the transition into gameplay. The host enters gameplay once its own startup is complete.

**Responsibilities during Initialization Phase:** Show the loading screen, prepare and validate gameplay definitions and compatibility data, and load the game scene. Construct the session and connect the player interface to the world and gameplay systems. Start hosting or connect to the host, establish the player's ship and loadout, and receive authoritative state when joining as a client. Verify that the local ship, inventory, equipment, and hotbar are ready, then activate synchronization. Only after these steps succeed does loading end and the GamePlay Phase begin. A failed initialization stops and cleans up the partially created session.

**Responsibilities during GamePlay Phase:** Keep the local session active so players can control their ships and use gameplay features while the gameplay and networking systems run. Handle requests to leave, return to the menu, or quit the game. On departure or session failure, disable gameplay and release the session's network connection, world objects, and interface bindings. Failures show a return-to-menu message; a normal departure returns to the start menu.

### InventorySystem

**Ownership:** Each player owns a personal inventory for the current session. Installed equipment belongs to the ship they control. The host controls actual inventory contents; the owner's client receives a synchronized copy. Search, filters, sorting, and visual arrangement belong to the local player's interface.

**Responsibility:** Manage the player's carried-item container and provide an up-to-date view of its contents and available slots. Work with ItemSystem and the equipment system when the player combines stacks or moves items between inventory and equipment.

**Contribution to gameplay:** Players can browse a scrolling inventory alongside their ship's equipment, search by item name, filter by category or weapon size, and sort by name. The unfiltered All view also allows a custom arrangement. Drag-and-drop or click-to-place lets players arrange items, combine matching stacks up to their stack limit, and equip or unequip compatible items. Excess items remain in the source stack when a merge fills the destination. Ordinary inventory rearrangement changes the local visual layout. The inventory begins with 128 slots and can expand as items are created.

**Synced: Yes.** Clients send reliable transfer requests to the host, which checks ownership, current contents, and placement rules. The host reliably sends the resulting inventory and controlled ship's equipment to their owner. Changed contents are published on the 30 Hz gameplay network cadence, and transfer replies include a fresh snapshot before the result. Other clients do not receive that player's personal inventory contents. Display filters and layout remain local.

**Responsibilities during Initialization Phase:** Create the player's inventory container, apply the session's starting-item and starter-equipment setup, and connect the inventory interface to the player's inventory and controlled ship's equipment. Joining clients receive authoritative contents before item transfers become available. This prepares the current session's inventory; it does not load a saved inventory from disk.

**Responsibilities during GamePlay Phase:** Keep the carried-item view current, submit stack merges and equipment transfers for host validation, and show confirmed contents and rejected-transfer feedback. If the controlled ship changes, reconnect the equipment view while retaining the player's personal inventory.

### ItemSystem

**Ownership:** The world owns the session's item store. Each item or stack has an identity, quantity, and one location in an inventory or equipment slot. Item definitions describe shared properties such as category, stack limit, equipment values, and granted abilities. The host controls item changes; each local player owns their hotbar layout.

**Responsibility:** Load and validate item definitions, track item instances and their locations, and perform valid moves, swaps, and stack merges. Supply item information to the interface and equipment properties to the systems that calculate ship capabilities.

**Contribution to gameplay:** Item choices determine the equipment values and supported abilities available to a ship. Stackable items can be combined within their limits. Players can configure ten hotbar positions with shortcuts to consumables, equipped weapons, and supported equipment abilities; assigning a shortcut does not move or duplicate the item. Supported equipment abilities can be activated through targeting. Consumable and weapon shortcuts can be assigned, but their use effects, item consumption, and weapon firing are not implemented through the hotbar.

**Synced: Yes.** Item identities, types, quantities, and inventory/equipment placement are included in reliable host snapshots sent to the owning player. Transfers are host-approved, with current contents sent before the transfer result. Other players receive resulting ship stats and ability effects through gameplay synchronization. Item definitions load locally and participate in compatibility checks. Hotbar layout remains local; supported ability activations use AbilitySystem's synchronized request-and-result flow.

**Responsibilities during Initialization Phase:** Load item categories and definitions, validate their values and gameplay references, and create the session's item store and containers. Create and equip starting items on the host, or establish their synchronized copies on clients. Once equipment is ready, initialize the local hotbar with its supported equipment abilities.

**Responsibilities during GamePlay Phase:** Validate and apply item moves, swaps, and merges while maintaining consistent quantities and locations. Notify equipment and interface systems when contents change. Allow players to assign, rearrange, or clear hotbar shortcuts, and update shortcut availability as the inventory or loadout changes.

### MovementSystem

**Ownership:** Each ship owns its position, rotation, velocity, and movement configuration. Its controlling player supplies movement input. The host determines authoritative movement; clients predict their own ship's movement and display synchronized movement for other players. The local player owns their camera and input settings.

**Responsibility:** Turn steering input into ship movement, including thrust, turning, afterburner, braking, and collision response. Use the ship's equipment-derived movement values and power availability, and keep the displayed motion consistent with the host's results.

**Contribution to gameplay:** Players thrust forward or backward, strafe relative to their ship, and turn toward the mouse cursor while aiming. Afterburner uses the engine's boosted speed and acceleration at increased energy demand. Manual braking and a toggle for automatic braking let players control slowing and coasting. Ship mass affects turning and braking response. Without an available engine or sufficient power, thrust, turning, and braking stop responding while existing velocity continues to carry the ship. Movement collisions prevent passing through solid surfaces and allow sliding along them.

**Synced: Yes.** The host simulates movement at 60 Hz. Clients send movement commands and the host sends movement snapshots on the 30 Hz network cadence using unreliable messages. Local prediction makes a client's ship respond before the host's reply; authoritative updates correct differences and replay pending input. Other ships are displayed smoothly between received states. Initial state and recovery snapshots use reliable delivery when joining or resynchronizing.

**Responsibilities during Initialization Phase:** Establish each ship's starting position, rotation, and velocity, then configure movement from its equipment, mass, and power state. Connect local input and the following camera to the controlled ship. Joining clients apply the host's initial movement state and prepare local prediction and remote movement presentation. Activate movement synchronization only after session readiness checks succeed.

**Responsibilities during GamePlay Phase:** Read steering and braking input, simulate movement and collisions, and update movement capabilities when equipment or power changes. Exchange commands and host results, correct local prediction, and smooth remote ships. Clear held actions when player control becomes unavailable, and request fresh authoritative state if movement synchronization needs recovery.

### NetworkSynchronisation

**Ownership:** The host owns the authoritative simulation timeline and completed gameplay state. Each session has its own synchronization coordinator. Clients maintain local movement prediction, timing estimates, and presentation of other ships while accepting the host's gameplay results.

**Responsibility:** Keep players' views of the shared world consistent, apply commands to the correct player and ship, and coordinate simulation updates with network publication. Handle the initial state needed to join, ongoing updates, and recovery when movement synchronization falls behind.

**Contribution to gameplay:** Players see the same ships, resource values, equipment-driven capabilities, and supported ability effects. Local movement prediction keeps steering responsive, while smoothing makes other ships move continuously between updates. Equipment transfers, ability activation, resource changes, and movement are processed in a defined order on the host so their effects agree within the shared simulation. Joining players receive the current world state while existing players continue playing.

**Synced: Yes.** The host simulates at 60 Hz and publishes movement at 30 Hz through unreliable messages. Changed gameplay state is sent reliably on the 30 Hz cadence. World membership and ship-control changes, item contents, and action results use reliable delivery; personal inventory and equipment contents go to their owner. Clients reconcile predicted movement with host results and reject outdated or unrelated-session updates. Initial and recovery movement snapshots are delivered reliably.

**Responsibilities during Initialization Phase:** Establish the host's session identity and starting simulation time. Supply joining clients with the ships and control assignments in the world, movement state, gameplay resources and effects, and their item contents. Apply and validate this initial state before activating synchronization. Prepare client timing, prediction history, and remote-ship presentation, then begin normal updates after readiness checks succeed.

**Responsibilities during GamePlay Phase:** Collect and validate player commands, advance the host simulation, and distribute completed results. Adjust client prediction timing for network delay, reconcile local movement, and smooth remote movement. When movement updates stop arriving or local prediction can no longer be reconciled, temporarily suspend prediction and request fresh authoritative movement state. Resume after recovery; a recovery timeout ends the affected client's session. Clear obsolete commands when players leave or their ship assignment changes.

### NetworkSystem

**Ownership:** Each local session owns its network manager and active connection transport. The host controls player admission, membership, and spawn approval. Clients connect to that host and submit gameplay requests for authoritative handling.

**Responsibility:** Establish and maintain multiplayer connections, check compatibility and player capacity, coordinate joining, carry gameplay messages, and handle departures or connection failures.

**Contribution to gameplay:** Players can host or join through Steam lobbies using GodotSteam MultiplayerPeer, or connect directly by IP address and port over LAN using ENet. Sessions support up to ten players including the host. Joining players receive their starting world and player state before gaining control, and session messages announce joins and departures. Compatibility checks prevent players with mismatched gameplay data or configuration from joining the same game.

**Synced: Yes.** Connection setup and readiness are coordinated between the host and clients. Join confirmations, membership changes, initial world state, gameplay updates, item transfers, and ability requests and results use reliable messages. Frequent movement commands and snapshots use unreliable messages. The transport carries synchronized state; its local connection objects are not themselves replicated.

**Responsibilities during Initialization Phase:** Start the selected Steam or LAN transport and establish the lobby or endpoint. Compare game, protocol, configuration, and gameplay-data compatibility, and enforce the player limit. Steam connections also verify lobby membership and the expected host identity. Complete the join exchange, obtain host approval for the player's ship, deliver initial world and player state, and report readiness after confirmation.

**Responsibilities during GamePlay Phase:** Route gameplay requests and updates, accept eligible new players, and monitor connection health. Remove disconnected or timed-out players and their pending requests, then notify remaining players. Losing the host ends a client's session; host migration is not implemented. Closing a session releases the connection and leaves its Steam lobby when applicable.

### Sessions

**Ownership:** Each running game has a local session coordinator responsible for the session's lifetime. The world owns ships and the shared item store. Each connected player has a player session owning their personal inventory and current ship-control assignment; equipment belongs to the ship.

**Responsibility:** Keep the world, player ownership, gameplay services, and interface connections together for one play session. Coordinate startup and activation, and release acquired resources when the session ends or fails, including when startup only partially completes.

**Contribution to gameplay:** Players act through their assigned ships and retain a separate personal inventory for the current session. A ship can be controlled by at most one player at a time. When a player leaves, their current ship and personal inventory are removed, while the remaining players can continue. Ending the host's session ends the clients' participation as well.

**Synced: Yes.** Participant membership and ship-control assignments are reliably distributed by the host. Personal item contents and ship gameplay state follow their respective synchronization paths. Requests identify the current ship assignment so delayed actions from an earlier assignment cannot affect a new one. Each machine's session lifetime, interface connections, and cleanup remain local.

**Responsibilities during Initialization Phase:** Create the world, item store, gameplay services, network connection, and local player interface. Establish participant inventories and host-approved ship assignments, and track the resources that must be released if startup fails. Activate the session only after network state and local gameplay bindings are ready. Existing session capture and restoration support uses in-memory state; gameplay disk save/load is not implemented.

**Responsibilities during GamePlay Phase:** Maintain participant ownership and current ship assignments, and reconnect controls and equipment views if an assignment changes. Coordinate player departures and session shutdown with the world and network systems. On exit or failure, stop gameplay activity and release the session's connections, objects, and interface bindings so a subsequent session starts independently.

### ShieldSystem

**Ownership:** Each ship owns its shield charge, capacity, and recovery downtime. Its installed shield generator and active modifiers determine shield capabilities. The host controls shield changes; clients receive the authoritative state.

**Responsibility:** Maintain shield charge within its capacity, recharge it when powered, drain it when power is unavailable, and track the recovery delay after depletion. Determine whether shield protection is available from the installed generator, power supply, remaining charge, and recovery state.

**Contribution to gameplay:** A powered shield generator automatically restores charge over its configured recharge time. Losing power immediately makes protection unavailable and drains remaining charge at the same rate used for recharge. When charge falls from a positive amount to zero, the generator's downtime must elapse before powered recharge resumes. Equipment and temporary ability effects can change maximum capacity; added capacity fills through normal recharge, while a reduced maximum caps the remaining charge. Removing the generator clears shield capacity and charge, so reinstalling it requires recharging. The HUD shows current and maximum shields and dims the shield display when protection is unavailable. These resource and readiness mechanics are implemented; absorbing combat damage is not yet implemented.

**Synced: Yes.** The host advances shield resources during the 60 Hz gameplay simulation. Changed shield charge, capacity, protection availability, recovery timing, and relevant ship stats are included in reliable gameplay snapshots on the 30 Hz network cadence. Clients apply the host's confirmed state for their shield displays. Joining players receive the current shield state during initial synchronization.

**Responsibilities during Initialization Phase:** Establish each ship's shield resource and derive its capacity, recharge time, and downtime from equipment and modifiers. Newly initialized ships start with full shields at their initial capacity, although protection still depends on power availability. In-memory session restoration recovers shield charge and recovery timing with the ship's state. Joining clients apply the host's initial shield state before gameplay becomes ready.

**Responsibilities during GamePlay Phase:** Recharge or drain shields according to power availability, enforce depletion downtime, and update protection readiness. Apply capacity changes from equipment and temporary effects without granting an immediate refill. Keep shield values within the current maximum and publish changes through the ship's shared gameplay state so the HUD and other players remain up to date.

### ShipSystem

**Ownership:** The world owns each ship and its lifetime. A player session holds the assignment to the ship that player controls, and a ship can have at most one controlling player. Equipment, health, shields, energy, and active ability effects belong to the ship; the player's personal inventory remains separate. The host has authority over ship gameplay state.

**Responsibility:** Bring together a ship's definition, physical presence, equipment, movement, resources, and abilities. Its definition supplies its appearance, collision shape, base mass, health capacity, and stat modifiers. Its loadout connects installed equipment to the capabilities used by the gameplay systems.

**Contribution to gameplay:** Players currently start in a fighter. Equipping an engine, hull, shield generator, reactor, battery, and weapon equipment changes the ship's loadout, with supported equipment values determining movement, power balance, shields, and available abilities. The HUD shows the controlled ship's health, shields, energy, velocity, armor, and mass. Player ships share an allied faction for cooperative ability targeting. Health is tracked and displayed, but combat damage, combat destruction, and respawning are not yet implemented gameplay features.

**Synced: Yes.** The host reliably distributes ship creation, removal, and control assignments. Changed resources, effective stats, conditions, and active effects are sent in reliable gameplay snapshots on the 30 Hz network cadence. Movement uses unreliable snapshots at 30 Hz, with local prediction and correction for the controlled ship and smoothing for other ships. Detailed equipment contents are sent reliably to the controlling player; other players receive the resulting ship capabilities. Ship definitions load locally and participate in compatibility checks.

**Responsibilities during Initialization Phase:** Load and validate ship definitions, then construct host-approved ships with their appearance, collision shape, starting movement state, equipment containers, and gameplay resources. Apply the starting loadout and allied faction, and assign each player their ship. Newly initialized ships start with full health and shields and empty energy storage. In-memory restoration can recover existing ship state instead. Joining clients receive authoritative ship state, and the local player's input, camera, equipment view, and HUD connect to their controlled ship before gameplay becomes ready.

**Responsibilities during GamePlay Phase:** Keep each ship's capabilities consistent with equipment changes, power availability, and temporary effects. Participate in the shared simulation so resource and ability updates feed into movement and synchronized gameplay state. Present the controlled ship and other ships using their respective movement updates. Clear held input when control is lost, and release equipment and runtime state when the world removes the ship, including when its controlling player leaves or the session ends.

### StatSystem

**Ownership:** Each ship owns its equipment-derived and effective gameplay stats. Shared stat definitions are loaded locally by each running game. The host calculates authoritative ship values; clients receive the confirmed results.

**Responsibility:** Combine ship modifiers, installed equipment, and supported temporary effects into the values used by movement, energy, shields, and the HUD. These include movement speeds and acceleration, mass, armor, shield capacity and recovery timing, and energy production, consumption, and storage. Keep calculated values non-negative and reject invalid results.

**Contribution to gameplay:** Equipment choices determine the ship's capabilities: engines supply movement values, hulls supply armor, shield generators supply shield values, reactors supply energy production, and batteries supply storage. Ship bonuses apply only when the corresponding equipment is installed, so a movement bonus cannot replace a missing engine. Every installed item's mass contributes to the ship's total mass, affecting turning and braking. Equipment also contributes its configured continuous energy demand; weapon fixtures add mass without adding energy demand. Temporary stat effects currently modify shield capacity, combining flat additions before percentage adjustments. The supported Shield Capacity Boost increases the maximum without immediately filling the extra capacity. Armor is calculated and displayed, but combat damage reduction is not yet implemented.

**Synced: Yes.** Effective ship stats and the presence of the relevant equipment are included in reliable host gameplay snapshots when state changes, on the 30 Hz network cadence. Joining clients receive the initial values before gameplay becomes ready. Clients can use another ship's confirmed capabilities without receiving its detailed equipment contents. Shared stat definitions load locally and participate in compatibility checks.

**Responsibilities during Initialization Phase:** Load and validate the stat catalogue and gameplay references, prepare equipment values, and calculate starting capabilities from each ship's definition and installed loadout. Supply resource capacities and movement values before initial resource setup. In-memory restoration rebuilds the ship's effective stats with its saved active effects; joining clients apply the host's confirmed values.

**Responsibilities during GamePlay Phase:** Recalculate affected values when equipment changes or temporary effects are applied or expire. Supply updated movement settings, energy balance, and resource limits to the systems that use them. Removing equipment removes its associated capabilities and contributions, while an expiring shield boost returns capacity to the remaining calculated maximum. Publish resulting changes through the ship's gameplay state for synchronization and HUD updates; EnergySystem and ShieldSystem use these values to advance resource amounts.

### WeaponSystem

**Ownership:** Carried weapon items belong to the player's inventory; installed weapons and their weapon fixture belong to the ship. The host controls equipment changes. Each player owns their local weapon hotbar shortcuts. Current weapon support is implemented through ItemSystem and EquipmentSystem, without a separate firing runtime.

**Responsibility:** Manage weapon categories, equipment placement, and the capacity supplied by the installed weapon fixture. Validate weapon definitions and their damage-source references, and supply equipped weapon mass and continuous energy demand to ship stats.

**Contribution to gameplay:** Players can equip light, medium, and heavy weapons, consuming one, two, and three units of fixture capacity respectively. A fixture provides up to nine capacity units; without one, no weapons can remain equipped. Players can mix weapon sizes within that limit, and the equipment view shows remaining capacity. For example, nine units can accommodate nine light weapons or three heavy weapons. Equipped weapons add mass and continuous energy demand, while the fixture adds mass only. Weapon shortcuts can be assigned to the hotbar. Firing, projectile attacks, ammunition consumption, reloading, and damage application are not yet implemented; the corresponding weapon values currently remain definition data.

**Synced: Yes.** Weapon and fixture transfers use reliable requests validated by the host. Confirmed inventory and equipment contents are sent reliably to the controlling player, including weapons returned to inventory after a fixture change. Changed contents are published on the 30 Hz network cadence, and transfer replies include the current contents. Other players receive resulting ship stats through gameplay synchronization. Hotbar shortcuts remain local, and there are no weapon-firing events to synchronize yet.

**Responsibilities during Initialization Phase:** Load and validate weapon categories, weapon values, damage-source references, and fixture capacity. Create the ship's weapon equipment positions and install the starting fixture before its weapons. The current starter loadout includes a nine-capacity fixture and one light, one medium, and one heavy weapon, using six capacity units. Joining clients receive the authoritative equipment state before transfers become available.

**Responsibilities during GamePlay Phase:** Validate equipping, swapping, rearranging, and unequipping weapons against the fixture's capacity. Removing or replacing a fixture with a smaller one automatically returns enough weapons to inventory, starting with the highest-numbered occupied weapon positions; reject the change if the return cannot complete. Update ship mass, energy demand, and the capacity display after confirmed changes. Clear local hotbar shortcuts when their weapon positions become empty.

### WorldSystem

**Ownership:** Each session owns a world containing its ships, player sessions, and shared item store. Ships own their equipment and gameplay state; player sessions own personal inventories and ship-control assignments. The host controls the authoritative world, while clients maintain synchronized local copies.

**Responsibility:** Track which ships and players exist, create and remove ships, and keep player-control assignments consistent. Provide the shared collection of ships used by gameplay simulation, ability targeting, and synchronization.

**Contribution to gameplay:** Players join the same play space with ships placed at separate starting positions. Their ships share an allied faction so supported cooperative abilities can target teammates. Existing players can continue playing as others join or leave. When a client leaves, their current ship and personal inventory are removed while the remaining players stay in the session.

**Synced: Yes.** The host reliably distributes the current ships and player-control assignments when players join and when those relationships change. Clients create missing ships, update assignments, and remove entities that no longer belong to the world. Movement uses its own 30 Hz synchronization, while ship resources and effects use reliable gameplay snapshots. Personal inventory and equipment contents follow the owner-specific item synchronization path.

**Responsibilities during Initialization Phase:** Create the session's world and item store, validate requested ship types, and establish host-approved ships with their starting positions and player assignments. Apply the session's starting inventory and equipment, initialize ship resources, and connect the local player to their ship. Joining clients reconstruct the host's current world before gameplay becomes ready. Existing in-memory capture and restoration can rebuild world state; player-facing disk save/load is not implemented.

**Responsibilities during GamePlay Phase:** Maintain the ships and player assignments used by the shared simulation and targeting systems. Add newly approved players, publish changes to world membership and control, and remove departing players and their associated state. Preserve ship identity and equipment when internal control assignments change, and clear held input when a ship loses control. Release remaining ships and player state when the session ends.
