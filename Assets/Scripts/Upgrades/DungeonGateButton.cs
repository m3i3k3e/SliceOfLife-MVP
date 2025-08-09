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

    private void Awake() { btn = GetComponent<Button>(); }

    private void OnEnable()
    {
        Refresh();
        Upgrades.OnPurchased += _ => Refresh();
    }

    private void OnDisable()
    {
        Upgrades.OnPurchased -= _ => Refresh(); // safe if not subscribed
    }

    private void Refresh()
    {
        btn.interactable = Upgrades.IsPurchased(requiredUpgradeId);
    }
}
