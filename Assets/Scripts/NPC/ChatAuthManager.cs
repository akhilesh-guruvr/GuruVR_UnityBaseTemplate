using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

/// <summary>
/// Enhanced Chat Authentication Manager with event system
/// </summary>
public class ChatAuthManager : MonoBehaviour
{
    [Header("API Settings")]
    public string loginUrl = "https://chatbackenddev.guruvrmetaversity.com/auth/login";

    [Header("Credentials")]
    [Tooltip("Enter the username/email for the dev account")]
    public string username;

    [Tooltip("Enter the password for the dev account")]
    public string password;

    [Header("Dependencies")]
    public NewChatCreator chatCreator;

    [Header("Debug")]
    [SerializeField] private bool enableEventLogs = true;

    // Helper class to parse the response
    [Serializable]
    public class LoginResponse
    {
        public string access_token;
        public string token_type;
    }

    void Start()
    {
        StartCoroutine(PerformLogin());
    }

    IEnumerator PerformLogin()
    {
        if (enableEventLogs)
            Debug.Log("[ChatAuthManager] Attempting to log in via FORM DATA...");

        // Use WWWForm instead of JSON (sends as application/x-www-form-urlencoded)
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginUrl, form))
        {
            // UnityWebRequest.Post automatically sets correct Content-Type header
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = $"{www.error}: {www.downloadHandler.text}";
                Debug.LogError($"[ChatAuthManager] Login Failed: {errorMessage}");

                // 🔥 FIRE EVENT: Authentication failed
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeAuthenticationFailed(errorMessage);
            }
            else
            {
                string responseText = www.downloadHandler.text;

                if (enableEventLogs)
                    Debug.Log($"[ChatAuthManager] Login Successful. Response: {responseText}");

                try
                {
                    LoginResponse data = JsonUtility.FromJson<LoginResponse>(responseText);

                    if (!string.IsNullOrEmpty(data.access_token))
                    {
                        // Store the token
                        ChatSessionManager.Token = data.access_token;

                        if (enableEventLogs)
                            Debug.Log("[ChatAuthManager] ✅ Token updated successfully.");

                        // 🔥 FIRE EVENT: Authentication success
                        if (NPCEventSystem.Instance != null)
                            NPCEventSystem.Instance.InvokeAuthenticationSuccess(data.access_token);

                        // Initialize the chat
                        if (chatCreator != null)
                        {
                            chatCreator.InitializeChat();
                        }
                        else
                        {
                            Debug.LogWarning("[ChatAuthManager] NewChatCreator reference is null!");
                        }
                    }
                    else
                    {
                        string errorMsg = "Token not found in response";
                        Debug.LogError($"[ChatAuthManager] {errorMsg}");

                        // 🔥 FIRE EVENT: Authentication failed
                        if (NPCEventSystem.Instance != null)
                            NPCEventSystem.Instance.InvokeAuthenticationFailed(errorMsg);
                    }
                }
                catch (Exception e)
                {
                    string errorMsg = $"JSON Parse Error: {e.Message}";
                    Debug.LogError($"[ChatAuthManager] {errorMsg}");

                    // 🔥 FIRE EVENT: Authentication failed
                    if (NPCEventSystem.Instance != null)
                        NPCEventSystem.Instance.InvokeAuthenticationFailed(errorMsg);
                }
            }
        }
    }

    /// <summary>
    /// Public method to retry authentication (useful for retry buttons)
    /// </summary>
    public void RetryAuthentication()
    {
        if (enableEventLogs)
            Debug.Log("[ChatAuthManager] Retrying authentication...");

        StartCoroutine(PerformLogin());
    }
}

//using System.Collections;
//using System;
//using UnityEngine;
//using UnityEngine.Networking;
//using System.Text;

//public class ChatAuthManager : MonoBehaviour
//{
//    [Header("API Settings")]
//    public string loginUrl = "https://chatbackenddev.guruvrmetaversity.com/auth/login";

//    [Header("Credentials")]
//    [Tooltip("Enter the username/email for the dev account")]
//    public string username;
//    [Tooltip("Enter the password for the dev account")]
//    public string password;

//    [Header("Dependencies")]
//    public NewChatCreator chatCreator;

//    // Helper class to parse the response
//    [Serializable]
//    public class LoginResponse
//    {
//        public string access_token;
//        public string token_type;
//    }

//    void Start()
//    {
//        StartCoroutine(PerformLogin());
//    }

//    IEnumerator PerformLogin()
//    {
//        Debug.Log("[ChatAuthManager] Attempting to log in via FORM DATA...");

//        // =================================================================
//        // CHANGE 1: Use WWWForm instead of JSON
//        // This sends data as "application/x-www-form-urlencoded"
//        // =================================================================
//        WWWForm form = new WWWForm();
//        form.AddField("username", username);
//        form.AddField("password", password);

//        // =================================================================
//        // CHANGE 2: Use UnityWebRequest.Post with the form
//        // =================================================================
//        using (UnityWebRequest www = UnityWebRequest.Post(loginUrl, form))
//        {
//            // Note: UnityWebRequest.Post automatically sets the correct Content-Type header.
//            // Do NOT manually set "application/json" here.

//            yield return www.SendWebRequest();

//            if (www.result != UnityWebRequest.Result.Success)
//            {
//                Debug.LogError($"[ChatAuthManager] Login Failed: {www.error}");
//                Debug.LogError($"[ChatAuthManager] Response: {www.downloadHandler.text}");
//            }
//            else
//            {
//                string responseText = www.downloadHandler.text;
//                Debug.Log($"[ChatAuthManager] Login Successful. Response: {responseText}");

//                try
//                {
//                    LoginResponse data = JsonUtility.FromJson<LoginResponse>(responseText);

//                    if (!string.IsNullOrEmpty(data.access_token))
//                    {
//                        // Store the token
//                        ChatSessionManager.Token = data.access_token;
//                        Debug.Log("[ChatAuthManager] Token updated successfully.");

//                        // Initialize the chat
//                        if (chatCreator != null)
//                        {
//                            chatCreator.InitializeChat();
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogError("[ChatAuthManager] Token not found in response.");
//                    }
//                }
//                catch (Exception e)
//                {
//                    Debug.LogError($"[ChatAuthManager] JSON Parse Error: {e.Message}");
//                }
//            }
//        }
//    }
//}