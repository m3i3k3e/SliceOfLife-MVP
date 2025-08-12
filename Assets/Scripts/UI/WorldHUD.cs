using TMPro;
using UnityEngine;

/// <summary>
/// Minimal HUD overlay for world scenes. Displays essence, dungeon keys,
/// current day, and the active tutorial task. Listens to the global
/// <see cref="IEventBus"/> via <see cref="GameManager.Events"/> so it stays in
/// sync without holding explicit references to each manager.
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
    private IEventBus _events;

    private void OnEnable()
    {
        _gm = GameManager.Instance;
        _events = _gm?.Events;

        if (_events != null)
        {
            // Subscribe to bus events for essence, day, task, and key updates.
            _events.EssenceChanged      += HandleEssenceChanged;
            _events.DayChanged          += HandleDayChanged;
            _events.TaskAdvanced        += HandleTaskChanged;
            _events.TaskCompleted       += HandleTaskChanged;
            _events.DungeonKeysChanged  += HandleKeysChanged;
        }

        // Initialize labels with current values so the HUD is immediately correct.
        HandleEssenceChanged(_gm?.Essence?.CurrentEssence ?? 0);
        HandleDayChanged(_gm?.Day ?? 1);
        if (_gm != null)
            HandleKeysChanged(_gm.DungeonKeysRemaining, _gm.DungeonKeysPerDay);
        UpdateTaskLabel();
    }

    private void OnDisable()
    {
        if (_events != null)
        {
            _events.EssenceChanged     -= HandleEssenceChanged;
            _events.DayChanged         -= HandleDayChanged;
            _events.TaskAdvanced       -= HandleTaskChanged;
            _events.TaskCompleted      -= HandleTaskChanged;
            _events.DungeonKeysChanged -= HandleKeysChanged;
        }
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
