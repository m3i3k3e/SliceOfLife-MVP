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

    private readonly List<CardView> _spawned = new();
    private readonly List<CardSO> _currentHand = new(); // last set of cards shown

    private void Awake()
    {
        // Allow designers to wire the BattleManager in the inspector.
        // If they forget, try to locate one up the hierarchy as a safety net.
        if (battle == null)
            battle = GetComponentInParent<BattleManager>();
    }

    /// <summary>
    /// Rebuilds the visible hand from a list of cards.
    /// </summary>
    /// <param name="cards">Cards to display in order.</param>
    public void PopulateHand(IEnumerable<CardSO> cards)
    {
        if (!handParent || !cardViewPrefab)
        {
            Debug.LogError("CardHandUI: Missing handParent or cardViewPrefab", this);
            return;
        }

        Clear(); // remove old views

        _currentHand.Clear();
        foreach (var c in cards)
        {
            _currentHand.Add(c);                      // remember which cards are shown
            var v = Instantiate(cardViewPrefab, handParent); // spawn a view for each card
            v.Bind(c);                                 // CardView wires its own onClick to BattleManager
            _spawned.Add(v);                           // track spawned view for later cleanup
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void Clear()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i]) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
        _currentHand.Clear();
    }

    /// <summary>Toggle cards based on current energy.</summary>
    public void RefreshAffordability(int currentEnergy)
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var view = _spawned[i];
            var card = i < _currentHand.Count ? _currentHand[i] : null;
            if (!view || card == null) continue;
            bool canPlay = card.cost <= currentEnergy;
            view.SetInteractable(canPlay);
        }
    }
}
