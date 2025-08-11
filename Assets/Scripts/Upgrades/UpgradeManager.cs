/*
 * UpgradeManager.cs
 * Role: Maintains upgrade catalog, purchase flow, and derived stats like reward multipliers.
 * Expansion: Add new UpgradeEffect cases in ApplyOneShot/ApplyDerivedEffect to support new upgrades.
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
public class UpgradeManager : MonoBehaviour, IUpgradeProvider, ISaveable
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
        SaveSystem.Save(GameManager.Instance);
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
        var essence = GameManager.Instance.Essence;

        switch (up.effect)
        {
            case UpgradeEffect.IncreaseClick:
                // Treat value as a flat increase to essence-per-click
                essence.AddEssencePerClick(Mathf.RoundToInt(up.value));
                break;

            case UpgradeEffect.IncreasePassive:
                // Treat value as flat passive essence per second
                essence.AddPassivePerSecond(up.value);
                break;

            case UpgradeEffect.UnlockBattle:
                // No numeric state; UI checks IsPurchased(UpgradeIds.UnlockBattle) to enable the button
                break;

            case UpgradeEffect.BattleRewardBonus:
                // IMPORTANT: Do NOT bake multipliers into saved numbers here.
                // Handled in ApplyDerivedEffect/RecalculateDerivedStats.
                break;
            // Add new UpgradeEffect cases here by implementing corresponding logic.
        }
    }

    /// <summary>
    /// Apply effects that should be *derived* from the purchased set (not baked),
    /// such as multiplicative reward bonuses.
    /// </summary>
    private void ApplyDerivedEffect(UpgradeSO up)
    {
        switch (up.effect)
        {
            case UpgradeEffect.BattleRewardBonus:
                // Interpret UpgradeSO.value as a percentage (25 => +25% => x1.25)
                // Stacks multiplicatively with other bonuses.
                RewardMultiplier *= 1f + (up.value / 100f);
                break;
            // Add new UpgradeEffect cases here for additional derived stats.
        }
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
            if (so != null)
                ApplyDerivedEffect(so);
        }
    }

    // ---- Save/Load integration ----
    
    // ---- ISaveable implementation ----

    /// <summary>Key used for the upgrades section in the save file.</summary>
    public string SaveKey => "Upgrades";

    /// <summary>
    /// Extract the minimal persistence data for upgrades.
    /// </summary>
    public object ToData()
    {
        // Copy IDs to a list so the DTO is decoupled from our HashSet.
        return new SaveData
        {
            purchasedUpgradeIds = _purchased.ToList()
        };
    }

    /// <summary>
    /// Restore upgrade ownership and rebuild runtime effects.
    /// </summary>
    public void LoadFrom(object data)
    {
        var d = data as SaveData;
        LoadPurchased(d?.purchasedUpgradeIds);
    }

    /// <summary>Serializable list of purchased upgrade IDs.</summary>
    [Serializable]
    public class SaveData
    {
        public List<string> purchasedUpgradeIds = new();
    }

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
}
