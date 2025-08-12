using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all known crafting recipes and which ones the player has unlocked.
/// Currently recipes reset each session and do not persist.
/// </summary>
public class RecipeManager : MonoBehaviour, ISaveParticipant
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

    /// <summary>
    /// Refresh the lookup cache whenever the list changes in the inspector.
    /// This keeps edits made during iterative content creation reflected
    /// immediately without having to enter Play Mode first.
    /// </summary>
    private void OnValidate()
    {
        _recipeLookup.Clear();
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && !_recipeLookup.ContainsKey(recipe.Id))
                _recipeLookup.Add(recipe.Id, recipe); // skip nulls and duplicate Ids
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
        // Schedule persistence so newly unlocked recipes survive restarts.
        SaveScheduler.RequestSave(GameManager.Instance);
        return true;
    }

    // ---- Save/Load via SaveModelV2 ----

    /// <summary>Restore unlocked recipe IDs from the save model.</summary>
    public void ApplyLoadedState(SaveModelV2 data)
    {
        _unlocked.Clear();
        if (data == null) return;

        foreach (var id in data.unlockedRecipeIds)
        {
            if (_recipeLookup.TryGetValue(id, out var recipe))
            {
                _unlocked.Add(id);
                OnRecipeUnlocked?.Invoke(recipe); // inform listeners for UI rebuild
            }
        }
    }

    /// <summary>Write unlocked recipe IDs into the save model.</summary>
    public void Capture(SaveModelV2 model)
    {
        if (model == null) return;
        foreach (var id in _unlocked)
            model.unlockedRecipeIds.Add(id);
    }

    /// <summary>Load unlocked recipe IDs from the save model.</summary>
    public void Apply(SaveModelV2 model)
    {
        ApplyLoadedState(model);
    }
}
