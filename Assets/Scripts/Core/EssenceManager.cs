using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Interface exposing ONLY what other systems need from the essence/currency brain.
/// By coding to this interface (instead of the concrete EssenceManager), we keep
/// UpgradeManager, Battle, UI, etc. loosely coupled.
/// 
/// NOTE: In the original code, UpgradeManager needed to adjust EssencePerClick
/// and PassivePerSecond, but those mutator methods were NOT declared here.
/// C# binds calls by the *static type*, so even though EssenceManager had those
/// methods, calling them through IEssenceProvider failed. We add them below.
/// </summary>
public interface IEssenceProvider
{
    // --- Read-only state for UI and other systems ---
    int CurrentEssence { get; }
    int DailyClicksRemaining { get; }
    int EssencePerClick { get; }
    float PassivePerSecond { get; }

    // --- Core actions used by the clicker loop and purchases ---
    /// <summary>Attempt a manual harvest (adds EssencePerClick) respecting the daily cap.</summary>
    bool TryClickHarvest();

    /// <summary>Spend essence if affordable. Returns true on success.</summary>
    bool TrySpend(int amount);

    /// <summary>Add essence from sources that ignore the daily click cap (passive, battle rewards).</summary>
    void AddExternal(int amount);

    /// <summary>Reset the daily click allowance to the configured cap (call on day change).</summary>
    void ResetDailyClicks();

    // --- Mutators needed by upgrades (MISSING BEFORE → caused CS1061) ---
    /// <summary>Increase the EssencePerClick stat by a positive delta.</summary>
    void AddEssencePerClick(int delta);

    /// <summary>Increase the passive-per-second income by a positive delta.</summary>
    void AddPassivePerSecond(float delta);

    // --- Events (UI can subscribe to update without polling) ---
    event Action<int> OnEssenceChanged;
    event Action<int> OnDailyClicksChanged;
}

public class EssenceManager : MonoBehaviour, IEssenceProvider
{
    [Header("Tuning")]
    [Tooltip("How many manual clicks are allowed each day (MVP default = 10).")]
    [SerializeField] private int dailyClickCap = 10;

    [Tooltip("Base essence added per valid click before upgrades.")]
    [SerializeField] private int baseEssencePerClick = 1;

    [Tooltip("Passive essence added every second. This bypasses the click cap.")]
    [SerializeField] private float passivePerSecond = 0f;

    // Backing fields for properties. Keep them private to control writes.
    private int _currentEssence = 0;
    private int _dailyClicksRemaining;
    private int _essencePerClick; // runtime total = base + upgrades

    // Public read-only properties expose state safely.
    public int CurrentEssence => _currentEssence;
    public int DailyClicksRemaining => _dailyClicksRemaining;
    public int EssencePerClick => _essencePerClick;
    public float PassivePerSecond => passivePerSecond;

    // C# events let multiple listeners (HUD, audio, analytics) react to changes.
    public event Action<int> OnEssenceChanged;
    public event Action<int> OnDailyClicksChanged;

    private Coroutine passiveRoutine;

    private void Awake()
    {
        _essencePerClick = baseEssencePerClick; // Start with base value
        _dailyClicksRemaining = dailyClickCap;  // Initialize daily allowance
    }

    private void OnEnable()
    {
        // Start passive ticker if configured (> 0). You can toggle this via upgrades later.
        StartPassiveIfNeeded();
    }

    /// <summary>
    /// Tries to add essence via a manual click. Enforces the daily cap.
    /// Returns true if the click granted essence, false if cap was reached.
    /// </summary>
    public bool TryClickHarvest()
    {
        if (_dailyClicksRemaining <= 0)
            return false; // Cap reached: ignore click cleanly (no errors, just no reward)

        _dailyClicksRemaining--;
        AddInternal(_essencePerClick); // Internal path: updates essence + events
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
        return true;
    }

    /// <summary>
    /// Adds essence from non-click sources (battle rewards, quests, passive).
    /// This intentionally IGNORES the daily click cap so "idle" feels rewarding.
    /// </summary>
    public void AddExternal(int amount)
    {
        if (amount == 0) return;
        AddInternal(amount);
    }

    /// <summary>
    /// Attempts to spend essence. Returns true if successful.
    /// Spending never touches the daily click counter.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (_currentEssence < amount) return false;

        _currentEssence -= amount;
        OnEssenceChanged?.Invoke(_currentEssence);
        return true;
    }

    /// <summary>
    /// Increase essence-per-click by a positive delta (used by upgrades).
    /// </summary>
    public void AddEssencePerClick(int delta)
    {
        _essencePerClick = Mathf.Max(0, _essencePerClick + delta);
    }

    /// <summary>
    /// Increase passive/sec by a positive delta (used by upgrades/recruits).
    /// </summary>
    public void AddPassivePerSecond(float delta)
    {
        passivePerSecond = Mathf.Max(0f, passivePerSecond + delta);
        StartPassiveIfNeeded();
    }

    /// <summary>
    /// Resets the daily clicks to the configured cap. Call this at day change.
    /// </summary>
    public void ResetDailyClicks()
    {
        _dailyClicksRemaining = dailyClickCap;
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
    }

    // --- Internal helpers ---

    private void AddInternal(int amount)
    {
        _currentEssence += amount;
        OnEssenceChanged?.Invoke(_currentEssence);
    }

    private void StartPassiveIfNeeded()
    {
        if (passivePerSecond > 0f && passiveRoutine == null)
            passiveRoutine = StartCoroutine(PassiveTick());
        else if (passivePerSecond <= 0f && passiveRoutine != null)
        {
            StopCoroutine(passiveRoutine);
            passiveRoutine = null;
        }
    }

    private IEnumerator PassiveTick()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            // AddExternal ignores daily cap, which is exactly what we want for idle.
            AddExternal(Mathf.FloorToInt(passivePerSecond));
            yield return wait;
        }
    }
}
