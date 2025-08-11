using System;
using UnityEngine;

/*
 * Interactable.cs
 * Purpose: Base MonoBehaviour implementing <see cref="IInteractable"/>. Holds a simple prompt string
 * and raises an event when interacted with so other components can respond modularly.
 * Dependencies: Designed to work with <see cref="InteractionController"/> for detection and activation.
 * Expansion Hooks: Subscribe to <see cref="OnInteracted"/> to perform custom behavior when the player activates the object.
 */

/// <summary>
/// Drop this on any GameObject with a collider to make it respond to player interaction.
/// Other behaviours can subscribe to <see cref="OnInteracted"/> rather than subclassing.
/// </summary>
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour, IInteractable
{
    [Tooltip("Text shown to the player when looking at this object.")]
    [SerializeField] private string prompt = "Interact";

    /// <inheritdoc />
    public string Prompt => prompt;

    /// <summary>Raised after <see cref="Interact"/> is called.</summary>
    public event Action<GameObject> OnInteracted;

    /// <inheritdoc />
    public virtual void Interact(GameObject interactor)
    {
        // Simply forward to any listeners. Specialized components can subscribe.
        OnInteracted?.Invoke(interactor);
    }
}
