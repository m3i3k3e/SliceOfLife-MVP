using UnityEngine;

/*
 * InteractionController.cs
 * Purpose: Casts a ray from the active camera through the cursor each frame to detect
 *          objects implementing <see cref="IInteractable"/>. Displays their prompt and
 *          invokes <see cref="IInteractable.Interact"/> when the player clicks.
 * Dependencies: Camera reference, Physics raycasts, <see cref="IInteractable"/> components.
 * Expansion Hooks: Replace Debug.Log with UI prompt display; support controller input, highlight effects, etc.
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
                    Debug.Log(_current.Prompt); // show prompt when entering a new target
            }

            if (_current != null && Input.GetMouseButtonDown(0))
            {
                _current.Interact(gameObject);
                Debug.Log($"Interacted: {_current.Prompt}");
            }
        }
        else
        {
            _current = null; // nothing hit
        }
    }
}
