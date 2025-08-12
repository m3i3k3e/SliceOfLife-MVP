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
    public event Action<SkillSO> SkillUnlocked;

    /// <inheritdoc />
    public event Action<ResourceSO, int> ResourceChanged;

    /// <inheritdoc />
    public event Action<int> EssenceChanged;

    /// <inheritdoc />
    public event Action InventoryChanged;

    /// <inheritdoc />
    public event Action TaskAdvanced;

    /// <inheritdoc />
    public event Action TaskCompleted;

    /// <inheritdoc />
    public event Action<RecipeSO> RecipeUnlocked;

    /// <inheritdoc />
    public event Action<int> FloorReached;

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
    public void RaiseSkillUnlocked(SkillSO skill)
        => SkillUnlocked?.Invoke(skill);

    /// <inheritdoc />
    public void RaiseResourceChanged(ResourceSO resource, int amount)
        => ResourceChanged?.Invoke(resource, amount);

    /// <inheritdoc />
    public void RaiseEssenceChanged(int amount)
        => EssenceChanged?.Invoke(amount);

    /// <inheritdoc />
    public void RaiseInventoryChanged()
        => InventoryChanged?.Invoke();

    /// <inheritdoc />
    public void RaiseTaskAdvanced()
        => TaskAdvanced?.Invoke();

    /// <inheritdoc />
    public void RaiseTaskCompleted()
        => TaskCompleted?.Invoke();

    /// <inheritdoc />
    public void RaiseRecipeUnlocked(RecipeSO recipe)
        => RecipeUnlocked?.Invoke(recipe);

    /// <inheritdoc />
    public void RaiseFloorReached(int floor)
        => FloorReached?.Invoke(floor);

    /// <inheritdoc />
    public void RaiseMinigameCompleted(MinigameResult result)
        => MinigameCompleted?.Invoke(result);

    /// <inheritdoc />
    public void RaiseUpgradePurchased(UpgradeSO upgrade)
        => UpgradePurchased?.Invoke(upgrade);
}
