using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// HUD wires UI to the Essence/Day systems. It defers setup until GameManager
/// is initialized so we never dereference a null singleton on scene load.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI essenceText;   // e.g., "Essence: 0"
    [SerializeField] private TextMeshProUGUI clicksText;    // e.g., "Clicks Left: 10"
    // If you dragged your buttons’ OnClick in the Inspector (Option A), you
    // don't need Button fields here. If you want to wire in code, add them.

    // Convenience getter NOTE: do not use this in OnEnable until GM is ready.
    private IEssenceProvider Essence => GameManager.Instance.Essence;

    private bool _bound; // track whether we've subscribed to events

    private void OnEnable()
    {
        // Start a tiny coroutine that waits until GameManager exists, then binds.
        // This avoids race conditions where HUD enables before GM.Awake().
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // Wait until the singleton exists AND the essence provider is assigned.
        while (GameManager.Instance == null)
            yield return null;

        while (GameManager.Instance.Essence == null)
            yield return null;

        if (_bound) yield break; // guard against double-binding

        // SUBSCRIBE to currency events so UI updates whenever values change.
        Essence.OnEssenceChanged += HandleEssenceChanged;
        Essence.OnDailyClicksChanged += HandleClicksChanged;
        _bound = true;

        // Initialize labels immediately with current values.
        HandleEssenceChanged(Essence.CurrentEssence);
        HandleClicksChanged(Essence.DailyClicksRemaining);
    }

    private void OnDisable()
    {
        // Safe unsubscribe: only if we bound and singleton still exists.
        if (_bound && GameManager.Instance != null && GameManager.Instance.Essence != null)
        {
            Essence.OnEssenceChanged -= HandleEssenceChanged;
            Essence.OnDailyClicksChanged -= HandleClicksChanged;
        }
        _bound = false;
    }

    // --- Button methods (Inspector OnClick points here) ---

    /// <summary>Called by the "Gather" button (Inspector → OnClick → HUD.Gather).</summary>
    public void Gather()
    {
        // If clicked extremely early, guard against null.
        if (GameManager.Instance == null || GameManager.Instance.Essence == null) return;

        bool gained = Essence.TryClickHarvest();
        // Optional: add feedback if capped (sound/flash). Not required for MVP.
    }

    /// <summary>Called by the "Sleep" button (Inspector → OnClick → HUD.Sleep).</summary>
    public void Sleep()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.AdvanceDay();
    }

    // --- Event handlers that refresh text ---

    private void HandleEssenceChanged(int newTotal)
        => essenceText.text = $"Essence: {newTotal}";

    private void HandleClicksChanged(int clicksLeft)
        => clicksText.text = $"Clicks Left: {clicksLeft}";
}
