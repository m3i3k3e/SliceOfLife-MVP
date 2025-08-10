using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the Sleep button's interactable state and explanatory label.
/// </summary>
public class SleepHUDPanel : HUDPanel
{
    [Header("UI References")]
    [SerializeField] private Button sleepButton;              // optional
    [SerializeField] private TextMeshProUGUI sleepReasonText; // optional

    protected override void OnBind()
    {
        if (Events != null)
            Events.SleepEligibilityChanged += HandleSleepEligibility;

        // Ask the GameManager to broadcast the current sleep gate so we start
        // with the correct state.
        GM?.ReevaluateSleepGate();
    }

    protected override void OnUnbind()
    {
        if (Events != null)
            Events.SleepEligibilityChanged -= HandleSleepEligibility;
    }

    /// <summary>Called by the Sleep button.</summary>
    public void Sleep()
    {
        var gm = GM;
        if (gm == null) return; // dependency missing

        // Respect the daily gate. If TrySleep fails the event handler already
        // shows the reason to the player.
        if (!gm.TrySleep())
        {
            return; // optional: add feedback here later
        }
    }

    private void HandleSleepEligibility(bool canSleep, string reason)
    {
        if (sleepButton) sleepButton.interactable = canSleep;
        if (sleepReasonText) sleepReasonText.text = canSleep ? "" : reason;
    }
}

