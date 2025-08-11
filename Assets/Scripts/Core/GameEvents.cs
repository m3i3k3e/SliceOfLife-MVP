using System;

/// <summary>
/// Static hub for global game events. Managers forward their own events here so
/// transient listeners can subscribe without direct references.
/// </summary>
public static class GameEvents
{
    /// <summary>Fired whenever total essence changes.</summary>
    public static event Action<int> OnEssenceChanged;
    /// <summary>Fired whenever inventory contents mutate.</summary>
    public static event Action OnInventoryChanged;
    /// <summary>Fired when a task or quest advances.</summary>
    public static event Action OnTaskAdvanced;
    /// <summary>Fired when a task fully completes.</summary>
    public static event Action OnTaskCompleted;
    /// <summary>Fired after a successful upgrade purchase.</summary>
    public static event Action<UpgradeSO> OnUpgradePurchased;
    /// <summary>Fired after the day counter increments.</summary>
    public static event Action<int> OnDayChanged;

    /// <summary>Helper to invoke <see cref="OnEssenceChanged"/> safely.</summary>
    public static void RaiseEssenceChanged(int amount) => OnEssenceChanged?.Invoke(amount);
    /// <summary>Helper to invoke <see cref="OnInventoryChanged"/> safely.</summary>
    public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
    /// <summary>Helper to invoke <see cref="OnTaskAdvanced"/> safely.</summary>
    public static void RaiseTaskAdvanced() => OnTaskAdvanced?.Invoke();
    /// <summary>Helper to invoke <see cref="OnTaskCompleted"/> safely.</summary>
    public static void RaiseTaskCompleted() => OnTaskCompleted?.Invoke();
    /// <summary>Helper to invoke <see cref="OnUpgradePurchased"/> safely.</summary>
    public static void RaiseUpgradePurchased(UpgradeSO upgrade) => OnUpgradePurchased?.Invoke(upgrade);
    /// <summary>Helper to invoke <see cref="OnDayChanged"/> safely.</summary>
    public static void RaiseDayChanged(int day) => OnDayChanged?.Invoke(day);
}

