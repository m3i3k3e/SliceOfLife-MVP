using System.Collections;
using UnityEngine;

/// <summary>
/// Simple driver that marches through all tutorial tasks and logs
/// when <see cref="GameEvents"/> fire. Useful for verifying the
/// TaskService wiring without full gameplay.
/// </summary>
public class TaskServiceSanityTest : MonoBehaviour
{
    [SerializeField] private TaskService service;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private ItemSO wood;
    [SerializeField] private UpgradeSO unlockDungeon;

    private void OnEnable()
    {
        GameEvents.OnTaskAdvanced += HandleAdvanced;
        GameEvents.OnTaskCompleted += HandleCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnTaskAdvanced -= HandleAdvanced;
        GameEvents.OnTaskCompleted -= HandleCompleted;
    }

    private IEnumerator Start()
    {
        // Defer a frame so TaskService initializes.
        yield return null;

        // 1) Collect wood
        inventory?.TryAdd(wood, 3);
        yield return null; // allow event to propagate

        // Subsequent tasks rely on interaction notifications.
        service.NotifyInteraction("rubble_clear");
        service.NotifyInteraction("altar_polish");
        service.NotifyInteraction("build_bed");
        service.NotifyInteraction("cook_meal");
        service.NotifyInteraction("brew_potion");
        GameEvents.RaiseUpgradePurchased(unlockDungeon);
        service.NotifyInteraction("enter_dungeon");
    }

    private void HandleAdvanced() => Debug.Log("Task advanced");
    private void HandleCompleted() => Debug.Log("Task completed");
}
