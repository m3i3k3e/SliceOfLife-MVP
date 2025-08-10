using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-100)]
/// <summary>
/// Central orchestrator. Uses the Singleton pattern so other systems can easily locate
/// one persistent instance that survives scene loads.
/// </summary>
public class GameManager : MonoBehaviour, IGameManager
{
    // -------- Singleton --------
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Register core systems for saving. Doing this in Awake ensures the list
        // is ready before any save/load operations occur.
        RegisterSaveable(this); // GameManager persists its own day counter
        if (essenceManager != null) RegisterSaveable(essenceManager);
        if (upgradeManager != null) RegisterSaveable(upgradeManager);
        if (stationManager != null) RegisterSaveable(stationManager);
        if (inventoryManager != null) RegisterSaveable(inventoryManager);
        if (skillTreeManager != null) RegisterSaveable(skillTreeManager);
    }

    // -------- Core systems --------
    [Header("Core Systems (assign in Inspector)")]
    [SerializeField] private EssenceManager essenceManager;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private StationManager stationManager; // manages stations/companions
    [SerializeField] private InventoryManager inventoryManager; // holds items
    [SerializeField] private SkillTreeManager skillTreeManager; // governs skill unlocks
    [SerializeField] private string unlockUpgradeId = UpgradeIds.UnlockBattle; // default to constant to avoid typos

    // ----- Saveable registration -----
    // Maintain a list of participating systems so SaveSystem can iterate them generically.
    private readonly List<ISaveable> _saveables = new();

    /// <summary>Expose registered saveables to <see cref="SaveSystem"/>.</summary>
    public IReadOnlyList<ISaveable> Saveables => _saveables;

    /// <summary>Allow subsystems to enlist for persistence.</summary>
    public void RegisterSaveable(ISaveable saveable)
    {
        if (saveable != null && !_saveables.Contains(saveable))
            _saveables.Add(saveable);
    }

    /// <summary>Remove a saveable when it is destroyed/disabled.</summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
        if (saveable != null)
            _saveables.Remove(saveable);
    }

    [Header("Events")]
    [Tooltip("Reference to an event bus implementing IEventBus.")]
    [SerializeField] private MonoBehaviour eventBusSource;

    // Helper to cast the serialized reference to the interface. Keeps consumers
    // ignorant of the concrete implementation while still allowing Inspector wiring.
    public IEventBus Events => eventBusSource as IEventBus;

    /// <summary>Read-only inventory access for other systems.</summary>
    public IInventory Inventory => inventoryManager;
    
    /// <summary>How many dungeon keys the player receives each day once unlocked.</summary>
    public int DungeonKeysPerDay => dungeonKeysPerDay; // expose for HUD

    /// <summary>Access to the skill tree for UI and systems.</summary>
    public SkillTreeManager Skills => skillTreeManager;

    private UpgradeSO FindUpgradeById(string id)
    {
    var up = Upgrades;
    if (up == null) return null;
    var list = up.Available;
    if (list == null) return null;
    for (int i = 0; i < list.Count; i++)
        if (list[i] != null && list[i].id == id) return list[i];
    return null;
    }

    private bool CanAffordUnlock()
    {
        var so = FindUpgradeById(unlockUpgradeId);
        if (so == null || Essence == null) return false;
        return !Upgrades.IsPurchased(unlockUpgradeId) && Essence.CurrentEssence >= so.cost;
    }

    /// <summary>
    /// Hook up to manager events once the object becomes active.
    /// Assumes references are assigned via the Inspector, avoiding costly lookups.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to upgrade purchase notifications so we can grant dungeon keys
        // and update the Sleep gate when the dungeon unlocks.
        if (upgradeManager != null)
            upgradeManager.OnPurchased += HandleUpgradePurchased;

        // Watch daily click changes to reevaluate Sleep eligibility each time.
        if (essenceManager != null)
            essenceManager.OnDailyClicksChanged += HandleDailyClicksChanged;

        // Initialize UI state for the current day immediately.
        ReevaluateSleepGate();

        // Bridge station/companion events onto the global bus so UI can react
        // without referencing StationManager directly.
        if (stationManager != null)
        {
            stationManager.OnStationUnlocked += HandleStationUnlocked;
            stationManager.OnCompanionRecruited += HandleCompanionRecruited;
        }

        // Persist inventory changes automatically whenever items shift.
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += HandleInventoryChanged;

        // Bubble skill unlocks through the global event bus.
        if (skillTreeManager != null)
            skillTreeManager.OnSkillUnlocked += HandleSkillUnlocked;
    }

    private async void HandleUpgradePurchased(UpgradeSO up)
    {
        // Always bridge the purchase onto the global event bus so UI systems can react.
        Events?.RaiseUpgradePurchased(up);

        if (up != null && up.id == unlockUpgradeId)
        {
            // Give keys right away on the day you unlock
            DungeonKeysRemaining = Mathf.Max(0, dungeonKeysPerDay);
            // Broadcast via the injected event bus so UI stays decoupled from GameManager.
            Events?.RaiseDungeonKeysChanged(DungeonKeysRemaining, dungeonKeysPerDay);
            ReevaluateSleepGate();
            // Persist the newly unlocked state without blocking callers.
            await SaveSystem.SaveAsync(this);
        }
    }

    private void HandleDailyClicksChanged(int _)
    {
        ReevaluateSleepGate();
    }

    /// <summary>
    /// Forward station unlocks to the static event hub.
    /// </summary>
    private void HandleStationUnlocked(IStation station)
    {
        // Forward the event onto the bus so listeners don't require a StationManager reference.
        Events?.RaiseStationUnlocked(station);
    }

    /// <summary>
    /// Forward companion recruitment to the static event hub.
    /// </summary>
    private void HandleCompanionRecruited(ICompanion companion, IReadOnlyList<CardSO> cards, IReadOnlyList<UpgradeSO> buffs)
    {
        // Broadcast companion recruitment through the bus for UI or analytics.
        // Battle/upgrade systems subscribe directly to StationManager for loadout data.
        Events?.RaiseCompanionRecruited(companion);
    }

    /// <summary>
    /// Persist inventory whenever items are added or removed.
    /// </summary>
    private async void HandleInventoryChanged()
    {
        await SaveSystem.SaveAsync(this);
    }

    /// <summary>
    /// Persist and broadcast newly unlocked skills.
    /// </summary>
    private async void HandleSkillUnlocked(SkillSO skill)
    {
        Events?.RaiseSkillUnlocked(skill);
        await SaveSystem.SaveAsync(this);
    }

    /// <summary>Read-only access to the currency system via its interface.</summary>
    public IEssenceProvider Essence => essenceManager;
    /// <summary>Read-only access to upgrades; decoupled through an interface.</summary>
    public IUpgradeProvider Upgrades => upgradeManager;

    /// <summary>Access to station and companion collections.</summary>
    public StationManager Stations => stationManager;

    // -------- Day progression --------
    /// <summary>Current in-game day (starts at 1).</summary>
    public int Day { get; private set; } = 1;

    // -------- Daily rules / penalties --------
    [Header("Daily Rules")]
    [Tooltip("Flat essence removed immediately upon dungeon defeat.")]
    [SerializeField] private int lossPenaltyEssence = 5;

    [Tooltip("Click-cap reduction applied to TOMORROW after a defeat (one day only).")]
    [SerializeField] private int nextDayClickDebuffOnLoss = 2;

    [Tooltip("How many dungeon keys the player receives each day once the dungeon is unlocked.")]
    [SerializeField] private int dungeonKeysPerDay = 1;

    /// <summary>Keys remaining TODAY. Reset each day to dungeonKeysPerDay (when unlocked).</summary>
    public int DungeonKeysRemaining { get; private set; }


    /// <summary>Has the player attempted the dungeon at least once today?</summary>
    public bool DungeonAttemptedToday { get; private set; }

    /// <summary>Click-cap reduction queued for the next day only.</summary>
    private int _tempNextDayClickDebuff;


    // -------- Public API used by UI / buttons --------

    /// <summary>Entry point from UI to advance to the next day if the gate allows it.</summary>
    public bool TrySleep()
    {
        if (!CanSleep)
        {
            ReevaluateSleepGate();
            return false; // Gate denied; reason broadcast via event
        }
        // Block until day advancement (and save) completes to keep API synchronous.
        AdvanceDay().GetAwaiter().GetResult();
        return true;
    }

    /// <summary>Consume one dungeon key if available. Returns true on success.</summary>
    public bool TryConsumeDungeonKey() => TryConsumeDungeonKeyAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Async version so callers can await the save operation if desired.
    /// </summary>
    public async Task<bool> TryConsumeDungeonKeyAsync()
    {
        if (!IsDungeonUnlocked()) return true; // before unlock, don't block
        if (DungeonKeysRemaining <= 0) return false;

        DungeonKeysRemaining = Mathf.Max(0, DungeonKeysRemaining - 1);
        // Let listeners know key counts changed (UI, save system, etc.).
        Events?.RaiseDungeonKeysChanged(DungeonKeysRemaining, dungeonKeysPerDay);
        ReevaluateSleepGate();
        await SaveSystem.SaveAsync(this);
        return true;
    }

    /// <summary>Mark that the player attempted a run today (for analytics/UI flavor).</summary>
    public void MarkDungeonAttempted()
    {
        if (!DungeonAttemptedToday)
        {
            DungeonAttemptedToday = true;
            ReevaluateSleepGate();
        }
    }

    /// <summary>
    /// Applies defeat repercussions: immediate essence loss and a temporary click-cap debuff
    /// for the following day. Encapsulated here so battle code stays unaware of economy rules.
    /// </summary>
    public void ApplyDungeonLossPenalty() => ApplyDungeonLossPenaltyAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Async variant of the loss penalty so callers may await persistence.
    /// </summary>
    public async Task ApplyDungeonLossPenaltyAsync()
    {
        if (Essence != null)
        {
            int take = Mathf.Min(lossPenaltyEssence, Essence.CurrentEssence);
            if (take > 0) Essence.TrySpend(take);
        }

        int baseCap = essenceManager ? essenceManager.DailyClickCap : 10;
        _tempNextDayClickDebuff = Mathf.Clamp(_tempNextDayClickDebuff + nextDayClickDebuffOnLoss, 0, baseCap);

        await SaveSystem.SaveAsync(this);
    }

    // -------- Sleep gate --------

    private bool IsDungeonUnlocked()
    {
        var up = Upgrades;
        // Check the upgrade using the centralized ID constant instead of a magic string.
        return up != null && up.IsPurchased(unlockUpgradeId);
    }

    /// <summary>
    /// Computed property backing the Sleep gate. Encapsulates the rules so callers don't
    /// duplicate logic. Returns true only when all daily tasks are complete.
    /// </summary>
    public bool CanSleep
    {
        get
        {
            if (essenceManager == null) return false;

            bool clicksUsed   = essenceManager.DailyClicksRemaining <= 0; // spent all manual clicks
            bool unlocked     = IsDungeonUnlocked();                       // dungeon available yet?
            bool keysUsed     = !unlocked || DungeonKeysRemaining <= 0;    // no keys left today

            // Before unlocking the dungeon, player must purchase it if affordable
            bool mustBuyUnlock = !unlocked && CanAffordUnlock();

            return clicksUsed && keysUsed && !mustBuyUnlock;
        }
    }


    /// <summary>
    /// Recomputes whether Sleep is allowed and tells listeners why it might be blocked.
    /// Keeps UI logic centralized instead of polling state in Update.
    /// </summary>
    public void ReevaluateSleepGate()
    {
        bool ok = CanSleep;
        string reason = string.Empty;

        if (!ok && essenceManager != null)
        {
            if (essenceManager.DailyClicksRemaining > 0)
                reason = $"{essenceManager.DailyClicksRemaining} clicks remaining";
            else if (!IsDungeonUnlocked() && CanAffordUnlock())
                reason = "Buy 'Unlock Dungeon' to sleep"; // gently push unlock
            else if (IsDungeonUnlocked() && DungeonKeysRemaining > 0)
                reason = DungeonKeysRemaining == 1 ? "Use your dungeon key" : $"Use dungeon keys ({DungeonKeysRemaining})";
        }
        // Tell UI whether Sleep is allowed and why not.
        Events?.RaiseSleepEligibilityChanged(ok, reason);
    }

    // -------- Day change internals --------

    private async Task AdvanceDay() // keep this private so everyone uses TrySleep()
    {
        Day++;

        // Today's click cap after any queued debuff
        int capToday = EssenceManagerDailyCapMinusDebuff();
        _tempNextDayClickDebuff = 0;

        if (essenceManager != null)
            essenceManager.ResetDailyClicks(capToday);

        // Reset keys: before unlock → 0; after unlock → dungeonKeysPerDay
        DungeonKeysRemaining = IsDungeonUnlocked() ? Mathf.Max(0, dungeonKeysPerDay) : 0;
        // Reset keys for the new day and notify any listeners.
        Events?.RaiseDungeonKeysChanged(DungeonKeysRemaining, dungeonKeysPerDay);

        // New day → must attempt again if you want (for flavor), but Sleep gate uses keys now.
        DungeonAttemptedToday = false;

        // Inform listeners of the new day index.
        Events?.RaiseDayChanged(Day);
        await SaveSystem.SaveAsync(this);
        ReevaluateSleepGate();
    }

    private int EssenceManagerDailyCapMinusDebuff()
    {
        int baseCap = essenceManager ? essenceManager.DailyClickCap : 10;
        return Mathf.Max(0, baseCap - _tempNextDayClickDebuff);
    }

    // ---- Save/Load integration ----

    // ---- ISaveable implementation ----

    /// <summary>Key used in the save file dictionary.</summary>
    public string SaveKey => "Game";

    /// <summary>
    /// Package the tiny bit of GameManager state that needs to persist.
    /// </summary>
    public object ToData() => new GameData { day = Day };

    /// <summary>
    /// Restore GameManager values from serialized data.
    /// </summary>
    public void LoadFrom(object data)
    {
        var d = data as GameData;
        if (d == null) return;
        Day = Mathf.Max(1, d.day); // clamp to sensible minimum
    }

    /// <summary>
    /// Plain serializable container for GameManager's tiny bit of state.
    /// Kept nested to emphasize its exclusive use by this manager.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int day;
    }

    /// <summary>
    /// Detach event listeners when the object is disabled to prevent leaks.
    /// </summary>
    private void OnDisable()
    {
        if (essenceManager != null)
            essenceManager.OnDailyClicksChanged -= HandleDailyClicksChanged;

        if (upgradeManager != null)
            upgradeManager.OnPurchased -= HandleUpgradePurchased;

        if (stationManager != null)
        {
            stationManager.OnStationUnlocked -= HandleStationUnlocked;
            stationManager.OnCompanionRecruited -= HandleCompanionRecruited;
        }

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= HandleInventoryChanged;

        if (skillTreeManager != null)
            skillTreeManager.OnSkillUnlocked -= HandleSkillUnlocked;
    }

    private void OnApplicationQuit() => SaveSystem.SaveAsync(this).GetAwaiter().GetResult();
}
