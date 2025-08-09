using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Public surface that other systems (UI, battle, etc.) use.
/// Keeping this small keeps everything loosely coupled.
/// </summary>
public interface IUpgradeProvider
{
    bool TryPurchase(UpgradeSO upgrade);         // Attempt to buy an upgrade
    bool IsPurchased(string upgradeId);          // Query: already bought?
    IReadOnlyCollection<string> PurchasedIds { get; }  // For saving
    IReadOnlyList<UpgradeSO> Available { get; }        // Upgrades UI will list

    /// <summary>Fired after a successful purchase so UI can refresh buttons, gates, etc.</summary>
    event Action<UpgradeSO> OnPurchased;
}

/// <summary>
/// Owns the upgrade catalog & purchase logic.
/// - Checks affordability via Essence
/// - Applies the effect
/// - Remembers purchased IDs
/// </summary>
public class UpgradeManager : MonoBehaviour, IUpgradeProvider
{
    [Tooltip("Drag your UpgradeSO assets here in the Inspector.")]
    [SerializeField] private List<UpgradeSO> available = new();

    // Quick lookup of what the player already owns.
    private readonly HashSet<string> _purchased = new();

    public IReadOnlyList<UpgradeSO> Available => available;
    public IReadOnlyCollection<string> PurchasedIds => _purchased;

    public event Action<UpgradeSO> OnPurchased;

    public bool TryPurchase(UpgradeSO upgrade)
    {
        if (upgrade == null) return false;
        if (_purchased.Contains(upgrade.id)) return false; // no double-buy

        var essence = GameManager.Instance.Essence;
        if (!essence.TrySpend(upgrade.cost)) return false; // not enough money

        Apply(upgrade);                 // actually grant the benefit
        _purchased.Add(upgrade.id);     // remember it
        OnPurchased?.Invoke(upgrade);   // let UI react
        SaveSystem.Save(GameManager.Instance);
        return true;
    }

    public bool IsPurchased(string upgradeId) => _purchased.Contains(upgradeId);

    private void Apply(UpgradeSO up)
    {
        var essence = GameManager.Instance.Essence;

        switch (up.effect)
        {
            case UpgradeEffect.IncreaseClick:
                essence.AddEssencePerClick(Mathf.RoundToInt(up.value));
                break;

            case UpgradeEffect.IncreasePassive:
                essence.AddPassivePerSecond(up.value);
                break;

            case UpgradeEffect.UnlockBattle:
                // Nothing to do here. UI will query IsPurchased("unlock_battle") to enable the Dungeon button.
                break;

            case UpgradeEffect.BattleRewardBonus:
                // Placeholder for later when battle rewards exist.
                break;
        }
    }

    // Called by SaveSystem to rehydrate
    public void LoadPurchased(IEnumerable<string> ids)
    {
        _purchased.Clear();
        foreach (var id in ids ?? Enumerable.Empty<string>())
        {
            var up = available.FirstOrDefault(u => u.id == id);
            if (up != null)
            {
                _purchased.Add(id);
                Apply(up); // re-apply effects
            }
        }
    }
}
