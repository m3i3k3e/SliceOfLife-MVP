using TMPro;
using UnityEngine;

/// <summary>
/// Tiny view component exposing the label on a location button.
/// Having a dedicated script avoids costly string-based lookups at runtime.
/// </summary>
public class LocationButtonView : MonoBehaviour
{
    [Header("Assigned in Prefab")]
    [SerializeField] private TMP_Text label; // reference to the button's text component

    /// <summary>Read-only access so other scripts can set the label text.</summary>
    public TMP_Text Label => label;
}
