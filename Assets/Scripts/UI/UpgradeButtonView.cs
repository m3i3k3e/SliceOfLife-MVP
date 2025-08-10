using TMPro;
using UnityEngine;

/// <summary>
/// Simple view component that exposes references to the Upgrade button's labels.
/// Keeping this data on a dedicated script avoids string-based lookups at runtime.
/// </summary>
public class UpgradeButtonView : MonoBehaviour
{
    [Header("Assigned in Prefab")]
    [SerializeField] private TMP_Text titleText; // label showing the upgrade's title
    [SerializeField] private TMP_Text costText;  // label showing the upgrade's cost or purchase state

    /// <summary>Public read-only access to the title label.</summary>
    public TMP_Text TitleText => titleText;

    /// <summary>Public read-only access to the cost label.</summary>
    public TMP_Text CostText => costText;
}
