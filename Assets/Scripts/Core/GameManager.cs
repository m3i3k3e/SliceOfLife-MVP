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

    /// <summary>
    /// Optional runtime injection for tests or bootstrap scripts.
    /// Assigns dependencies and wires up events just like inspector references.
    /// </summary>
    /// <param name="essence">Essence system to bind.</param>
    /// <param name="upgrades">Upgrade system to bind.</param>
    public void Initialize(EssenceManager essence, UpgradeManager upgrades)
    {
        // Avoid duplicate event subscriptions if Initialize is called more than once.
        UnsubscribeFromManagers();

        essenceManager = essence;
        upgradeManager = upgrades;

        SubscribeToManagers();
        ReevaluateSleepGate(); // keep UI/state in sync with new deps
    }

    private void OnEnable()
    {
        // Managers are expected to be assigned via Inspector or Initialize().
        SubscribeToManagers();

        // Ensure the Sleep button reflects current state immediately on scene load.
        ReevaluateSleepGate();
    }

    private void OnDisable()
    {
        // Clean up event subscriptions to avoid leaks when disabled or destroyed.
        UnsubscribeFromManagers();
    }

    /// <summary>
    /// Subscribe to events on the injected managers.
    /// Split into a helper to reuse for both OnEnable and Initialize.
    /// </summary>
    private void SubscribeToManagers()
    {
        if (upgradeManager != null)
            upgradeManager.OnPurchased += HandleUpgradePurchased;
        if (essenceManager != null)
            essenceManager.OnDailyClicksChanged += HandleDailyClicksChanged;
    }

    /// <summary>
    /// Undo subscriptions created in <see cref="SubscribeToManagers"/>.
    /// </summary>
    private void UnsubscribeFromManagers()
    {
        if (upgradeManager != null)
            upgradeManager.OnPurchased -= HandleUpgradePurchased;
        if (essenceManager != null)
            essenceManager.OnDailyClicksChanged -= HandleDailyClicksChanged;
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

    private void OnApplicationQuit() => SaveSystem.Save(this);
}
