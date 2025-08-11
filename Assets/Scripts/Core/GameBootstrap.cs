using UnityEngine;

/// <summary>
/// Creates and persists core service singletons at startup.
/// Lives on a tiny bootstrapper prefab placed in scenes so gameplay code
/// remains decoupled from specific scene setups.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Service Prefabs")]
    [Tooltip("GameManager responsible for high level state and saving.")]
    [SerializeField] private GameManager gameManagerPrefab;

    [Tooltip("Inventory system holding player items.")]
    [SerializeField] private InventoryManager inventoryManagerPrefab;

    // TaskService is typed loosely because the concrete class will evolve.
    [Tooltip("Future task/quest coordinator.")]
    [SerializeField] private MonoBehaviour taskServicePrefab;

    [Tooltip("Concrete event bus used for global notifications.")]
    [SerializeField] private DefaultEventBus eventBusPrefab;

    /// <summary>
    /// Ensure a single instance of each service exists and survives scene loads.
    /// </summary>
    private void Awake()
    {
        // If another bootstrap already exists (active or not), this instance is a
        // duplicate from an additional scene load. Destroy it immediately so only
        // one orchestrator persists for the lifetime of the application.
        if (FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return; // Abort startup; a previous bootstrap is already in charge.
        }

        // Keep the surviving bootstrap object alive across scene loads so it can
        // spawn services just once.
        DontDestroyOnLoad(gameObject);

        // Instantiate or locate each required service. These calls are cheap and
        // run only once at boot, so searching the scene hierarchy is acceptable.
        Ensure(inventoryManagerPrefab);
        Ensure(taskServicePrefab);
        Ensure(eventBusPrefab);
        Ensure(gameManagerPrefab); // game manager last so above services exist first
    }

    /// <summary>
    /// After services spin up, attempt to load any saved data.
    /// Missing files are silently ignored by SaveSystem.
    /// </summary>
    private void Start()
    {
        var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
        var tasks = FindFirstObjectByType<TaskService>(FindObjectsInactive.Include);
        if (gm != null)
        {
            // Wire the task service into GameManager before loading so SaveSystem has references.
            gm.InjectTaskService(tasks);
            // Load() returns default data if no save exists; we intentionally ignore it.
            SaveSystem.Load(gm, tasks);
        }
    }

    /// <summary>
    /// Helper to instantiate a prefab only when its type isn't already present.
    /// </summary>
    private static void Ensure(MonoBehaviour prefab)
    {
        if (prefab == null) return; // nothing to create

        // Check for an existing instance of the same component type. We include
        // inactive objects so services created in earlier scenes are still
        // detected even if they've been temporarily disabled.
        var existing = FindFirstObjectByType(prefab.GetType(), FindObjectsInactive.Include);
        if (existing != null) return; // already present

        // Spawn and mark the instance to persist between scene loads.
        var instance = Instantiate(prefab);
        DontDestroyOnLoad(instance.gameObject);
    }
}

