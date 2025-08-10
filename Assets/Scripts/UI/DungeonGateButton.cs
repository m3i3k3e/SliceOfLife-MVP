using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a button disabled until a specific upgrade ID is owned.
/// Default looks for 'unlock_battle'.
/// </summary>
[RequireComponent(typeof(Button))]
public class DungeonGateButton : MonoBehaviour
{
    [SerializeField] private string requiredUpgradeId = "unlock_battle";

    private Button btn;
    private IUpgradeProvider Upgrades => GameManager.Instance.Upgrades;

    // Cache the delegate so we unsubscribe the exact same instance.
    // Using new lambdas on both subscribe/unsubscribe would leak handlers.
    private System.Action<UpgradeSO> _purchasedHandler;

    private void Awake()
    {
        btn = GetComponent<Button>();
        _purchasedHandler = _ => Refresh();
    }

    private void OnEnable()
    {
        Refresh();

        var upgrades = Upgrades;
        if (upgrades != null)
        {
            // Subscribe once using the cached delegate.
            upgrades.OnPurchased += _purchasedHandler;
        }
    }

    private void OnDisable()
    {
        var upgrades = Upgrades;
        if (upgrades != null)
        {
            // Unsubscribe using the same delegate instance we added.
            upgrades.OnPurchased -= _purchasedHandler;
        }
    }

    private void Refresh()
    {
        var upgrades = Upgrades;
        // Only enable the button when the upgrade is known and purchased.
        btn.interactable = upgrades != null && upgrades.IsPurchased(requiredUpgradeId);
    }
}
