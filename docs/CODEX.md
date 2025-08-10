\# Slice of Life — CODEX (Vertical-Slice MVP)



\_Last updated: 2025-08-10 • Engine: Unity 6.2 URP • Target: PC (1080p)\_



This file is the source of truth for design + tech. Keep it short and link out to auto-generated snapshots:

\- Project snapshots: `docs/CONTEXT.md`, `docs/assets-files.txt`, `docs/assets-dirs.txt`

  (Update via \*\*Unity Menu → Tools → Generate Project Snapshot\*\*)



---



\## 1) One-Sentence Pitch

“Slice of Life” is a darkly comedic, adult-themed harem-isekai incremental/RPG where you collect essence to grow a basement camp into a tavern-guild, recruit sexy waifus, and battle in card-based, turn-based combat—interspersed with 3D exploration and bite-sized station mini-games.



\## 2) High-Level Overview

\- \*\*Genre \& Tone\*\*: Incremental/RPG hybrid; turn-based, card-driven combat; station mini-games; dark humor; suggestive adult flavor (no explicit porn).

\- \*\*Framing\*\*: Reincarnated adventurer rebuilds an abandoned tavern’s \*\*basement camp\*\* into a kingdom, crafting a harem dynasty across generations.



\## 3) Target Audience

\- \*\*18–45 (primarily male)\*\*; fans of isekai/harem anime, dark comedy, “edgy” niche games.

\- \*\*Platforms\*\*: PC (Itch → EA → Steam).

\- \*\*Community\*\*: YouTube + Patreon.



\## 4) Player Motivations \& Hooks

1\) \*\*Essence Collection\*\*: cheeky panty-themed currency powers upgrades.

2\) \*\*Harem Building\*\*: recruit/level sexy waifus—generic cards for combat + \*\*4 Story Harem\*\* station managers.

3\) \*\*Tactical Battles\*\*: card-driven, turn-based fights (Demeo vibes).

4\) \*\*Stations \& Automation\*\*: Farm, Kitchen, Alchemy, Blacksmith = micro-games and idle sinks.

5\) \*\*Generational Prestige\*\*: heirs carry legacy buffs into tougher biomes.



\## 5) Core Gameplay Loops

\### 5.1 Daily / Session (≈ 10–15 min)

1\. \*\*Manual essence\*\* clicks (≤10/day, scales via altar upgrades).

2\. Spend essence on upgrades (incl. \*\*unlock Dungeon\*\*).

3\. \*\*Dungeon run\*\* (3D static scene): short sequence of fights → boss; harvest resources.

   - Combat: turn-based cards via \*\*waifu cards\*\* (Attack/Guard/Mend/Skill).

4\. \*\*Stations\*\* (once unlocked): quick micro-games; later automate via managers.

5\. \*\*Sleep\*\*: resets click cap, advances day; passives/autos tick.



\### 5.2 Station Unlocks (in-run)

\- Floor 10: \*\*Tavern above basement\*\* + \*\*Farm\*\* (secondary currency: Gold).

\- Floor 20: \*\*Kitchen\*\* • Floor 40: \*\*Alchemy\*\* • Floor 60: \*\*Blacksmith\*\*.

\- Assign 1 of 4 \*\*Story Harem\*\* managers; their levels boost auto-gen and can bypass manual mini-games post-intro.



\### 5.3 Generational Prestige (Legacy)

\- \*\*Trigger\*\*: player chooses to “Fertilize” after major milestone.

\- \*\*Effect\*\*: reset in-run progress; \*\*carry permanent bonuses\*\* (e.g., +10% essence, deeper start).

\- \*\*Progression\*\*: unlock new biomes (Forest → Desert → Arctic), new waifu classes, deeper floors.



\## 6) Systems Overview (MVP status)

| System | Status | Responsibilities |

|---|---|---|

| \*\*GameManager\*\* | Full | Tracks essence/day/clicks; serialized refs to Essence/Upgrades/Stations; broadcasts events. |

| \*\*Essence \& Currency\*\* | Full | Manual clicks (≤10/day), dungeon rewards, passive/sec; spend/add APIs. |

| \*\*Upgrade System\*\* | Partial | SO-driven upgrades (unlock Dungeon, altar buffs, reward multiplier). |

| \*\*Dungeon / Combat\*\* | Partial | Single floor; 3D stub arena; turn-based card combat; rewards → essence. |

| \*\*Exploration/Harvesting\*\* | Stub | Single room interactables (Altar, Bed, Door). |

| \*\*Stations (Farm/Kitchen/Alchemy/Blacksmith)\*\* | Data Stubs | Interfaces + SOs + manager to track stations/companions. |

| \*\*Waifu Collection\*\* | Partial Stub | Placeholder waifu cards; first passive buff hook. |

| \*\*Inventory\*\* | Stub | Item stacks, slot limits, add/remove APIs, emits OnInventoryChanged. |

| \*\*Generational Legacy\*\* | UI Stub | “Fertilize” button + future reset/bonus flow. |

| \*\*Save/Load\*\* | Full | JSON/PlayerPrefs; re-applies one-shot upgrades on load. |



\## 7) Technical Architecture

\- \*\*Pattern\*\*: MVC-ish. \*\*Model\*\* = ScriptableObjects \& C# state; \*\*View\*\* = TMP UI; \*\*Controller\*\* = buttons, Battle logic; \*\*GameManager\*\* orchestrates.

\- \*\*Singleton\*\*: `GameManager` (DontDestroyOnLoad).
\- \*\*Core wiring\*\*: `GameManager` expects `EssenceManager`, `UpgradeManager`, `StationManager`, and `InventoryManager` references assigned in the scene/prefab; it no longer searches at runtime.

\- \*\*Stations\*\*: `StationManager` maintains `IStation`/`ICompanion` lists, exposed via `GameManager`.
  `UnlockStation(id)` and `RecruitCompanion(id)` update internal collections and fire
  `OnStationUnlocked` / `OnCompanionRecruited(ICompanion, IReadOnlyList<CardSO>, IReadOnlyList<UpgradeSO>)`
  events so battle and upgrade systems can claim a companion's starting deck and buffs.
  Each `CompanionSO` serializes these via `GetStartingCards()` and `GetPassiveBuffs()` accessors.

\- \*\*Event bus\*\*: `IEventBus` interface (default `DefaultEventBus` component) exposes cross-system events:
  - `DayChanged(int day)`
  - `DungeonKeysChanged(int current, int perDay)`
  - `SleepEligibilityChanged(bool canSleep, string reason)`
  - `StationUnlocked(IStation station)`
  - `CompanionRecruited(ICompanion companion)`
  - `UpgradePurchased(UpgradeSO upgrade)`
  - `MinigameCompleted(MinigameResult result)`
  Subscribe in `OnEnable` and unsubscribe in `OnDisable`.
\- \*\*Other events\*\*: `OnEssenceChanged`, `OnClicksLeftChanged`, `OnPurchased`, `OnInventoryChanged`, `OnBattleEnded`, `OnPlayerStatsChanged`, `OnEnemyStatsChanged` remain on their respective systems.

\- \*\*Persistence\*\*: `SaveSystem` now stores a dictionary of JSON sections keyed by system name inside `GameSaveData`. Any manager implementing `ISaveable` registers with `GameManager`, which exposes a read-only list for iteration. During save, each `ISaveable` contributes its own `ToData()` payload; on load, `SaveSystem` fetches the section and passes the deserialized object to `LoadFrom()`. This decoupled approach lets new systems plug into persistence without modifying central code. To add a new participant, implement `ISaveable`, provide a unique `SaveKey`, and call `GameManager.RegisterSaveable(this)` in `Awake`. Disk I/O remains wrapped in try/catch. Version 4 introduces the section-based format. **Test**: recruit a companion, buy an upgrade, save, then reload to ensure state persists.
\- **Test**: Call `Stations.UnlockStation("farm")` or `Stations.RecruitCompanion("alice")` in play mode and watch the console/UI react via the event bus (`StationUnlocked` or `CompanionRecruited`).

\- \*\*Scenes\*\*: `Start`, `Battle`.

\- \*\*Prefabs\*\*: `BattleRoot` (BattleManager + UI), `CardView`, `UpgradeButtonPrefab`.

### Architecture Diagram

```mermaid
flowchart LR
    GM[GameManager]
    EM[EssenceManager]
    UM[UpgradeManager]
    BM[BattleManager]
    HUD
    BUI[BattleUI]
    UP[UpgradesPanel]

    GM --> EM
    GM --> UM
    GM --> BM
    BM --> HUD
    BM --> BUI
    UM --> UP
    EM --> HUD
```

### Glossary

| Term | Definition |
|---|---|
| **GameManager** | Singleton orchestrator that mediates cross-system communication. Exposed via `IGameManager` so callers can depend on an interface. |
| **IGameManager** | Interface describing the GameManager's public API for injection. |
| **Essence** | Primary currency earned from clicks or battles, spent on upgrades. |
| **Dungeon Key** | Daily token consumed to attempt a dungeon run. |
| **Upgrade** | ScriptableObject-driven improvement purchased with essence. |
| **BattleManager** | Orchestrates turn flow by composing DeckManager, EnergyPool, StatusController and BattleRewardService. |
| **DeckManager** | Shuffles the starting deck and handles draw/discard/hand operations. |
| **EnergyPool** | Tracks current/max energy and spending. Raises `OnEnergyChanged`. |
| **StatusController** | Maintains Weak/Vulnerable timers and exposes formatted labels. |
| **BattleRewardService** | Calculates victory rewards and grants essence. |

### Recent Changes
- Added `IGameManager` interface. `GameManager` now implements it and callers receive `IGameManager` references instead of using the static singleton.
- Introduced `IEventBus` with `DefaultEventBus` implementation. GameManager, HUD, and BattleManager now receive an event bus reference instead of calling static `GameEvents`.
- To test: assign a `DefaultEventBus` component to `eventBusSource` fields on GameManager, HUD, and BattleManager. Run the game, unlock a station or recruit a companion, and ensure HUD and other listeners react via bus events.
- Added `MinigameResult`, updated `IMinigame` to `PlayAsync`, and introduced `MinigameLauncher` + new `MinigameCompleted` event.
- To test: create a scriptable object implementing `IMinigame`, then call `MinigameLauncher.LaunchAsync` and watch listeners receive the result via the event bus.
- Added `LocationSO` data and `MapUI` that spawns buttons for unlocked locations. Event bus now exposes `UpgradePurchased` for UI refreshes.
- To test: create LocationSO assets for Hub and Battle, assign them to MapUI, purchase the battle unlock upgrade, and verify the Battle button appears and loads the scene.

\## 8) Art \& Audio Direction

\- \*\*MVP\*\*: grey-box tavern basement \& dungeon; simple essence icon; placeholder waifu portraits/cards.

\- \*\*Future\*\*: AI-generated waifu art; stylized stations (NV3D packs); SFX for clicks/combat/mini-games.



\## 9) MVP Scope \& Roadmap

| Phase | Focus | Deliverable |

|---|---|---|

| 1 | Core setup \& manual essence | URP project + GameManager + HUD + click logic (≤10/day) + persistence |

| 2 | Upgrades \& dungeon unlock | Upgrade SOs + affordability UI + doorUnlocked gate |

| 3 | Dungeon + combat prototype | Single-floor 3D scene + turn-based card stub + essence reward |

| 4 | Day/cycle \& passive buff | Sleep resets + dayIndex; first waifu passive/sec |

| 5 | Waifu recruit stub \& HUD | Placeholder card spawn + passive rate display |

| 6 | Station \& legacy stubs | Buttons for Farm/Kitchen/Alchemy/Blacksmith + “Fertilize” UI |

| 7 | Vertical slice tie-in | Click → upgrade → dungeon → sleep → recruit → passive flow |

| 8 | Polish | Persistence audit, event reliability, UX tweaks |



\### Milestones (targets)

\- Day 2: manual clicks ≤10/day update HUD \& persist.

\- Day 4: buy upgrade → unlock dungeon door → enter battle scene.

\- Day 6: finish one dungeon floor → grant essence → return to Hub.

\- Day 8: sleep resets clicks; recruit stub; passive/sec displays.



\## 10) Long-Term Vision

\- \*\*Stations\*\*: Trees, automation, farm→kitchen→alchemy→blacksmith resource chains.

\- \*\*Deck-building\*\*: card drops, upgrades, party formation, rarities/tags.

\- \*\*Biomes \& Trade\*\*: chapters with unique managers; cross-kingdom routes.

\- \*\*Generational\*\*: heir selection, legacy system, narrative chapters.

\- \*\*Polish\*\*: particles, popups, audio, AI-assisted art pipeline.



---



\# IMPLEMENTATION (what exists today)



\## A. Public APIs (stable entry points)

\*\*EssenceManager\*\*

\- `bool TryClickHarvest()` • `bool TrySpend(int amount)` • `void AddExternal(int amount)`

\- Events: `OnEssenceChanged(int)`, `OnClicksLeftChanged(int)`



\*\*UpgradeManager : IUpgradeProvider\*\*

\- `bool TryPurchase(UpgradeSO)` • `bool IsPurchased(string id)`

\- `IReadOnlyCollection<string> PurchasedIds` • `IReadOnlyList<UpgradeSO> Available`

\- `float RewardMultiplier` (×1.0 base; multiplicative stack)

\- `event Action<UpgradeSO> OnPurchased`

\- `LoadPurchased(IEnumerable<string> ids)` → reapply one-shots → `RecalculateDerivedStats()`



**BattleManager**

- `void PlayCard(CardSO)`

- Events: `OnInfoChanged(string)`, `OnPlayerStatsChanged(int hp, int maxHp, int armor)`, `OnEnemyStatsChanged(int hp, int maxHp)`, `OnEnergyChanged(int current, int max)`, `OnHandChanged(IEnumerable<CardSO>)`, `OnPlayerStatusChanged(string)`, `OnEnemyStatusChanged(string)`, `OnBattleEnded(bool win, int reward)`

- Victory rewards delegated to `BattleRewardService`

**SceneLoader**

- `Task LoadSceneAsync(string sceneName)` • hooks: `FadeOutAsync()`, `FadeInAsync()`



> \\\\\\\*\\\\\\\*Rule\\\\\\\*\\\\\\\*: world-space interactions (3D Altar/Bed/Door) must call these APIs—no bypassing.



\## B. Data Contracts (SO)

\- \*\*BattleConfigSO\*\*: `playerMaxHP, enemyMaxHP, playerAttack, enemyLight/Heavy, enemyLeech, baseEssenceReward, returnDelay`

\- \*\*UpgradeSO\*\*: `id, title, cost, effect (IncreaseClick | IncreasePassive | UnlockBattle | BattleRewardBonus), value`

\- \*\*CardSO\*\*: `id, title, description, action (Attack | Guard | Mend), cost (reserved for energy later)`



\## C. Scene \& Prefab Wiring (required)

**Start**: Systems (GameManager, EssenceManager, UpgradeManager, SceneLoader), `HUD` (Gather/Sleep), `UpgradesPanel`, `DungeonGateButton` (needs `unlock_battle`, loads "Battle")

\*\*BattleRoot prefab\*\*: BattleManager + EnemyAI w/ `BattleConfig`; `BattleUI` with Player/Enemy/Info + HP bars; `HandPanel + CardHandUI` (`PopulateHand(IEnumerable<CardSO>)` + `RefreshAffordability(int)` spawns `CardView` from `CardSO`s)



\## D. Tuning (defaults)

\- Click cap \*\*10\*\* • Per click \*\*1\*\* • Passive/sec \*\*0\*\*

\- Base dungeon reward \*\*20\*\* • RewardMultiplier \*\*1.0\*\* (e.g., `+25%` → ×1.25)



---



\# WORKING AGREEMENTS

1\) After each milestone: run \*\*Tools → Generate Project Snapshot\*\*; update \*\*APIs\*\* above if changed; commit.

2\) Keep `Assets/TextMesh Pro/Resources/\\\\\\\*\\\\\\\*`; delete/ignore TMP \*\*Examples \& Extras\*\*.

3\) Third-party packs live under `Assets/ThirdParty/PackName/` (ignored if solo; use LFS if shared).

\## Recent Changes
- 2025-08-10: Added `SceneLoader` service and migrated `LoadSceneButton`/`DungeonGateButton` to use it.
  - How to test: assign `SceneLoader` in Start scene and click the gate to load `Battle`.
\- 2025-08-10: Added `UpgradeIds` static class to centralize upgrade ID strings.
  - How to test: project compiles; dungeon gate button and HUD use `UpgradeIds.UnlockBattle`.
\- 2025-08-10: Introduced `ItemSO`, `InventoryManager`, and inventory persistence. `MinigameResult` can now carry item rewards which `MinigameLauncher` deposits automatically.
  - How to test: create an `ItemSO`, assign it in a test mini-game returning `(true, 0, item, qty)`, run the mini-game and confirm the item appears in `InventoryManager` and remains after save/load.
- 2025-08-10: Added basic skill tree framework (`SkillSO`, `SkillTreeManager`) and `SkillUnlocked` event on `IEventBus`.
  - How to test: create a couple `SkillSO` assets, assign them to a `SkillTreeManager` hooked into `GameManager`. Call `Unlock` on a skill and verify the event fires and the skill ID persists in the save file.




---



\# LINKS

\- Snapshots: `docs/CONTEXT.md`, `docs/assets-files.txt`, `docs/assets-dirs.txt`

\- (Optional) Vision docs: `docs/vision/HighLevel.md`, `docs/vision/3DWrapper.md`, `docs/vision/Companions.md`, `docs/vision/Systems.md`

