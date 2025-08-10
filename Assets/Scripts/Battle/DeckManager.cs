using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles draw/discard/hand logic for a simple deck.
/// Separated from BattleManager so it can be tested in isolation
/// and replaced later with a richer deck-building system.
/// </summary>
public interface IDeckManager
{
    /// <summary>Raised whenever the current hand contents change.</summary>
    event Action<IReadOnlyList<CardSO>> OnHandChanged;

    /// <summary>Copy and shuffle the starting deck to prepare for battle.</summary>
    void Initialize(IEnumerable<CardSO> startingDeck);

    /// <summary>Draw <paramref name="count"/> cards into the hand.</summary>
    void Draw(int count);

    /// <summary>Move every card in hand to the discard pile.</summary>
    void DiscardHand();

    /// <summary>Check if the given card is currently in hand.</summary>
    bool Contains(CardSO card);

    /// <summary>Remove a specific card from the hand (e.g., after playing).</summary>
    void RemoveFromHand(CardSO card);

    /// <summary>Read-only access to the current hand for iteration.</summary>
    IReadOnlyList<CardSO> Hand { get; }
}

/// <inheritdoc/>
public class DeckManager : IDeckManager
{
    // Runtime piles. Using lists keeps operations explicit for newcomers.
    private readonly List<CardSO> _drawPile = new();
    private readonly List<CardSO> _discardPile = new();
    private readonly List<CardSO> _hand = new();

    public event Action<IReadOnlyList<CardSO>> OnHandChanged;

    public IReadOnlyList<CardSO> Hand => _hand;

    public void Initialize(IEnumerable<CardSO> startingDeck)
    {
        // Clear any prior state so this object can be reused.
        _drawPile.Clear();
        _discardPile.Clear();
        _hand.Clear();

        // Copy provided cards, ignoring nulls for safety.
        foreach (var c in startingDeck)
        {
            if (c) _drawPile.Add(c);
        }

        Shuffle(_drawPile);
        OnHandChanged?.Invoke(_hand); // emits an empty hand so UI can clear.
    }

    public void Draw(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
            {
                // When the draw pile empties, recycle the discard once.
                if (_discardPile.Count == 0) break; // nothing left to draw
                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
            }

            var top = _drawPile[^1]; // last element is the top of the pile
            _drawPile.RemoveAt(_drawPile.Count - 1);
            _hand.Add(top);
        }

        OnHandChanged?.Invoke(_hand);
    }

    public void DiscardHand()
    {
        if (_hand.Count > 0)
        {
            _discardPile.AddRange(_hand);
            _hand.Clear();
            OnHandChanged?.Invoke(_hand);
        }
    }

    public bool Contains(CardSO card) => _hand.Contains(card);

    public void RemoveFromHand(CardSO card)
    {
        if (_hand.Remove(card))
        {
            _discardPile.Add(card);
            OnHandChanged?.Invoke(_hand);
        }
    }

    /// <summary>Fisher–Yates shuffle keeps distribution uniform.</summary>
    private static void Shuffle(List<CardSO> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
