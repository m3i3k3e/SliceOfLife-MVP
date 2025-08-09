using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Call from a UI Button to load a scene by name.
/// Keep it tiny and reusable.
/// </summary>
public class LoadSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "Battle";

    // Hook this via the Button's OnClick in the Inspector (or via code)
    public void LoadScene() => SceneManager.LoadScene(sceneName);
}
