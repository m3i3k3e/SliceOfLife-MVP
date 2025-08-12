using UnityEngine;

/// <summary>
/// Contract for a single upgrade effect. Concrete implementations live as ScriptableObjects
/// so designers can author reusable effect assets in the editor.
/// </summary>
public interface IUpgradeEffect
{
    /// <summary>
    /// Apply persistent state changes when an upgrade is purchased.
    /// One-shot effects (e.g., +click power) should mutate <see cref="GameManager"/> state here.
    /// </summary>
    void Apply(GameManager gm);

    /// <summary>
    /// Optionally modify derived stats that depend on the full set of purchased upgrades.
    /// Default behavior is no-op; override when an effect contributes to values like multipliers.
    /// </summary>
    void ApplyDerived(UpgradeManager um);
}
