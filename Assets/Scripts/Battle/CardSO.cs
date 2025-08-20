using UnityEngine;

/// <summary>
/// Data-only description of a card. Each card now references a <see cref="CardEffect"/>
/// ScriptableObject which performs the actual gameplay logic when played.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Card", fileName = "Card")]
public class CardSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // e.g., "attack_basic"
    public string title = "Card";
    [TextArea] public string description;

    [Header("Gameplay")]
    [SerializeField] private CardEffect effect; // effect asset defining this card's behavior
    public CardEffect Effect => effect;
    public int cost = 0;              // For later (energy), ignored in MVP

    // ------------------------------------------------------------------
    // Rarity
    // ------------------------------------------------------------------
    // Cards now have a rarity tier. Designers can tune drop rates or
    // combine rules based on this classification.
    [Header("Progression")]
    [SerializeField] private CardRarity rarity = CardRarity.Common;

    /// <summary>
    /// Rarity tier for this card. Read-only so runtime systems can't mutate
    /// the ScriptableObject asset. DeckManager's CombineCards uses this
    /// property to identify which cards can be fused into higher tiers.
    /// </summary>
    public CardRarity Rarity => rarity;
}

/// <summary>Simple rarity ladder for battle cards.</summary>
public enum CardRarity { Common, Rare, Epic, Legendary }
