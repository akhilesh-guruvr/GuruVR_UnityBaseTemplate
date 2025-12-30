public class NullLogger : ILogger
{
    public void Info(string tag, string message) { /* no-op */ }
    public void Warn(string tag, string message) { /* no-op */ }
    public void Error(string tag, string message, System.Exception ex = null) { /* no-op */ }
}
