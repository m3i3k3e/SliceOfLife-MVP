// This entire file is only meant for in-editor testing utilities. Wrapping in
// UNITY_EDITOR ensures it is stripped from player builds, preventing accidental
// inclusion of debugging UI in production.
#if UNITY_EDITOR

using UnityEngine;

/// <summary>
/// Quick-and-dirty runtime GUI for exercising the inventory service.
/// Place on any scene object, assign an <see cref="InventoryManager"/>
/// and an <see cref="ItemSO"/> to test with.
/// </summary>
public class InventoryDebugMenu : MonoBehaviour
{
    [Tooltip("Inventory service to mutate during tests.")]
    [SerializeField] private InventoryManager inventory;

    [Tooltip("Item to add/remove when pressing the buttons.")]
    [SerializeField] private ItemSO testItem;

    [Tooltip("How many items to add or remove per click.")]
    [SerializeField] private int quantity = 1;

    private void OnEnable()
    {
        // Listen to the global event to prove our actions fire notifications.
        GameEvents.OnInventoryChanged += HandleChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged -= HandleChanged;
    }

    private void HandleChanged()
    {
        Debug.Log("Inventory changed");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 120, 80));
        if (GUILayout.Button("Add"))
            inventory?.TryAdd(testItem, quantity);
        if (GUILayout.Button("Remove"))
            inventory?.TryRemove(testItem, quantity);
        GUILayout.EndArea();
    }
}

#endif
