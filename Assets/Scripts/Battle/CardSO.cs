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
}
