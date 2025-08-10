using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a button to load a specific scene when clicked.
/// Uses <see cref="SceneLoader"/> to perform the transition so we can add
/// fades or other polish in one place later.
/// </summary>
[RequireComponent(typeof(Button))]
public class LoadSceneButton : MonoBehaviour
{
    [Tooltip("Name of the scene to load. Must match an entry in Build Settings.")]
    [SerializeField] private string sceneName = "Start";

    [Tooltip("SceneLoader responsible for executing the load.")]
    [SerializeField] private SceneLoader sceneLoader;

    private Button btn;

    private void Awake()
    {
        // Cache the Button component; avoids repeated GetComponent calls.
        btn = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Subscribe to the click event when the object becomes active.
        if (btn != null)
        {
            btn.onClick.AddListener(OnClicked);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks or unexpected calls.
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClicked);
        }
    }

    /// <summary>
    /// Handler invoked when the button is clicked.
    /// </summary>
    private async void OnClicked()
    {
        if (sceneLoader == null)
        {
            // Fail gracefully; the scene simply won't load but we warn in the console.
            Debug.LogWarning($"SceneLoader not set on {name}", this);
            return;
        }

        // Delegate the actual loading to our SceneLoader.
        await sceneLoader.LoadSceneAsync(sceneName);
    }
}
