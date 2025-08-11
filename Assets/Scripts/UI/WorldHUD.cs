using TMPro;
using UnityEngine;

/// <summary>
/// Minimal HUD overlay for world scenes. Displays essence, dungeon keys,
/// current day, and the active tutorial task. Listens to <see cref="GameEvents"/>
/// so it stays in sync without direct event bus references.
/// </summary>
public class WorldHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI essenceText;  // "Essence: 0"
    [SerializeField] private TextMeshProUGUI keysText;     // "Keys: 0/0"
    [SerializeField] private TextMeshProUGUI dayText;      // "Day: 1"
    [SerializeField] private TextMeshProUGUI taskText;     // active task title

    [Header("Dependencies")]
    [SerializeField] private TaskService taskService;      // injected via inspector

    // Cache GameManager so we can grab initial values safely.
    private GameManager _gm;

    private void OnEnable()
    {
        _gm = GameManager.Instance;

        // Subscribe to static GameEvents for essence, day, task, and key updates.
        GameEvents.OnEssenceChanged      += HandleEssenceChanged;
        GameEvents.OnDayChanged          += HandleDayChanged;
        GameEvents.OnTaskAdvanced        += HandleTaskChanged;
        GameEvents.OnTaskCompleted       += HandleTaskChanged;
        GameEvents.OnDungeonKeysChanged  += HandleKeysChanged;

        // Initialize labels with current values so the HUD is immediately correct.
        HandleEssenceChanged(_gm?.Essence?.CurrentEssence ?? 0);
        HandleDayChanged(_gm?.Day ?? 1);
        if (_gm != null)
            HandleKeysChanged(_gm.DungeonKeysRemaining, _gm.DungeonKeysPerDay);
        UpdateTaskLabel();
    }

    private void OnDisable()
    {
        GameEvents.OnEssenceChanged     -= HandleEssenceChanged;
        GameEvents.OnDayChanged         -= HandleDayChanged;
        GameEvents.OnTaskAdvanced       -= HandleTaskChanged;
        GameEvents.OnTaskCompleted      -= HandleTaskChanged;
        GameEvents.OnDungeonKeysChanged -= HandleKeysChanged;
    }

    private void HandleEssenceChanged(int amount)
    {
        if (essenceText)
            essenceText.text = $"Essence: {amount}";
    }

    private void HandleKeysChanged(int current, int perDay)
    {
        if (keysText)
            keysText.text = $"Keys: {current}/{perDay}";
    }

    private void HandleDayChanged(int day)
    {
        if (dayText)
            dayText.text = $"Day: {day}";
    }

    private void HandleTaskChanged()
    {
        UpdateTaskLabel();
    }

    // Helper to fetch the task title from the service and update the label.
    private void UpdateTaskLabel()
    {
        if (!taskText) return;

        if (taskService == null)
        {
            taskText.text = string.Empty;
            return;
        }

        var title = taskService.CurrentTaskTitle;
        taskText.text = string.IsNullOrEmpty(title) ? "All tasks complete" : title;
    }
}
