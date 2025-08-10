using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a button disabled until a specific upgrade ID is owned.
/// Default looks for <see cref="UpgradeIds.UnlockBattle"/> to gate the dungeon door.
/// </summary>
[RequireComponent(typeof(Button))]
public class DungeonGateButton : MonoBehaviour
{
    [SerializeField] private string requiredUpgradeId = UpgradeIds.UnlockBattle; // use constant to avoid typos in Inspector

    [Tooltip("Scene to load when the gate is opened and clicked.")]
    [SerializeField] private string sceneName = "Battle";

    [Tooltip("Reference to the global SceneLoader. Inject via inspector.")]
    [SerializeField] private SceneLoader sceneLoader;

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

        // Hook the click event so we can trigger scene loads.
        if (btn != null)
        {
            btn.onClick.AddListener(OnClicked);
        }

        var upgrades = Upgrades;
        if (upgrades != null)
        {
            // Subscribe once using the cached delegate.
            upgrades.OnPurchased += _purchasedHandler;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from click event to avoid memory leaks.
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClicked);
        }

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

    /// <summary>
    /// Loads the dungeon scene once the button is pressed.
    /// </summary>
    private async void OnClicked()
    {
        if (sceneLoader == null)
        {
            Debug.LogWarning($"SceneLoader not assigned on {name}", this);
            return;
        }

        await sceneLoader.LoadSceneAsync(sceneName);
    }
}
