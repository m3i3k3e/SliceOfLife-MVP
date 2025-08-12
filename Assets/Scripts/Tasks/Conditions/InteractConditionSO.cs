using UnityEngine;

/// <summary>
/// Condition that passes when <see cref="TaskService.NotifyInteraction"/> reports a matching identifier.
/// </summary>
[CreateAssetMenu(fileName = "InteractCondition", menuName = "SliceOfLife/TaskConditions/Interact")]
public class InteractConditionSO : TaskConditionSO
{
    [Tooltip("Interaction identifier to match.")]
    [SerializeField] private string interactId;

    /// <summary>True on the frame the desired interaction id was reported.</summary>
    public override bool IsMet(TaskService svc)
    {
        if (svc == null) return false;
        return svc.LastInteractionId == interactId;
    }
}
