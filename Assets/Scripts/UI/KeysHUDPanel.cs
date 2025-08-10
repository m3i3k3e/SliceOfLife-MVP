using TMPro;
using UnityEngine;

/// <summary>
/// Displays the player's remaining dungeon keys.
/// Hidden until the UnlockBattle upgrade is purchased.
/// </summary>
public class KeysHUDPanel : HUDPanel
{
    [SerializeField] private TextMeshProUGUI keysText; // "Keys: 0/1"

    protected override void OnBind()
    {
        if (Events != null)
            Events.DungeonKeysChanged += HandleKeysChanged;

        // Initialize immediately with current values.
        HandleKeysChanged(GM.DungeonKeysRemaining, GM.DungeonKeysPerDay);
    }

    protected override void OnUnbind()
    {
        if (Events != null)
            Events.DungeonKeysChanged -= HandleKeysChanged;
    }

    private void HandleKeysChanged(int current, int perDay)
    {
        if (!keysText) return;

        // Hide label until the battle system is unlocked.
        if (GM?.Upgrades != null &&
            GM.Upgrades.IsPurchased(UpgradeIds.UnlockBattle))
            keysText.text = $"Keys: {current}/{perDay}";
        else
            keysText.text = "";
    }
}

