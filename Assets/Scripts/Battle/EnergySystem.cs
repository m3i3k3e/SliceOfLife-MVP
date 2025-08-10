using System;

/// <summary>
/// Tracks current and maximum energy for the player.
/// Emits events when values change so UI can stay reactive.
/// </summary>
public interface IEnergySystem
{
    /// <summary>Fired whenever energy changes (current, max).</summary>
    event Action<int, int> OnEnergyChanged;

    /// <summary>Set up the maximum energy allowed for this battle.</summary>
    void Initialize(int maxEnergy);

    /// <summary>Refill energy at the start of the player's turn.</summary>
    void Refill(int amount);

    /// <summary>Attempt to spend energy. Returns false if insufficient.</summary>
    bool TrySpend(int amount);

    /// <summary>Current energy available for card plays.</summary>
    int Current { get; }

    /// <summary>Maximum energy the player can ever have.</summary>
    int Max { get; }
}

/// <inheritdoc/>
public class EnergySystem : IEnergySystem
{
    private int _current;
    private int _max;

    public event Action<int, int> OnEnergyChanged;

    public int Current => _current;
    public int Max => _max;

    public void Initialize(int maxEnergy)
    {
        _max = Math.Max(0, maxEnergy);
        _current = _max; // start full by default
        OnEnergyChanged?.Invoke(_current, _max);
    }

    public void Refill(int amount)
    {
        // Clamp refill amount to the defined maximum.
        _current = Math.Max(0, Math.Min(amount, _max));
        OnEnergyChanged?.Invoke(_current, _max);
    }

    public bool TrySpend(int amount)
    {
        if (amount > _current)
            return false; // not enough juice

        _current -= amount;
        OnEnergyChanged?.Invoke(_current, _max);
        return true;
    }
}
