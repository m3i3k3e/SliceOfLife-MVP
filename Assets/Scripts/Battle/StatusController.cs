using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks temporary combat statuses (Weak/Vulnerable) for both the player and enemy.
/// Responsible for ticking durations, formatting labels, and modifying damage.
/// </summary>
[Serializable]
public class StatusController
{
    // Duration counters for each side. "Turns" tick down at the end of that unit's own turn.
    private int _playerWeak, _playerVuln;
    private int _enemyWeak, _enemyVuln;

    /// <summary>UI consumes these to show status chips or labels.</summary>
    public event Action<string> OnPlayerStatusChanged;
    public event Action<string> OnEnemyStatusChanged;

    /// <summary>Apply Weak to the enemy for a number of turns.</summary>
    public void ApplyWeakToEnemy(int turns)
    {
        _enemyWeak += Mathf.Max(0, turns);
        PushLabels();
    }

    /// <summary>Apply Vulnerable to the enemy for a number of turns.</summary>
    public void ApplyVulnerableToEnemy(int turns)
    {
        _enemyVuln += Mathf.Max(0, turns);
        PushLabels();
    }

    /// <summary>Apply Weak to the player.</summary>
    public void ApplyWeakToPlayer(int turns)
    {
        _playerWeak += Mathf.Max(0, turns);
        PushLabels();
    }

    /// <summary>Apply Vulnerable to the player.</summary>
    public void ApplyVulnerableToPlayer(int turns)
    {
        _playerVuln += Mathf.Max(0, turns);
        PushLabels();
    }

    /// <summary>Invoke events with the current label strings.</summary>
    public void PushLabels() => PushLabelsInternal();

    private void PushLabelsInternal()
    {
        OnPlayerStatusChanged?.Invoke(BuildLabel(_playerWeak, _playerVuln));
        OnEnemyStatusChanged?.Invoke(BuildLabel(_enemyWeak, _enemyVuln));
    }

    private static string BuildLabel(int weak, int vuln)
    {
        List<string> parts = new();
        if (weak > 0) parts.Add($"Weak ({weak})");
        if (vuln > 0) parts.Add($"Vulnerable ({vuln})");
        return string.Join(", ", parts);
    }

    /// <summary>Outgoing damage from the player → affected by player Weak and enemy Vulnerable.</summary>
    public int ModPlayerToEnemyDamage(int baseDmg)
    {
        float d = baseDmg;
        if (_playerWeak > 0) d *= 0.75f; // Weak: -25% damage
        if (_enemyVuln > 0) d *= 1.50f; // Vulnerable: +50% damage taken
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    /// <summary>Outgoing damage from the enemy → affected by enemy Weak and player Vulnerable.</summary>
    public int ModEnemyToPlayerDamage(int baseDmg)
    {
        float d = baseDmg;
        if (_enemyWeak > 0) d *= 0.75f;
        if (_playerVuln > 0) d *= 1.50f;
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    /// <summary>
    /// Called when the player ends their turn. Decrements their own debuffs.
    /// </summary>
    public void TickEndOfPlayerTurn()
    {
        if (_playerWeak > 0) _playerWeak--;
        if (_playerVuln > 0) _playerVuln--;
        PushLabels();
    }

    /// <summary>
    /// Called when the enemy ends their turn. Decrements their own debuffs.
    /// </summary>
    public void TickEndOfEnemyTurn()
    {
        if (_enemyWeak > 0) _enemyWeak--;
        if (_enemyVuln > 0) _enemyVuln--;
        PushLabels();
    }
}

