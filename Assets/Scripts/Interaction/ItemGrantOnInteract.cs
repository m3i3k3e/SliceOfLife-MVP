using UnityEngine;

/*
 * ItemGrantOnInteract.cs
 * Purpose: Grants a specified item to the player's inventory when the attached
 *          <see cref="Interactable"/> is activated.
 * Dependencies: Requires <see cref="GameManager"/> to expose an inventory service.
 * Expansion Hooks: Additional rewards (XP, resources) can subscribe to the same interactable event.
 */

/// <summary>
/// Add to the same GameObject as <see cref="Interactable"/> to grant items on use.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class ItemGrantOnInteract : MonoBehaviour
{
    [Tooltip("Item to give the player.")]
    [SerializeField] private ItemSO item;

    [Tooltip("How many items to give.")]
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
            Debug.LogWarning("No item configured for ItemGrantOnInteract.");
            return;
        }

        bool added = inventory.TryAdd(item, Mathf.Max(1, quantity));
        Debug.Log(added
            ? $"Granted {quantity}x {item.name}."
            : $"Failed to add {item.name}; inventory full.");
    }
}
