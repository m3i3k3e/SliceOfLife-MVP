/// <summary>
/// Central registry for upgrade identifiers.
/// Keeping string IDs in one place avoids magic strings scattered through the codebase.
/// </summary>
public static class UpgradeIds
{
    /// <summary>
    /// Upgrade ID that unlocks the dungeon/battle system.
    /// Using a const ensures compile-time checking wherever referenced.
    /// </summary>
    public const string UnlockBattle = "unlock_battle";
}
