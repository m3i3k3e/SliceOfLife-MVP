using UnityEngine;

/// <summary>
/// Base class for all upgrade effect assets.
/// Provides a default no-op implementation for derived stats so most effects
/// only need to override <see cref="Apply(GameManager)"/>.
/// </summary>
public abstract class UpgradeEffectSO : ScriptableObject, IUpgradeEffect
{
    /// <summary>
    /// Execute the effect's one-shot behavior when the upgrade is purchased.
    /// </summary>
    public abstract void Apply(GameManager gm);

    /// <summary>
    /// Override to adjust derived stats (e.g., multipliers) that depend on the purchased set.
    /// Default implementation does nothing.
    /// </summary>
    public virtual void ApplyDerived(UpgradeManager um) { }
}
