# CharmCore

The engine behind the Charm mods. A small valuable in your pocket gives you a power, and every use wears the valuable down. CharmCore does nothing on its own: install it together with at least one Charm mod, such as Charm Coffee Cup.

> **Everyone in the lobby must install this mod, together with Pockets and the Charm mods you play with.** Charms change what your own character can do and show their state on your inventory slots, so friends without them would see empty slots and be unable to use the pockets.

## How to Use

1. Store a valuable that has a charm in a pocket with [Pockets](https://thunderstore.io/c/repo/p/OsmarBriones/Pockets/) (grab it and press a free inventory slot key).
2. Its power is active while it stays in the pocket. Take it out and the power stops.
3. Each use wears the valuable down, like a hit would. Its value shows on the slot. When it is worn out it breaks, the power ends, and you lose it.

## Features

- **Powers from your pockets:** every Charm mod ties one power to one valuable.
- **Wear:** a power costs part of the valuable's original value each time it works, so you decide when it is worth using.
- **Cooldown on the slot:** powers that need a pause show a dark veil that empties on the slot, with the seconds left.
- **The host decides:** the host's settings apply to everyone, so nobody has an unfair edge.
- **Nothing carries over:** pockets are emptied when the level ends, and so are the powers.

## Requirements
- [BepInEx Pack for R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
- [Pockets](https://thunderstore.io/c/repo/p/OsmarBriones/Pockets/)

## Installation
1. Install the latest [BepInEx Pack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).
2. Install through r2modman / Thunderstore Mod Manager, or place `CharmCore.dll` in your `BepInEx/plugins` folder next to `Pockets.dll`. **All players need it.**
3. Install the Charm mods you want to play with.
4. Launch the game once; the configuration file is created inside `BepInEx/config`.

## Configuration
All settings are in `com.osmar.CharmCore.cfg` inside `BepInEx/config`. Each Charm mod has its own file with its own settings (`Enabled`, `WearPercent`, `CooldownSeconds`, and any extras).

- `Enabled` (default `true`): turn every charm on or off. The host's choice applies to everyone.
- `WearMultiplier` (default `1`): scales how fast every charm wears its valuable down. `0.5` halves it, `2` doubles it. The host's choice applies to everyone.

Every Charm mod also has a `Debug` section with `DebugMode` (default `false`). For testing: when a level starts, that charm's valuable is placed on the floor in front of you, and extra log lines are written. `SpawnNow` (default `false`, needs `DebugMode`) places one more near you right away when you set it to `true` while a level is running; it switches itself back off. Both only place the valuable if you are the host or playing alone.

## For mod authors
CharmCore exposes a small API: derive from `Charm`, register it with `CharmsApi.Register` (this also gives your charm `Debug.DebugMode`) and call `TryUse()` when your power triggers. Write debug-only log lines with `Charm.LogDebug`. New charms are created with the `create-charm` skill of RepoKit, which starts them following these rules. See [ARCHITECTURE.md](https://github.com/OsmarBriones/REPO-CharmCore/blob/master/ARCHITECTURE.md).

## Issues & Bug Reports
The official way to report issues, suggest improvements, or submit feedback is by opening an issue on the official GitHub repository:
👉 [GitHub Issues](https://github.com/OsmarBriones/REPO-CharmCore/issues)

## Credits
Developed by **Osmar Briones**
