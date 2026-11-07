# Plan: Charm engine

Design and flow: see [ARCHITECTURE.md](../../ARCHITECTURE.md). This plan records the decisions and the gates.

## Decisions

- Charms are looked up by exact valuable prefab name; one charm may list several prefabs.
- Powers run on the carrier's machine; wear is requested by the carrier and applied by the master with the game's own `PhysGrabObjectImpactDetector.Break`, so the value, the floating loss number and destruction at under 15% behave like a hit.
- Host settings travel as room custom properties, like Pockets does.
- Pockets is referenced as a built DLL (`REPO-Pockets/bin/Release/net48/Pockets.dll`, or `-p:PocketsDll=<path>`), never copied next to CharmCore.
- RepoAPI reuse gate: nothing generic here; the host-settings helper is a promotion candidate shared with Pockets (`HostSettings`).

## Known Pitfalls Check

| Pitfall | Verdict | Safeguard |
|---------|---------|-----------|
| P-001 Item list growth | Not relevant | No item lists used. |
| P-002 Shop battery inheritance | Not relevant | No shop items. |
| P-003 Singleplayer view id 0 | Relevant | `CharmWear` uses the RPC only in multiplayer; alone it applies wear directly. The local player key is -1 alone, as Pockets does. |
| P-004 Running game keeps old DLL | Relevant | Deployment compared by hash; restart the game before testing. |
| P-005 Hidden valuables vs extraction rooms | Not relevant | Pockets handles hiding; CharmCore only wears valuables. |
| P-006 Hard-coded content lists go stale | Relevant | The prefab names live in each Charm mod, unknown valuables simply have no charm; a missing prefab logs nothing and does nothing. |
| P-007 Switching a mod off strands state | Relevant | A charm holds no game state: `Enabled=false` makes `IsActive` false, wear already applied stays on the valuable, and Pockets owns the pocket state. |
