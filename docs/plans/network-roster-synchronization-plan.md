# Future roster synchronization

Status: deferred until after the current network message-exchange epic. Refine then; not a prerequisite for NET-14 or this epic.

Goal: the host maintains authoritative participant membership and synchronizes it to every client. Each process maintains one local `MpController` Node per participant, including itself.

Discuss full roster delivery on join, updates on join/leave, duplicate/stale messages, controller lifecycle and cleanup, and tick dispatch. Target co-op sessions of typically 5 players, at most 10; keep the solution proportionate.

Earlier architecture proposals remain in the [refinement plan](network-vertical-slice-refinement-plan.md#global-registry-and-controller-ownership) as background, not adopted implementation requirements.
