/*
 * UpgradeManager.cs
 * Role: Maintains upgrade catalog, purchase flow, and derived stats like reward multipliers.
 * Expansion: New upgrade types are added by authoring new IUpgradeEffect implementations.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Public surface other systems read/use (UI, battle, GameManager).
/// Keeping an interface helps you swap implementations or mock in tests.
/// </summary>
public interface IUpgradeProvider
{
    /// <summary>Attempt to buy an upgrade; returns true on success.</summary>
    bool TryPurchase(UpgradeSO upgrade);
    /// <summary>Query whether an upgrade ID has already been purchased.</summary>
    bool IsPurchased(string upgradeId);
    /// <summary>IDs of purchased upgrades (for saving).</summary>
    IReadOnlyCollection<string> PurchasedIds { get; }
    /// <summary>Catalog of all upgrades available to buy.</summary>
    IReadOnlyList<UpgradeSO> Available { get; }

    /// <summary>Multiplicative bonus for battle win rewards (1.0 = no change).</summary>
    float RewardMultiplier { get; }

    /// <summary>Fired after a successful purchase so UI can refresh buttons, gates, etc.</summary>
    event Action<UpgradeSO> OnPurchased;
}

/// <summary>
/// Owns the upgrade catalog & purchase logic.
/// - "One-shot" effects (e.g., +click power, +passive) immediately mutate game state
///   and must be re-applied on load.
/// - "Derived" effects (e.g., multipliers) are recomputed from PurchasedIds so they
///   never double-apply.
/// </summary>
public class UpgradeManager : MonoBehaviour, IUpgradeProvider, ISaveParticipant
{
    [Tooltip("Drag your UpgradeSO assets here in the Inspector.")]
    [SerializeField] private List<UpgradeSO> available = new();

    // Tracks what the player owns (IDs only, safe to save).
    private readonly HashSet<string> _purchased = new();

    // ---- IUpgradeProvider surface ----
    public IReadOnlyList<UpgradeSO> Available => available;
    public IReadOnlyCollection<string> PurchasedIds => _purchased;

    /// <summary>
    /// Multiplicative battle reward multiplier (1.0 = no change).
    /// Computed from purchased upgrades in RecalculateDerivedStats().
    /// </summary>
    public float RewardMultiplier { get; private set; } = 1f;

    public event Action<UpgradeSO> OnPurchased;

    // Consumers subscribe to OnPurchased directly; no legacy GameEvents bridge.

    // ---- Purchase flow ----
    /// <summary>
    /// Attempt to buy an upgrade, applying one-shot effects and persisting ownership.
    /// </summary>
    public bool TryPurchase(UpgradeSO upgrade)
    {
        if (upgrade == null) return false;
        if (_purchased.Contains(upgrade.id)) return false; // already bought

        var essence = GameManager.Instance.Essence;
        if (!essence.TrySpend(upgrade.cost)) return false; // can't afford

        // 1) Apply one-shot effects immediately (persist via SaveSystem downstream)
        ApplyOneShot(upgrade);

        // 2) Remember ownership
        _purchased.Add(upgrade.id);

        // 3) Update derived stats that depend on the purchased set (e.g., multipliers)
        ApplyDerivedEffect(upgrade); // incremental update for snappy UI

        OnPurchased?.Invoke(upgrade);
        // Persist new ownership through the scheduler to avoid write thrash.
        SaveScheduler.RequestSave(GameManager.Instance);
        return true;
    }

    /// <summary>
    /// Check whether the given upgrade ID has been purchased.
    /// </summary>
    public bool IsPurchased(string upgradeId) => _purchased.Contains(upgradeId);

    // ---- Effect application helpers ----

    /// <summary>
    /// Apply effects that alter persistent numbers once (e.g., click power, passive rate).
    /// These must be re-applied on load to reconstruct runtime state.
    /// </summary>
    private void ApplyOneShot(UpgradeSO up)
    {
        // Delegate the behavior to the effect asset. Null-check keeps faulty data from crashing.
        up?.Effect?.Apply(GameManager.Instance);
    }

    /// <summary>
    /// Apply effects that should be *derived* from the purchased set (not baked),
    /// such as multiplicative reward bonuses.
    /// </summary>
    private void ApplyDerivedEffect(UpgradeSO up)
    {
        // Effects that influence derived stats override ApplyDerived. Others no-op by default.
        up?.Effect?.ApplyDerived(this);
    }

    /// <summary>
    /// Rebuild all derived stats based on PurchasedIds.
    /// Call after loading a save (after restoring PurchasedIds).
    /// </summary>
    public void RecalculateDerivedStats()
    {
        RewardMultiplier = 1f; // reset to neutral

        foreach (var id in _purchased)
        {
            var so = available.FirstOrDefault(u => u != null && u.id == id);
            so?.Effect?.ApplyDerived(this);
        }
    }

    // ---- Save/Load integration via SaveModelV2 ----

    /// <summary>
    /// Called internally to rehydrate from disk.
    /// Rebuilds one-shot effects first, then recomputes derived stats.
    /// </summary>
    public void LoadPurchased(IEnumerable<string> ids)
    {
        _purchased.Clear();

        // Restore ownership & reapply one-shot effects so live numbers are correct.
        foreach (var id in ids ?? Enumerable.Empty<string>())
        {
            var up = available.FirstOrDefault(u => u != null && u.id == id);
            if (up == null) continue;

            _purchased.Add(id);
            ApplyOneShot(up); // rebuild persistent numbers (click power, passive, etc.)
        }

        // Then recompute all derived stats from the set (so multipliers are correct, with no double-apply).
        RecalculateDerivedStats();
    }

    /// <summary>
    /// Rehydrate upgrade state from the aggregate save model.
    /// </summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        if (data == null) return;
        LoadPurchased(data.purchasedUpgradeIds);
    }

    // ---- ISaveParticipant implementation ----

    /// <summary>
    /// Persist purchased upgrade identifiers into the save model.
    /// </summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        model.purchasedUpgradeIds.AddRange(_purchased);
        model.dungeonUnlocked = _purchased.Contains(UpgradeIds.UnlockBattle);
    }

    /// <summary>
    /// Restore purchased upgrades from the save model and rebuild effects.
    /// </summary>
    public void Apply(SaveModelV2 model)
    {
        ApplyLoadedState(model);
    }
}
