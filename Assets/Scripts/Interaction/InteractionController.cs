using UnityEngine;

/*
 * InteractionController.cs
 * Purpose: Casts a ray from the active camera through the cursor each frame to detect
 *          objects implementing <see cref="IInteractable"/>. Displays their prompt and
 *          invokes <see cref="IInteractable.Interact"/> when the player clicks.
 * Dependencies: Camera reference, <see cref="IInputReader"/> for pointer/interaction data,
 *               Physics raycasts, <see cref="IInteractable"/> components, optional
 *               <see cref="InteractionPromptUI"/> for on-screen messages.
 * Expansion Hooks: Swap out the <see cref="IInputReader"/> to support controllers,
 *                  highlight effects, etc., without touching this logic.
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

    [Tooltip("Component providing pointer position and interact input.")]
    [SerializeField] private MonoBehaviour _inputReaderSource;

    // Cache of the currently looked-at interactable so we only log prompts on change.
    private IInteractable _current;
    // Cached interface to avoid repeated casts every frame.
    private IInputReader _inputReader;

    private Camera Cam => sourceCamera != null ? sourceCamera : Camera.main;

    private void Awake()
    {
        // Convert the serialized MonoBehaviour into the input interface. Unity cannot
        // serialize interfaces directly, so we cast at runtime.
        _inputReader = _inputReaderSource as IInputReader;
        if (_inputReader == null && _inputReaderSource != null)
            Debug.LogError($"{name} has an input source that does not implement IInputReader.", this);
    }

    private void Update()
    {
        var cam = Cam;
        // Ensure we have both a camera and an input provider before doing any work.
        if (cam == null || _inputReader == null) return; // nothing to do

        // Ask the input reader for the current pointer position. For a mouse this is
        // the cursor; a future gamepad reader could return a virtual cursor instead.
        Vector2 pointerPos = _inputReader.GetPointerPosition();
        Ray ray = cam.ScreenPointToRay(pointerPos);
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

            // Delegate the interact check to the input reader so devices remain decoupled.
            if (_current != null && _inputReader.GetInteractPressed())
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
