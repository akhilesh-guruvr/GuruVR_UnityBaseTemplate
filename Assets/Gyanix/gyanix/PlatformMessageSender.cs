using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;


//[System.Serializable]
//public class ChatMessagePayload
//{
//    public string content;
//    public string model;
//    public string role;
//    public bool voice;
//}


public class PlatformMessageSender : MonoBehaviour
{
    [Header("Chat API")]
    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    private string modelName = "llama";

    [Header("Message")]
    [TextArea]
    public string message = "Hello from platform!";

    private bool sendAtStart = true; // if true, send automatically

    public void SendMessageToBackend(string customMessage = null)
    {
        string msgToSend = string.IsNullOrEmpty(customMessage) ? message : customMessage;
        StartCoroutine(SendMessageCoroutine(msgToSend));
    }

    void Start()
    {
        var newChat = UnityEngine.Object.FindFirstObjectByType<NewChatCreator>();
        if (newChat != null)
        {
            // Subscribe your function to run automatically after chat is created
            newChat.OnChatCreated += () => SendMessageToBackend();
        }
    }

    private IEnumerator SendMessageCoroutine(string msg)
    {
        string token = ChatSessionManager.Token;
        string chatId = ChatSessionManager.ChatId;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            Debug.LogError("[PlatformMessageSender] Token or chatId missing. Create chat first.");
            yield break;
        }

        string url = chatBaseUrl + chatId + "/messages/";
        ChatMessagePayloadGyanix payload = new ChatMessagePayloadGyanix
        {
            content = msg,
            model = modelName,
            role = "platform",
            voice = false
        };


        string json = JsonUtility.ToJson(payload);

        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", "Bearer " + token);

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[PlatformMessageSender] Error: " + uwr.error + " | " + uwr.downloadHandler.text);
            }
            else
            {
                Debug.Log("[PlatformMessageSender] Message sent: " + msg);
            }
        }
    }
}
