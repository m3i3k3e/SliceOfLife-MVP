using UnityEngine;

/*
 * ConsumeItemsOnInteract.cs
 * Purpose: Removes a specified quantity of an item from the player's inventory when
 *          the attached <see cref="Interactable"/> is activated.
 * Dependencies: Uses <see cref="GameManager"/> to reach the inventory service.
 * Expansion Hooks: Could gate interaction if items are missing; currently just logs failure.
 */

/// <summary>
/// Attach alongside <see cref="Interactable"/> to consume items on use.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class ConsumeItemsOnInteract : MonoBehaviour
{
    [Tooltip("Item required to interact.")]
    [SerializeField] private ItemSO item;

    [Tooltip("How many items to consume.")]
    [SerializeField] private int quantity = 1;

    private Interactable _interactable;

    private void Awake() => _interactable = GetComponent<Interactable>();

    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.OnInteracted += HandleInteract;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnInteracted -= HandleInteract;
    }

    private void HandleInteract(GameObject interactor)
    {
        var inventory = GameManager.Instance?.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning("No inventory service available.");
            return;
        }

        if (item == null)
        {
            Debug.LogWarning("No item configured for ConsumeItemsOnInteract.");
            return;
        }

        bool removed = inventory.TryRemove(item, Mathf.Max(1, quantity));
        Debug.Log(removed
            ? $"Consumed {quantity}x {item.name}."
            : $"Failed to consume {item.name}; insufficient quantity.");
    }
}
