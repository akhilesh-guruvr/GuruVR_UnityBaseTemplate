using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class InitialPoseRecorder : MonoBehaviour
{
    public Vector3 InitialPosition { get; private set; }
    public Quaternion InitialRotation { get; private set; }
    public bool HasRecordedPose { get; private set; }

    private void Awake()
    {
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
        HasRecordedPose = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Record Current Pose as Initial")]
    private void RecordCurrentPose()
    {
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}