using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Populates a simple map panel with buttons for each unlocked <see cref="LocationSO"/>.
/// Buttons load the target scene when clicked. List refreshes whenever upgrades unlock
/// new locations via the global <see cref="IEventBus"/>.
/// </summary>
public class MapUI : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("All possible locations. Order here is the order shown in UI.")]
    [SerializeField] private List<LocationSO> locations = new();

    [Header("Prefabs")]
    [Tooltip("Prefab containing a Button and LocationButtonView for each entry.")]
    [SerializeField] private LocationButtonView buttonPrefab;
    [Tooltip("Parent transform that will receive the instantiated buttons.")]
    [SerializeField] private Transform contentParent;

    // Convenience accessors to the game's upgrade system and event bus.
    private IUpgradeProvider Upgrades => GameManager.Instance?.Upgrades;
    private IEventBus Events => GameManager.Instance?.Events;

    // Cached delegate so we unsubscribe correctly in OnDisable.
    private System.Action<UpgradeSO> _refreshHandler;

    private void Awake()
    {
        // Cache the delegate once to avoid lambda allocations each subscribe/unsubscribe.
        _refreshHandler = _ => Refresh();
    }

    private void OnEnable()
    {
        Refresh();
        var ev = Events;
        if (ev != null)
        {
            // Listen for upgrade purchases so newly unlocked locations appear immediately.
            ev.UpgradePurchased += _refreshHandler;
        }
    }

    private void OnDisable()
    {
        var ev = Events;
        if (ev != null)
        {
            ev.UpgradePurchased -= _refreshHandler;
        }
    }

    /// <summary>
    /// Rebuilds the list of location buttons based on current upgrade ownership.
    /// </summary>
    private void Refresh()
    {
        if (contentParent == null || buttonPrefab == null)
            return; // Missing references; bail quietly.

        // Clear out any previous children so we can rebuild from scratch.
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var upgrades = Upgrades;
        foreach (var loc in locations)
        {
            if (loc == null)
                continue;

            // Skip locations that require an upgrade we don't own yet.
            if (!string.IsNullOrEmpty(loc.UnlockUpgradeId) && (upgrades == null || !upgrades.IsPurchased(loc.UnlockUpgradeId)))
                continue;

            // Instantiate a fresh button for the location.
            var view = Instantiate(buttonPrefab, contentParent);
            view.Label.text = loc.DisplayName;

            // Use local variable to capture for the lambda.
            var scene = loc.SceneName;
            var btn = view.GetComponent<Button>();
            if (btn != null)
            {
                // Load the desired scene when clicked.
                btn.onClick.AddListener(() => SceneManager.LoadScene(scene));
            }
        }
    }
}
