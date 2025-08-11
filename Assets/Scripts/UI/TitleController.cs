using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles button callbacks for the Title screen.
/// Provides New Game and Continue flows along with stubs for
/// future Load and Settings menus.
/// </summary>
public class TitleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button continueButton;      // toggled based on save presence

    [Header("Config")]
    [Tooltip("Scene loaded when starting or continuing a game.")]
    [SerializeField] private string firstScene = "Basement";

    private void Start()
    {
        // Enable the Continue button only if a save file exists.
        if (continueButton)
            continueButton.interactable = SaveSystem.HasAnySave();
    }

    /// <summary>
    /// Begin a fresh game. Any existing save is deleted so
    /// the player starts from day one.
    /// </summary>
    public void NewGame()
    {
        SaveSystem.Delete();         // throw away previous progress
        Time.timeScale = 1f;         // ensure time is running
        SceneManager.LoadScene(firstScene); // jump to first gameplay scene
    }

    /// <summary>
    /// Continue from a previous save. Simply loads the first
    /// gameplay scene; the bootstrapper will hydrate state.
    /// </summary>
    public void ContinueGame()
    {
        Time.timeScale = 1f;         // just in case the title scene was paused
        SceneManager.LoadScene(firstScene);
    }

    /// <summary>Placeholder for a future Load menu.</summary>
    public void Load()
    {
        Debug.Log("Load menu not implemented yet.");
    }

    /// <summary>Placeholder for a future Settings menu.</summary>
    public void Settings()
    {
        Debug.Log("Settings menu not implemented yet.");
    }
}
