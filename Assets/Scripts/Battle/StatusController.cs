using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Types of temporary battle statuses supported.</summary>
public enum StatusType { Weak, Vulnerable }

/// <summary>
/// Central place to track temporary statuses (e.g., Weak, Vulnerable)
/// for both the player and enemy. Provides helper methods for
/// damage modification and turn-based ticking.
/// </summary>
public interface IStatusController
{
    event Action<string> OnPlayerStatusChanged;
    event Action<string> OnEnemyStatusChanged;

    /// <summary>Reset all status counters and emit blank labels.</summary>
    void Initialize();

    /// <summary>Apply <paramref name="turns"/> of a status to the player.</summary>
    void ApplyToPlayer(StatusType type, int turns);

    /// <summary>Apply <paramref name="turns"/> of a status to the enemy.</summary>
    void ApplyToEnemy(StatusType type, int turns);

    /// <summary>Reduce outgoing damage from player → enemy based on statuses.</summary>
    int ModifyPlayerToEnemy(int baseDamage);

    /// <summary>Reduce outgoing damage from enemy → player based on statuses.</summary>
    int ModifyEnemyToPlayer(int baseDamage);

    /// <summary>Countdown player statuses at the end of the player's turn.</summary>
    void TickEndOfPlayerTurn();

    /// <summary>Countdown enemy statuses at the end of the enemy's turn.</summary>
    void TickEndOfEnemyTurn();
}

/// <inheritdoc/>
public class StatusController : IStatusController
{
    private readonly Dictionary<StatusType, int> _player = new();
    private readonly Dictionary<StatusType, int> _enemy = new();

    public event Action<string> OnPlayerStatusChanged;
    public event Action<string> OnEnemyStatusChanged;

    public void Initialize()
    {
        _player.Clear();
        _enemy.Clear();
        PushStatusLabels();
    }

    public void ApplyToPlayer(StatusType type, int turns)
    {
        if (turns <= 0) return;
        _player.TryGetValue(type, out int existing);
        _player[type] = existing + turns; // stack
        PushStatusLabels();
    }

    public void ApplyToEnemy(StatusType type, int turns)
    {
        if (turns <= 0) return;
        _enemy.TryGetValue(type, out int existing);
        _enemy[type] = existing + turns;
        PushStatusLabels();
    }

    public int ModifyPlayerToEnemy(int baseDamage)
    {
        float d = baseDamage;
        if (_player.TryGetValue(StatusType.Weak, out int pw) && pw > 0)
            d *= 0.75f; // Weak reduces damage by 25%
        if (_enemy.TryGetValue(StatusType.Vulnerable, out int ev) && ev > 0)
            d *= 1.5f; // Enemy vulnerable increases damage by 50%
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    public int ModifyEnemyToPlayer(int baseDamage)
    {
        float d = baseDamage;
        if (_enemy.TryGetValue(StatusType.Weak, out int ew) && ew > 0)
            d *= 0.75f;
        if (_player.TryGetValue(StatusType.Vulnerable, out int pv) && pv > 0)
            d *= 1.5f;
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    public void TickEndOfPlayerTurn()
    {
        Decrement(_player);
        PushStatusLabels();
    }

    public void TickEndOfEnemyTurn()
    {
        Decrement(_enemy);
        PushStatusLabels();
    }

    private void Decrement(Dictionary<StatusType, int> dict)
    {
        // Iterate through keys, decreasing counters where present.
        var keys = new List<StatusType>(dict.Keys);
        foreach (var k in keys)
        {
            dict[k] = Math.Max(0, dict[k] - 1);
            if (dict[k] == 0) dict.Remove(k);
        }
    }

    private void PushStatusLabels()
    {
        OnPlayerStatusChanged?.Invoke(LabelFor(_player));
        OnEnemyStatusChanged?.Invoke(LabelFor(_enemy));
    }

    private static string LabelFor(Dictionary<StatusType, int> dict)
    {
        List<string> parts = new();
        if (dict.TryGetValue(StatusType.Weak, out int weak) && weak > 0)
            parts.Add($"Weak ({weak})");
        if (dict.TryGetValue(StatusType.Vulnerable, out int vuln) && vuln > 0)
            parts.Add($"Vulnerable ({vuln})");
        return string.Join(", ", parts);
    }
}
