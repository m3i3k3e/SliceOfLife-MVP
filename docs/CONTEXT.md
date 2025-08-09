\# SliceOfLife-MVP — Context Snapshot (YYYY-MM-DD)



\## Engine / Render

\- Unity 6.2 (URP). TextMeshPro UI.



\## Scenes

\- Start: Systems GO with GameManager, EssenceManager, UpgradeManager; HUD, UpgradesPanel; EnterDungeon button (DungeonGateButton + LoadSceneButton → "Battle").

\- Battle: Uses `BattleRoot` prefab instance.



\## Prefabs

\- BattleRoot (base): 

&nbsp; - BattleSystem (BattleManager + EnemyAI) – Config: `BattleConfig`.

&nbsp; - Canvas (BattleUI) – Texts: Player/Enemy/Info; HP bars: PlayerHPBar, EnemyHPBar; optional PlayerArmorText.

&nbsp; - HandPanel + CardHandUI (spawns cards from CardSOs).

\- BattleRoot\_Boss (variant): (if exists) same as base but different `BattleConfig`.



\## ScriptableObjects

\- BattleConfigSO: player/enemy stats, baseEssenceReward=20, returnDelay=1s.

\- Upgrades (UpgradeSO):

&nbsp; - `plus\_one\_click` (+1 essence per click, 30)

&nbsp; - `unlock\_battle` (enables Enter Dungeon, 50)

&nbsp; - `battle\_reward\_bonus\_25` (+25% dungeon rewards, 75)

\- CardSO:

&nbsp; - Attack (action=Attack; UI says “Deal 6”)

&nbsp; - Guard  (action=Guard; UI says “Armor 4”)

&nbsp; - Mend   (action=Mend; UI says “Heal 5 (1 use)”)



\## Key Scripts

\- GameManager (singleton): Day advance, references Essence/Upgrades.

\- EssenceManager: TryClickHarvest, TrySpend, AddExternal, events for essence/clicks.

\- UpgradeManager (IUpgradeProvider):

&nbsp; - Tracks PurchasedIds, Available; OnPurchased event.

&nbsp; - One-shot effects: IncreaseClick, IncreasePassive, UnlockBattle (gate).

&nbsp; - Derived: RewardMultiplier (starts at 1.0, stacks multiplicatively via BattleRewardBonus).

&nbsp; - LoadPurchased(...) re-applies one-shots then RecalculateDerivedStats().

\- SaveSystem: simple persist stub (expansion planned).

\- HUD: Gather(), Sleep(); listens to essence events.

\- UpgradesPanel: spawns buttons from UpgradeSO.

\- DungeonGateButton: enables EnterDungeon when `unlock\_battle` purchased.

\- LoadSceneButton: loads scene by name (set to "Battle").

\- BattleManager: turn loop, events for UI; methods PlayerAttack/Guard/Mend; `PlayAction`/`PlayCard`; Victory() multiplies by `Upgrades.RewardMultiplier`.

\- EnemyAI: random intent (light/heavy/leech).

\- BattleUI: auto-wires BattleManager; updates texts + HP bars; hooks buttons.

\- CardView: renders a CardSO; calls `BattleManager.PlayCard`.

\- CardHandUI: spawns static starting hand (Attack/Guard/Mend).



\## Build Settings

\- Scenes added: Start (index 0), Battle (index 1+).



\## Next Up

\- (choose) Draw/Discard/Energy for cards, or Boss variant with tuned `BattleConfig`.



