using System;
using System.Collections;
using UnityEngine;

public interface IEssenceProvider
{
    int CurrentEssence { get; }
    int DailyClicksRemaining { get; }
    int EssencePerClick { get; }
    float PassivePerSecond { get; }

    bool TryClickHarvest();
    bool TrySpend(int amount);
    void AddExternal(int amount);

    // Reset daily clicks (base cap) — existing call sites
    void ResetDailyClicks();

    // New: reset to a specific cap for *today* (used by GameManager when applying debuffs)
    void ResetDailyClicks(int todayCap);

    void AddEssencePerClick(int delta);
    void AddPassivePerSecond(float delta);

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

    // Expose the base cap so GameManager can compute debuffs against it
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

    public bool TryClickHarvest()
    {
        if (_dailyClicksRemaining <= 0) return false;

        _dailyClicksRemaining--;
        AddInternal(_essencePerClick);
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
        return true;
    }

    public void AddExternal(int amount)
    {
        if (amount == 0) return;
        AddInternal(amount);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (_currentEssence < amount) return false;

        _currentEssence -= amount;
        OnEssenceChanged?.Invoke(_currentEssence);
        return true;
    }

    public void AddEssencePerClick(int delta)
    {
        _essencePerClick = Mathf.Max(0, _essencePerClick + delta);
    }

    public void AddPassivePerSecond(float delta)
    {
        passivePerSecond = Mathf.Max(0f, passivePerSecond + delta);
        StartPassiveIfNeeded();
    }

    // Old signature — keep for existing callers
    public void ResetDailyClicks()
    {
        _dailyClicksRemaining = dailyClickCap;
        OnDailyClicksChanged?.Invoke(_dailyClicksRemaining);
    }

    // New signature — used by GameManager when a temporary debuff applies
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
}
