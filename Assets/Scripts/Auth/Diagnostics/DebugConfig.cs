using UnityEngine;

[CreateAssetMenu(menuName = "Auth/DebugConfig", fileName = "AuthDebugConfig")]
public class DebugConfig : ScriptableObject
{
    [Tooltip("When true, verbose Info logs are printed.")]
    public bool verboseLogs = false;

    [Tooltip("When true, the full raw server response will be logged (use for debugging only).")]
    public bool logRawResponse = false;
}
