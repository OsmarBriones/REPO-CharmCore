## User Scenarios

### User Story 1 - A pocketed valuable grants its power (Priority: P1)
A player stores a small valuable with Pockets. If a Charm mod exists for that valuable, its effect becomes active while the valuable stays in the pocket and stops when it is taken out.
**Acceptance**: store a valuable with a registered charm -> effect active; take it out -> effect inactive; level change -> effects cleared.

### User Story 2 - Charms wear down (Priority: P1)
Each use of a charm wears down the valuable's dollar value like a hit. At 0% the valuable breaks and the effect ends.
**Acceptance**: a charm trigger lowers the valuable's value by its wear percent times the global wear multiplier; the valuable breaks at zero.

### User Story 3 - Cooldown shown on the slot (Priority: P2)
A charm with a cooldown shows it on the pocket slot as a darkened fill that empties, plus the seconds left. A cooldown of 0 shows nothing.

### User Story 4 - Host-authoritative settings (Priority: P2)
The global switch, wear multiplier and per-charm cooldown/wear values are the host's, applied to everyone.

## Requirements
- FR-001: Charm mods register an effect by valuable name through a public API of CharmCore.
- FR-002: CharmCore listens to the Pockets API (Stored/Removed events) and activates or deactivates the registered effect.
- FR-003: Wear engine lowers a valuable's value and breaks it at zero.
- FR-004: Cooldown API: an effect reports its remaining cooldown; CharmCore draws it on the slot.
- FR-005: Config: Enabled, WearMultiplier (default 1.0), EffectMaxSize (default Tiny); host-authoritative.
- FR-006: All players install it (Category 2); it depends on Pockets.
- FR-007: Effects are cleared when the level changes or the mod is switched off.
