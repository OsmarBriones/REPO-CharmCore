# CharmCore Architecture

CharmCore is layer 2 of the PocketCharms family (see `sandbox/PocketCharms_spec.md` in the workspace). It depends on **Pockets** (storage) and is depended on by every `Charm<Valuable>` mod.

## Responsibilities

- A registry of charms (`CharmsApi`), each tied to one or more valuable prefab names.
- Tracks the **local player's** pockets through `PocketsApi.Stored` / `Removed` and switches the matching `Charm` on and off.
- A wear engine: wearing a valuable down is requested by the carrier and applied by the master.
- Cooldown timing and its display on the inventory slot.
- Host-authoritative settings.

It does not implement any power. Powers are local to the carrier (energy, damage taken, speed), so each Charm mod reads game state itself and calls `Charm.TryUse()`.

## Public API (`CharmCore.Api`)

- `Charm` (abstract): constructor takes id, display name, default wear percent, default cooldown (null for none) and prefab names. Members: `IsActive`, `IsReady`, `WearPercent`, `CooldownSeconds`, `CooldownRemaining`, `TryUse()`, `TryUse(float wear)`, `Wear(float)`, `AddSetting(...)`, `OnActivated`, `OnDeactivated`.
- `CharmSetting`: a numeric setting with the host's value in force (`Value`).
- `CharmsApi.Register(charm, config)`: binds `Charm.<Id>` config section (`Enabled`, `WearPercent`, `CooldownSeconds`, extras).

## Flow

1. `PocketsApi.Stored` fires for a valuable. `CharmTracker` ignores other players, looks the valuable's prefab name up in the registry and calls `Charm.Carry`. `Charm.IsActive` is true while at least one matching valuable is pocketed.
2. The Charm mod's own code decides when its power triggers and calls `TryUse()`. If the charm is ready, the cooldown starts, wear is requested and the call returns true.
3. `CharmWear` (a component added to every valuable by `ValuableObject_Start_Patch`) sends `RequestWearRpc(percent, position)` to the master (or applies it directly on the master or alone). The master checks the valuable is pocketed and calls `PhysGrabObjectImpactDetector.Break(valueLost, position, breakLevelLight, true)`, the routine vanilla uses for hits. That lowers the value on every client, shows the floating loss number and destroys the valuable under 15% of its original value.
4. A destroyed valuable raises no pocket event, so `CharmTracker.Update` drops the charm when its valuable disappears.
5. `CharmSlotView` (on each `InventorySpot`) draws the veil and seconds while `CharmTracker.TryGetCooldown` reports a running cooldown for that slot.

## Settings and the host

`ConfigurationController` holds `Enabled` and `WearMultiplier`. Each charm adds `Enabled`, `WearPercent`, `CooldownSeconds` and its own `CharmSetting`s. The master publishes all of them as room custom properties (`HostSettings.Publish`) on level start, when a setting changes and when the master changes; clients read them on demand and alone a machine uses its own values.

## Debug mode (every charm)

Each charm gets a local `Debug.DebugMode` setting (off by default) in its own config file, bound by `Charm.Bind`. While it is on:

- `DebugSpawner` (started from `EnemyDirector.Start`) waits for `LevelGenerator.Generated` plus a short settle time, then, on the host or alone, places the charm's valuable on the floor about 2.5 m in front of the player (several debug charms are spread sideways). Valuable prefabs sit in the game's Resources folder (`Valuables/01 Tiny/<prefab>`), so they can be spawned in any level; no particular level is needed. In multiplayer it uses `PhotonNetwork.InstantiateRoomObject`, alone `Object.Instantiate`.
- `Charm.LogDebug(message)` writes the log lines that only matter while debugging (carry/drop, each use, each wear step with original and current value, worn-out destruction). Normal use logs only startup, registration and Pockets' own store/release lines. Charm mods use `LogDebug` for their own detail.

## Hooks

| Target | Why |
|--------|-----|
| `EnemyDirector.Start` (postfix) | reload config, clear tracked charms, publish host settings |
| `ValuableObject.Start` (postfix) | add `CharmWear` and refresh the RPC cache |
| `InventorySpot.UpdateUI` (postfix) | attach `CharmSlotView` |

## Known limits

- The master does not verify who asked for a wear step beyond checking the valuable is pocketed; a modified client could wear its own valuable, which only hurts itself.
- Charms are looked up by exact prefab name (for example `Valuable Arctic Coffee Cup`), so a game update that renames a prefab needs a mod update.
- Only the local player's pockets are tracked.

## Tests

`CharmCore.Tests`: Tier 1 for `WearMath` and `CooldownTimer`; Tier 2 reflection checks of the game members used (`dotnet test`).
