using UnityEngine;

/// <summary>
/// Condition satisfied when a specific upgrade purchase is observed.
/// </summary>
[CreateAssetMenu(fileName = "UpgradePurchasedCondition", menuName = "SliceOfLife/TaskConditions/UpgradePurchased")]
public class UpgradePurchasedConditionSO : TaskConditionSO
{
    [Tooltip("Upgrade required for completion.")]
    [SerializeField] private UpgradeSO upgrade;

    /// <summary>True on the frame the matching upgrade id is reported.</summary>
    public override bool IsMet(TaskService svc)
    {
        if (svc == null || upgrade == null) return false;
        return svc.LastUpgradeId == upgrade.id;
    }
}
