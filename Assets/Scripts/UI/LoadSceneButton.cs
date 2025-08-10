using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple command-style component for scene changes. Optional key gating keeps
/// dungeon entry logic outside of UI prefabs.
/// </summary>
public class LoadSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "Battle";

    // If this button is used for the dungeon entry, leave this true.
    // For other buttons that just change scenes, uncheck in Inspector.
    [SerializeField] private bool requireDungeonKey = true;

    /// <summary>
    /// Triggered by UI. Spends a dungeon key if required before loading the target scene.
    /// </summary>
    public void LoadScene()
    {
        if (requireDungeonKey)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Spend a key; bail if none left.
            if (!gm.TryConsumeDungeonKey())
            {
                // (Optional) play a denied sound or flash a “No key left” label.
                return;
            }

            // For telemetry/UX flavor (idempotent).
            gm.MarkDungeonAttempted();
        }

        SceneManager.LoadScene(sceneName);
    }
}
