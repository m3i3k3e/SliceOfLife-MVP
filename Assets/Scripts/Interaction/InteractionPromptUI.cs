using TMPro;
using UnityEngine;

/*
 * InteractionPromptUI.cs
 * Purpose: Simple helper that writes interaction prompts to a TMP_Text label.
 * Dependencies: TextMesh Pro for UI rendering.
 * Expansion Hooks: Could animate fade-in/out, support localization, etc.
 */

/// <summary>
/// Small utility component that exposes methods to show or clear a prompt label.
/// Attach to a UI object (e.g., inside a Canvas).
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Tooltip("Label that displays the interaction prompt text.")]
    [SerializeField] private TMP_Text _label; // serialized so designers can wire via Inspector

    /// <summary>
    /// Writes the given prompt to the label. Empty or null hides it.
    /// </summary>
    public void ShowPrompt(string prompt)
    {
        if (_label == null) return; // safety: if label missing, bail out

        _label.text = prompt; // update the visual text
        _label.enabled = !string.IsNullOrEmpty(prompt); // only show when there's something to say
    }

    /// <summary>
    /// Convenience wrapper to clear the prompt.
    /// </summary>
    public void ClearPrompt() => ShowPrompt(string.Empty);
}
