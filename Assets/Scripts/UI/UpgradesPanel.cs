using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void OnDisable()
    {
        var upgrades = Upgrades;
        var essence = Essence;
        if (upgrades == null || essence == null) return;

        upgrades.OnPurchased -= OnPurchased;
        essence.OnEssenceChanged -= OnEssenceChanged;
    }

    private void BuildList()
    {
        // Clear any old rows (useful during hot reloads)
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        foreach (var up in Upgrades.Available)
        {
            var row = Instantiate(upgradeButtonPrefab, contentParent);

            // Find the two TMP labels on the prefab by name (case-insensitive contains match).
            TextMeshProUGUI title = null, cost = null;
            foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var n = t.name.ToLower();
                if (n.Contains("title")) title = t;
                if (n.Contains("cost"))  cost  = t;
            }

            if (title) title.text = up.title;
            if (cost)  cost.text  = $"Cost: {up.cost}";

            var localUp = up; // capture for lambda
            row.onClick.AddListener(() =>
            {
                if (!Upgrades.TryPurchase(localUp))
                {
                    // Optional: play a "not enough" sound or flash here.
                }
                else
                {
                    RefreshRow(row, localUp);
                }
            });

            RefreshRow(row, localUp);
        }
    }

    private void RefreshRow(Button row, UpgradeSO up)
    {
        // Purchased rows become non-interactable and show "Purchased"
        if (Upgrades.IsPurchased(up.id))
        {
            row.interactable = false;
            foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name.ToLower().Contains("cost")) t.text = "Purchased";
            return;
        }

        // Not purchased: enable only if affordable
        row.interactable = Essence.CurrentEssence >= up.cost;

        // Keep cost label accurate (optional, in case Essence changed)
        foreach (var t in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name.ToLower().Contains("cost"))
                t.text = $"Cost: {up.cost}";
    }

    private void OnPurchased(UpgradeSO _)
    {
        // After any purchase, rescan and refresh
        foreach (Transform child in contentParent)
        {
            var btn = child.GetComponent<Button>();
            if (!btn) continue;

            // Identify the upgrade by title text (good enough for MVP)
            var title = child.GetComponentInChildren<TextMeshProUGUI>();
            if (!title) continue;

            foreach (var up in Upgrades.Available)
                if (title.text == up.title)
                    RefreshRow(btn, up);
        }
    }

    private void OnEssenceChanged(int _)
    {
        // When money changes, re-evaluate affordability for all rows
        foreach (Transform child in contentParent)
        {
            var btn = child.GetComponent<Button>();
            if (!btn) continue;

            var title = child.GetComponentInChildren<TextMeshProUGUI>();
            if (!title) continue;

            foreach (var up in Upgrades.Available)
                if (title.text == up.title)
                    RefreshRow(btn, up);
        }
    }
}
