using System;

/// <summary>
/// Represents a runtime system whose state can be serialized to and from disk.
/// Implement this on managers that want to participate in the <see cref="SaveSystem"/>.
/// Each implementer provides a unique <see cref="SaveKey"/> so its data can be
/// stored in the save file's dictionary.
/// </summary>
public interface ISaveable
{
    /// <summary>Unique key identifying this system's save data.</summary>
    string SaveKey { get; }

    /// <summary>
    /// Convert live runtime state into a plain data object that JsonUtility can serialize.
    /// </summary>
    object ToData();

    /// <summary>
    /// Rehydrate the runtime state from a data object previously produced by <see cref="ToData"/>.
    /// Implementers are expected to cast <paramref name="data"/> to their concrete type.
    /// </summary>
    /// <param name="data">The deserialized data container.</param>
    void LoadFrom(object data);
}
