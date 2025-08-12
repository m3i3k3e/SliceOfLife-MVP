using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all known crafting recipes and which ones the player has unlocked.
/// Currently recipes reset each session and do not persist.
/// </summary>
public class RecipeManager : MonoBehaviour
{
    [Header("Catalog")]
    [Tooltip("List of every recipe available in the game. Referenced by Id.")]
    [SerializeField] private List<RecipeSO> allRecipes = new();

    // Quick lookup dictionary built on Awake for efficient id → recipe resolution.
    private readonly Dictionary<string, RecipeSO> _recipeLookup = new();

    // Store unlocked recipes by their IDs to keep save data lightweight.
    private readonly HashSet<string> _unlocked = new();

    /// <summary>Fired whenever a new recipe becomes available.</summary>
    public event Action<RecipeSO> OnRecipeUnlocked;

    private void Awake()
    {
        // Populate the lookup dictionary defensively, skipping null or duplicate entries.
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && !_recipeLookup.ContainsKey(recipe.Id))
                _recipeLookup.Add(recipe.Id, recipe);
        }
    }

    /// <summary>Check if the player already knows a recipe.</summary>
    public bool IsUnlocked(string id) => _unlocked.Contains(id);

    /// <summary>
    /// Unlock a recipe by its identifier.
    /// Returns true only if the recipe existed and was newly unlocked.
    /// </summary>
    public bool UnlockRecipe(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;     // guard against bad input
        if (_unlocked.Contains(id)) return false;       // already unlocked
        if (!_recipeLookup.TryGetValue(id, out var recipe)) return false; // unknown id

        _unlocked.Add(id);                             // mutate state
        OnRecipeUnlocked?.Invoke(recipe);              // notify listeners
        return true;
    }

    // Persistence removed for now; recipes reset each session.
}
