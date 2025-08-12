using UnityEngine;

/// <summary>
/// Data defining a single enemy type: stats, intent weights, and AI strategy.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Enemy", fileName = "Enemy")]
public class EnemySO : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 18;            // starting/max HP for this enemy
    [SerializeField] private int lightDamage = 4;       // light attack damage
    [SerializeField] private int heavyDamage = 7;       // heavy attack damage
    [SerializeField] private int leechHeal = 3;         // heal amount when using Leech

    [Header("Intent Weights")]
    [SerializeField, Min(0)] private int lightWeight = 60; // chance weight for light attack
    [SerializeField, Min(0)] private int heavyWeight = 30; // chance weight for heavy attack
    [SerializeField, Min(0)] private int leechWeight = 10; // chance weight for leech heal

    [Header("AI Strategy")]
    [Tooltip("ScriptableObject implementing IEnemyAI for this enemy.")]
    [SerializeField] private EnemyAI ai;

    // Public read-only properties expose the data while keeping fields private.
    public int MaxHP => maxHP;
    public int LightDamage => lightDamage;
    public int HeavyDamage => heavyDamage;
    public int LeechHeal => leechHeal;

    public int LightWeight => lightWeight;
    public int HeavyWeight => heavyWeight;
    public int LeechWeight => leechWeight;

    public IEnemyAI AIStrategy => ai;
}
