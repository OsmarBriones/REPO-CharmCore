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
5. `CharmSlotView` (on each `InventorySpot`) draws the veil and seconds while `CharmTracker.TryGetCooldown` reports a running cooldown for that slot. The veil is placed right above the picture in the UI order (below Pockets' value label, so the value is never covered) and the seconds are small, at the top of the picture; the value stays at the bottom. The slot is tiny (about 60 pixels wide, and the screen cuts it off at the bottom, leaving about 40 pixels of height), so while a cooldown shows, `CharmSlotView` fits both texts to the VISIBLE part of the slot: it clips the slot's rectangle to the screen, `SlotLayout.Split` divides it into an upper box for the seconds and a lower box for the value (the boxes are tall and may overlap, because the digits fill only about half the height of a line; the centres are kept far enough apart that the digits never touch, which lets the auto-sized font be about 40% larger than with two separate half-height boxes), and each text is moved and sized into its box with auto-sizing (largest font that fits). The value label is Pockets' (found by its name `PocketValue`, a child of the slot's `noItem` parent); its original anchors, size, position and font settings are saved and restored when the cooldown ends. The layout is recomputed every frame while the cooldown shows, so a resolution change does not break it. The offsets are in `Domain/SlotLayout.cs`.

## Settings and the host

`ConfigurationController` holds `Enabled` and `WearMultiplier`. Each charm adds `Enabled`, `WearPercent`, `CooldownSeconds` and its own `CharmSetting`s. The master publishes all of them as room custom properties (`HostSettings.Publish`) on level start, when a setting changes and when the master changes; clients read them on demand and alone a machine uses its own values.

## Shared hooks and helpers for charm mods

- **Player damage (`CharmsApi.OnPlayerHurting`).** `PlayerHealth_Hurt_Patch` is the only patch on `PlayerHealth.Hurt` (which acts only on the damaged player's own machine). It checks the same conditions the game checks first (not invincible, not dead, not god mode, game running), so handlers only see hits that would really land, and passes a `PlayerHurtContext` through the handlers in priority order (suggested: 0 cancelling effects, 10 last-chance saves, 20 damage reduction). A handler may lower `Damage`, set `Cancel`, or ask for `InvincibleSecondsAfter`.
- **Wear over time (`Charm.WearForSeconds`).** For charms whose `WearPercent` is per second (set `WearsPerSecond` in the constructor): it accumulates and asks for wear in steps of at least 1%. This wear is **quiet**: the game's break routine plays a sound, smoke and a floating number on every loss, which would tick several times a second, so continuous wear is requested with a special break level (`CharmWear.QuietBreakLevel`) that `PhysGrabObjectImpactDetector_BreakRPC_Patch` recognises on every machine; it then subtracts the value and destroys the valuable under 15% of its original value exactly as the game does, but skips the effects (the destruction still plays its own effects once). Per-use wear stays audible as feedback. A host effect that wears continuously uses `Charm.WearValuable(valuable, percent, quiet: true)`.
- **Host effects.** Some powers only work on the host (moving a valuable, hurting a monster, spawning an enemy). Two ways: the host applies the effect itself for every carrier with `CharmsApi.ForEachCarrier` / `CarriedBy` (Pockets tracks every player's pockets on every machine) and wears with `Charm.WearValuable`; or the owner sends a request with `Charm.SendToHost(action, args)` (Photon event 183, delivered to the master, which checks the sender carries the charm and calls `OnHostRequest`).
- **Wear modifiers (`CharmsApi.OnWearing`).** A `WearContext` is offered to handlers on the host before value is removed; a charm may lower `Loss` for valuables carried by a player (for example a charm that protects other charms). `CharmsApi.CarrierOf` finds who carries a valuable.

## Charging charms

A power tied to an event that can be waited for (an extraction point completing) could otherwise be used by pocketing the valuable at the last second. A charm calls `EnableCharge(defaultSeconds)` in its constructor to become a *charging* charm: it gets the host-synchronised setting `ChargeSeconds` (5 to 1800), its charge belongs to the VALUABLE, not to the pocket: it starts at 0, grows linearly only while the valuable is stored in a pocket (any player's), is kept when the valuable is taken out (it just stops growing), continues for whoever stores it next, and is spent with `CharmsApi.ConsumeCharge`. How it works:
- `CharmTracker` keeps a `Domain/ChargeTimer` per charging valuable, from the pocket events of EVERY player (every machine receives them), in a dictionary by instance id (`Charges`, cleared when a level ends): a store starts the clock, a removal adds the elapsed time to what was kept and stops the clock (so the charge survives being taken out and is passed on to the next player who stores it), a spend resets it to zero.
- `Charm.ChargeFraction` is the charge of the valuable the local player carries; `CharmsApi.ChargeOf(avatar, charm)` is the charge of any player's charm (for effects the host applies). A start that was missed counts as starting now.
- `CharmsApi.ConsumeCharge(charm, valuable)` restarts the charge on this machine and `ChargeEvents` (Photon event 184, host to the others) restarts it everywhere, so the owner's slot shows the empty charge too.
- The slot view shows a charge like a cooldown with the opposite meaning: the percentage charged in place of the seconds, and a veil that lifts as it fills; nothing is shown once it is full.
- The tracking is per machine and driven by the pocket events, so charges agree within the network latency; a charm that must be exact should treat the host's reading as final. Limit: a player who joins in the middle of a level only learns about valuables currently stored (Pockets resends those), so their local reading of a charge that was built before they joined starts from zero; the host's reading, which decides the effects, is right.

## Runtime hygiene

Reviewed against `RepoKit/references/runtime-hygiene.md`:
- **References:** the tracker's slot arrays, each charm's carried list, the charm-of-valuable cache and every charm's `OnLevelEnded` are cleared when a level ends (see below). Nothing static holds a game object after that. The slot view keeps references only to HUD objects, which live as long as the HUD.
- **Allocations:** the network keys of a charm's settings (`Enabled`, `WearPercent`, `CooldownSeconds`, and each custom setting) are built once instead of on every `IsActive`, `WearPercent` or `Value` read (they were rebuilt as strings each call while a charm was active). `CharmsApi.CharmOf` caches which charm a valuable belongs to by instance id (the name lookup allocated strings), and `ForEachCarrier` / the host-request check use `FirstCarried`, which allocates only Pockets' small copy of the slots (no lists, no `ToArray`).
- **Work frequency:** the level check runs twice a second, the spawn switches once a second, and the slot view recomputes text and layout only when its inputs changed.
- **Known remaining allocation:** `CharmsApi.CarriedBy` (public) still builds a list per call; hot paths should prefer `ForEachCarrier`.

## Cleanup when a level ends

`CharmTracker` checks twice a second whether the game is still inside a level (`SemiFunc.RunIsLevel`). When a level ends it releases every tracked slot, deactivates the charms (`OnDeactivated`) and calls `Charm.OnLevelEnded` on each charm (also called when the next level starts). Charm mods override `OnLevelEnded` to clear what they remember about the level (caches, sets, lists of game objects, per-player accumulators), so no field keeps pointing at objects the game is about to destroy. This was added after a crash inside Unity's memory scan when leaving a level, where the cause could not be shown to be ours but static references to level objects are exactly what that scan walks. The slot view also touches the UI only when something changed (text, position), because every property change makes a TMP text rebuild its mesh.

## Debug mode (every charm)

Each charm gets a local `Debug.DebugMode` setting (off by default) in its own config file, bound by `Charm.Bind`. While it is on:

- `DebugSpawner` (started from `EnemyDirector.Start`) waits for `LevelGenerator.Generated` plus a short settle time, then, on the host or alone, places the charm's valuable on the floor about 2.5 m in front of the player (several debug charms are spread sideways). Valuable prefabs sit in the game's Resources folder (`Valuables/01 Tiny/<prefab>`), so they can be spawned in any level; no particular level is needed. In multiplayer it uses `PhotonNetwork.InstantiateRoomObject`, alone `Object.Instantiate`.
- **`Debug.SpawnNow` switch** (bound next to `DebugMode` by `Charm.Bind`): `CharmTracker` calls `DebugSpawner.ServeSpawnSwitches` about once a second; a charm in debug mode re-reads its config file (so editing the file while the game runs works, as does an in-game config menu), and when the switch is on the valuable is placed near the player with `DebugSpawner.TryPlace` and the switch is turned back off. Without `DebugMode` the switch is just turned off with a warning; only the host or a singleplayer game can place valuables.
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
