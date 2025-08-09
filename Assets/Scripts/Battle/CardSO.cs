using UnityEngine;

/// <summary>
/// Data-only description of a card. For MVP we just map to one of the
/// existing actions; later you can swap this to a polymorphic Execute().
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Card", fileName = "Card")]
public class CardSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // e.g., "attack_basic"
    public string title = "Card";
    [TextArea] public string description;

    [Header("Gameplay")]
    public BattleAction action;       // Which built-in action this card invokes
    public int cost = 0;              // For later (energy), ignored in MVP
}

/// <summary>Actions BattleManager understands.</summary>
public enum BattleAction { Attack, Guard, Mend, ApplyWeak, ApplyVulnerable }
