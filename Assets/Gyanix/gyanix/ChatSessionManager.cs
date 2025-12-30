//using System.IO;
//using UnityEngine;

//public class ChatSessionManager : MonoBehaviour
//{
//    private static string _token = null;
//    private static string _chatId = null;
//    private static bool _loaded = false;

//    public static string Token
//    {
//        get
//        {
//            if (!_loaded) LoadConfig();
//            return _token;
//        }
//    }

//    public static string ChatId
//    {
//        get => _chatId;
//        set => _chatId = value;
//    }

//    private static void LoadConfig()
//    {
//        _loaded = true;
//        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
//        if (!File.Exists(configPath))
//        {
//            Debug.LogError("[ChatSessionManager] config.json not found at: " + configPath + ". Create a file with { \"token\": \"your_token_here\" }");
//            _token = "";
//            return;
//        }

//        try
//        {
//            string json = File.ReadAllText(configPath);
//            Config cfg = JsonUtility.FromJson<Config>(json);
//            _token = cfg.token ?? "";
//            Debug.Log("[ChatSessionManager] Token loaded (length: " + _token.Length + ")");
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError("[ChatSessionManager] Failed to read config.json: " + ex.Message);
//            _token = "";
//        }
//    }

//    [System.Serializable]
//    private class Config
//    {
//        public string token;
//    }
//}

using System.IO;
using UnityEngine;

public class ChatSessionManager : MonoBehaviour
{
    private static string _token = null;
    private static string _chatId = null;
    private static bool _loaded = false;

    public static string Token
    {
        get
        {
            if (!_loaded) LoadConfig();
            return _token;
        }
        set
        {
            _token = value;
            _loaded = true; // Mark as loaded since we set it manually
            Debug.Log("[ChatSessionManager] Token manually updated via AuthManager.");
        }
    }

    public static string ChatId
    {
        get => _chatId;
        set => _chatId = value;
    }

    private static void LoadConfig()
    {
        _loaded = true;
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");

        // If file doesn't exist, we just return empty. 
        // AuthManager will handle filling this later.
        if (!File.Exists(configPath))
        {
            // Optional: You can comment out this error if you rely 100% on AuthManager
            Debug.LogWarning("[ChatSessionManager] config.json not found. Waiting for AuthManager to provide token.");
            _token = "";
            return;
        }

        try
        {
            string json = File.ReadAllText(configPath);
            Config cfg = JsonUtility.FromJson<Config>(json);
            _token = cfg.token ?? "";
            Debug.Log("[ChatSessionManager] Token loaded from file (length: " + _token.Length + ")");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ChatSessionManager] Failed to read config.json: " + ex.Message);
            _token = "";
        }
    }

    [System.Serializable]
    private class Config
    {
        public string token;
    }
}