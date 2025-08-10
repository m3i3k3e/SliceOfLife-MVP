using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
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
    [SerializeField] private string unlockUpgradeId = "unlock_battle";
    
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

    private void Start()
    {
        if (!essenceManager)
            essenceManager = UnityEngine.Object.FindFirstObjectByType<EssenceManager>(UnityEngine.FindObjectsInactive.Include);
        if (!upgradeManager)
            upgradeManager = UnityEngine.Object.FindFirstObjectByType<UpgradeManager>(UnityEngine.FindObjectsInactive.Include);
        if (upgradeManager != null)
            upgradeManager.OnPurchased += HandleUpgradePurchased;
        if (essenceManager != null)
            essenceManager.OnDailyClicksChanged += HandleDailyClicksChanged;

    ReevaluateSleepGate(); // initialize the Sleep button state
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

    public IEssenceProvider Essence => essenceManager;
    public IUpgradeProvider Upgrades => upgradeManager;

    // -------- Day progression --------
    public int Day { get; private set; } = 1;
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

    public event Action<int,int> OnDungeonKeysChanged; // (current, perDay)

    /// <summary>Has the player attempted the dungeon at least once today?</summary>
    public bool DungeonAttemptedToday { get; private set; }

    /// <summary>Click-cap reduction queued for the next day only.</summary>
    private int _tempNextDayClickDebuff;

    /// <summary>Exposes the queued debuff so the save system can persist it.</summary>
    public int TempNextDayClickDebuff => _tempNextDayClickDebuff;

    public event Action<bool, string> OnSleepEligibilityChanged; // (canSleep, reason)

    // -------- Public API used by UI / buttons --------

    public bool TrySleep()
    {
        if (!CanSleep)
        {
            ReevaluateSleepGate();
            return false;
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

public bool CanSleep
{
    get
    {
        if (essenceManager == null) return false;

        bool clicksUsed   = essenceManager.DailyClicksRemaining <= 0;
        bool unlocked     = IsDungeonUnlocked();
        bool keysUsed     = !unlocked || DungeonKeysRemaining <= 0;

        // NEW: before unlock, if you CAN afford it, you must buy it before sleeping
        bool mustBuyUnlock = !unlocked && CanAffordUnlock();

        return clicksUsed && keysUsed && !mustBuyUnlock;
    }
}


    public void ReevaluateSleepGate()
    {
        bool ok = CanSleep;
        string reason = string.Empty;

if (!ok && essenceManager != null)
    {
    if (essenceManager.DailyClicksRemaining > 0)
        reason = $"{essenceManager.DailyClicksRemaining} clicks remaining";
    else if (!IsDungeonUnlocked() && CanAffordUnlock())
        reason = "Buy 'Unlock Dungeon' to sleep";
    else if (IsDungeonUnlocked() && DungeonKeysRemaining > 0)
        reason = DungeonKeysRemaining == 1 ? "Use your dungeon key" : $"Use dungeon keys ({DungeonKeysRemaining})";
    }
        OnSleepEligibilityChanged?.Invoke(ok, reason);
    }

    /// <summary>
    /// Rehydrate manager state from a persisted snapshot.
    /// </summary>
    public void Load(GameSaveData data)
    {
        if (data == null) return; // Defensive: nothing to load

        // Restore simple fields
        Day                  = data.day;
        DungeonKeysRemaining = data.dungeonKeysRemaining;
        DungeonAttemptedToday = data.dungeonAttemptedToday;
        _tempNextDayClickDebuff = data.tempNextDayClickDebuff;

        // Notify listeners so UI and other systems sync to the loaded values
        OnDayChanged?.Invoke(Day);
        OnDungeonKeysChanged?.Invoke(DungeonKeysRemaining, dungeonKeysPerDay);

        // Recompute sleep eligibility after state changes
        ReevaluateSleepGate();
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
private void OnDestroy()
{
    if (essenceManager != null)
        essenceManager.OnDailyClicksChanged -= HandleDailyClicksChanged;
    if (upgradeManager != null)
        upgradeManager.OnPurchased -= HandleUpgradePurchased;
}

    private void OnApplicationQuit() => SaveSystem.Save(this);
}
