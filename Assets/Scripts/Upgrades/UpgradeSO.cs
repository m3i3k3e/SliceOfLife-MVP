using UnityEngine;

/// <summary>
/// Data-only asset describing a single upgrade. Designers tweak values in the Inspector.
/// Each upgrade references an <see cref="IUpgradeEffect"/> ScriptableObject that performs the logic.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade", menuName = "SliceOfLife/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [Header("Identity")]
    public string id = "upgrade_id";    // Stable string saved to disk (safer than index)
    public string title = "New Upgrade";
    [TextArea] public string description;

    [Header("Economy")]
    public int cost = 10;               // Essence cost (use a gentle geometric curve later)

    [Header("Effect")]
    [SerializeField] private UpgradeEffectSO effect; // Serialized polymorphic effect asset

    /// <summary>
    /// Exposes the effect through the interface so callers stay decoupled from concrete types.
    /// </summary>
    public IUpgradeEffect Effect => effect;
}
