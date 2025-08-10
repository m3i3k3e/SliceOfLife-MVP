using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a simple "hand" from a list of CardSOs and binds each to a CardView.
/// MVP: static hand (Attack, Guard, Mend) shown on player's turn.
/// Later: plug in draw/discard/energy.
/// </summary>
public class CardHandUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private BattleManager battle;     // auto-wire if left null
    [SerializeField] private Transform handParent;     // where to spawn cards (e.g., a Horizontal/Vertical Layout)
    [SerializeField] private CardView cardViewPrefab;  // the prefab you just made

    [Header("Hand Definition (MVP)")]
    [SerializeField] private List<CardSO> startingHand = new(); // assign Attack/Guard/Mend here

    private readonly List<CardView> _spawned = new();

    private void Awake()
    {
        if (!battle) battle = GetComponentInParent<BattleManager>();
        if (!battle) battle = FindFirstObjectByType<BattleManager>();
    }

    /// <summary>Rebuilds the visible hand from a manager-provided list.</summary>
    public void Show(System.Collections.Generic.IReadOnlyList<CardSO> cards)
    {
        if (!handParent || !cardViewPrefab)
        {
            Debug.LogError("CardHandUI: Missing handParent or cardViewPrefab", this);
            return;
        }

        Clear();

        foreach (var c in cards)
        {
            var v = Instantiate(cardViewPrefab, handParent);
            v.Bind(c);           // CardView wires its own onClick to BattleManager
            _spawned.Add(v);
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void BuildHand()
    {
        if (!handParent || !cardViewPrefab) { Debug.LogError("CardHandUI: Missing handParent or cardViewPrefab", this); return; }
        Clear();

        foreach (var card in startingHand)
        {
            var view = Instantiate(cardViewPrefab, handParent);
            view.Bind(card);
            _spawned.Add(view);
        }
    }

    private void Clear()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i]) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }
        /// <summary>Toggle cards based on current energy.</summary>
    public void RefreshAffordability(int currentEnergy)
    {
        foreach (var v in _spawned)
        {
            if (!v || !v.card) continue;
            bool canPlay = v.card.cost <= currentEnergy;
            v.SetInteractable(canPlay);
        }
    }
}
