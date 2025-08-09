using UnityEngine;

/// <summary>
/// All battle tuning knobs live here so you can tweak numbers without touching code.
/// Create in Project via: Create → SliceOfLife → Battle Config
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Battle Config", fileName = "BattleConfig")]
public class BattleConfigSO : ScriptableObject
{
    [Header("Player")]
    public int playerMaxHP = 20;          // starting/max HP
    public int attackDamage = 6;          // Attack card damage
    public int guardBlock = 4;            // Guard adds temporary armor (consumed by next enemy hit)
    public int mendHeal = 5;              // Mend heals this much
    public int mendUses = 1;              // Mend is limited-use (1 for MVP)

    [Header("Enemy")]
    public int enemyMaxHP = 18;
    public int enemyLightDamage = 4;
    public int enemyHeavyDamage = 7;
    public int enemyLeechHeal = 3;        // Enemy heal amount when it uses "Leech"

    [Header("Rewards")]
    public int baseEssenceReward = 20;    // Essence granted on win (before any future multipliers)

    [Header("Flow")]
    public float returnDelay = 1.0f;      // seconds before returning to Start scene after win/lose
}
