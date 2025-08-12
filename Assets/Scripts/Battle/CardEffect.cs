using UnityEngine;

/// <summary>
/// Base class for card effects. Each concrete effect holds tuning values and knows how to
/// modify the battle via <see cref="Execute"/>.
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    [Header("Tuning")]
    [SerializeField] private int damage;        // how much damage to deal to the enemy
    [SerializeField] private int block;         // how much armor to grant the player
    [SerializeField] private int heal;          // how much HP to restore to the player
    [Tooltip("-1 = unlimited uses. Any positive value is consumed on each heal.")]
    [SerializeField] private int maxUses = -1;  // limited use count for heal-type cards
    [SerializeField] private int weak;          // stacks of Weak to apply
    [SerializeField] private int vulnerable;    // stacks of Vulnerable to apply

    /// <summary>Amount of enemy damage this effect should inflict.</summary>
    public int Damage => damage;
    /// <summary>Amount of armor this effect should grant.</summary>
    public int Block => block;
    /// <summary>Amount of HP this effect should heal.</summary>
    public int Heal => heal;
    /// <summary>How many times the heal can be used (-1 = unlimited).</summary>
    public int MaxUses => maxUses;
    /// <summary>How many stacks of Weak to apply.</summary>
    public int Weak => weak;
    /// <summary>How many stacks of Vulnerable to apply.</summary>
    public int Vulnerable => vulnerable;

    /// <summary>
    /// Apply the effect to the provided battle context.
    /// </summary>
    public abstract void Execute(BattleContext ctx);
}
