using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class FixedMessageSender : MonoBehaviour
{
    [Header("Chat API")]
    public string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    public string modelName = "llama";

    [Header("Message")]
    [TextArea]
    public string fixedMessage = "Hello, how can you help me?";
    public string role = "platform"; // "platform" or "user"
    public bool usePiper = false;

    [Header("Refs")]
    public PiperTTS piperTTS;

    public void SendOnce()
    {
        StartCoroutine(SendFixed());
    }

    //void Start()
    //{
    //    SendOnce(); // Automatically send the message when scene starts
    //}
    void Start()
    {
        // Find the NewChatCreator in the scene
        var newChat = UnityEngine.Object.FindFirstObjectByType<NewChatCreator>();
        if (newChat != null)
        {
            // Subscribe to the event. 
            // SendOnce will now ONLY run *after* the chat is created.
            newChat.OnChatCreated += SendOnce;
        }
        else
        {
            Debug.LogError("[FixedMessageSender] Could not find NewChatCreator to subscribe to!");
        }

        // DO NOT call SendOnce() here anymore.
        // SendOnce(); // <-- DELETE OR COMMENT OUT THIS LINE
    }

    private IEnumerator SendFixed()
    {
        string token = ChatSessionManager.Token;
        string chatId = ChatSessionManager.ChatId;
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            Debug.LogError("[FixedMessageSender] Token or chatId missing. Create chat first.");
            yield break;
        }

        string url = chatBaseUrl + chatId + "/messages/";
        var payload = new
        {
            content = fixedMessage,
            model = modelName,
            role = role,
            voice = usePiper
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
                Debug.LogError("[FixedMessageSender] Error: " + uwr.error + " | " + uwr.downloadHandler.text);
                yield break;
            }

            string resp = uwr.downloadHandler.text;
            Debug.Log("[FixedMessageSender] Sent message, backend returned: " + resp);

            // Extract assistant reply similarly to VoiceChatHandler if usePiper is true
            if (usePiper && piperTTS != null)
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
