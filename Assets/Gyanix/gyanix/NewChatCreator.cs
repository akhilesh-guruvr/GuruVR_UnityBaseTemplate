using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
public class NewChatCreator : MonoBehaviour
{
    [Header("API")]
    private string createChatUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    [Header("New Chat")]
    private string chatTitle = "My Chat Session";
    [Header("Auto-create")]
    private bool createOnStart = true;

    public Action OnChatCreated;
    //private void Start()
    //{
    //    if (createOnStart)
    //        StartCoroutine(CreateNewChat());
    //}
    public void InitializeChat()
    {
        Debug.Log("[NewChatCreator] Token received. Starting chat creation...");
        StartCoroutine(CreateNewChat());
    }

    public void CreateChatButton()
    {
        StartCoroutine(CreateNewChat());
    }

    private IEnumerator CreateNewChat()
    {
        string token = ChatSessionManager.Token;
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[NewChatCreator] No token loaded.");
            yield break;
        }

        var payload = new CreateChatPayload { title = chatTitle };
        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest uwr = new UnityWebRequest(createChatUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", "Bearer " + token);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[NewChatCreator] Error creating chat: " + uwr.error + " | " + uwr.downloadHandler.text);
            }
            else
            {
                string resp = uwr.downloadHandler.text;
                // Response contains fields including chat_id. Use simple wrapper to parse.
                ChatCreateResponse createResp = JsonUtility.FromJson<ChatCreateResponse>(resp);
                if (!string.IsNullOrEmpty(createResp.chat_id))
                {
                    ChatSessionManager.ChatId = createResp.chat_id;
                    Debug.Log("[NewChatCreator] New chat created: " + createResp.chat_id);
                    // ✅ Invoke callback so other scripts know chat is ready
                    OnChatCreated?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[NewChatCreator] Response did not contain chat_id. Raw response: " + resp);
                }
            }
        }
    }
    [System.Serializable]
    private class CreateChatPayload
    {
        public string title;
    }
    [System.Serializable]
    private class ChatCreateResponse
    {
        public string chat_id;
        // other fields ignored
    }
}