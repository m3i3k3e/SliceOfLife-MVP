using UnityEngine;

/// <summary>
/// Temporary harness that listens to <see cref="GameEvents"/> and logs when each
/// callback fires. Useful for verifying event bridging in play mode.
/// </summary>
public class GameEventsSanityTest : MonoBehaviour
{
    private int _essenceCalls;
    private int _inventoryCalls;
    private int _taskCalls;
    private int _upgradeCalls;
    private int _dayCalls;

    /// <summary>
    /// Subscribe to global events when the object becomes active.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnEssenceChanged += HandleEssenceChanged;
        GameEvents.OnInventoryChanged += HandleInventoryChanged;
        GameEvents.OnTaskAdvanced += HandleTaskAdvanced;
        GameEvents.OnUpgradePurchased += HandleUpgradePurchased;
        GameEvents.OnDayChanged += HandleDayChanged;
    }

    /// <summary>
    /// Unsubscribe from all events to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnEssenceChanged -= HandleEssenceChanged;
        GameEvents.OnInventoryChanged -= HandleInventoryChanged;
        GameEvents.OnTaskAdvanced -= HandleTaskAdvanced;
        GameEvents.OnUpgradePurchased -= HandleUpgradePurchased;
        GameEvents.OnDayChanged -= HandleDayChanged;
    }

    // --- Individual handlers simply log invocation counts for manual verification ---
    private void HandleEssenceChanged(int amount)
    {
        _essenceCalls++;
        Debug.Log($"Essence changed to {amount}. Call #{_essenceCalls}");
    }

    private void HandleInventoryChanged()
    {
        _inventoryCalls++;
        Debug.Log($"Inventory changed. Call #{_inventoryCalls}");
    }

    private void HandleTaskAdvanced()
    {
        _taskCalls++;
        Debug.Log($"Task advanced. Call #{_taskCalls}");
    }

    private void HandleUpgradePurchased(UpgradeSO up)
    {
        _upgradeCalls++;
        Debug.Log($"Upgrade purchased: {up?.name}. Call #{_upgradeCalls}");
    }

    private void HandleDayChanged(int day)
    {
        _dayCalls++;
        Debug.Log($"Day changed to {day}. Call #{_dayCalls}");
    }
}

