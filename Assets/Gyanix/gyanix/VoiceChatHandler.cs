//using UnityEngine;
//using System.Collections;
//using System.IO;
//using UnityEngine.Networking;
//using System;
//using System.Text;
//using Vosk;

//public class VoiceChatHandler : MonoBehaviour
//{
//    [Header("Vosk Config")]
//    private string voskModelFolder = "vosk-model-small-en-us-0.15";
//    private VoskRecognizer recognizer;
//    private Model model;
//    private AudioClip micClip;
//    private string micDevice;
//    private int sampleRate = 16000;
//    private int readSize = 1024;
//    private int lastPos = 0;

//    private bool isRecording = false;
//    private string finalText = "";

//    [Header("Chat API")]
//    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
//    private string modelName = "llama";

//    [Header("Piper")]
//    private PiperTTS piperTTS;
//    private bool usePiperForReply = true; // if true => voice=true in payload

//    private void Start()
//    {
//        // Microphone check
//        if (Microphone.devices.Length == 0)
//        {
//            Debug.LogError("[VoiceChatHandler] No microphone found!");
//            return;
//        }
//        micDevice = Microphone.devices[0];

//        // Vosk model path
//        string modelPath = Path.Combine(Application.streamingAssetsPath, voskModelFolder);
//        if (!Directory.Exists(modelPath))
//        {
//            Debug.LogError("[VoiceChatHandler] Vosk model not found at: " + modelPath);
//            return;
//        }
//        Vosk.Vosk.SetLogLevel(0);
//        model = new Model(modelPath);
//        recognizer = new VoskRecognizer(model, sampleRate);

//        if (piperTTS == null)
//            piperTTS = GetComponent<PiperTTS>();
//    }

//    private void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.P) && !isRecording)
//        {
//            StartRecording();
//        }

//        if (Input.GetKeyUp(KeyCode.P) && isRecording)
//        {
//            StopRecording();
//        }

//        if (isRecording && micClip != null)
//        {
//            ProcessMicData();
//        }
//    }

//    void StartRecording()
//    {
//        micClip = Microphone.Start(micDevice, true, 10, sampleRate);
//        isRecording = true;
//        lastPos = 0;
//        Debug.Log("[VoiceChatHandler] Recording started");
//    }

//    private string ExtractText(string json)
//    {
//        int idx = json.IndexOf("\"text\"");
//        if (idx == -1) return json;
//        int start = json.IndexOf(":", idx) + 1;
//        int quote1 = json.IndexOf("\"", start) + 1;
//        int quote2 = json.IndexOf("\"", quote1);
//        if (quote1 == -1 || quote2 == -1) return json;
//        return json.Substring(quote1, quote2 - quote1);
//    }

//    void StopRecording()
//    {
//        isRecording = false;
//        Microphone.End(micDevice);

//        finalText = recognizer.FinalResult();
//        Debug.Log("[VoiceChatHandler] Final STT raw: " + finalText);

//        string cleanText = ExtractText(finalText);
//        Debug.Log("[VoiceChatHandler] Final STT clean: " + cleanText);

//        if (!string.IsNullOrEmpty(cleanText))
//        {
//            StartCoroutine(SendToChat(cleanText));
//        }
//    }

//    void ProcessMicData()
//    {
//        int pos = Microphone.GetPosition(micDevice);
//        int diff = pos - lastPos;
//        if (diff < 0) diff += micClip.samples;

//        while (diff >= readSize)
//        {
//            float[] samples = new float[readSize];
//            micClip.GetData(samples, lastPos);
//            byte[] buffer = FloatToPCM16(samples);

//            if (recognizer.AcceptWaveform(buffer, buffer.Length))
//            {
//                Debug.Log("[VoiceChatHandler] Result: " + recognizer.Result());
//            }
//            else
//            {
//                Debug.Log("[VoiceChatHandler] Partial: " + recognizer.PartialResult());
//            }

//            lastPos += readSize;
//            if (lastPos >= micClip.samples) lastPos = 0;
//            diff -= readSize;
//        }
//    }

//    private byte[] FloatToPCM16(float[] samples)
//    {
//        Int16[] intData = new Int16[samples.Length];
//        byte[] bytesData = new byte[samples.Length * 2];
//        for (int i = 0; i < samples.Length; i++)
//        {
//            float f = Mathf.Clamp(samples[i], -1f, 1f);
//            intData[i] = (short)(f * short.MaxValue);
//        }
//        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
//        return bytesData;
//    }

//    [System.Serializable]
//    public class ChatPayload
//    {
//        public string content;
//        public string model;
//        public string role;
//        public bool voice;
//    }

//    // Helper wrapper for parsing array responses via JsonUtility
//    [System.Serializable]
//    public class MessageArrayWrapper
//    {
//        public Message[] items;
//    }

//    [System.Serializable]
//    public class Message
//    {
//        public string message_id;
//        public string chat_id;
//        public string user_id;
//        public string role;
//        public string content;
//        public string model;
//        public bool voice;
//    }

//    private IEnumerator SendToChat(string text)
//    {
//        string token = ChatSessionManager.Token;
//        string chatId = ChatSessionManager.ChatId;
//        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
//        {
//            Debug.LogError("[VoiceChatHandler] Token or chatId is missing. Create a chat first via NewChatCreator.");
//            yield break;
//        }

//        string url = chatBaseUrl + chatId + "/messages/";
//        ChatPayload payload = new ChatPayload
//        {
//            content = text,
//            model = modelName,
//            role = "user",
//            voice = usePiperForReply
//        };

//        string json = JsonUtility.ToJson(payload);
//        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
//        {
//            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
//            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
//            uwr.downloadHandler = new DownloadHandlerBuffer();
//            uwr.SetRequestHeader("Content-Type", "application/json");
//            uwr.SetRequestHeader("Authorization", "Bearer " + token);

//            yield return uwr.SendWebRequest();

//            if (uwr.result != UnityWebRequest.Result.Success)
//            {
//                Debug.LogError("[VoiceChatHandler] SendToChat error: " + uwr.error + " | " + uwr.downloadHandler.text);
//                yield break;
//            }

//            string resp = uwr.downloadHandler.text;
//            // Resp is an array. Wrap it to parse via JsonUtility:
//            string wrapped = "{\"items\":" + resp + "}";
//            MessageArrayWrapper wrapper = null;
//            try
//            {
//                wrapper = JsonUtility.FromJson<MessageArrayWrapper>(wrapped);
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError("[VoiceChatHandler] JSON parse error: " + ex.Message + " | raw: " + resp);
//            }

//            if (wrapper == null || wrapper.items == null || wrapper.items.Length == 0)
//            {
//                Debug.LogWarning("[VoiceChatHandler] No messages in response. Raw: " + resp);
//                yield break;
//            }

//            // find assistant/platform reply
//            Message assistantMsg = null;
//            for (int i = wrapper.items.Length - 1; i >= 0; i--)
//            {
//                var m = wrapper.items[i];
//                if (m.role == "assistant" || m.role == "platform")
//                {
//                    assistantMsg = m;
//                    break;
//                }
//            }

//            if (assistantMsg == null)
//            {
//                Debug.LogWarning("[VoiceChatHandler] No assistant/platform reply found in response array.");
//                yield break;
//            }

//            Debug.Log("[VoiceChatHandler] Assistant reply: " + assistantMsg.content);

//            // If using Piper, send assistant content to Piper
//            if (usePiperForReply && piperTTS != null)
//            {
//                piperTTS.Speak(assistantMsg.content);
//            }
//        }
//    }
//}

using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Text;
using Vosk;
using UnityEngine.InputSystem; // For the new Input System
using UnityEngine.UI;          // For UI Text
using TMPro;                   // For TMP_Text (optional)

public class VoiceChatHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Assign an InputActionReference mapped to your VR controller button.")]
    public InputActionReference pushToTalkAction;

    [Header("UI References")]
    [Tooltip("Assign a UI Text or TMP_Text to display logs in the scene.")]
    public Text uiDebugText; // For legacy UI
    // public TMP_Text uiDebugTMP; // Uncomment if using TextMeshPro
    private StringBuilder debugBuilder = new StringBuilder();
    private int maxDebugLines = 30; // Max lines to show on UI

    [Header("Vosk Config")]
    private string voskModelFolder = "vosk-model-small-en-us-0.15";
    private VoskRecognizer recognizer;
    private Model model;
    private AudioClip micClip;
    private string micDevice;
    private int sampleRate = 16000;
    private int readSize = 1024;
    private int lastPos = 0;

    private bool isRecording = false;
    private string finalText = "";

    [Header("Chat API")]
    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    private string modelName = "llama";

    [Header("Piper")]
    private PiperTTS piperTTS;
    private bool usePiperForReply = true;

    private void Start()
    {
        Log("[VoiceChatHandler] Starting...");

        // 1. Microphone check
        if (Microphone.devices.Length == 0)
        {
            Log("[VoiceChatHandler] No microphone found!", true);
            return;
        }
        micDevice = Microphone.devices[0];
        Log("[VoiceChatHandler] Using mic: " + micDevice);

        // 2. Vosk model path
        string modelPath = Path.Combine(Application.streamingAssetsPath, voskModelFolder);

        // 3. Load Vosk model (FIXED for Android/VR)
        // We use try...catch instead of Directory.Exists, which fails on Android.
        try
        {
            Vosk.Vosk.SetLogLevel(0);
            model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, sampleRate);
            Log("[VoiceChatHandler] Vosk recognizer initialized.");
        }
        catch (Exception ex)
        {
            Log("[VoiceChatHandler] Vosk model failed to load from: " + modelPath, true);
            Log("[VoiceChatHandler] Error: " + ex.Message, true);
            Log("[VoiceChatHandler] (Did you put the '" + voskModelFolder + "' folder inside Assets/StreamingAssets?)", true);
            return; // Stop if the model failed
        }

        // 4. Find Piper
        if (piperTTS == null)
            piperTTS = GetComponent<PiperTTS>();

        // 5. Setup VR input
        SetupInput();
    }

    private void SetupInput()
    {
        if (pushToTalkAction == null)
        {
            Log("[VoiceChatHandler] 'Push To Talk Action' not assigned in Inspector!", true);
            return;
        }

        pushToTalkAction.action.started += ctx => StartRecording();
        pushToTalkAction.action.canceled += ctx => StopRecording();
        pushToTalkAction.action.Enable();

        Log("[VoiceChatHandler] VR Push-to-Talk input is ready.");
    }

    private void OnDestroy()
    {
        if (pushToTalkAction != null)
        {
            pushToTalkAction.action.started -= ctx => StartRecording();
            pushToTalkAction.action.canceled -= ctx => StopRecording();
        }

        // Clean up Vosk resources
        if (recognizer != null) recognizer.Dispose();
        if (model != null) model.Dispose();
    }

    private void Update()
    {
        if (isRecording && micClip != null)
        {
            ProcessMicData();
        }
    }

    void StartRecording()
    {
        if (isRecording) return;

        micClip = Microphone.Start(micDevice, true, 10, sampleRate);
        isRecording = true;
        lastPos = 0;
        Log("🎙️ Recording...");
    }

    void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        Microphone.End(micDevice);

        finalText = recognizer.FinalResult();
        Log("Processing..."); // User-friendly status

        string cleanText = ExtractText(finalText, "text");
        Log("You said: " + cleanText);

        if (!string.IsNullOrEmpty(cleanText))
        {
            StartCoroutine(SendToChat(cleanText));
        }
        else
        {
            Log("Could not understand. Try again.");
        }
    }

    // Helper class for Vosk's JSON response
    [System.Serializable]
    private class VoskResult
    {
        public string text = "";
        public string partial = "";
    }

    private string ExtractText(string json, string key = "partial")
    {
        // Use JsonUtility to parse
        try
        {
            VoskResult result = JsonUtility.FromJson<VoskResult>(json);
            if (key == "text" && !string.IsNullOrEmpty(result.text))
            {
                return result.text;
            }
            if (key == "partial" && !string.IsNullOrEmpty(result.partial))
            {
                return result.partial;
            }
        }
        catch (Exception)
        {
            // Fallback for non-JSON or malformed strings
        }
        return string.Empty; // Return empty if key not found
    }


    void ProcessMicData()
    {
        int pos = Microphone.GetPosition(micDevice);
        int diff = pos - lastPos;
        if (diff < 0) diff += micClip.samples;

        while (diff >= readSize)
        {
            float[] samples = new float[readSize];
            micClip.GetData(samples, lastPos);
            byte[] buffer = FloatToPCM16(samples);

            if (recognizer.AcceptWaveform(buffer, buffer.Length))
            {
                // Full result (though we get this in StopRecording)
                string fullResult = ExtractText(recognizer.Result(), "text");
                if (!string.IsNullOrEmpty(fullResult)) Log(fullResult);
            }
            else
            {
                // Show partial result
                string partialResult = ExtractText(recognizer.PartialResult(), "partial");
                if (!string.IsNullOrEmpty(partialResult)) Log("..." + partialResult);
            }

            lastPos += readSize;
            if (lastPos >= micClip.samples) lastPos = 0;
            diff -= readSize;
        }
    }

    private byte[] FloatToPCM16(float[] samples)
    {
        Int16[] intData = new Int16[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float f = Mathf.Clamp(samples[i], -1f, 1f);
            intData[i] = (short)(f * short.MaxValue);
        }
        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
        return bytesData;
    }

    // ---------------------------
    // Custom unified logging
    // ---------------------------
    private void Log(string message, bool isError = false)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (isError)
            Debug.LogError(message);
        else
            Debug.Log(message);

        // Update UI Text
        if (uiDebugText != null)
        {
            debugBuilder.AppendLine(message);

            // Trim old lines
            var lines = debugBuilder.ToString().Split('\n');
            if (lines.Length > maxDebugLines)
            {
                debugBuilder.Clear();
                int start = lines.Length - maxDebugLines;
                for (int i = start; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                        debugBuilder.AppendLine(lines[i]);
                }
            }

            uiDebugText.text = debugBuilder.ToString();
        }

        // Uncomment this block if you switch to TextMeshPro
        // if (uiDebugTMP != null)
        // {
        //     debugBuilder.AppendLine(message);
        //     var lines = debugBuilder.ToString().Split('\n');
        //     if (lines.Length > maxDebugLines)
        //     {
        //         debugBuilder.Clear();
        //         int start = lines.Length - maxDebugLines;
        //         for (int i = start; i < lines.Length; i++)
        //         {
        //              if(!string.IsNullOrEmpty(lines[i]))
        //                 debugBuilder.AppendLine(lines[i]);
        //         }
        //     }
        //     uiDebugTMP.text = debugBuilder.ToString();
        // }
    }


    #region === Chat API Section ===

    [System.Serializable]
    public class ChatPayload
    {
        public string content;
        public string model;
        public string role;
        public bool voice;
    }

    [System.Serializable]
    public class MessageArrayWrapper
    {
        public Message[] items;
    }

    [System.Serializable]
    public class Message
    {
        public string message_id;
        public string chat_id;
        public string user_id;
        public string role;
        public string content;
        public string model;
        public bool voice;
    }

    private IEnumerator SendToChat(string text)
    {
        string token = ChatSessionManager.Token;
        string chatId = ChatSessionManager.ChatId;
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            Log("[VoiceChatHandler] Token or chatId missing. Create chat first.", true);
            yield break;
        }

        string url = chatBaseUrl + chatId + "/messages/";
        ChatPayload payload = new ChatPayload
        {
            content = text,
            model = modelName,
            role = "user",
            voice = usePiperForReply
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
                Log("[VoiceChatHandler] SendToChat error: " + uwr.error + " | " + uwr.downloadHandler.text, true);
                yield break;
            }

            string resp = uwr.downloadHandler.text;
            string wrapped = "{\"items\":" + resp + "}";
            MessageArrayWrapper wrapper = null;
            try
            {
                wrapper = JsonUtility.FromJson<MessageArrayWrapper>(wrapped);
            }
            catch (Exception ex)
            {
                Log("[VoiceChatHandler] JSON parse error: " + ex.Message + " | raw: " + resp, true);
            }

            if (wrapper == null || wrapper.items == null || wrapper.items.Length == 0)
            {
                Log("[VoiceChatHandler] No messages in response. Raw: " + resp, true);
                yield break;
            }

            Message assistantMsg = null;
            for (int i = wrapper.items.Length - 1; i >= 0; i--)
            {
                var m = wrapper.items[i];
                if (m.role == "assistant" || m.role == "platform")
                {
                    assistantMsg = m;
                    break;
                }
            }

            if (assistantMsg == null)
            {
                Log("[VoiceChatHandler] No assistant/platform reply found in response.", true);
                yield break;
            }

            Log("Assistant: " + assistantMsg.content);

            if (usePiperForReply && piperTTS != null)
            {
                piperTTS.Speak(assistantMsg.content);
            }
        }
    }
    #endregion
}