using System;
using UnityEngine;

/// <summary>
/// GameManager is the tiny "orchestrator" that lives across scenes.
/// - Implements a classic, minimal Singleton so other scripts can find it.
/// - Holds references to other core managers (Essence, Upgrades).
/// - Exposes C# events (OnDayChanged, etc.) so UI can listen without
///   hard references. This keeps systems modular.
/// IMPORTANT: Keep this class boring. It shouldn't do game logic,
/// just route and coordinate.
/// </summary>
/// 
/// 
[DefaultExecutionOrder(-100)] // Run this MonoBehaviour's Awake/OnEnable very early

public class GameManager : MonoBehaviour
{
    // Public, read-only access to the single instance of GameManager.
    // The setter is private so only this class can assign it.
    public static GameManager Instance { get; private set; }

    [Header("Core Systems (assign in Inspector)")]
    [SerializeField] private EssenceManager essenceManager;
    [SerializeField] private UpgradeManager upgradeManager;

    // Example of a tiny piece of "global" state: which in-game day we are on.
    // We keep the setter private so only GameManager can advance the day.
    public int Day { get; private set; } = 1;

    /// <summary>
    /// Event fired whenever the Day value changes. The int payload is the new day.
    /// C# events are type-safe and cannot be invoked from outside this class,
    /// which reduces accidental misuse compared to UnityEvent.
    /// </summary>
    public event Action<int> OnDayChanged;

    private void Awake()
    {
        // Basic Singleton pattern. We keep exactly one GameManager.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist when loading new scenes.
        }
        else
        {
            Destroy(gameObject); // If a duplicate loads, kill it immediately.
            return;
        }
    }

    private void Start()
    {
        // Prefer assigning these in the Inspector. This is only a safety net.
        if (!essenceManager)
            essenceManager = UnityEngine.Object.FindFirstObjectByType<EssenceManager>(
                UnityEngine.FindObjectsInactive.Include); // Explicit namespace avoids ambiguity

        if (!upgradeManager)
            upgradeManager = UnityEngine.Object.FindFirstObjectByType<UpgradeManager>(
                UnityEngine.FindObjectsInactive.Include);
    }

    public void AdvanceDay()
    {
        Day++;
        essenceManager.ResetDailyClicks();        // New day → new click allowance
        OnDayChanged?.Invoke(Day);                // Notify any listeners (HUD, etc.)
        SaveSystem.Save(this);                    // Persist progress on day change
    }

    // Small accessors so other code doesn't need to know concrete types.
    public IEssenceProvider Essence => essenceManager;
    public IUpgradeProvider Upgrades => upgradeManager;

    private void OnApplicationQuit()
    {
        SaveSystem.Save(this); // Save on quit as a safety net.
    }
}
