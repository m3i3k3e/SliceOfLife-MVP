using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
/// <summary>
/// Central orchestrator. Uses the Singleton pattern so other systems can easily locate
/// one persistent instance that survives scene loads.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -------- Singleton --------
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    // -------- Core systems --------
    [Header("Core Systems (assign in Inspector)")]
    [SerializeField] private EssenceManager essenceManager;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private StationManager stationManager; // manages stations/companions
    [SerializeField] private string unlockUpgradeId = "unlock_battle";
    
    /// <summary>How many dungeon keys the player receives each day once unlocked.</summary>
    public int DungeonKeysPerDay => dungeonKeysPerDay; // expose for HUD

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
    }

    private void HandleUpgradePurchased(UpgradeSO up)
{
    if (up != null && up.id == unlockUpgradeId)
    {
        // Give keys right away on the day you unlock
        DungeonKeysRemaining = Mathf.Max(0, dungeonKeysPerDay);
        OnDungeonKeysChanged?.Invoke(DungeonKeysRemaining, dungeonKeysPerDay);
        ReevaluateSleepGate();
        SaveSystem.Save(this);
    }
}

    private void HandleDailyClicksChanged(int _)
    {
        ReevaluateSleepGate();
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
    /// <summary>Fired after Day increments.</summary>
    public event Action<int> OnDayChanged;

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

    /// <summary>Invoked when dungeon keys change; args = (current, perDay).</summary>
    public event Action<int,int> OnDungeonKeysChanged; // (current, perDay)

    /// <summary>Has the player attempted the dungeon at least once today?</summary>
    public bool DungeonAttemptedToday { get; private set; }

    /// <summary>Click-cap reduction queued for the next day only.</summary>
    private int _tempNextDayClickDebuff;

    /// <summary>Notifies UI whether the player can sleep and why not.</summary>
    public event Action<bool, string> OnSleepEligibilityChanged; // (canSleep, reason)

    // -------- Public API used by UI / buttons --------

    /// <summary>Entry point from UI to advance to the next day if the gate allows it.</summary>
    public bool TrySleep()
    {
        if (!CanSleep)
        {
            ReevaluateSleepGate();
            return false; // Gate denied; reason broadcast via event
        }
        AdvanceDay();
        return true;
    }

    /// <summary>Consume one dungeon key if available. Returns true on success.</summary>
    public bool TryConsumeDungeonKey()
    {
        if (!IsDungeonUnlocked()) return true; // before unlock, don't block
        if (DungeonKeysRemaining <= 0) return false;

        DungeonKeysRemaining = Mathf.Max(0, DungeonKeysRemaining - 1);
        OnDungeonKeysChanged?.Invoke(DungeonKeysRemaining, dungeonKeysPerDay);
        ReevaluateSleepGate();
        SaveSystem.Save(this);
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
    public void ApplyDungeonLossPenalty()
    {
        if (Essence != null)
        {
            int take = Mathf.Min(lossPenaltyEssence, Essence.CurrentEssence);
            if (take > 0) Essence.TrySpend(take);
        }

        int baseCap = essenceManager ? essenceManager.DailyClickCap : 10;
        _tempNextDayClickDebuff = Mathf.Clamp(_tempNextDayClickDebuff + nextDayClickDebuffOnLoss, 0, baseCap);

        SaveSystem.Save(this);
    }

    // -------- Sleep gate --------

    private bool IsDungeonUnlocked()
    {
        var up = Upgrades;
        return up != null && up.IsPurchased("unlock_battle");
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
        OnSleepEligibilityChanged?.Invoke(ok, reason);
    }

    // -------- Day change internals --------

    private void AdvanceDay() // keep this private so everyone uses TrySleep()
    {
        Day++;

        // Today's click cap after any queued debuff
        int capToday = EssenceManagerDailyCapMinusDebuff();
        _tempNextDayClickDebuff = 0;

        if (essenceManager != null)
            essenceManager.ResetDailyClicks(capToday);

        // Reset keys: before unlock → 0; after unlock → dungeonKeysPerDay
        DungeonKeysRemaining = IsDungeonUnlocked() ? Mathf.Max(0, dungeonKeysPerDay) : 0;
        OnDungeonKeysChanged?.Invoke(DungeonKeysRemaining, dungeonKeysPerDay);

        // New day → must attempt again if you want (for flavor), but Sleep gate uses keys now.
        DungeonAttemptedToday = false;

        OnDayChanged?.Invoke(Day);
        SaveSystem.Save(this);
        ReevaluateSleepGate();
    }

    private int EssenceManagerDailyCapMinusDebuff()
    {
        int baseCap = essenceManager ? essenceManager.DailyClickCap : 10;
        return Mathf.Max(0, baseCap - _tempNextDayClickDebuff);
    }

    // ---- Save/Load integration ----

    /// <summary>
    /// Package the tiny bit of GameManager state that needs to persist.
    /// </summary>
    public GameSaveData.GameData ToData()
    {
        return new GameSaveData.GameData { day = Day };
    }

    /// <summary>
    /// Restore GameManager values from serialized data.
    /// </summary>
    public void LoadFrom(GameSaveData.GameData data)
    {
        if (data == null) return;
        Day = Mathf.Max(1, data.day); // clamp to sensible minimum
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
    }

    private void OnApplicationQuit() => SaveSystem.Save(this);
}
