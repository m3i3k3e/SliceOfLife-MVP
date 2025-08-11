using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a craftable recipe: which ingredients combine into an output item.
/// ScriptableObject lets designers author data in the editor without code changes.
/// </summary>
[CreateAssetMenu(menuName = "SliceOfLife/Recipe", fileName = "Recipe")]
public class RecipeSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier used for save/load and lookups.")]
    [SerializeField] private string id = "recipe_new";
    /// <summary>Public read-only access to the recipe's identifier.</summary>
    public string Id => id;

    [Header("Ingredients")]
    [Tooltip("Items required to craft this recipe. Order is not important.")]
    [SerializeField] private List<ItemCardSO> ingredients = new();
    /// <summary>Read-only list of input items needed to craft.</summary>
    public IReadOnlyList<ItemCardSO> Ingredients => ingredients;

    [Header("Result")]
    [Tooltip("Item produced when the recipe is crafted.")]
    [SerializeField] private ItemCardSO result;
    /// <summary>The item created by this recipe.</summary>
    public ItemCardSO Result => result;
}
