using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Root DTO representing all persistent state. Instead of hardcoding fields for every
/// system, each <see cref="ISaveable"/> writes a JSON payload into the <see cref="sections"/>
/// collection keyed by its system name. This makes the save file extensible without
/// modifying this class when new systems appear.
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>Schema version for future migrations.</summary>
    public int version = 6;

    /// <summary>Serializable key/value pair storing one system's JSON payload.</summary>
    [Serializable]
    public class Section
    {
        public string key;
        public string json;
    }

    /// <summary>All serialized sections keyed by <see cref="Section.key"/>.</summary>
    public List<Section> sections = new();

    /// <summary>Retrieve the JSON payload for a given system key.</summary>
    public string GetSection(string key)
    {
        var sec = sections.FirstOrDefault(s => s.key == key);
        return sec != null ? sec.json : null;
    }

    /// <summary>Insert or replace a section's JSON payload.</summary>
    public void SetSection(string key, string json)
    {
        var sec = sections.FirstOrDefault(s => s.key == key);
        if (sec != null) sec.json = json;
        else sections.Add(new Section { key = key, json = json });
    }
}
