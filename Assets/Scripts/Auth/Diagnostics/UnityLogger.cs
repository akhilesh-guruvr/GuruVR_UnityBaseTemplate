using UnityEngine;

public class UnityLogger : ILogger
{
    private readonly bool _verbose;

    /// <summary>
    /// Create a logger. If verbose==true, Info() messages are logged.
    /// </summary>
    public UnityLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void Info(string tag, string message)
    {
        if (!_verbose) return;
        Debug.Log($"[INFO] [{tag}] {message}");
    }

    public void Warn(string tag, string message)
    {
        Debug.LogWarning($"[WARN] [{tag}] {message}");
    }

    public void Error(string tag, string message, System.Exception ex = null)
    {
        if (ex != null)
            Debug.LogError($"[ERROR] [{tag}] {message}\nException: {ex}");
        else
            Debug.LogError($"[ERROR] [{tag}] {message}");
    }
}
