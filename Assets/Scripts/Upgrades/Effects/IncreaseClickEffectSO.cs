using UnityEngine;

/// <summary>
/// Flat bonus to Essence gained per manual click.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/UpgradeEffects/IncreaseClick")]
public class IncreaseClickEffectSO : UpgradeEffectSO
{
    [SerializeField] private int amount = 1; // amount of essence per click to add

    /// <inheritdoc />
    public override void Apply(GameManager gm)
    {
        // Defensive: ensure GameManager and EssenceManager exist before applying
        if (gm?.Essence == null) return;
        gm.Essence.AddEssencePerClick(amount);
    }
}
