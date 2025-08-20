/*
 * GameManager.cs
 * Role: Central orchestrator for high-level game state and persistence.
 * Expansion: Forward new global events via IEventBus.
 */
using System;
using System.Collections.Generic; // maintains save participant list
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
/// <summary>
/// Central orchestrator. Uses the Singleton pattern so other systems can easily locate
/// one persistent instance that survives scene loads.
/// </summary>
public class GameManager : MonoBehaviour, IGameManager, ISaveParticipant
{
    // -------- Singleton --------
    public static GameManager Instance { get; private set; }
    /// <summary>
    /// Unity lifecycle: first entry point. Establishes the singleton instance
    /// and captures the starting scene for persistence.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Capture the initial scene so a fresh save knows where the player started.
        _currentScene = SceneManager.GetActiveScene().name;
        _spawnPointId = string.Empty; // default until a spawn point sets this

        // Automatically register built-in managers for persistence. Future systems
        // can call <see cref="RegisterSaveParticipant"/> during their own Awake/OnEnable.
        RegisterSaveParticipant(essenceManager);
        RegisterSaveParticipant(upgradeManager);
        RegisterSaveParticipant(inventoryManager);
        RegisterSaveParticipant(taskService);
        RegisterSaveParticipant(resourceManager);
        RegisterSaveParticipant(heartsManager);
        RegisterSaveParticipant(recipeManager);
        RegisterSaveParticipant(skillTreeManager);
        RegisterSaveParticipant(stationManager);
        RegisterSaveParticipant(dungeonProgression);
    }

    // -------- Core systems --------
    [Header("Core Systems (assign in Inspector)")]
    [SerializeField] private EssenceManager essenceManager;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private StationManager stationManager; // manages stations/companions
    [SerializeField] private DungeonProgression dungeonProgression; // tracks dungeon floors
    [SerializeField] private InventoryManager inventoryManager; // holds items
    [SerializeField] private ResourceManager resourceManager; // tracks generic resources
    [SerializeField] private HeartsManager heartsManager; // tracks companion hearts
    [SerializeField] private SkillTreeManager skillTreeManager; // governs skill unlocks
    [SerializeField] private RecipeManager recipeManager; // crafting recipes
    [SerializeField] private TaskService taskService; // drives tutorial-style task progression
    [SerializeField] private string unlockUpgradeId = UpgradeIds.UnlockBattle; // default to constant to avoid typos

    // -------- Daily companion assignments --------
    // Runtime-only map of companions to their roles for the current day.
    // Cleared each morning in <see cref="AdvanceDay"/>.
    private readonly Dictionary<CompanionSO, AssignmentRole> _dailyAssignments = new();

    [Header("Scene Tracking")]
    [SerializeField] private string _currentScene;
    [SerializeField] private string _spawnPointId;

    /// <summary>Scene name the player last occupied. Used for save/load.</summary>
    public string CurrentScene
    {
        get => _currentScene;
        set => _currentScene = value;
    }

    /// <summary>Identifier for the spawn point used when loading the scene.</summary>
    public string SpawnPointId
    {
        get => _spawnPointId;
        set => _spawnPointId = value;
    }


    [Header("Events")]
    [Tooltip("Reference to an event bus implementing IEventBus.")]
    [SerializeField] private MonoBehaviour eventBusSource;

    // Helper to cast the serialized reference to the interface. Keeps consumers
    // ignorant of the concrete implementation while still allowing Inspector wiring.
    public IEventBus Events => eventBusSource as IEventBus;

    /// <summary>Read-only inventory access for other systems.</summary>
    public IInventoryService Inventory => inventoryManager;

    /// <summary>Access to global resource counts.</summary>
    public ResourceManager Resources => resourceManager;

    /// <summary>Access to per-companion heart totals.</summary>
    public HeartsManager Hearts => heartsManager;

    /// <summary>Access to unlocked crafting recipes.</summary>
    public RecipeManager Recipes => recipeManager;

    /// <summary>Access to dungeon floor tracking and milestones.</summary>
    public DungeonProgression Dungeon => dungeonProgression;

    /// <summary>How many dungeon keys the player receives each day once unlocked.</summary>
    public int DungeonKeysPerDay => dungeonKeysPerDay; // expose for HUD

    /// <summary>Access to the skill tree for UI and systems.</summary>
    public SkillTreeManager Skills => skillTreeManager;

    /// <summary>Access to the tutorial task service.</summary>
    public TaskService Tasks => taskService;

    /// <summary>Read-only view of today's companion role assignments.</summary>
    public IReadOnlyDictionary<CompanionSO, AssignmentRole> CurrentAssignments => _dailyAssignments;

    /// <summary>Allow the bootstrapper to inject a TaskService instance at runtime.</summary>
    public void InjectTaskService(TaskService svc)
    {
        taskService = svc;
        // Ensure newly injected services participate in saves automatically.
        RegisterSaveParticipant(svc);
    }

    // -------- Save participant registry --------
    /// <summary>
    /// Internal list of systems that contribute to the save file. New gameplay
    /// systems can register themselves here so <see cref="SaveSystem"/> remains
    /// unaware of concrete types.
    /// </summary>
    private readonly List<ISaveParticipant> _saveParticipants = new();

    /// <summary>Read-only view of all save-aware systems.</summary>
    public IReadOnlyList<ISaveParticipant> SaveParticipants => _saveParticipants;

    /// <summary>
    /// Register a system so it participates in save/load operations.
    /// Call from Awake or OnEnable in the participant to hook into persistence
    /// without modifying <see cref="SaveSystem"/>.
    /// </summary>
    public void RegisterSaveParticipant(ISaveParticipant participant)
    {
        if (participant == null || _saveParticipants.Contains(participant)) return;
        _saveParticipants.Add(participant);
    }

    /// <summary>
    /// Remove a system from the save list, e.g. when it is destroyed.
    /// </summary>
    public void UnregisterSaveParticipant(ISaveParticipant participant)
    {
        if (participant == null) return;
        _saveParticipants.Remove(participant);
    }

    // -------- Daily companion assignments --------
    /// <summary>
    /// Assign a companion a role for the current day.
    /// Returns false if the companion already has an assignment.
    /// </summary>
    public bool AssignWaifu(CompanionSO companion, AssignmentRole role)
    {
        if (companion == null || _dailyAssignments.ContainsKey(companion))
            return false; // invalid or already assigned

        _dailyAssignments[companion] = role; // record today's role

        // Allow interested systems to react (e.g., stations applying multipliers)
        stationManager?.NotifyAssignment(companion, role);
        return true;
    }

    // ---- ISaveParticipant implementation ----

    /// <summary>
    /// Write GameManager-owned fields into the save model.
    /// </summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        model.day = Day;
        model.dungeonKeysRemaining = DungeonKeysRemaining;
        model.dungeonKeysPerDay = dungeonKeysPerDay;
        model.tempNextDayClickDebuff = _tempNextDayClickDebuff;
        model.lastScene = CurrentScene;
        model.spawnPointId = SpawnPointId;
    }

    /// <summary>
    /// Restore GameManager-owned fields from the save model.
    /// </summary>
    public void Apply(SaveModelV2 model)
    {
        ApplyLoadedState(model); // reuse existing method
    }

    /// <summary>
    /// Helper to locate an upgrade by id without allocating LINQ structures.
    /// </summary>
    private UpgradeSO FindUpgradeById(string id)
    {
        var up = Upgrades;
        if (up == null) return null;
        var list = up.Available;
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)   // manual loop avoids LINQ GC
            if (list[i] != null && list[i].id == id) return list[i];
        return null;
    }

    /// <summary>
    /// Determine whether the player can currently purchase the dungeon unlock upgrade.
    /// </summary>
    private bool CanAffordUnlock()
    {
        var so = FindUpgradeById(unlockUpgradeId);
        if (so == null || Essence == null) return false;  // missing data or no currency system
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

        // Watch daily click changes to reevaluate Sleep eligibility each time and
        // forward total essence changes through the event bus for HUD consumers.
        if (essenceManager != null)
        {
            essenceManager.OnDailyClicksChanged += HandleDailyClicksChanged;
            essenceManager.OnEssenceChanged += HandleEssenceChanged;
        }

        // Initialize UI state for the current day immediately.
        ReevaluateSleepGate();

        // Persist inventory changes automatically whenever items shift.
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += HandleInventoryChanged;

        // Watch resource counts so saves and UI stay up to date.
        if (resourceManager != null)
            resourceManager.OnResourceChanged += HandleResourceChanged;

        // Forward recipe unlocks onto the global event bus and persist progress.
        if (recipeManager != null)
            recipeManager.OnRecipeUnlocked += HandleRecipeUnlocked;

        // Bubble skill unlocks through the global event bus.
        if (skillTreeManager != null)
            skillTreeManager.OnSkillUnlocked += HandleSkillUnlocked;

        // Notify listeners when deeper dungeon floors are reached.
        if (dungeonProgression != null)
            dungeonProgression.OnFloorReached += HandleFloorReached;

        // Watch heart totals to check for relationship milestones.
        if (heartsManager != null)
            heartsManager.OnHeartsChanged += HandleHeartsChanged;
    }

    /// <summary>
    /// React to newly bought upgrades so dependent systems update immediately.
    /// </summary>
    private void HandleUpgradePurchased(UpgradeSO up)
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
            // Persist the newly unlocked state so the unlock isn't lost.
            // We route through the scheduler so multiple rapid events don't hammer disk.
            SaveScheduler.RequestSave(this);
        }
    }

    /// <summary>
    /// Invoked when the player's remaining manual clicks change.
    /// Used to recompute the Sleep gate each time.
    /// </summary>
    private void HandleDailyClicksChanged(int _)
    {
        ReevaluateSleepGate();
    }

    /// <summary>
    /// Persist inventory whenever items are added or removed and notify listeners.
    /// </summary>
    private void HandleInventoryChanged()
    {
        Events?.RaiseInventoryChanged();
        // Persist inventory mutations without spamming disk writes.
        SaveScheduler.RequestSave(this);
    }

    /// <summary>
    /// Forward essence changes through the event bus.
    /// </summary>
    private void HandleEssenceChanged(int amount)
    {
        Events?.RaiseEssenceChanged(amount);
    }

    /// <summary>
    /// Persist and broadcast whenever resource totals change.
    /// </summary>
    private void HandleResourceChanged(ResourceSO resource, int amount)
    {
        Events?.RaiseResourceChanged(resource, amount);
        SaveScheduler.RequestSave(this);
    }

    /// <summary>
    /// Persist and broadcast newly unlocked skills.
    /// </summary>
    private void HandleSkillUnlocked(SkillSO skill)
    {
        Events?.RaiseSkillUnlocked(skill);
        SaveScheduler.RequestSave(this);
    }

    /// <summary>
    /// Persist and broadcast newly unlocked crafting recipes.
    /// </summary>
    private void HandleRecipeUnlocked(RecipeSO recipe)
    {
        Events?.RaiseRecipeUnlocked(recipe);
        SaveScheduler.RequestSave(this);
    }

    /// <summary>
    /// Broadcast deeper dungeon progress to interested systems.
    /// </summary>
    private void HandleFloorReached(int floor)
        => Events?.RaiseFloorReached(floor);

    /// <summary>
    /// React to companion heart changes and evaluate milestone rewards.
    /// </summary>
    private void HandleHeartsChanged(CompanionSO companion, int total)
    {
        // Heart milestones may unlock content in the future.
        CheckHeartMilestones();
    }

    /// <summary>
    /// Placeholder for future heart-based milestone logic.
    /// </summary>
    private void CheckHeartMilestones()
    {
        // TODO: implement milestone checks (e.g., recruit bonuses).
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

    /// <summary>Read-only access for systems needing to persist the pending debuff.</summary>
    public int TempNextDayClickDebuff => _tempNextDayClickDebuff;


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
        AdvanceDay();
        return true;
    }

    /// <summary>Consume one dungeon key if available. Returns true on success.</summary>
    public bool TryConsumeDungeonKey()
    {
        if (!IsDungeonUnlocked()) return true; // before unlock, don't block
        if (DungeonKeysRemaining <= 0) return false;

        DungeonKeysRemaining = Mathf.Max(0, DungeonKeysRemaining - 1);
        // Let listeners know key counts changed (UI, save system, etc.).
        Events?.RaiseDungeonKeysChanged(DungeonKeysRemaining, dungeonKeysPerDay);
        ReevaluateSleepGate();
        // Persist new key count via scheduler (debounced).
        SaveScheduler.RequestSave(this);
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

        // Capture penalty and debuff in the next save tick.
        SaveScheduler.RequestSave(this);
    }

    // -------- Sleep gate --------

    /// <summary>
    /// Convenience check: has the player bought the dungeon unlock upgrade yet?
    /// </summary>
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

    /// <summary>
    /// Internal routine that advances the day and resets daily-limited resources.
    /// </summary>
    private void AdvanceDay() // keep this private so everyone uses TrySleep()
    {
        Day++; // increment first so save file reflects the new day

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

        // New day clears any temporary companion roles and station bonuses.
        _dailyAssignments.Clear();
        stationManager?.ResetProductionMultipliers();

        // Inform listeners of the new day index.
        Events?.RaiseDayChanged(Day);
        // Day advancement touches many systems; batch the save to cover them all.
        SaveScheduler.RequestSave(this);
        ReevaluateSleepGate();
    }

    /// <summary>
    /// Helper to compute today's click cap after applying any temporary debuffs.
    /// </summary>
    private int EssenceManagerDailyCapMinusDebuff()
    {
        int baseCap = essenceManager ? essenceManager.DailyClickCap : 10; // default if manager missing
        return Mathf.Max(0, baseCap - _tempNextDayClickDebuff);
    }

    // ---- Save/Load integration ----

    /// <summary>
    /// Populate runtime fields from the aggregated <see cref="SaveModelV2"/> data.
    /// Only touches values owned by <see cref="GameManager"/> itself.
    /// </summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        if (data == null) return;
        Day = Mathf.Max(1, data.day);
        DungeonKeysRemaining = Mathf.Max(0, data.dungeonKeysRemaining);
        dungeonKeysPerDay = data.dungeonKeysPerDay;
        _tempNextDayClickDebuff = Mathf.Max(0, data.tempNextDayClickDebuff);
        // Scene + spawn info restore the player's last location on boot.
        CurrentScene = data.lastScene;
        SpawnPointId = data.spawnPointId;
    }

    /// <summary>
    /// Detach event listeners when the object is disabled to prevent leaks.
    /// </summary>
    private void OnDisable()
    {
        if (essenceManager != null)
        {
            essenceManager.OnDailyClicksChanged -= HandleDailyClicksChanged;
            essenceManager.OnEssenceChanged -= HandleEssenceChanged;
        }

        if (upgradeManager != null)
            upgradeManager.OnPurchased -= HandleUpgradePurchased;

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= HandleInventoryChanged;

        if (resourceManager != null)
            resourceManager.OnResourceChanged -= HandleResourceChanged;

        if (recipeManager != null)
            recipeManager.OnRecipeUnlocked -= HandleRecipeUnlocked;

        if (skillTreeManager != null)
            skillTreeManager.OnSkillUnlocked -= HandleSkillUnlocked;

        if (dungeonProgression != null)
            dungeonProgression.OnFloorReached -= HandleFloorReached;

        if (heartsManager != null)
            heartsManager.OnHeartsChanged -= HandleHeartsChanged;
    }

    /// <summary>
    /// Unity lifecycle: called when the application is closing. Forces a final save.
    /// </summary>
    // Ensure a final save happens on shutdown. Scheduler flushes immediately on quit.
    private void OnApplicationQuit() => SaveScheduler.RequestSave(this);
}
