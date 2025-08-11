using UnityEngine;

/// <summary>
/// Basic surface for anything the player can interact with via the <see cref="InteractionController"/>.
/// </summary>
public interface IInteractable
{
    /// <summary>Short text shown to the player when hovering the object.</summary>
    string Prompt { get; }

    /// <summary>
    /// Called by the <see cref="InteractionController"/> when the player activates the object.
    /// </summary>
    /// <param name="interactor">The GameObject triggering the interaction (typically the player).</param>
    void Interact(GameObject interactor);
}
