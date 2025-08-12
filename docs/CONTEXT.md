# SliceOfLife-MVP — Context Snapshot
_Generated: 2025-08-10 21:04_

## Scenes
- Battle
- Start
- Title

## Prefabs
- Prefabs/Battle/BattleRoot.prefab
- Prefabs/Battle/CardView.prefab
- Prefabs/UI/UpgradeButtonPrefab.prefab

## ScriptableObjects
- ScriptableObjects/Battle/BattleConfig.asset  _(type: BattleConfigSO)_
- ScriptableObjects/Cards/Attack.asset  _(type: CardSO)_
- ScriptableObjects/Cards/Expose.asset  _(type: CardSO)_
- ScriptableObjects/Cards/Guard.asset  _(type: CardSO)_
- ScriptableObjects/Cards/Hamstring.asset  _(type: CardSO)_
- ScriptableObjects/Cards/Mend.asset  _(type: CardSO)_
- ScriptableObjects/Locations/Battle.asset  _(type: LocationSO)_
- ScriptableObjects/Locations/Hub.asset  _(type: LocationSO)_
- ScriptableObjects/Upgrades/BattleRewardBonus25.asset  _(type: UpgradeSO)_
- ScriptableObjects/Upgrades/Plus One Click.asset  _(type: UpgradeSO)_
- ScriptableObjects/Upgrades/Unlock Dungeon.asset  _(type: UpgradeSO)_

## Scripts
### Battle
- BattleConfigSO
- BattleManager
- BattleRewardService
- BattleUI
- CardHandUI
- CardSO
- CardView
- DeckManager
- EnemyAI
- EnergyPool
- StatusController
### Core
- CompanionSO
- DefaultEventBus
- DungeonProgression
- EssenceManager
- GameManager
- ICompanion
- IEventBus
- IGameManager
 - IInventoryService
- IMinigame
- InventoryManager
- IsExternalInit
- IStation
 - ItemCardSO
 - ItemSO
- LocationSO
- MinigameLauncher
- MinigameResult
- RecipeManager
- RecipeRewardStub
- RecipeSO
- ResourceManager
- ResourceSO
- SaveSystem
- SceneLoader
- SkillSO
- SkillTreeManager
- StationManager
- StationSO
### UI
- CurrencyHUDPanel
- DungeonGateButton
- HUD
- HUDPanel
- KeysHUDPanel
- LoadSceneButton
- LocationButtonView
- MapUI
- SleepHUDPanel
- UpgradeButtonView
- UpgradesPanel
### Upgrades
- UpgradeIds
- UpgradeManager
- UpgradeSO
