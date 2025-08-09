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

    private BattleManager _battle;

    private void Awake()
    {
        // Find the battle brain in our prefab root / scene
        _battle = GetComponentInParent<BattleManager>();
        if (!_battle) _battle = FindFirstObjectByType<BattleManager>();

        if (playButton == null) playButton = GetComponent<Button>(); // allow button on root
        if (playButton != null) playButton.onClick.AddListener(OnClicked);
    }

    public void Bind(CardSO data)
    {
        card = data;
        if (titleText) titleText.text = data ? data.title : "";
        if (descText)  descText.text  = data ? data.description : "";
    }

    private void OnClicked()
    {
        if (card != null && _battle != null)
            _battle.PlayCard(card);
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnClicked);
    }
}
