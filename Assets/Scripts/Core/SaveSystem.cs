using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Simple JSON persistence built around <see cref="SaveModelV2"/>.
/// Uses Unity's built-in <see cref="JsonUtility"/> since data volume is tiny.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    /// <summary>Full path to the save file in Unity's persistent data location.</summary>
    private static string PathToFile => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>Does a save file exist on disk?</summary>
    public static bool HasAnySave() => File.Exists(PathToFile);

    /// <summary>Delete the current save if it exists.</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(PathToFile)) File.Delete(PathToFile);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete save file: {ex}");
        }
    }

    /// <summary>
    /// Serialize runtime state to disk.
    /// </summary>
    public static void Save(GameManager gm)
    {
        if (gm == null) return;
        var model = BuildModel(gm);
        WriteToDisk(model);
    }

    /// <summary>
    /// Load state from disk and apply to managers.
    /// Returns the loaded model for convenience.
    /// </summary>
    public static SaveModelV2 Load(GameManager gm)
    {
        var model = new SaveModelV2();
        if (gm == null) return model;
        var path = PathToFile;

        if (!File.Exists(path))
        {
            ApplyModel(gm, model);
            return model; // nothing to load yet
        }

        try
        {
            var json = File.ReadAllText(path);
            model = JsonUtility.FromJson<SaveModelV2>(json);

            // If deserialization fails or version mismatch, start fresh.
            if (model == null || model.version != SaveModelV2.Version)
            {
                model = new SaveModelV2();
                Debug.LogWarning("Save file version mismatch; starting with defaults.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load save file: {ex}");
            model = new SaveModelV2();
        }

        ApplyModel(gm, model);
        return model;
    }

    /// <summary>Gather data from registered systems into a <see cref="SaveModelV2"/>.</summary>
    private static SaveModelV2 BuildModel(GameManager gm)
    {
        var model = new SaveModelV2();
        // GameManager captures its own fields first.
        gm.Capture(model);

        // Then ask each registered participant to append their data.
        var participants = gm.SaveParticipants;
        for (int i = 0; i < participants.Count; i++)
            participants[i]?.Capture(model);

        return model;
    }

    /// <summary>Apply a loaded model to all registered systems.</summary>
    private static void ApplyModel(GameManager gm, SaveModelV2 model)
    {
        gm?.Apply(model);

        var participants = gm?.SaveParticipants;
        if (participants != null)
        {
            for (int i = 0; i < participants.Count; i++)
                participants[i]?.Apply(model);
        }

        gm?.ReevaluateSleepGate();
    }

    /// <summary>Write the model to disk as JSON.</summary>
    private static void WriteToDisk(SaveModelV2 model)
    {
        var path = PathToFile;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = JsonUtility.ToJson(model, prettyPrint: true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save file: {ex}");
        }
    }

}

