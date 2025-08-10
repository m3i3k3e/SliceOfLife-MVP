/// <summary>
/// Temporary helper so early crafting recipes can deposit their outputs
/// into the central inventory. Replace once a full recipe system exists.
/// </summary>
public static class RecipeRewardStub
{
    /// <summary>Grant crafted items to the player.</summary>
    public static void Grant(ItemCardSO item, int quantity)
    {
        GameManager.Instance?.Inventory?.TryAdd(item, quantity);
    }
}
