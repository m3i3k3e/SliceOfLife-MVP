using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized helper responsible for loading scenes asynchronously.
/// Provides hook methods for fade/transition effects.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Loads a scene by name asynchronously.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load. Must be added to build settings.</param>
    public async Task LoadSceneAsync(string sceneName)
    {
        // Run pre-load transition (e.g., fade-out). Currently no-op.
        await FadeOutAsync();

        // Begin loading the scene. Unity returns an AsyncOperation we can await.
        var loadOp = SceneManager.LoadSceneAsync(sceneName);

        // While the async operation progresses, yield control each frame.
        while (!loadOp.isDone)
        {
            await Task.Yield();
        }

        // Run post-load transition (e.g., fade-in). Currently no-op.
        await FadeInAsync();
    }

    /// <summary>
    /// Placeholder for future fade-out logic.
    /// </summary>
    protected virtual Task FadeOutAsync()
    {
        // TODO: add screen fade-out animation.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Placeholder for future fade-in logic.
    /// </summary>
    protected virtual Task FadeInAsync()
    {
        // TODO: add screen fade-in animation.
        return Task.CompletedTask;
    }
}
