# Changelog

## [Unreleased]

### Changed
- Runtime hygiene pass: network keys of the settings are built once (they were rebuilt on every read while a charm was active), the charm of a valuable is cached by instance id, and carrier lookups no longer allocate lists.

### Added
- `Charm.OnLevelEnded`: charms are told when a level ends (and when the next one starts) so they clear what they remember about it; the tracker also forgets its slots at the end of the level instead of at the start of the next one.
- The cooldown slot view updates text and layout only when something changed.
- While a cooldown is shown on a slot, the seconds and the stored valuable's value are fitted to the visible part of the slot (seconds above, value below, each auto-sized to use as much room as possible) so they are as large as the slot allows and do not overlap; the value returns to its normal size and place when the cooldown ends.
- The cooldown veil no longer covers the stored valuable's value: the veil is drawn under the value label and the seconds are shown small at the top of the slot.
- Quiet wear: continuous wear (`WearForSeconds`, and `WearValuable`/`Wear` with `quiet: true`) no longer plays the game's break sound, smoke and floating number on every step; it only subtracts the value and destroys the valuable at the usual 15% limit. Per-use wear is unchanged.
- `Debug.SpawnNow` switch for every charm: with `DebugMode` on, setting it to `true` during a level places the charm's valuable near you and turns the switch back off.
- Shared building blocks for charm mods: one hook for damage to the local player (`OnPlayerHurting`, with priorities, cancel, damage change and post-hit invincibility), continuous wear (`WearForSeconds`), host effects (`ForEachCarrier`, `CarriedBy`, `SendToHost`/`OnHostRequest`, `WearValuable`) and wear modifiers (`OnWearing`, `CarrierOf`).
- Debug mode for every charm (`Debug.DebugMode`, off by default): its valuable appears on the floor in front of you when a level starts, and detailed log lines are written. Debug-only log lines moved behind it.
- Charm engine: register a `Charm` per valuable, active while the local player carries it in a Pockets slot.
- Wear: each use wears the valuable down through the game's own break routine, applied by the host.
- Cooldown shown on the pocket slot (dark veil that empties, plus seconds left).
- Host-authoritative settings: `Enabled`, `WearMultiplier`, and per charm `Enabled`, `WearPercent`, `CooldownSeconds` and custom settings.
