using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's deck for a single battle.
/// Keeps draw/discard/hand piles and exposes events so UI can stay reactive.
/// Pure C# class so it can be unit-tested outside of Unity.
/// </summary>
[Serializable]
public class DeckManager
{
    // Internal piles. We serialize to make debugging in the Inspector easier.
    [SerializeField] private List<CardSO> _drawPile    = new();
    [SerializeField] private List<CardSO> _discardPile = new();
    [SerializeField] private List<CardSO> _hand        = new();

    /// <summary>Read-only view so outsiders can't mutate the hand directly.</summary>
    public IReadOnlyList<CardSO> Hand => _hand;

    /// <summary>Notify listeners (UI) whenever the hand contents change.</summary>
    public event Action<IReadOnlyList<CardSO>> OnHandChanged;

    /// <summary>
    /// Copies the provided starting deck into the draw pile and shuffles it.
    /// Clears any previous state so this can be reused across battles.
    /// </summary>
    public void BuildAndShuffle(IEnumerable<CardSO> startingDeck)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        _hand.Clear();

        foreach (var c in startingDeck)
        {
            if (c) _drawPile.Add(c); // null-safe because decks are edited in the Inspector
        }

        Shuffle(_drawPile);
        OnHandChanged?.Invoke(_hand); // show empty hand until the first draw
    }

    /// <summary>Fisher–Yates in-place shuffle.</summary>
    private static void Shuffle(List<CardSO> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Draws up to <paramref name="count"/> cards into hand.
    /// If the draw pile is empty, the discard pile is reshuffled once.
    /// </summary>
    public void Draw(int count)
    {
        for (int k = 0; k < count; k++)
        {
            if (_drawPile.Count == 0)
            {
                // No cards left to draw. If the discard pile has anything, recycle it.
                if (_discardPile.Count == 0) break; // nothing to do
                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
            }

            // Take the top card and place it in hand.
            var top = _drawPile[^1];
            _drawPile.RemoveAt(_drawPile.Count - 1);
            _hand.Add(top);
        }

        OnHandChanged?.Invoke(_hand);
    }

    /// <summary>
    /// Moves all cards currently in the hand to the discard pile.
    /// Used at the end of a player turn so the enemy can't see your hand.
    /// </summary>
    public void DiscardHand()
    {
        if (_hand.Count > 0)
        {
            _discardPile.AddRange(_hand);
            _hand.Clear();
            OnHandChanged?.Invoke(_hand);
        }
    }

    /// <summary>
    /// Removes a specific card from hand and adds it to the discard pile.
    /// Called after the card's effect resolves.
    /// </summary>
    public void Discard(CardSO card)
    {
        if (_hand.Remove(card))
        {
            _discardPile.Add(card);
            OnHandChanged?.Invoke(_hand);
        }
    }
}

