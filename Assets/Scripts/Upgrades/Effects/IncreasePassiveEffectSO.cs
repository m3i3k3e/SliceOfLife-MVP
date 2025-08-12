using UnityEngine;

/// <summary>
/// Flat bonus to passive Essence generation per second.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/UpgradeEffects/IncreasePassive")]
public class IncreasePassiveEffectSO : UpgradeEffectSO
{
    [SerializeField] private float amount = 1f; // essence per second to add

    /// <inheritdoc />
    public override void Apply(GameManager gm)
    {
        if (gm?.Essence == null) return;
        gm.Essence.AddPassivePerSecond(amount);
    }
}
