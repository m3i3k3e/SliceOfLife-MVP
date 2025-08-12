using UnityEngine;

/*
 * IInputReader.cs
 * Purpose: Defines an abstraction layer for interaction-related inputs so gameplay
 *          systems can remain agnostic about the actual device (mouse, gamepad, etc.).
 * Expansion Hooks: Implement this interface for new control schemes, e.g.,
 *                  GamepadInputReader without touching interaction logic.
 */

/// <summary>
/// Provides access to player input needed for world interactions.
/// </summary>
public interface IInputReader
{
    /// <summary>
    /// Returns <c>true</c> only on the frame the player initiates an interaction
    /// (e.g., left mouse button, controller A button).
    /// </summary>
    bool GetInteractPressed();

    /// <summary>
    /// Current pointer position in screen coordinates.
    /// For mice this is the cursor position; for controllers this could map to
    /// the center of the screen or a virtual cursor.
    /// </summary>
    Vector2 GetPointerPosition();
}
