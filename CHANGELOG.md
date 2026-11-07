# Changelog

## [Unreleased]

### Added
- `Debug.SpawnNow` switch for every charm: with `DebugMode` on, setting it to `true` during a level places the charm's valuable near you and turns the switch back off.
- Shared building blocks for charm mods: one hook for damage to the local player (`OnPlayerHurting`, with priorities, cancel, damage change and post-hit invincibility), continuous wear (`WearForSeconds`), host effects (`ForEachCarrier`, `CarriedBy`, `SendToHost`/`OnHostRequest`, `WearValuable`) and wear modifiers (`OnWearing`, `CarrierOf`).
- Debug mode for every charm (`Debug.DebugMode`, off by default): its valuable appears on the floor in front of you when a level starts, and detailed log lines are written. Debug-only log lines moved behind it.
- Charm engine: register a `Charm` per valuable, active while the local player carries it in a Pockets slot.
- Wear: each use wears the valuable down through the game's own break routine, applied by the host.
- Cooldown shown on the pocket slot (dark veil that empties, plus seconds left).
- Host-authoritative settings: `Enabled`, `WearMultiplier`, and per charm `Enabled`, `WearPercent`, `CooldownSeconds` and custom settings.
