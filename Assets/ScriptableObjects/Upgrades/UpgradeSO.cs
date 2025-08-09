using UnityEngine;

/// <summary>
/// Data-only asset describing a single upgrade. Designers tweak values in the Inspector.
/// Keeping effects as an enum + value is enough for MVP and easy to extend later.
/// </summary>
public enum UpgradeEffect
{
    IncreaseClick,      // +value to EssencePerClick
    IncreasePassive,    // +value to PassivePerSecond
    UnlockBattle,       // Flip a bool gate the UI can read (Enter Dungeon button)
    BattleRewardBonus   // % multiplier for battle rewards (MVP can ignore until battle stub)
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "SliceOfLife/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    [Header("Identity")]
    public string id = "upgrade_id";    // Stable string saved to disk (safer than index)
    public string title = "New Upgrade";
    [TextArea] public string description;

    [Header("Economy")]
    public int cost = 10;               // Essence cost (use a gentle geometric curve later)

    [Header("Effect")]
    public UpgradeEffect effect = UpgradeEffect.IncreaseClick;
    public float value = 1f;            // Interpreted per effect (int cast for click/passive)
}
