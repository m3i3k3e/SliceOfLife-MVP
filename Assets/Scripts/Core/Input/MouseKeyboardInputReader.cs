using UnityEngine;

/*
 * MouseKeyboardInputReader.cs
 * Purpose: Concrete <see cref="IInputReader"/> that pulls data from Unity's built-in
 *          mouse and keyboard input. Keeps <see cref="InteractionController"/>
 *          decoupled from specific devices.
 * Expansion Hooks: Swap this component for a GamepadInputReader in the Inspector
 *                  once controller support is implemented.
 */

/// <summary>
/// Reads interaction input from the standard mouse and keyboard setup.
/// </summary>
public class MouseKeyboardInputReader : MonoBehaviour, IInputReader
{
    /// <inheritdoc />
    public bool GetInteractPressed()
    {
        // Left mouse button acts as our generic "interact" control for now.
        return Input.GetMouseButtonDown(0);
    }

    /// <inheritdoc />
    public Vector2 GetPointerPosition()
    {
        // Unity reports mouse position in pixel coordinates relative to the screen.
        return Input.mousePosition;
    }
}
