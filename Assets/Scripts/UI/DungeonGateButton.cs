using System; // For Action delegates
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

    // Safely fetch upgrades, guarding against a missing GameManager.
    private IUpgradeProvider Upgrades
    {
        get
        {
            var gm = GameManager.Instance; // may be null in tests or early in boot
            return gm != null ? gm.Upgrades : null;
        }
    }

    // Cache the delegate so we unsubscribe the exact same instance.
    // Using new lambdas on both subscribe/unsubscribe would leak handlers.
    private Action<UpgradeSO> _purchasedHandler;

    // Cache a lambda that reacts to key count changes.
    private Action<int, int> _keysChangedHandler;

    private void Awake()
    {
        btn = GetComponent<Button>();

        // Cache delegates so the same instance is removed on unsubscribe.
        _purchasedHandler   = _ => Refresh();
        _keysChangedHandler = (_, _) => Refresh();
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

        // Listen for dungeon key count changes so the button updates interactability.
        var gm = GameManager.Instance;
        if (gm != null && gm.Events != null)
        {
            gm.Events.DungeonKeysChanged += _keysChangedHandler;
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

        // Remove key change listener to avoid dangling delegates.
        var gm = GameManager.Instance;
        if (gm != null && gm.Events != null)
        {
            gm.Events.DungeonKeysChanged -= _keysChangedHandler;
        }
    }

    private void Refresh()
    {
        var gm       = GameManager.Instance; // can be null during shutdown
        var upgrades = Upgrades;

        // Only enable the button when the gate is unlocked AND the player has keys.
        bool hasUpgrade = upgrades != null && upgrades.IsPurchased(requiredUpgradeId);
        bool hasKeys    = gm != null && gm.DungeonKeysRemaining > 0;

        btn.interactable = hasUpgrade && hasKeys;
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
