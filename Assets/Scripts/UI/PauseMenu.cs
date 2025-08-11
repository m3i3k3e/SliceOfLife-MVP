using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple pause menu controller. Toggled via the Escape key and exposes
/// button hooks for resuming, opening settings, and saving then quitting
/// back to the Title screen.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject menuRoot;     // panel to show/hide

    [Header("Config")]
    [Tooltip("Scene loaded when quitting to title.")]
    [SerializeField] private string titleScene = "Title";

    private void Awake()
    {
        // If no explicit root is provided, assume this GameObject is the menu.
        if (menuRoot == null)
            menuRoot = gameObject;

        menuRoot.SetActive(false); // start hidden
    }

    private void Update()
    {
        // Toggle visibility when the player presses Escape.
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    /// <summary>
    /// Show or hide the pause menu and adjust time scale accordingly.
    /// </summary>
    public void Toggle()
    {
        bool show = !menuRoot.activeSelf;
        menuRoot.SetActive(show);
        Time.timeScale = show ? 0f : 1f; // pause game when menu visible
    }

    /// <summary>Resume gameplay if the menu is open.</summary>
    public void Resume()
    {
        if (menuRoot.activeSelf)
            Toggle();
    }

    /// <summary>Placeholder for future settings dialog.</summary>
    public void Settings()
    {
        Debug.Log("Settings menu not implemented yet.");
    }

    /// <summary>
    /// Save progress and return to the Title scene.
    /// </summary>
    public void SaveAndQuit()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            SaveScheduler.RequestSave(gm); // queue save then return to title

        Time.timeScale = 1f; // ensure time resumes in title scene
        SceneManager.LoadScene(titleScene);
    }
}
