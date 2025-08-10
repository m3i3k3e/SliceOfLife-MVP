using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Abstraction for the currency system. Exposing an interface lets other systems depend
/// on a small surface instead of the concrete <see cref="EssenceManager"/> implementation.
/// </summary>
public interface IEssenceProvider
{
    /// <summary>Current essence owned by the player.</summary>
    int CurrentEssence { get; }
    /// <summary>How many manual clicks remain today.</summary>
    int DailyClicksRemaining { get; }
    /// <summary>Essence gained per valid click.</summary>
    int EssencePerClick { get; }
    /// <summary>Passive essence generated per second.</summary>
    float PassivePerSecond { get; }

    /// <summary>Attempt a manual harvest. Returns false if the daily cap was reached.</summary>
    bool TryClickHarvest();
    /// <summary>Try to spend a chunk of essence; returns false if insufficient funds.</summary>
    bool TrySpend(int amount);
    /// <summary>Add essence from an external source (bypasses click cap).</summary>
    void AddExternal(int amount);

    /// <summary>Reset daily clicks back to the base cap.</summary>
    void ResetDailyClicks();

    /// <summary>Reset daily clicks to a specific cap for today only (e.g., debuffs).</summary>
    void ResetDailyClicks(int todayCap);

    /// <summary>Increase essence granted per click.</summary>
    void AddEssencePerClick(int delta);
    /// <summary>Increase passive essence generated per second.</summary>
    void AddPassivePerSecond(float delta);

    /// <summary>Fired whenever total essence changes.</summary>
    event Action<int> OnEssenceChanged;
    /// <summary>Fired whenever remaining clicks change.</summary>
    event Action<int> OnDailyClicksChanged;
}

/// <summary>
/// Concrete currency manager. Implements <see cref="IEssenceProvider"/> so the rest of
/// the game can stay decoupled from MonoBehaviour specifics.
/// </summary>
public class EssenceManager : MonoBehaviour, IEssenceProvider
{
    [Header("Tuning")]
    [Tooltip("How many manual clicks are allowed each day (MVP default = 10).")]
    [SerializeField] private int dailyClickCap = 10;

    [Tooltip("Base essence added per valid click before upgrades.")]
    [SerializeField] private int baseEssencePerClick = 1;

    [Tooltip("Passive essence added every second. This bypasses the click cap.")]
    [SerializeField] private float passivePerSecond = 0f;

    /// <summary>Base click cap before any temporary debuffs are applied.</summary>
    public int DailyClickCap => dailyClickCap;

    private int _currentEssence = 0;
    private int _dailyClicksRemaining;
    private int _essencePerClick;

    public int CurrentEssence => _currentEssence;
    public int DailyClicksRemaining => _dailyClicksRemaining;
    public int EssencePerClick => _essencePerClick;
    public float PassivePerSecond => passivePerSecond;

    public event Action<int> OnEssenceChanged;
    public event Action<int> OnDailyClicksChanged;

    private Coroutine passiveRoutine;

    private void Awake()
    {
        _essencePerClick = baseEssencePerClick;
        _dailyClicksRemaining = dailyClickCap;
    }

    private void OnEnable() => StartPassiveIfNeeded();

    /// <summary>Consume one click and add essence if under the daily cap.</summary>
    public bool TryClickHarvest()
    {
        if (_dailyClicksRemaining <= 0) return false;

        _dailyClicksRemaining--;
        AddInternal(_essencePerClick);
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
        return true;
    }

    /// <summary>Add essence from rewards or passive income (ignores click cap).</summary>
    public void AddExternal(int amount)
    {
        if (amount == 0) return;
        AddInternal(amount);
    }

    /// <summary>Attempt to subtract essence; returns false if not enough.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (_currentEssence < amount) return false;

        _currentEssence -= amount;
        OnEssenceChanged?.Invoke(_currentEssence);
        return true;
    }

    /// <summary>Adjust the essence granted per click. Used by upgrades.</summary>
    public void AddEssencePerClick(int delta)
    {
        _essencePerClick = Mathf.Max(0, _essencePerClick + delta);
    }

    /// <summary>Adjust passive essence generation and restart the tick routine if needed.</summary>
    public void AddPassivePerSecond(float delta)
    {
        passivePerSecond = Mathf.Max(0f, passivePerSecond + delta);
        StartPassiveIfNeeded();
    }

    // Old signature — keep for existing callers
    /// <summary>Reset daily clicks to the base cap.</summary>
    public void ResetDailyClicks()
    {
        _dailyClicksRemaining = dailyClickCap;
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
    }

    // New signature — used by GameManager when a temporary debuff applies
    /// <summary>Reset daily clicks to a specific cap for today only.</summary>
    public void ResetDailyClicks(int todayCap)
    {
        _dailyClicksRemaining = Mathf.Max(0, todayCap);
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
    }

    // ---- internals ----

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
            AddExternal(Mathf.FloorToInt(passivePerSecond));
            yield return wait;
        }
    }

    // ---- Save/Load ----

    /// <summary>
    /// Extract the minimal persistence data for the essence system.
    /// </summary>
    public GameSaveData.EssenceData ToData()
    {
        // Build a plain container that captures only the fields we want to persist.
        return new GameSaveData.EssenceData
        {
            currentEssence = _currentEssence,
            dailyClicksRemaining = _dailyClicksRemaining,
            essencePerClick = _essencePerClick,
            passivePerSecond = passivePerSecond
        };
    }

    /// <summary>
    /// Restore state from disk. Called by <see cref="SaveSystem"/> after deserializing.
    /// </summary>
    public void LoadFrom(GameSaveData.EssenceData data)
    {
        if (data == null) return; // defensive: save file may be missing fields

        // Directly assign fields instead of using reflection. We notify listeners so
        // any UI hooked up at load time refreshes with accurate numbers.
        _currentEssence = data.currentEssence;
        _dailyClicksRemaining = data.dailyClicksRemaining;
        _essencePerClick = data.essencePerClick;
        passivePerSecond = data.passivePerSecond;

        OnEssenceChanged?.Invoke(_currentEssence);
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);

        // Make sure passive income coroutine reflects the loaded rate.
        StartPassiveIfNeeded();
    }
}
