# Design Flow — Boot → Interaction → Task → Save

This document outlines the runtime pipeline from launching the game to persisting progress. It links the major scripts and scene wiring steps so contributors can see where systems hand off control.

## Boot Stage
- **GameBootstrap** (`Assets/Scripts/Core/GameBootstrap.cs`) lives on the `Prefabs/System/Bootstrapper` prefab. Place this prefab at the root of every scene (Title, Basement, Dungeon) so core services spawn exactly once.
- On `Awake`, the bootstrapper `Ensure`s singletons for `GameManager`, `InventoryManager`, `TaskService`, and `DefaultEventBus`.
- On `Start`, it wires the `TaskService` into `GameManager` and calls `SaveSystem.Load` to restore state before gameplay begins.

### Scene wiring
1. Drag `Prefabs/System/Bootstrapper` into the scene hierarchy.
2. In the prefab, assign the `GameManager`, `InventoryManager`, `TaskService`, and `DefaultEventBus` prefabs if they are not already set.

## Interaction Stage
- **InteractionController** (`Assets/Scripts/Interaction/InteractionController.cs`) sits on the player camera and raycasts each frame for `IInteractable` targets.
- Interactables use `Interactable` (`Assets/Scripts/Interaction/Interactable.cs`) and may include helpers like `ItemGrantOnInteract` or `ConsumeItemsOnInteract`.
- Successful interactions should forward their `interactId` to `TaskService.NotifyInteraction` so tasks can progress.

### Scene wiring
1. Attach `InteractionController` to the active camera.
2. Assign:
   - **Source Camera** (usually the same camera).
   - **Input Reader** implementing `IInputReader`.
   - **Prompt UI** (`InteractionPromptUI` on a Canvas).
3. Give each interactable a collider and unique `Interactable` component.

## Task Stage
- **TaskService** (`Assets/Scripts/Tasks/TaskService.cs`) tracks the ordered tutorial via a `TaskGraphSO`.
- It subscribes to inventory and upgrade events through `GameManager.Events` and reacts when `NotifyInteraction` is called.
- `GameManager.InjectTaskService` registers the service for saves.

### Scene wiring
1. Ensure `TaskService` exists (spawned by bootstrapper) and its **Graph** and **Inventory** fields are assigned.
2. For world objects, route interaction callbacks to `TaskService.NotifyInteraction`.

## Save Stage
- **SaveSystem** (`Assets/Scripts/Core/SaveSystem.cs`) writes and loads `SaveModelV2` JSON.
- **SaveScheduler** (`Assets/Scripts/Core/SaveScheduler.cs`) batches save requests; call `SaveScheduler.RequestSave(GameManager.Instance)` after meaningful changes.
- World props like the book in the Basement can call `SaveSystem.Save` directly (via a `BookSave` script) to force immediate persistence.

### Scene wiring
1. Interactions that should save (e.g., a book) need a component invoking `SaveScheduler.RequestSave` or `SaveSystem.Save`.
2. The `PauseMenu` uses `SaveScheduler` when the player chooses "Save & Quit." 

## Pipeline Summary
1. **Boot:** `GameBootstrap` instantiates services and loads the last save.
2. **Interaction:** `InteractionController` detects `Interactable` objects; interaction scripts dispatch IDs and side effects.
3. **Task:** `TaskService` evaluates conditions based on inventory, upgrades, and interactions.
4. **Save:** `SaveScheduler`/`SaveSystem` persist aggregated state, ready for the next boot.
