using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class ChatMessagePayload2
{
    public string content;
    public string model;
    public string role;
    public bool voice;
}

public class UserMessageWithVoice : MonoBehaviour
{
    [Header("Chat API")]
    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    private string modelName = "llama";

    [Header("Message")]
    [TextArea]
    public string message = "Hello AI!";

    [Header("Refs")]
    public PiperTTS piperTTS; // optional, will auto-find if null

    private void Start()
    {
        // Automatically find PiperTTS in scene if not assigned
        if (piperTTS == null)
        {
            piperTTS = UnityEngine.Object.FindFirstObjectByType<PiperTTS>();
            if (piperTTS == null)
            {
                Debug.LogError("[UserMessageWithVoice] PiperTTS not found in scene!");
            }
        }

        // Automatically subscribe to new chat creation
        var newChat = UnityEngine.Object.FindFirstObjectByType<NewChatCreator>();
        if (newChat != null)
        {
            newChat.OnChatCreated += () => SendMessageToBackend();
        }
    }

    public void SendMessageToBackend(string customMessage = null)
    {
        string msgToSend = string.IsNullOrEmpty(customMessage) ? message : customMessage;
        StartCoroutine(SendMessageCoroutine(msgToSend));
    }

    private IEnumerator SendMessageCoroutine(string msg)
    {
        string token = ChatSessionManager.Token;
        string chatId = ChatSessionManager.ChatId;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            Debug.LogError("[UserMessageWithVoice] Token or chatId missing. Create chat first.");
            yield break;
        }

        string url = chatBaseUrl + chatId + "/messages/";

        ChatMessagePayload2 payload = new ChatMessagePayload2
        {
            content = msg,
            model = modelName,
            role = "user",
            voice = true
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
                Debug.LogError("[UserMessageWithVoice] Error: " + uwr.error + " | " + uwr.downloadHandler.text);
                yield break;
            }

            string resp = uwr.downloadHandler.text;
            Debug.Log("[UserMessageWithVoice] Message sent: " + msg);

            // Play assistant response via Piper
            if (piperTTS != null)
            {
                string wrapped = "{\"items\":" + resp + "}";
                var wrapper = JsonUtility.FromJson<VoiceChatHandler.MessageArrayWrapper>(wrapped);
                if (wrapper != null && wrapper.items != null)
                {
                    for (int i = wrapper.items.Length - 1; i >= 0; i--)
                    {
                        var m = wrapper.items[i];
                        if (m.role == "assistant" || m.role == "platform")
                        {
                            piperTTS.Speak(m.content);
                            break;
                        }
                    }
                }
            }
        }
    }
}
