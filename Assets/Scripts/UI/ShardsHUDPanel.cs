using TMPro;
using UnityEngine;

/// <summary>
/// Simple HUD panel that displays the player's total shard fragments.
/// Listens to <see cref="ResourceManager.OnResourceChanged"/> so the label
/// stays in sync even when shards are earned outside combat.
/// </summary>
public class ShardsHUDPanel : HUDPanel
{
    [SerializeField] private TextMeshProUGUI shardsText; // e.g., "Shards: 0"

    protected override void OnBind()
    {
        var resources = GM?.Resources;
        if (resources == null) return;

        resources.OnResourceChanged += HandleResourceChanged;
        HandleResourceChanged(resources.ShardResource, resources.GetShardCount());
    }

    protected override void OnUnbind()
    {
        var resources = GM?.Resources;
        if (resources == null) return;
        resources.OnResourceChanged -= HandleResourceChanged;
    }

    private void HandleResourceChanged(ResourceSO resource, int amount)
    {
        var resMgr = GM?.Resources;
        if (resMgr == null || resource != resMgr.ShardResource) return;
        if (shardsText != null)
            shardsText.text = $"Shards: {amount}";
    }
}
