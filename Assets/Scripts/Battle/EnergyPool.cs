using System;
using UnityEngine;

/// <summary>
/// Lightweight container for battle energy.
/// Keeps current/max values and raises events whenever either changes.
/// </summary>
[Serializable]
public class EnergyPool
{
    [SerializeField] private int _current;
    [SerializeField] private int _max;

    /// <summary>Current energy available to play cards.</summary>
    public int Current => _current;

    /// <summary>Maximum energy the player can ever have this battle.</summary>
    public int Max => _max;

    /// <summary>UI listens so it can update the "X/Y" label and card affordability.</summary>
    public event Action<int, int> OnEnergyChanged;

    /// <summary>Initialize the pool with a maximum value.</summary>
    public void SetMax(int max)
    {
        _max = Mathf.Max(0, max);
        _current = _max;
        OnEnergyChanged?.Invoke(_current, _max);
    }

    /// <summary>
    /// Refill energy at the start of a turn.
    /// Cap at the maximum so upgrades can permanently raise max energy without overflow.
    /// </summary>
    public void RefillForTurn(int amount)
    {
        _current = Mathf.Min(amount, _max);
        OnEnergyChanged?.Invoke(_current, _max);
    }

    /// <summary>Attempt to spend energy. Returns false if insufficient.</summary>
    public bool TrySpend(int amount)
    {
        if (amount > _current) return false;
        _current -= amount;
        OnEnergyChanged?.Invoke(_current, _max);
        return true;
    }
}

