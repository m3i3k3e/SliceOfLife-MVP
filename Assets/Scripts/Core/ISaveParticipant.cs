using UnityEngine;

/// <summary>
/// Lightweight contract for systems that participate in save/load.
/// </summary>
public interface ISaveParticipant
{
    /// <summary>
    /// Write the system's state into the aggregated <see cref="SaveModelV2"/>.
    /// </summary>
    void Capture(SaveModelV2 model);

    /// <summary>
    /// Restore the system's state from the aggregated <see cref="SaveModelV2"/>.
    /// </summary>
    void Apply(SaveModelV2 model);
}
