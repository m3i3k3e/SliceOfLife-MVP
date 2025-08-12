using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the player's unlocked skills and enforces prerequisite chains.
/// Lives as a MonoBehaviour so GameManager and UI can reference it directly.
/// Currently does not persist unlocked IDs.
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    [Header("Catalog")]
    [Tooltip("All skills available in the game. Order is irrelevant; referenced by Id.")]
    [SerializeField] private List<SkillSO> allSkills = new();

    // Quick lookup table built once on Awake for O(1) id access.
    private readonly Dictionary<string, SkillSO> _skillLookup = new();

    // Store unlocked skills by their IDs so the save file stays lightweight.
    private readonly HashSet<string> _unlocked = new();

    /// <summary>
    /// Fired after a skill is newly unlocked. Consumers (UI, stations) can react.
    /// </summary>
    public event Action<SkillSO> OnSkillUnlocked;

    private void Awake()
    {
        // Populate dictionary while guarding against nulls/dupes.
        foreach (var skill in allSkills)
        {
            if (skill != null && !_skillLookup.ContainsKey(skill.Id))
                _skillLookup.Add(skill.Id, skill);
        }
    }

    /// <summary>Public view of unlocked IDs for save/load or debugging.</summary>
    public IReadOnlyCollection<string> UnlockedIds => _unlocked;

    /// <summary>Check if the player already owns a given skill.</summary>
    public bool IsUnlocked(string skillId) => _unlocked.Contains(skillId);

    /// <summary>
    /// Determines whether all prerequisites for a skill are satisfied.
    /// </summary>
    public bool CanUnlock(SkillSO skill)
    {
        if (skill == null) return false;
        if (_unlocked.Contains(skill.Id)) return false; // already have it

        foreach (var pre in skill.Prerequisites)
        {
            if (pre == null || !_unlocked.Contains(pre.Id))
                return false; // missing a requirement
        }
        return true;
    }

    /// <summary>
    /// Attempt to unlock a skill. Returns true only if state changed.
    /// </summary>
    public bool Unlock(SkillSO skill)
    {
        if (!CanUnlock(skill)) return false;

        _unlocked.Add(skill.Id);
        OnSkillUnlocked?.Invoke(skill); // fire after mutating state
        return true;
    }

    // Persistence removed; skills reset between sessions.
}

