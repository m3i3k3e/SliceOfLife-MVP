using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current daily companion assignments.
/// Expects a simple TMP text prefab for each row.
/// </summary>
public class AssignmentListView : HUDPanel
{
    [Header("UI References")]
    [Tooltip("Parent transform where assignment rows will be instantiated.")]
    [SerializeField] private Transform contentRoot;
    [Tooltip("Prefab containing a TextMeshProUGUI for each assignment entry.")]
    [SerializeField] private TextMeshProUGUI entryPrefab;

    // Keep spawned labels so we can cleanly destroy them on refresh.
    private readonly List<TextMeshProUGUI> _spawned = new();

    protected override void OnBind()
    {
        Refresh(); // populate immediately when dependencies arrive
    }

    protected override void OnUnbind()
    {
        Clear(); // drop spawned objects when panel is disabled/unbound
    }

    /// <summary>
    /// Rebuild the list by querying GameManager for assignments.
    /// Call this after <see cref="GameManager.AssignWaifu"/>.
    /// </summary>
    public void Refresh()
    {
        if (GM == null || contentRoot == null || entryPrefab == null) return;
        Clear();

        foreach (var kvp in GM.CurrentAssignments)
        {
            var label = Instantiate(entryPrefab, contentRoot);
            label.text = $"{kvp.Key.DisplayName}: {kvp.Value}";
            _spawned.Add(label);
        }
    }

    // Destroy existing labels so Refresh starts from a clean slate.
    private void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        _spawned.Clear();
    }
}
