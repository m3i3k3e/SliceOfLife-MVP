using UnityEngine;

/// <summary>
/// Condition that succeeds when the player has collected a target number of a specific item.
/// </summary>
[CreateAssetMenu(fileName = "CollectItemCondition", menuName = "SliceOfLife/TaskConditions/CollectItem")]
public class CollectItemConditionSO : TaskConditionSO
{
    [Tooltip("Item to check in the inventory.")]
    [SerializeField] private ItemSO item;
    [Tooltip("Required quantity of the item.")]
    [SerializeField] private int requiredQty = 1;

    /// <summary>Returns true when the inventory holds enough of the target item.</summary>
    public override bool IsMet(TaskService svc)
    {
        // Defensive: ensure all refs exist before querying.
        if (svc == null || item == null || svc.Inventory == null) return false;
        // Query the inventory; comparison uses >= so overshooting still satisfies the condition.
        return svc.Inventory.GetCount(item) >= requiredQty;
    }
}
