\# Slice of Life — Implementation Plan (3D-first Basement Tutorial)



\_Last updated: 2025-08-11\_
\## Status Snapshot

\- SaveModel v2 implemented with unified JSON save.
\- Event bus (`IEventBus`/`DefaultEventBus`) available; some legacy `GameEvents` hooks remain.
\- Inventory seeds in place via starter `ItemSO` assets.
\- Task graph (`TaskGraphSO` + `TaskService`) driving tutorial flow.

\### Deviations
\- `TaskService` listens to `GameEvents` instead of exposing `NotifyItemChanged`; inventory changes are observed via events.
\- Legacy `GameEvents` still mirrors inventory and task events alongside the event bus.
\- `SaveSystem.Save` and `Load` now accept an optional `TaskService` parameter to persist tutorial state.




\## Purpose

We’re pivoting from a 2D UI prototype to a 3D-first, card-driven experience with a guided “Basement Camp” tutorial loop. This document defines the \*\*systems\*\*, \*\*APIs\*\*, \*\*save model\*\*, and \*\*scene wiring\*\* so engineers and assistants (Codex) can implement in small, safe PRs.



\## Target Flow (MVP)

Title → Basement (static 3D scene with interactables) → Dungeon (card battle) → return to Basement.



Baseline tutorial tasks in Basement:

1\) Gather Wood (collect few pieces)

2\) Clear Rubble

3\) Polish Altar (progress over several interacts)

4\) Build Bed (consumes wood)

5\) Cook Simple Meal (rat meat + mushroom)

6\) Brew First Potion (bottle + weeds)

7\) Unlock Dungeon Door (upgrade or interact)

8\) Attempt Dungeon (one short battle; return)



\## Project Structure (folders)

Assets/

Scripts/

Core/ (Bootstrap, GameManager glue, EventBus, SaveSystem v2)

Inventory/ (ItemSO, InventoryManager)

Interaction/ (IInteractable, Interactable, InteractionController, helpers)

Tasks/ (TaskService, TaskSO, TaskGraphSO, Task conditions)

Cards/ (existing CardSO + minor extensions)

UI/ (TitleController, WorldHUD, PauseMenu)

ScriptableObjects/

Items/

Tasks/

Config/

Prefabs/

System/ (Bootstrapper)

UI/

World/

Scenes/

Title.unity

Basement.unity

Dungeon.unity





\## Systems (scope \& APIs)



\### A) Bootstrap / Service Wiring

\- \*\*Prefab\*\*: `Prefabs/System/Bootstrapper` with `GameBootstrap` (DontDestroyOnLoad).

\- Ensures singletons/services exist (GameManager, InventoryManager, TaskService), sets up EventBus, and loads save.

\- Must tolerate entering any scene directly (editor workflow).



\### B) Save System v2 (versioned)

\- \*\*DTO\*\*: `SaveModelV2`

&nbsp; - `int version = 2`

&nbsp; - Meta: `string lastScene`, `string spawnPointId`

&nbsp; - Day/Rules: `int day`, `int dungeonKeysRemaining`, `int dungeonKeysPerDay`, `int tempNextDayClickDebuff`

&nbsp; - Currency: `int essence`, `int dailyClicksRemaining`, `int essencePerClick`, `float passivePerSecond`

&nbsp; - Upgrades: `List<string> purchasedUpgradeIds`

&nbsp; - Inventory: `List<ItemStackDTO { string itemId; int qty; }>`

&nbsp; - Tasks: `List<TaskStateDTO { string taskId; bool completed; int progress; }>`

&nbsp; - Flags: `float altarPolishProgress`, `bool familiarAwakened`, `bool dungeonUnlocked`

\- `SaveSystem.Save(GameManager gm)`: gather from services → write JSON.

\- `SaveSystem.Load(GameManager gm)`: load JSON, migrate v1→v2 if needed, call services’ `ApplyLoadedState(...)`.

\- `SaveSystem.HasAnySave()` and `SaveSystem.Delete()` helpers.



\### C) Event Bus (decoupling)

Static `GameEvents` with typed events (legacy shim; `IEventBus` available for decoupling):

\- `OnEssenceChanged(int)`, `OnDungeonKeysChanged(int current, int perDay)`, `OnSleepEligibilityChanged(bool ok, string reason)`

\- `OnItemAdded(ItemSO item, int qty)`

\- `OnTaskAdvanced(string taskId)` / `OnTaskCompleted(string taskId)`

(UI listens; managers raise.)



\### D) Inventory

\- \*\*SO\*\*: `ItemSO { string id; string title; Sprite icon; int stackLimit = 999 }`

\- \*\*Service\*\*: `InventoryManager : IInventoryService`

&nbsp; - `bool Add(ItemSO item, int qty)`, `bool Remove(ItemSO item, int qty)`

&nbsp; - `int GetQuantity(ItemSO item)`, `IReadOnlyList<ItemStack> AllStacks`

&nbsp; - `void ApplyLoadedState(List<ItemStackDTO>)`

\- Seed items (ScriptableObjects): `wood`, `rubble`, `rat\_meat`, `wild\_mushroom`, `glass\_bottle`, `weeds`, `altar\_dust`.



\### E) Interaction (3D clickables)

\- \*\*Interface\*\*: `IInteractable { string InteractId; string Prompt; void Use(GameObject instigator); }`

\- \*\*Component\*\*: `Interactable` (requires collider; has `interactId`, `prompt`, `singleUse`, UnityEvent `OnInteract`)

\- \*\*Controller\*\*: `InteractionController` on camera; cursor raycasts “Interactable” layer; shows prompt “\[LMB] {Prompt}”; Left click calls `Use`.

\- Helper components:

&nbsp; - `ItemGrantOnInteract` (list of item+qty to grant)

&nbsp; - `ConsumeItemsOnInteract` (list of item+qty required; blocks if missing)

&nbsp; - Custom scripts: `AltarPolish` (increments progress 0..1), `RubbleClear`, `BedBuild`, `DoorEnterDungeon`, `BookSave`.



\### F) Tasks (guided tutorial)

\- \*\*SO\*\*: `TaskSO` (`id`, `title`, `description`, array of `TaskCondition` which AND together)

\- \*\*SO\*\*: `TaskGraphSO` (ordered list of `TaskSO` representing the tutorial path)

\- \*\*Conditions\*\* (initial):

&nbsp; - `CollectItemCondition(itemId, requiredQty)`

&nbsp; - `InteractCondition(interactId)` (fires from InteractionController)

&nbsp; - `UpgradePurchasedCondition(upgradeId)`

\- \*\*Service\*\*: `TaskService`

&nbsp; - `void Init(TaskGraphSO, IEnumerable<TaskStateDTO> loaded)`

&nbsp; - `bool IsComplete(string taskId)`

&nbsp; - `void NotifyInteraction(string interactId)` (inventory changes arrive via `GameEvents`)

&nbsp; - `List<TaskStateDTO> CaptureState()`

\- Raises `GameEvents.OnTaskAdvanced/OnTaskCompleted`.



\### G) UI (stubs)

\- \*\*TitleController\*\*: New Game (delete save → load Basement), Continue (if `HasAnySave()`), Load (stub), Settings (stub).

\- \*\*WorldHUD\*\*: essence/keys/day/current task label; subscribes to GameEvents.

\- \*\*PauseMenu\*\*: ESC toggles; Resume / Settings (stub) / Save \& Quit (save then load Title).



\### H) Integrate Existing Managers

\- \*\*GameManager\*\*: keep singleton, day/keys gate, `TrySleep()` behavior. Add `ApplyLoadedState(SaveModelV2)`.

\- \*\*EssenceManager\*\*: add safe setters used by load (`SetCurrentEssence`, `SetDailyClicks`, `SetEssencePerClick`, `SetPassivePerSecond`); mirror events to `GameEvents`.

\- \*\*UpgradeManager\*\*: `ApplyLoadedState(List<string>)`.

\- \*\*Battle\*\*: unchanged; on victory/defeat, reward/penalize as before and `SceneManager.LoadScene("Basement")`.



\## Scenes (wiring guide)



\### Title.unity

\- Root: `Bootstrapper` prefab.

\- Canvas: Title menu with buttons wired to `TitleController`.

\- `Continue` only interactable if `SaveSystem.HasAnySave()`.



\### Basement.unity (greybox)

\- Root: `Bootstrapper`.

\- Camera + `InteractionController` (layer mask = Interactable).

\- Interactables:

&nbsp; - `Altar (interactId: altar\_polish)` + `AltarPolish`

&nbsp; - `Rubble (rubble\_clear)` + `ItemGrantOnInteract(rubble x3)`

&nbsp; - `Firepit` (optional: consumes wood)

&nbsp; - `Bed (bed\_build)` + `ConsumeItemsOnInteract(wood x3)`

&nbsp; - `Book (book\_save)` + `BookSave` (calls `SaveSystem.Save`); also can open Pause.

&nbsp; - `DungeonDoor (dungeon\_enter)` + `DoorEnterDungeon` (checks unlock + key + scene load)

\- HUD: WorldHUD top-left.



\### Dungeon.unity

\- Keep existing battle prefab/flow; return to `Basement` after result.



\## Acceptance Criteria Summary

\- Entering any scene ensures singletons/services exist without duplicates.

\- New Game creates clean state; Continue resumes v2 save; v1 saves migrate.

\- Interactables are clickable with visible prompts; tasks advance and UI reflects progress.

\- Inventory adds/removes items and persists; tasks persist; day/keys/currency persist.

\- Battle returns to Basement and respects keys/penalties.



\## Coding Standards

\- Verbose XML docs on public APIs; short “why” comments on non-obvious blocks.

\- No reflection in save/load; use explicit setters.

\- Keep PRs small (≤ ~400 LOC diff) with a “Manual Test Plan” section.





