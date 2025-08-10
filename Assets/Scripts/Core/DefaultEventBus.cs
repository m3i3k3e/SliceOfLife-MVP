using System;
using UnityEngine;

/// <summary>
/// Default concrete implementation of <see cref="IEventBus"/>.
/// Lives as a tiny <see cref="MonoBehaviour"/> so references can be injected
/// via the Unity Inspector, keeping systems decoupled from a static singleton.
/// </summary>
public class DefaultEventBus : MonoBehaviour, IEventBus
{
    /// <inheritdoc />
    public event Action<int> DayChanged;

    /// <inheritdoc />
    public event Action<int, int> DungeonKeysChanged;

    /// <inheritdoc />
    public event Action<bool, string> SleepEligibilityChanged;

    /// <inheritdoc />
    public event Action<IStation> StationUnlocked;

    /// <inheritdoc />
    public event Action<ICompanion> CompanionRecruited;

    /// <inheritdoc />
    public event Action<MinigameResult> MinigameCompleted;

    /// <inheritdoc />
    public event Action<UpgradeSO> UpgradePurchased;

    /// <inheritdoc />
    public void RaiseDayChanged(int day) => DayChanged?.Invoke(day);

    /// <inheritdoc />
    public void RaiseDungeonKeysChanged(int current, int perDay)
        => DungeonKeysChanged?.Invoke(current, perDay);

    /// <inheritdoc />
    public void RaiseSleepEligibilityChanged(bool canSleep, string reason)
        => SleepEligibilityChanged?.Invoke(canSleep, reason);

    /// <inheritdoc />
    public void RaiseStationUnlocked(IStation station)
        => StationUnlocked?.Invoke(station);

    /// <inheritdoc />
    public void RaiseCompanionRecruited(ICompanion companion)
        => CompanionRecruited?.Invoke(companion);

    /// <inheritdoc />
    public void RaiseMinigameCompleted(MinigameResult result)
        => MinigameCompleted?.Invoke(result);

    /// <inheritdoc />
    public void RaiseUpgradePurchased(UpgradeSO upgrade)
        => UpgradePurchased?.Invoke(upgrade);
}
