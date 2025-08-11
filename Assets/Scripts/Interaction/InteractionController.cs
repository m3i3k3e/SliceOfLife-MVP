using UnityEngine;

/*
 * InteractionController.cs
 * Purpose: Casts a ray from the active camera through the cursor each frame to detect
 *          objects implementing <see cref="IInteractable"/>. Displays their prompt and
 *          invokes <see cref="IInteractable.Interact"/> when the player clicks.
 * Dependencies: Camera reference, Physics raycasts, <see cref="IInteractable"/> components,
 *               optional <see cref="InteractionPromptUI"/> for on-screen messages.
 * Expansion Hooks: Support controller input, highlight effects, etc.
 */

/// <summary>
/// Simple cursor-based interaction handler. Attach to a player or camera rig.
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Tooltip("Camera used for raycasts. If null, defaults to Camera.main.")]
    [SerializeField] private Camera sourceCamera;

    [Tooltip("How far the player can interact with objects.")]
    [SerializeField] private float maxDistance = 5f;

    /// <summary>
    /// UI widget used to display prompts. Drag the <see cref="InteractionPromptUI"/>
    /// component from your Canvas into this field in the Inspector.
    /// </summary>
    [Tooltip("Visual element for interaction hints.")]
    [SerializeField] private InteractionPromptUI _promptUI;

    // Cache of the currently looked-at interactable so we only log prompts on change.
    private IInteractable _current;

    private Camera Cam => sourceCamera != null ? sourceCamera : Camera.main;

    private void Update()
    {
        var cam = Cam;
        if (cam == null) return; // no camera → nothing to do

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != _current)
            {
                _current = interactable;
                if (_current != null)
                    _promptUI?.ShowPrompt(_current.Prompt); // show prompt when entering a new target
                else
                    _promptUI?.ClearPrompt(); // no interactable hit → clear any previous prompt
            }

            if (_current != null && Input.GetMouseButtonDown(0))
            {
                _current.Interact(gameObject);
                _promptUI?.ShowPrompt($"Interacted: {_current.Prompt}"); // mirror previous log behavior
            }
        }
        else
        {
            _current = null; // nothing hit
            _promptUI?.ClearPrompt(); // hide prompt when looking into empty space
        }
    }
}
