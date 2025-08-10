using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// HUD wires UI to the Essence/Day systems. It defers setup until GameManager
/// is initialized so we never dereference a null singleton on scene load.
/// Also listens to the Sleep gate so the Sleep button shows why it's disabled.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI essenceText;   // e.g., "Essence: 0"
    [SerializeField] private TextMeshProUGUI clicksText;    // e.g., "Clicks Left: 10"
    [SerializeField] private TextMeshProUGUI keysText; // “Keys: 0/1”

    [Header("Sleep (optional)")]
    [Tooltip("Sleep button will auto-enable/disable if assigned.")]
    [SerializeField] private Button sleepButton;

    [Tooltip("Small label under Sleep to explain why it's disabled.")]
    [SerializeField] private TextMeshProUGUI sleepReasonText;

    [Header("Dependencies")]
    [Tooltip("Reference to a GameManager instance implementing IGameManager.")]
    [SerializeField] private MonoBehaviour gameManagerSource;

    // Cast the serialized reference to the interface so callers stay decoupled.
    private IGameManager GM => gameManagerSource as IGameManager;

    // Convenience getter NOTE: do not use this in OnEnable until GM is ready.
    private IEssenceProvider Essence => GM?.Essence;

    private bool _bound; // track whether we've subscribed to events

    private void OnEnable()
    {
        // Wait for GM singletons to exist, then bind.
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // Wait until the singleton exists AND the essence provider is assigned.
        while (GM == null) yield return null;                // wait for dependency
        while (GM.Essence == null) yield return null;        // wait for Essence system

        if (_bound) yield break; // guard against double-binding

        // Subscribe to currency events so UI updates whenever values change.
        Essence.OnEssenceChanged += HandleEssenceChanged;
        Essence.OnDailyClicksChanged += HandleClicksChanged;

        // Subscribe to global events instead of directly referencing GameManager.
        GameEvents.DungeonKeysChanged += HandleKeysChanged;
        // Initialize key label immediately.
        HandleKeysChanged(GM.DungeonKeysRemaining, GM.DungeonKeysPerDay);

        // Subscribe to sleep-gate state (optional but recommended).
        GameEvents.SleepEligibilityChanged += HandleSleepEligibility;
        // Pull initial state for Sleep via GameManager so UI reflects current gate.
        GM.ReevaluateSleepGate();

        _bound = true;

        

        // Initialize labels immediately with current values.
        HandleEssenceChanged(Essence.CurrentEssence);
        HandleClicksChanged(Essence.DailyClicksRemaining);
    }

    private void OnDisable()
    {
        if (_bound && GM != null)
        {
            if (GM.Essence != null)
            {
                Essence.OnEssenceChanged -= HandleEssenceChanged;
                Essence.OnDailyClicksChanged -= HandleClicksChanged;
            }
            GameEvents.SleepEligibilityChanged -= HandleSleepEligibility;
            GameEvents.DungeonKeysChanged -= HandleKeysChanged;

        }
        _bound = false;

    }
    private void HandleKeysChanged(int current, int perDay)
{
    if (!keysText) return;
    // Hide before unlock; show once you have the system
    if (GM?.Upgrades != null &&
        GM.Upgrades.IsPurchased(UpgradeIds.UnlockBattle))
        keysText.text = $"Keys: {current}/{perDay}";
    else
        keysText.text = "";
}

    // --- Button methods (Inspector OnClick points here) ---

    /// <summary>Called by the "Gather" button (Inspector → OnClick → HUD.Gather).</summary>
    public void Gather()
    {
        if (Essence == null) return;
        _ = Essence.TryClickHarvest(); // if capped, it returns false; UI state will update via events
    }

    /// <summary>Called by the "Sleep" button (Inspector → OnClick → HUD.Sleep).</summary>
    public void Sleep()
    {
        var gm = GM;
        if (gm == null) return;            // dependency missing

        // Respect the daily gate; if false, the handler below already shows why.
        if (!gm.TrySleep())
        {
            // Optional: flash sleepReasonText or shake the button here.
            return;
        }
        // Success path: Day advanced; clicks reset; labels update via events.
    }

    // --- Event handlers that refresh text ---

    private void HandleEssenceChanged(int newTotal)
        => essenceText.text = $"Essence: {newTotal}";

    private void HandleClicksChanged(int clicksLeft)
        => clicksText.text = $"Clicks Left: {clicksLeft}";

    /// <summary>
    /// Sleep gate feedback: enable/disable the Sleep button and show a short reason.
    /// </summary>
    private void HandleSleepEligibility(bool canSleep, string reason)
    {
        if (sleepButton) sleepButton.interactable = canSleep;
        if (sleepReasonText) sleepReasonText.text = canSleep ? "" : reason;
    }
}
