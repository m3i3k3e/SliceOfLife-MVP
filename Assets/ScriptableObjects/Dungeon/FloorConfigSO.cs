using UnityEngine;

/// <summary>
/// Defines enemy stats and loot scaling for a 10-floor bracket.
/// Designers create one asset per bracket (1-10, 11-20, ...).
/// </summary>
[CreateAssetMenu(fileName = "FloorConfig", menuName = "SliceOfLife/Dungeon/Floor Config")]
public class FloorConfigSO : ScriptableObject
{
    [Tooltip("First floor covered by this config (inclusive). Should be a multiple of 10.")]
    [SerializeField] private int floorStart = 1;

    [Header("Enemy Stats")]
    [Tooltip("Max HP of enemies in this bracket.")]
    [SerializeField] private int enemyMaxHP = 20;
    [Tooltip("Damage dealt by the enemy's light attack.")]
    [SerializeField] private int enemyLightDamage = 4;
    [Tooltip("Damage dealt by the enemy's heavy attack.")]
    [SerializeField] private int enemyHeavyDamage = 7;

    [Header("Loot Scaling")]
    [Tooltip("Multiplier applied to loot rarity chances on these floors.")]
    [SerializeField] private float rarityMultiplier = 1f;

    /// <summary>Start floor (inclusive) represented by this config.</summary>
    public int FloorStart => floorStart;
    /// <summary>Enemy health used on floors in this bracket.</summary>
    public int EnemyMaxHP => enemyMaxHP;
    /// <summary>Light attack damage for this bracket.</summary>
    public int EnemyLightDamage => enemyLightDamage;
    /// <summary>Heavy attack damage for this bracket.</summary>
    public int EnemyHeavyDamage => enemyHeavyDamage;
    /// <summary>Scaling factor applied to drop rarity.</summary>
    public float RarityMultiplier => rarityMultiplier;
}

