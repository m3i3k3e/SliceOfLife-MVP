# Slice of Life — Systems Overview

This document sketches how major MVP systems talk to each other.  Boxes are major managers or services; arrows show the direction of the data flow.

## Economy
`EssenceManager` tracks currency and click limits.  UI components listen only through the event bus.

```text
Player Input
    |
    v
EssenceManager --(OnEssenceChanged)--> GameManager.Events --> HUD
        ^                                         |
        |                                         v
GameManager (daily click gates) <-------------- UpgradeManager
```

**Key API**
- `bool EssenceManager.TryClickHarvest()` – spend one click and add essence if the daily cap allows it.
- `bool GameManager.TrySleep()` – UI entry point to advance the day when the Sleep gate is open.

## Battle
`BattleManager` handles turn flow and grants essence rewards through the economy.

```text
BattleManager -> EventBus -> BattleUI
      |
      v
EssenceManager.AddExternal(reward)
```

## Upgrades
`UpgradeManager` owns purchasable upgrades and forwards unlocks via the bus.

```text
UpgradeManager --(OnPurchased)--> EventBus --> UpgradesPanel
        \
         -> GameManager (e.g., unlock battle)
```

## Save / Load
`SaveModelV2` captures state from any system implementing `ISaveParticipant`.  `GameManager` keeps a registry of these participants so `SaveSystem` can iterate them generically.  `SaveScheduler` coalesces rapid save calls before hitting disk.

```text
[GameManager, InventoryManager, UpgradeManager, ...]
    |                    |
    v                    v
        SaveModelV2 <--> SaveSystem --(deferred)--> Disk
```

## Event Bus
`IEventBus` is the decoupling layer between gameplay logic and presentation.  Managers raise events; UI listens.

```text
[EssenceManager, UpgradeManager, StationManager, BattleManager, ...]
        |
        v
      EventBus -> UI (HUD, UpgradesPanel, BattleUI, ...)
```

## Stations & Companions (stubs)
`StationManager` will map recruited companions to production stations.  Production results bubble through the bus.

```text
CompanionSO -> StationManager -> EventBus -> HUD
```

## Future Hooks
- **Minigames** – `StationManager` raises `MinigameCompleted`; future interfaces can plug new minigames into the same flow.
- **Companions as Cards/Stations** – companion recruitment already exposes starting cards and buffs; UI and battle systems can consume these once implemented.
- **Legacy Prestige** – plan for a prestige layer that consumes long‑term progress and resets `GameManager`/`EssenceManager` state for meta rewards.
