using UnityEngine;

/// <summary>
/// Specialized <see cref="ItemCardSO"/> representing shard drops.
/// Shards act as fragments of a larger reward and can be spent or
/// combined elsewhere. The class itself is intentionally empty; it
/// simply provides a distinct asset menu to help designers author
/// shard items and ensures the ItemType defaults to <see cref="ItemType.Shard"/>.
/// </summary>
[CreateAssetMenu(fileName = "ShardCard", menuName = "SliceOfLife/Shard Card")]
public class ShardCardSO : ItemCardSO
{
    private void OnEnable()
    {
        // Guarantee newly created assets classify themselves as shards.
        // Designers can still override other ItemCardSO fields as needed.
        var field = typeof(ItemCardSO).GetField("_itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, ItemType.Shard);
    }
}
