using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized helper responsible for loading scenes asynchronously.
/// Provides hook methods for fade/transition effects.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _fader;        // Full-screen UI overlay used for fades
    [SerializeField] private float _fadeDuration = 0.5f; // Seconds for a full fade transition

    /// <summary>
    /// Loads a scene by name asynchronously.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load. Must be added to build settings.</param>
    public async Task LoadSceneAsync(string sceneName)
    {
        // Fade the screen out before starting the load.
        await FadeOutAsync();

        // Begin loading the scene. Unity returns an AsyncOperation we can await.
        var loadOp = SceneManager.LoadSceneAsync(sceneName);

        // While the async operation progresses, yield control each frame.
        while (!loadOp.isDone)
        {
            await Task.Yield();
        }

        // After the new scene is ready, fade the screen back in.
        await FadeInAsync();
    }

    /// <summary>
    /// Gradually increases the fader alpha from transparent to opaque.
    /// </summary>
    protected virtual async Task FadeOutAsync()
    {
        // If no fader is wired, skip the effect entirely.
        if (_fader == null || _fadeDuration <= 0f)
        {
            return; // No fade: scenes will cut instantly.
        }

        // Ensure the overlay is visible and start fully transparent.
        _fader.gameObject.SetActive(true);
        _fader.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime; // Advance time
            // Normalize the elapsed time into a 0-1 range for alpha.
            _fader.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            await Task.Yield(); // Wait for next frame without blocking
        }

        // Ensure we end fully opaque.
        _fader.alpha = 1f;
    }

    /// <summary>
    /// Gradually decreases the fader alpha from opaque to transparent.
    /// </summary>
    protected virtual async Task FadeInAsync()
    {
        // If no fader is wired, skip the effect entirely.
        if (_fader == null || _fadeDuration <= 0f)
        {
            return; // No fade: scenes will cut instantly.
        }

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime; // Advance time
            // Invert progress so alpha goes from 1 → 0.
            _fader.alpha = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
            await Task.Yield(); // Wait for next frame without blocking
        }

        // Hide the overlay once we're fully transparent.
        _fader.alpha = 0f;
        _fader.gameObject.SetActive(false);
    }
}
