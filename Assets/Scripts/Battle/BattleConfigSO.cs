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
    // Card tuning now lives on CardEffect assets; attack/guard/mend numbers removed.

    [Header("Enemy")]
    [Tooltip("Reference to the enemy definition used for this battle.")]
    public EnemySO enemy;                 // defines HP, damages, and AI

    [Header("Rewards")]
    public int baseEssenceReward = 20;    // Essence granted on win (before any future multipliers)
    [Tooltip("Recipe unlocked on victory; leave null for none.")]
    public RecipeSO recipeReward;         // optional recipe unlock

    [Header("Flow")]
    public float returnDelay = 3.0f;      // seconds before returning to Start scene after win/lose
    public float autoEndTurnDelay = 3.0f; // NEW: delay before auto end turn

    [Header("Energy")]
    [Min(0)] public int energyPerTurn = 3; // how much you refill at the start of your turn
    [Min(0)] public int maxEnergy = 3; // cap; upgrades can raise this later
    
    [Header("Cards/Hand")]
    [Min(0)] public int handSize = 3;  // how many cards you start each turn with

}
