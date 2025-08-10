using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one CardSO and forwards click to BattleManager.PlayCard.
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("Data")]
    public CardSO card; // assigned when spawned

    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button playButton;
    [SerializeField] private BattleManager battle; // manually wired; falls back to parent search
    private CanvasGroup _cg;
    private void Awake()
    {
        // Designers can assign BattleManager in prefab; otherwise look upwards once.
        if (battle == null)
            battle = GetComponentInParent<BattleManager>();

        if (playButton == null) playButton = GetComponent<Button>(); // allow button on root
        if (playButton != null) playButton.onClick.AddListener(OnClicked);

        _cg = GetComponent<CanvasGroup>(); // ok if null
        // ...existing Awake...
    }

    /// <summary>Enable/disable interaction + subtle fade for affordability.</summary>
    public void SetInteractable(bool canPlay)
    {
        if (playButton) playButton.interactable = canPlay;
        if (_cg) _cg.alpha = canPlay ? 1f : 0.6f;
    }
    
    /// <summary>
    /// Populate the visual with card data. Simple data-binding pattern keeps
    /// the prefab reusable across different decks.
    /// </summary>
    public void Bind(CardSO data)
    {
        card = data;
        if (titleText) titleText.text = data ? $"{data.title}  [{data.cost}]" : "";
        if (descText)  descText.text  = data ? data.description : "";
    }


    private void OnClicked()
    {
        if (card != null && battle != null)
            battle.PlayCard(card);
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnClicked);
    }
}
