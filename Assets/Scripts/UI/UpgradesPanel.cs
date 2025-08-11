/*
 * UpgradesPanel.cs
 * Role: Renders upgrade buttons and reacts to currency/purchase events.
 * Expansion: Extend UpgradeButtonView or refresh logic to support new upgrade UI elements.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // for Dictionary<TKey, TValue>

/// <summary>
/// Spawns one button per UpgradeSO and updates their state when currency/purchases change.
/// Keep UI dumb; UpgradeManager owns the rules.
/// </summary>
public class UpgradesPanel : MonoBehaviour
{
    [Header("Prefab & Container (assign in Inspector)")]
    [SerializeField] private Button upgradeButtonPrefab; // a Button with two TMP texts: TitleText, CostText
    [SerializeField] private Transform contentParent;    // where to place the buttons

    private IUpgradeProvider Upgrades => GameManager.Instance.Upgrades;
    private IEssenceProvider Essence  => GameManager.Instance.Essence;

    /// <summary>
    /// Maps each UpgradeSO.id to its instantiated Button for quick lookups.
    /// </summary>
    private readonly Dictionary<string, Button> _buttonsById = new();

    /// <summary>
    /// Cache UpgradeSO by id so we don't search the provider list repeatedly.
    /// </summary>
    private readonly Dictionary<string, UpgradeSO> _upgradesById = new();

    /// <summary>
    /// Unity lifecycle: build button list and subscribe to events when panel becomes visible.
    /// </summary>
    private void OnEnable()
    {
        // Cache provider references and guard against missing systems during init/hot reloads.
        var upgrades = Upgrades;
        var essence = Essence;
        if (upgrades == null || essence == null) return;

        BuildList();
        upgrades.OnPurchased += OnPurchased;
        essence.OnEssenceChanged += OnEssenceChanged;
    }

    /// <summary>
    /// Unity lifecycle: detach listeners to avoid leaks when panel hides.
    /// </summary>
    private void OnDisable()
    {
        var upgrades = Upgrades;
        var essence = Essence;
        if (upgrades == null || essence == null) return;

        upgrades.OnPurchased -= OnPurchased;
        essence.OnEssenceChanged -= OnEssenceChanged;
    }

    /// <summary>
    /// (Re)creates the list of upgrade rows and caches lookups for later refreshes.
    /// </summary>
    private void BuildList()
    {
        // Clear any old rows (useful during hot reloads)
        foreach (Transform child in contentParent) Destroy(child.gameObject); // remove old rows
        _buttonsById.Clear();       // stale references are not valid after rebuild
        _upgradesById.Clear();      // keep upgrade lookup in sync with buttons

        foreach (var up in Upgrades.Available)
        {
            var row = Instantiate(upgradeButtonPrefab, contentParent);

            // Grab the view component and set its labels directly.
            // This avoids searching children by string every time we build the list.
            var view = row.GetComponent<UpgradeButtonView>();
            if (view != null)
            {
                view.TitleText.text = up.title;
                view.CostText.text  = $"Cost: {up.cost}";
            }

            var localUp = up; // capture for lambda
            row.onClick.AddListener(() =>
            {
                if (!Upgrades.TryPurchase(localUp))
                {
                    // Optional: play a "not enough" sound or flash here.
                }
                else
                {
                    RefreshRow(row, localUp); // purchase succeeded; row becomes disabled
                }
            });

            // Remember the mapping so later refreshes don't search by strings.
            _buttonsById[up.id] = row;
            _upgradesById[up.id] = up;

            RefreshRow(row, localUp);
        }
    }

    /// <summary>
    /// Update a single row's interactivity and labels based on purchase/affordability.
    /// </summary>
    private void RefreshRow(Button row, UpgradeSO up)
    {
        // Purchased rows become non-interactable and show "Purchased"
        if (Upgrades.IsPurchased(up.id))
        {
            row.interactable = false; // can't click again
            foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name.ToLower().Contains("cost")) t.text = "Purchased"; // show status
            return;
        }

        // Not purchased: enable only if affordable
        row.interactable = Essence.CurrentEssence >= up.cost; // enable only if player can afford

        // Keep cost label accurate (optional, in case Essence changed)
        foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name.ToLower().Contains("cost"))
                t.text = $"Cost: {up.cost}";
    }

    /// <summary>
    /// React to purchase events and refresh only the affected row.
    /// </summary>
    private void OnPurchased(UpgradeSO purchased)
    {
        // Refresh only the button that belongs to the purchased upgrade
        if (_buttonsById.TryGetValue(purchased.id, out var button))
        {
            RefreshRow(button, purchased);
        }
    }

    /// <summary>
    /// Fired when currency changes; reevaluates affordability across all upgrades.
    /// </summary>
    private void OnEssenceChanged(int _)
    {
        // When money changes, re-evaluate affordability for all rows
        foreach (var kvp in _buttonsById)
        {
            if (_upgradesById.TryGetValue(kvp.Key, out var up))
            {
                RefreshRow(kvp.Value, up);
            }
        }
    }
}
