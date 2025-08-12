using UnityEngine;

/// <summary>
/// Thin wrapper exposing the tiny surface area card effects need to modify the battle.
/// Holds a reference back to <see cref="BattleManager"/> but only forwards specific
/// operations so card logic stays decoupled from the manager's internals.
/// </summary>
public class BattleContext
{
    private readonly BattleManager _manager;

    public BattleContext(BattleManager manager) => _manager = manager;

    /// <summary>Deal player-sourced damage to the enemy.</summary>
    public void DamageEnemy(int amount) => _manager.DamageEnemy(amount);

    /// <summary>Add temporary armor to the player.</summary>
    public void AddPlayerArmor(int amount) => _manager.AddPlayerArmor(amount);

    /// <summary>Heal the player if any uses remain.</summary>
    public bool TryHealPlayer(int amount) => _manager.MendPlayer(amount);

    /// <summary>Apply Weak to the enemy.</summary>
    public void ApplyWeakToEnemy(int stacks) => _manager.ApplyWeak(stacks);

    /// <summary>Apply Vulnerable to the enemy.</summary>
    public void ApplyVulnerableToEnemy(int stacks) => _manager.ApplyVulnerable(stacks);
}
