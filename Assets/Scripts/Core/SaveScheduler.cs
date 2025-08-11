using System.Collections;
using UnityEngine;

/// <summary>
/// Utility that batches save requests and writes to disk after a short delay.
/// Prevents expensive disk writes when many systems request saves in quick succession.
/// </summary>
public class SaveScheduler : MonoBehaviour
{
    /// <summary>Singleton instance living on a hidden GameObject.</summary>
    private static SaveScheduler _instance;

    /// <summary>How long to wait before flushing a pending save.</summary>
    private const float DelaySeconds = 0.5f;

    /// <summary>GameManager waiting to be saved. Updated on each request.</summary>
    private GameManager _pendingGameManager;

    /// <summary>Reference to the active coroutine so it can be restarted on new requests.</summary>
    private Coroutine _flushRoutine;

    /// <summary>
    /// Public entry point for systems that want to persist state.
    /// Requests are debounced so multiple calls within <see cref="DelaySeconds"/> coalesce.
    /// </summary>
    public static void RequestSave(GameManager gm)
    {
        if (gm == null) return; // defensive: nothing to save

        // Ensure we have a host object before scheduling.
        EnsureInstance();

        // Remember the last GameManager supplied. If several managers call this
        // before the delay expires, the most recent reference wins which is fine
        // because all managers point to the same singleton.
        _instance._pendingGameManager = gm;

        // Restart the delay timer each time a save is requested.
        if (_instance._flushRoutine != null)
            _instance.StopCoroutine(_instance._flushRoutine);
        _instance._flushRoutine = _instance.StartCoroutine(_instance.FlushAfterDelay());
    }

    /// <summary>
    /// Create the hidden host object if it doesn't already exist.
    /// </summary>
    private static void EnsureInstance()
    {
        if (_instance != null) return;

        var go = new GameObject("~SaveScheduler");
        go.hideFlags = HideFlags.HideAndDontSave; // avoid clutter in hierarchy
        DontDestroyOnLoad(go); // persist across scene loads
        _instance = go.AddComponent<SaveScheduler>();
    }

    /// <summary>
    /// Waits for the debounce window to elapse then performs the actual save.
    /// </summary>
    private IEnumerator FlushAfterDelay()
    {
        // WaitForSeconds allocates but fires rarely so it's acceptable here.
        yield return new WaitForSeconds(DelaySeconds);

        // Finally perform the save using the most recent GameManager reference.
        SaveSystem.Save(_pendingGameManager);
        _flushRoutine = null; // mark coroutine as complete
    }

    /// <summary>
    /// Unity lifecycle: ensure any pending save is flushed when the application quits.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (_pendingGameManager != null)
        {
            // We bypass the delay to guarantee state hits disk before shutdown.
            SaveSystem.Save(_pendingGameManager);
        }
    }
}

