using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a single skill node. Holds metadata
/// and prerequisite relationships but no behavior.
/// </summary>
[CreateAssetMenu(fileName = "Skill", menuName = "SliceOfLife/Skill")]
public class SkillSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "skill_id"; // Stable string saved to disk
    [SerializeField] private string displayName = "New Skill"; // Designer-facing name

    [Header("Dependencies")]
    [Tooltip("Skills that must be unlocked before this one becomes available.")]
    [SerializeField] private List<SkillSO> prerequisites = new();

    [Header("Bonuses")]
    [Tooltip("Text descriptions of bonuses granted on unlock. Effects wired later.")]
    [SerializeField] private List<string> bonuses = new();

    /// <summary>Stable identifier used for save data and lookups.</summary>
    public string Id => id;

    /// <summary>Human readable name for UI.</summary>
    public string DisplayName => displayName;

    /// <summary>List of required skills; all must be unlocked first.</summary>
    public IReadOnlyList<SkillSO> Prerequisites => prerequisites;

    /// <summary>Descriptive bonus strings. Actual gameplay effects come later.</summary>
    public IReadOnlyList<string> Bonuses => bonuses;
}

