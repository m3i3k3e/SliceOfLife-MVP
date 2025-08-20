using TMPro;
using UnityEngine;

/// <summary>
/// Displays essence totals and remaining daily clicks.
/// Also exposes the Gather button handler.
/// </summary>
public class CurrencyHUDPanel : HUDPanel
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI essenceText;  // e.g., "Essence: 0"
    [SerializeField] private TextMeshProUGUI clicksText;   // e.g., "Clicks Left: 10"
    [SerializeField] private TextMeshProUGUI heartsText;   // e.g., "Hearts: 0"

    protected override void OnBind()
    {
        if (GM?.Essence == null) return;

        // Subscribe to currency events so labels stay in sync.
        GM.Essence.OnEssenceChanged += HandleEssenceChanged;
        GM.Essence.OnDailyClicksChanged += HandleClicksChanged;

        // Watch heart totals so the HUD reflects relationship progress.
        if (GM.Hearts != null)
            GM.Hearts.OnHeartsChanged += HandleHeartsChanged;

        // Initialize labels immediately with current values.
        HandleEssenceChanged(GM.Essence.CurrentEssence);
        HandleClicksChanged(GM.Essence.DailyClicksRemaining);
        if (heartsText != null)
            heartsText.text = "Hearts: 0"; // baseline until first event arrives
    }

    protected override void OnUnbind()
    {
        if (GM?.Essence == null) return;

        GM.Essence.OnEssenceChanged -= HandleEssenceChanged;
        GM.Essence.OnDailyClicksChanged -= HandleClicksChanged;

        if (GM.Hearts != null)
            GM.Hearts.OnHeartsChanged -= HandleHeartsChanged;
    }

    /// <summary>
    /// Called by the Gather button. Attempts a manual harvest via the
    /// Essence provider. UI updates arrive through the events subscribed
    /// above, so no local state is needed.
    /// </summary>
    public void Gather()
    {
        var essence = GM?.Essence;
        if (essence == null) return; // dependency missing

        _ = essence.TryClickHarvest();
    }

    private void HandleEssenceChanged(int newTotal)
        => essenceText.text = $"Essence: {newTotal}";

    private void HandleClicksChanged(int clicksLeft)
        => clicksText.text = $"Clicks Left: {clicksLeft}";

    private void HandleHeartsChanged(CompanionSO companion, int total)
        => heartsText.text = $"Hearts: {total}";
}

