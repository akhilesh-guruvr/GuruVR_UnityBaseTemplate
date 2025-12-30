public interface ILogger
{
    void Info(string tag, string message);
    void Warn(string tag, string message);
    void Error(string tag, string message, System.Exception ex = null);
}
