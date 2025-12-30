////Fuzzy Approach
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using System;
//using System.Text;
//using System.Linq;
//using GoogleTextToSpeech.Scripts.Data;
//using GoogleTextToSpeech.Scripts;

//// ========================================== 
//// Data Models
//// ========================================== 
//[System.Serializable]
//public class ChatMessagePayload
//{
//    public string content;
//    public string model;
//    public string role;
//    public bool voice;
//}

//[System.Serializable]
//public class MessageArrayWrapper
//{
//    public Message[] items;
//}

//[System.Serializable]
//public class Message
//{
//    public string message_id;
//    public string chat_id;
//    public string user_id;
//    public string role;
//    public string content;
//    public string model;
//    public bool voice;
//}

//[System.Serializable]
//public class LanguageConfig
//{
//    public string languageName; // "English", "Hindi", "Marathi"
//    public string languageCode; // "en-US", "hi-IN", "mr-IN"
//    public VoiceScriptableObject voiceConfig;

//    [TextArea(2, 5)]
//    public string firstMessage;

//    [TextArea(1, 3)]
//    public string confirmText;

//    [Header("Alternative Names & Keywords (auto-generated at runtime)")]
//    [Tooltip("Additional keywords/variations for detection. Leave empty - will be auto-populated.")]
//    public List<string> detectionKeywords = new List<string>();
//}

//// ========================================== 
//// Fuzzy String Matcher (Levenshtein Distance)
//// ========================================== 
//public static class FuzzyMatcher
//{
//    /// <summary>
//    /// Calculates Levenshtein distance between two strings
//    /// </summary>
//    public static int LevenshteinDistance(string s, string t)
//    {
//        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
//        if (string.IsNullOrEmpty(t)) return s.Length;

//        int n = s.Length;
//        int m = t.Length;
//        int[,] d = new int[n + 1, m + 1];

//        for (int i = 0; i <= n; i++) d[i, 0] = i;
//        for (int j = 0; j <= m; j++) d[0, j] = j;

//        for (int i = 1; i <= n; i++)
//        {
//            for (int j = 1; j <= m; j++)
//            {
//                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
//                d[i, j] = Mathf.Min(
//                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
//                    d[i - 1, j - 1] + cost
//                );
//            }
//        }

//        return d[n, m];
//    }

//    /// <summary>
//    /// Returns similarity score (0-100) between two strings
//    /// </summary>
//    public static float SimilarityScore(string s1, string s2)
//    {
//        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0f;

//        s1 = s1.ToLowerInvariant().Trim();
//        s2 = s2.ToLowerInvariant().Trim();

//        if (s1 == s2) return 100f;

//        int maxLen = Mathf.Max(s1.Length, s2.Length);
//        int distance = LevenshteinDistance(s1, s2);

//        return (1f - (float)distance / maxLen) * 100f;
//    }

//    /// <summary>
//    /// Finds best match from a list of candidates
//    /// </summary>
//    public static (string match, float score) FindBestMatch(string input, List<string> candidates)
//    {
//        if (string.IsNullOrEmpty(input) || candidates == null || candidates.Count == 0)
//            return (null, 0f);

//        string bestMatch = null;
//        float bestScore = 0f;

//        foreach (string candidate in candidates)
//        {
//            if (string.IsNullOrEmpty(candidate)) continue;

//            float score = SimilarityScore(input, candidate);
//            if (score > bestScore)
//            {
//                bestScore = score;
//                bestMatch = candidate;
//            }
//        }

//        return (bestMatch, bestScore);
//    }
//}

//// ========================================== 
//// Main NPC Chat Handler with Fuzzy Detection
//// ========================================== 
//public class CustomChatNPCSystem : MonoBehaviour
//{
//    [Header("API Settings")]
//    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
//    private string modelName = "llama";

//    [Header("NPC Persona & Environment")]
//    [TextArea(8, 16)]
//    public string systemPrompt = @"
//You are Dr. Priya Sharma, a warm, friendly, and professional female AI Lab Assistant in a fully interactive VR Chemistry Laboratory.

//STRICT RULES YOU MUST NEVER BREAK:
//1. You are in a CHEMISTRY lab. NEVER talk about physics, gravity, biology, math, or anything else.
//2. If the student asks about non-chemistry topics (like gravity, force, etc.), politely say: 'Sorry, we are in a chemistry lab. Let's talk about reactions, compounds, or equipment!'
//3. Reply EXCLUSIVELY in {{LANGUAGE}} — not a single word from any other language.
//4. Introduce yourself ONLY ONCE with the exact intro below. Never say 'welcome' or 'namaste' again.
//5. Keep replies short (2–4 sentences), natural, and encouraging.
//6. To talk to you, the student must PRESS AND HOLD the trigger button on VR controller.
//7. Never ask them to wear goggles or gloves.

//FIRST MESSAGE (use exactly this):
//{{FIRST_MESSAGE}}

//From now on, obey these rules forever.
//";

//    [Tooltip("If true, NPC will send the first-message to backend when session is ready.")]
//    public bool initializeWithIntroduction = true;

//    [Header("Language Settings")]
//    [SerializeField] private List<LanguageConfig> supportedLanguages = new List<LanguageConfig>();
//    [SerializeField] private int currentLanguageIndex = 0;

//    [Header("Fuzzy Detection Settings")]
//    [SerializeField]
//    [Range(50f, 100f)]
//    [Tooltip("Minimum similarity score (0-100) to accept a language match")]
//    private float minimumMatchThreshold = 70f;

//    [Header("Dependencies")]
//    [SerializeField] private MultilingualTextToSpeechManager googleServices;

//    private List<ChatMessagePayload> chatHistory = new List<ChatMessagePayload>();
//    private LanguageConfig currentLanguage;
//    private bool hasInitializedPersona = false;

//    // Cache for all detection keywords
//    private Dictionary<string, int> keywordToLanguageIndex = new Dictionary<string, int>();

//    void Start()
//    {
//        // 1. Setup Language Detection Keywords
//        InitializeLanguageDetection();

//        // 2. Setup Current Language
//        if (supportedLanguages != null && supportedLanguages.Count > 0 && currentLanguageIndex < supportedLanguages.Count)
//        {
//            currentLanguage = supportedLanguages[currentLanguageIndex];
//            Debug.Log($"[CustomChatNPCSystem] Initial Language: {currentLanguage.languageName}");
//        }
//        else
//        {
//            Debug.LogWarning("[CustomChatNPCSystem] No languages configured or index out of range!");
//        }

//        // 3. Find TTS Service
//        if (googleServices == null)
//            googleServices = FindFirstObjectByType<MultilingualTextToSpeechManager>();

//        // 4. Wait for Chat Token
//        if (string.IsNullOrEmpty(ChatSessionManager.Token) || string.IsNullOrEmpty(ChatSessionManager.ChatId))
//        {
//            Debug.LogWarning("[CustomChatNPCSystem] Waiting for session...");
//            var newChatCreator = FindFirstObjectByType<NewChatCreator>();
//            if (newChatCreator != null)
//            {
//                newChatCreator.OnChatCreated += OnChatSessionReady;
//            }
//        }
//        else
//        {
//            OnChatSessionReady();
//        }
//    }

//    // ========================================== 
//    // Fuzzy Language Detection Initialization
//    // ========================================== 
//    private void InitializeLanguageDetection()
//    {
//        keywordToLanguageIndex.Clear();

//        for (int i = 0; i < supportedLanguages.Count; i++)
//        {
//            LanguageConfig lang = supportedLanguages[i];
//            if (lang == null || string.IsNullOrEmpty(lang.languageName)) continue;

//            // Primary language name
//            string primaryName = lang.languageName.ToLowerInvariant().Trim();

//            // Add primary name to dictionary
//            if (!keywordToLanguageIndex.ContainsKey(primaryName))
//            {
//                keywordToLanguageIndex[primaryName] = i;
//            }

//            // Auto-generate detection keywords if not manually specified
//            if (lang.detectionKeywords == null || lang.detectionKeywords.Count == 0)
//            {
//                lang.detectionKeywords = GenerateDetectionKeywords(lang.languageName);
//            }

//            // Add all detection keywords to dictionary
//            foreach (string keyword in lang.detectionKeywords)
//            {
//                if (string.IsNullOrEmpty(keyword)) continue;
//                string key = keyword.ToLowerInvariant().Trim();
//                if (!keywordToLanguageIndex.ContainsKey(key))
//                {
//                    keywordToLanguageIndex[key] = i;
//                }
//            }

//            Debug.Log($"[Language Detection] {lang.languageName}: {lang.detectionKeywords.Count + 1} keywords registered");
//        }
//    }

//    /// <summary>
//    /// Auto-generates common variations and transliterations for a language name
//    /// </summary>
//    private List<string> GenerateDetectionKeywords(string languageName)
//    {
//        List<string> keywords = new List<string>();
//        string lower = languageName.ToLowerInvariant();

//        // Common transliterations and variations by language
//        switch (lower)
//        {
//            case "english":
//                keywords.AddRange(new[] { "english", "inglish", "angrezi", "angrez", "इंग्लिश", "अंग्रेजी" });
//                break;
//            case "hindi":
//                keywords.AddRange(new[] { "hindi", "hindee", "hindy", "हिंदी", "हिन्दी", "हिन्दि" });
//                break;
//            case "marathi":
//                keywords.AddRange(new[] { "marathi", "marathy", "marathee", "मराठी", "मराठि" });
//                break;
//            case "spanish":
//                keywords.AddRange(new[] { "spanish", "espanol", "español", "spanis" });
//                break;
//            case "french":
//                keywords.AddRange(new[] { "french", "francais", "français", "franch" });
//                break;
//            case "german":
//                keywords.AddRange(new[] { "german", "deutsch", "jerman", "germn" });
//                break;
//            case "gujarati":
//                keywords.AddRange(new[] { "gujarati", "gujrati", "gujarathi", "ગુજરાતી" });
//                break;
//            case "bengali":
//                keywords.AddRange(new[] { "bengali", "bangla", "bengalee", "বাংলা" });
//                break;
//            case "tamil":
//                keywords.AddRange(new[] { "tamil", "thamil", "tamizh", "தமிழ்" });
//                break;
//            case "telugu":
//                keywords.AddRange(new[] { "telugu", "telgu", "తెలుగు" });
//                break;
//            default:
//                // Just add the language name itself
//                keywords.Add(lower);
//                break;
//        }

//        return keywords;
//    }

//    // ========================================== 
//    // Smart Language Detection with Fuzzy Matching
//    // ========================================== 
//    private bool DetectAndSwitchLanguage(string userInput)
//    {
//        if (string.IsNullOrEmpty(userInput)) return false;

//        // Normalize input
//        string normalizedInput = userInput.ToLowerInvariant().Trim();

//        // Extract potential language words from input (split by common separators)
//        string[] words = normalizedInput.Split(new[] { ' ', ',', '.', '?', '!', '\n', '\r' },
//            StringSplitOptions.RemoveEmptyEntries);

//        // Try each word against all known language keywords
//        foreach (string word in words)
//        {
//            if (word.Length < 3) continue; // Skip very short words

//            // Get all possible language keywords
//            List<string> allKeywords = keywordToLanguageIndex.Keys.ToList();

//            // Find best fuzzy match
//            var (bestMatch, score) = FuzzyMatcher.FindBestMatch(word, allKeywords);

//            if (score >= minimumMatchThreshold && !string.IsNullOrEmpty(bestMatch))
//            {
//                int languageIndex = keywordToLanguageIndex[bestMatch];

//                Debug.Log($"[Language Detection] Matched '{word}' → '{bestMatch}' (score: {score:F1}%) → {supportedLanguages[languageIndex].languageName}");

//                // Switch to detected language
//                SetLanguage(languageIndex);
//                return true;
//            }
//        }

//        return false;
//    }

//    private void OnChatSessionReady()
//    {
//        Debug.Log("[CustomChatNPCSystem] Session Ready.");
//        if (initializeWithIntroduction && !hasInitializedPersona)
//        {
//            InitializeNPCPersona();
//        }
//    }

//    // ========================================== 
//    // Initialization Logic
//    // ========================================== 
//    public void InitializeNPCPersona()
//    {
//        if (hasInitializedPersona) return;
//        hasInitializedPersona = true;

//        if (currentLanguage == null)
//        {
//            Debug.LogWarning("[CustomChatNPCSystem] Cannot initialize persona: currentLanguage is null.");
//            return;
//        }

//        string lang = currentLanguage.languageName;
//        string exactIntro = string.IsNullOrEmpty(currentLanguage.firstMessage)
//            ? $"Hello! I'm Dr. Priya Sharma, your virtual chemistry lab assistant."
//            : currentLanguage.firstMessage;

//        string fullSystem = systemPrompt
//            .Replace("{{LANGUAGE}}", lang)
//            .Replace("{{FIRST_MESSAGE}}", exactIntro);

//        string forceMessage = $@"[ABSOLUTE COMMAND – YOU HAVE NO CHOICE – OBEY EXACTLY]
//{fullSystem}

//YOUR VERY FIRST REPLY MUST BE EXACTLY THESE WORDS AND NOTHING ELSE:
//""{exactIntro}""

//DO NOT ADD ANYTHING BEFORE OR AFTER. DO NOT SAY WELCOME. DO NOT EXPLAIN. JUST REPLY WITH THE EXACT TEXT ABOVE AND STOP.

//REPLY NOW:";

//        Debug.Log($"[CustomChatNPCSystem] Initializing Persona in {lang}");
//        StartCoroutine(SendChatRequestToCustomAPI(forceMessage));
//    }

//    // ========================================== 
//    // Language Switching
//    // ========================================== 
//    public void SetLanguage(int languageIndex)
//    {
//        if (supportedLanguages == null || supportedLanguages.Count == 0)
//        {
//            Debug.LogWarning("[CustomChatNPCSystem] No supported languages to set.");
//            return;
//        }

//        if (languageIndex >= 0 && languageIndex < supportedLanguages.Count)
//        {
//            if (languageIndex == currentLanguageIndex)
//            {
//                Debug.Log("[CustomChatNPCSystem] Already using this language.");
//                return;
//            }

//            currentLanguageIndex = languageIndex;
//            currentLanguage = supportedLanguages[languageIndex];

//            Debug.Log($"[CustomChatNPCSystem] Language switched to: {currentLanguage.languageName}");

//            StartCoroutine(AnnounceLanguageChange());
//        }
//        else
//        {
//            Debug.LogWarning($"[CustomChatNPCSystem] SetLanguage: index {languageIndex} out of range.");
//        }
//    }

//    private IEnumerator AnnounceLanguageChange()
//    {
//        yield return new WaitForEndOfFrame();

//        if (currentLanguage == null) yield break;

//        string lang = currentLanguage.languageName;
//        string confirmText = string.IsNullOrEmpty(currentLanguage.confirmText)
//            ? $"Got it! From now on, I will only speak in {lang}."
//            : currentLanguage.confirmText;

//        string switchCommand = $@"[ABSOLUTE COMMAND – YOU HAVE NO CHOICE – OBEY EXACTLY]
//THE LANGUAGE HAS BEEN PERMANENTLY CHANGED TO {lang.ToUpper()}

//FROM THIS MOMENT FORWARD:
//- You now speak, think, and reply EXCLUSIVELY in {lang} only.
//- Never use any other language again — EVER.
//- Your next reply must be EXACTLY this text and nothing else:
//""{confirmText}""

//DO NOT ADD ANYTHING BEFORE OR AFTER. DO NOT RE-INTRODUCE YOURSELF. JUST REPLY WITH THE EXACT TEXT ABOVE.

//REPLY NOW:";

//        StartCoroutine(SendChatRequestToCustomAPI(switchCommand));
//    }

//    public string GetCurrentLanguageCode()
//    {
//        return currentLanguage != null ? currentLanguage.languageCode : "en-US";
//    }

//    // ========================================== 
//    // Input Error Handling
//    // ========================================== 
//    public void SpeakInputErrorMessage()
//    {
//        if (currentLanguage == null) return;

//        string errorMessage = "I could not hear you. Please speak again.";
//        string code = currentLanguage.languageCode?.ToLower() ?? "";

//        if (code.Contains("hi"))
//            errorMessage = "क्षमा करें, मैंने सुना नहीं। कृपया पुनः प्रयास करें।";
//        else if (code.Contains("mr"))
//            errorMessage = "क्षमस्व, मला ऐकू आले नाही. कृपया पुन्हा बोला.";

//        Debug.Log($"[CustomChatNPCSystem] Input Error. Speaking: {errorMessage}");

//        if (googleServices != null && currentLanguage.voiceConfig != null)
//        {
//            googleServices.SendTextToGoogleWithVoice(errorMessage, currentLanguage.voiceConfig);
//        }
//        else if (googleServices != null)
//        {
//            googleServices.SendTextToGoogle(errorMessage);
//        }
//    }

//    // ========================================== 
//    // Main Chat Handling
//    // ========================================== 
//    public void SendChat(string userMessage)
//    {
//        if (string.IsNullOrEmpty(userMessage)) return;

//        // Try fuzzy language detection first
//        if (DetectAndSwitchLanguage(userMessage))
//        {
//            Debug.Log("[CustomChatNPCSystem] Language switch command detected and executed.");
//            return; // Don't send the language switch request as a chat message
//        }

//        // Normal chat processing
//        StartCoroutine(SendChatRequestToCustomAPI(userMessage));
//    }

//    private IEnumerator SendChatRequestToCustomAPI(string newMessage)
//    {
//        string token = ChatSessionManager.Token;
//        string chatId = ChatSessionManager.ChatId;

//        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId)) yield break;

//        string url = chatBaseUrl + chatId + "/messages/";
//        string targetLang = currentLanguage?.languageName ?? "English";

//        string finalContent = $"{newMessage}\n\n[REPLY ONLY IN {targetLang.ToUpper()} – NO OTHER LANGUAGE EVER]";

//        ChatMessagePayload payload = new ChatMessagePayload
//        {
//            content = finalContent,
//            model = modelName,
//            role = "user",
//            voice = true
//        };

//        string jsonData = JsonUtility.ToJson(payload);
//        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

//        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
//        {
//            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
//            www.downloadHandler = new DownloadHandlerBuffer();
//            www.SetRequestHeader("Content-Type", "application/json");
//            www.SetRequestHeader("Authorization", "Bearer " + token);

//            yield return www.SendWebRequest();

//            if (www.result != UnityWebRequest.Result.Success)
//            {
//                Debug.LogError($"[CustomChatNPCSystem] Error: {www.error} | {www.downloadHandler.text}");
//                yield break;
//            }

//            string responseText = www.downloadHandler.text;
//            string wrapped = "{\"items\":" + responseText + "}";

//            MessageArrayWrapper wrapper = null;
//            try
//            {
//                wrapper = JsonUtility.FromJson<MessageArrayWrapper>(wrapped);
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"[CustomChatNPCSystem] JSON Parse Error: {e.Message}");
//                yield break;
//            }

//            if (wrapper == null || wrapper.items == null) yield break;

//            Message assistantMsg = null;
//            for (int i = wrapper.items.Length - 1; i >= 0; i--)
//            {
//                if (wrapper.items[i].role == "assistant" || wrapper.items[i].role == "platform")
//                {
//                    assistantMsg = wrapper.items[i];
//                    break;
//                }
//            }

//            if (assistantMsg != null)
//            {
//                string reply = assistantMsg.content;
//                Debug.Log($"[NPC Reply]: {reply}");

//                if (googleServices != null)
//                {
//                    if (currentLanguage != null && currentLanguage.voiceConfig != null)
//                        googleServices.SendTextToGoogleWithVoice(reply, currentLanguage.voiceConfig);
//                    else
//                        googleServices.SendTextToGoogle(reply);
//                }
//            }
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Linq;
using GoogleTextToSpeech.Scripts.Data;
using GoogleTextToSpeech.Scripts;

// ========================================== 
// Data Models
// ========================================== 
[System.Serializable]
public class ChatMessagePayloadGyanix
{
    public string content;
    public string model;
    public string role;
    public bool voice;
}

[System.Serializable]
public class MessageArrayWrapperGyanix
{
    public MessageGyanix[] items;
}

[System.Serializable]
public class MessageGyanix
{
    public string message_id;
    public string chat_id;
    public string user_id;
    public string role;
    public string content;
    public string model;
    public bool voice;
}

[System.Serializable]
public class LanguageConfigGyanix
{
    public string languageName; // "English", "Hindi", "Marathi"
    public string languageCode; // "en-US", "hi-IN", "mr-IN"
    public VoiceScriptableObject voiceConfig;

    [TextArea(2, 5)]
    public string firstMessage;

    [TextArea(1, 3)]
    public string confirmText;

    [Header("Alternative Names & Keywords (auto-generated at runtime)")]
    [Tooltip("Additional keywords/variations for detection. Leave empty - will be auto-populated.")]
    public List<string> detectionKeywords = new List<string>();
}

// ========================================== 
// Fuzzy String Matcher (Levenshtein Distance)
// ========================================== 
public static class FuzzyMatcherGyanix
{
    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Returns similarity score (0-100) between two strings
    /// </summary>
    public static float SimilarityScore(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0f;

        s1 = s1.ToLowerInvariant().Trim();
        s2 = s2.ToLowerInvariant().Trim();

        if (s1 == s2) return 100f;

        int maxLen = Mathf.Max(s1.Length, s2.Length);
        int distance = LevenshteinDistance(s1, s2);

        return (1f - (float)distance / maxLen) * 100f;
    }

    /// <summary>
    /// Finds best match from a list of candidates
    /// </summary>
    public static (string match, float score) FindBestMatch(string input, List<string> candidates)
    {
        if (string.IsNullOrEmpty(input) || candidates == null || candidates.Count == 0)
            return (null, 0f);

        string bestMatch = null;
        float bestScore = 0f;

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate)) continue;

            float score = SimilarityScore(input, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = candidate;
            }
        }

        return (bestMatch, bestScore);
    }
}

// ========================================== 
// Main NPC Chat Handler with Events
// ========================================== 
public class GyanixChatNPCSystem : MonoBehaviour
{
    [Header("API Settings")]
    private string chatBaseUrl = "https://chatbackenddev.guruvrmetaversity.com/chats/";
    private string modelName = "llama";

    [Header("NPC Persona & Environment")]
    [TextArea(8, 16)]
    public string systemPrompt = @"
You are Dr. Priya Sharma, a warm, friendly, and professional female AI Lab Assistant in a fully interactive VR Chemistry Laboratory.

STRICT RULES YOU MUST NEVER BREAK:
1. You are in a CHEMISTRY lab. NEVER talk about physics, gravity, biology, math, or anything else.
2. If the student asks about non-chemistry topics (like gravity, force, etc.), politely say: 'Sorry, we are in a chemistry lab. Let's talk about reactions, compounds, or equipment!'
3. Reply EXCLUSIVELY in {{LANGUAGE}} — not a single word from any other language.
4. Introduce yourself ONLY ONCE with the exact intro below. Never say 'welcome' or 'namaste' again.
5. Keep replies short (2–4 sentences), natural, and encouraging.
6. To talk to you, the student must PRESS AND HOLD the trigger button on VR controller.
7. Never ask them to wear goggles or gloves.

FIRST MESSAGE (use exactly this):
{{FIRST_MESSAGE}}

From now on, obey these rules forever.
";

    [Tooltip("If true, NPC will send the first-message to backend when session is ready.")]
    public bool initializeWithIntroduction = true;

    [Header("Language Settings")]
    [SerializeField] private List<LanguageConfigGyanix> supportedLanguages = new List<LanguageConfigGyanix>();
    [SerializeField] private int currentLanguageIndex = 0;

    [Header("Fuzzy Detection Settings")]
    [SerializeField]
    [Range(50f, 100f)]
    [Tooltip("Minimum similarity score (0-100) to accept a language match")]
    private float minimumMatchThreshold = 70f;

    [Header("Dependencies")]
    [SerializeField] private MultilingualTextToSpeechManager googleServices;

    [Header("Debug")]
    [SerializeField] private bool enableEventLogs = true;

    private List<ChatMessagePayloadGyanix> chatHistory = new List<ChatMessagePayloadGyanix>();
    private LanguageConfigGyanix currentLanguage;
    private bool hasInitializedPersona = false;

    // Cache for all detection keywords
    private Dictionary<string, int> keywordToLanguageIndex = new Dictionary<string, int>();

    void Start()
    {
        // 1. Setup Language Detection Keywords
        InitializeLanguageDetection();

        // 2. Setup Current Language
        if (supportedLanguages != null && supportedLanguages.Count > 0 && currentLanguageIndex < supportedLanguages.Count)
        {
            currentLanguage = supportedLanguages[currentLanguageIndex];
            Debug.Log($"[CustomChatNPCSystem] Initial Language: {currentLanguage.languageName}");
        }
        else
        {
            Debug.LogWarning("[CustomChatNPCSystem] No languages configured or index out of range!");
        }

        // 3. Find TTS Service
        if (googleServices == null)
            googleServices = FindFirstObjectByType<MultilingualTextToSpeechManager>();

        // 4. Wait for Chat Token
        if (string.IsNullOrEmpty(ChatSessionManager.Token) || string.IsNullOrEmpty(ChatSessionManager.ChatId))
        {
            Debug.LogWarning("[CustomChatNPCSystem] Waiting for session...");
            var newChatCreator = FindFirstObjectByType<NewChatCreator>();
            if (newChatCreator != null)
            {
                newChatCreator.OnChatCreated += OnChatSessionReady;
            }
        }
        else
        {
            OnChatSessionReady();
        }
    }

    // ========================================== 
    // Fuzzy Language Detection Initialization
    // ========================================== 
    private void InitializeLanguageDetection()
    {
        keywordToLanguageIndex.Clear();

        for (int i = 0; i < supportedLanguages.Count; i++)
        {
            LanguageConfigGyanix lang = supportedLanguages[i];
            if (lang == null || string.IsNullOrEmpty(lang.languageName)) continue;

            // Primary language name
            string primaryName = lang.languageName.ToLowerInvariant().Trim();

            // Add primary name to dictionary
            if (!keywordToLanguageIndex.ContainsKey(primaryName))
            {
                keywordToLanguageIndex[primaryName] = i;
            }

            // Auto-generate detection keywords if not manually specified
            if (lang.detectionKeywords == null || lang.detectionKeywords.Count == 0)
            {
                lang.detectionKeywords = GenerateDetectionKeywords(lang.languageName);
            }

            // Add all detection keywords to dictionary
            foreach (string keyword in lang.detectionKeywords)
            {
                if (string.IsNullOrEmpty(keyword)) continue;
                string key = keyword.ToLowerInvariant().Trim();
                if (!keywordToLanguageIndex.ContainsKey(key))
                {
                    keywordToLanguageIndex[key] = i;
                }
            }

            Debug.Log($"[Language Detection] {lang.languageName}: {lang.detectionKeywords.Count + 1} keywords registered");
        }
    }

    /// <summary>
    /// Auto-generates common variations and transliterations for a language name
    /// </summary>
    private List<string> GenerateDetectionKeywords(string languageName)
    {
        List<string> keywords = new List<string>();
        string lower = languageName.ToLowerInvariant();

        // Common transliterations and variations by language
        switch (lower)
        {
            case "english":
                keywords.AddRange(new[] { "english", "inglish", "angrezi", "angrez", "इंग्लिश", "अंग्रेजी" });
                break;
            case "hindi":
                keywords.AddRange(new[] { "hindi", "hindee", "hindy", "हिंदी", "हिन्दी", "हिन्दि" });
                break;
            case "marathi":
                keywords.AddRange(new[] { "marathi", "marathy", "marathee", "मराठी", "मराठि" });
                break;
            case "spanish":
                keywords.AddRange(new[] { "spanish", "espanol", "español", "spanis" });
                break;
            case "french":
                keywords.AddRange(new[] { "french", "francais", "français", "franch" });
                break;
            case "german":
                keywords.AddRange(new[] { "german", "deutsch", "jerman", "germn" });
                break;
            case "gujarati":
                keywords.AddRange(new[] { "gujarati", "gujrati", "gujarathi", "ગુજરાતી" });
                break;
            case "bengali":
                keywords.AddRange(new[] { "bengali", "bangla", "bengalee", "বাংলা" });
                break;
            case "tamil":
                keywords.AddRange(new[] { "tamil", "thamil", "tamizh", "தமிழ்" });
                break;
            case "telugu":
                keywords.AddRange(new[] { "telugu", "telgu", "తెలుగు" });
                break;
            default:
                // Just add the language name itself
                keywords.Add(lower);
                break;
        }

        return keywords;
    }

    // ========================================== 
    // Smart Language Detection with Fuzzy Matching
    // ========================================== 
    private bool DetectAndSwitchLanguage(string userInput)
    {
        if (string.IsNullOrEmpty(userInput)) return false;

        // Normalize input
        string normalizedInput = userInput.ToLowerInvariant().Trim();

        // Extract potential language words from input (split by common separators)
        string[] words = normalizedInput.Split(new[] { ' ', ',', '.', '?', '!', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        // Try each word against all known language keywords
        foreach (string word in words)
        {
            if (word.Length < 3) continue; // Skip very short words

            // Get all possible language keywords
            List<string> allKeywords = keywordToLanguageIndex.Keys.ToList();

            // Find best fuzzy match
            var (bestMatch, score) = FuzzyMatcherGyanix.FindBestMatch(word, allKeywords);

            if (score >= minimumMatchThreshold && !string.IsNullOrEmpty(bestMatch))
            {
                int languageIndex = keywordToLanguageIndex[bestMatch];
                string detectedLanguage = supportedLanguages[languageIndex].languageName;

                if (enableEventLogs)
                    Debug.Log($"[Language Detection] Matched '{word}' → '{bestMatch}' (score: {score:F1}%) → {detectedLanguage}");

                // 🔥 FIRE EVENT: Language detection attempt
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeLanguageDetectionAttempt(detectedLanguage, true);

                // Switch to detected language
                SetLanguage(languageIndex);
                return true;
            }
        }

        // 🔥 FIRE EVENT: Language detection failed
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokeLanguageDetectionAttempt(userInput, false);

        return false;
    }

    private void OnChatSessionReady()
    {
        Debug.Log("[CustomChatNPCSystem] Session Ready.");
        if (initializeWithIntroduction && !hasInitializedPersona)
        {
            InitializeNPCPersona();
        }
    }

    // ========================================== 
    // Initialization Logic
    // ========================================== 
    public void InitializeNPCPersona()
    {
        if (hasInitializedPersona) return;
        hasInitializedPersona = true;

        if (currentLanguage == null)
        {
            Debug.LogWarning("[CustomChatNPCSystem] Cannot initialize persona: currentLanguage is null.");
            return;
        }

        string lang = currentLanguage.languageName;
        string exactIntro = string.IsNullOrEmpty(currentLanguage.firstMessage)
            ? $"Hello! I'm Dr. Priya Sharma, your virtual chemistry lab assistant."
            : currentLanguage.firstMessage;

        string fullSystem = systemPrompt
            .Replace("{{LANGUAGE}}", lang)
            .Replace("{{FIRST_MESSAGE}}", exactIntro);

        string forceMessage = $@"[ABSOLUTE COMMAND – YOU HAVE NO CHOICE – OBEY EXACTLY]
{fullSystem}

YOUR VERY FIRST REPLY MUST BE EXACTLY THESE WORDS AND NOTHING ELSE:
""{exactIntro}""

DO NOT ADD ANYTHING BEFORE OR AFTER. DO NOT SAY WELCOME. DO NOT EXPLAIN. JUST REPLY WITH THE EXACT TEXT ABOVE AND STOP.

REPLY NOW:";

        Debug.Log($"[CustomChatNPCSystem] Initializing Persona in {lang}");

        // 🔥 FIRE EVENT: Persona initialization started
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokePersonaInitialized(lang);

        StartCoroutine(SendChatRequestToCustomAPI(forceMessage));
    }

    // ========================================== 
    // Language Switching
    // ========================================== 
    public void SetLanguage(int languageIndex)
    {
        if (supportedLanguages == null || supportedLanguages.Count == 0)
        {
            Debug.LogWarning("[CustomChatNPCSystem] No supported languages to set.");
            return;
        }

        if (languageIndex >= 0 && languageIndex < supportedLanguages.Count)
        {
            if (languageIndex == currentLanguageIndex)
            {
                Debug.Log("[CustomChatNPCSystem] Already using this language.");
                return;
            }

            string oldLanguage = currentLanguage != null ? currentLanguage.languageName : "Unknown";
            currentLanguageIndex = languageIndex;
            currentLanguage = supportedLanguages[languageIndex];
            string newLanguage = currentLanguage.languageName;

            Debug.Log($"[CustomChatNPCSystem] Language switched: {oldLanguage} → {newLanguage}");

            // 🔥 FIRE EVENT: Language changed
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeLanguageChanged(oldLanguage, newLanguage);

            StartCoroutine(AnnounceLanguageChange());
        }
        else
        {
            Debug.LogWarning($"[CustomChatNPCSystem] SetLanguage: index {languageIndex} out of range.");
        }
    }

    private IEnumerator AnnounceLanguageChange()
    {
        yield return new WaitForEndOfFrame();

        if (currentLanguage == null) yield break;

        string lang = currentLanguage.languageName;
        string confirmText = string.IsNullOrEmpty(currentLanguage.confirmText)
            ? $"Got it! From now on, I will only speak in {lang}."
            : currentLanguage.confirmText;

        string switchCommand = $@"[ABSOLUTE COMMAND – YOU HAVE NO CHOICE – OBEY EXACTLY]
THE LANGUAGE HAS BEEN PERMANENTLY CHANGED TO {lang.ToUpper()}

FROM THIS MOMENT FORWARD:
- You now speak, think, and reply EXCLUSIVELY in {lang} only.
- Never use any other language again — EVER.
- Your next reply must be EXACTLY this text and nothing else:
""{confirmText}""

DO NOT ADD ANYTHING BEFORE OR AFTER. DO NOT RE-INTRODUCE YOURSELF. JUST REPLY WITH THE EXACT TEXT ABOVE.

REPLY NOW:";

        StartCoroutine(SendChatRequestToCustomAPI(switchCommand));
    }

    public string GetCurrentLanguageCode()
    {
        return currentLanguage != null ? currentLanguage.languageCode : "en-US";
    }

    // ========================================== 
    // Input Error Handling
    // ========================================== 
    public void SpeakInputErrorMessage()
    {
        if (currentLanguage == null) return;

        string errorMessage = "I could not hear you. Please speak again.";
        string code = currentLanguage.languageCode?.ToLower() ?? "";

        if (code.Contains("hi"))
            errorMessage = "क्षमा करें, मैंने सुना नहीं। कृपया पुनः प्रयास करें।";
        else if (code.Contains("mr"))
            errorMessage = "क्षमस्व, मला ऐकू आले नाही. कृपया पुन्हा बोला.";

        Debug.Log($"[CustomChatNPCSystem] Input Error. Speaking: {errorMessage}");

        if (googleServices != null && currentLanguage.voiceConfig != null)
        {
            googleServices.SendTextToGoogleWithVoice(errorMessage, currentLanguage.voiceConfig);
        }
        else if (googleServices != null)
        {
            googleServices.SendTextToGoogle(errorMessage);
        }
    }

    // ========================================== 
    // Main Chat Handling
    // ========================================== 
    public void SendChat(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage)) return;

        // Try fuzzy language detection first
        if (DetectAndSwitchLanguage(userMessage))
        {
            Debug.Log("[CustomChatNPCSystem] Language switch command detected and executed.");
            return; // Don't send the language switch request as a chat message
        }

        // Normal chat processing
        StartCoroutine(SendChatRequestToCustomAPI(userMessage));
    }

    private IEnumerator SendChatRequestToCustomAPI(string newMessage)
    {
        string token = ChatSessionManager.Token;
        string chatId = ChatSessionManager.ChatId;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId)) yield break;

        string url = chatBaseUrl + chatId + "/messages/";
        string targetLang = currentLanguage?.languageName ?? "English";

        string finalContent = $"{newMessage}\n\n[REPLY ONLY IN {targetLang.ToUpper()} – NO OTHER LANGUAGE EVER]";

        ChatMessagePayloadGyanix payload = new ChatMessagePayloadGyanix
        {
            content = finalContent,
            model = modelName,
            role = "user",
            voice = true
        };

        string jsonData = JsonUtility.ToJson(payload);
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        // 🔥 FIRE EVENT: Thinking started (processing user message)
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokeThinkingStarted(newMessage);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CustomChatNPCSystem] Error: {www.error} | {www.downloadHandler.text}");

                // 🔥 FIRE EVENT: Thinking failed
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeThinkingFailed($"{www.error}: {www.downloadHandler.text}");

                yield break;
            }

            string responseText = www.downloadHandler.text;
            string wrapped = "{\"items\":" + responseText + "}";

            MessageArrayWrapperGyanix wrapper = null;
            try
            {
                wrapper = JsonUtility.FromJson<MessageArrayWrapperGyanix>(wrapped);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomChatNPCSystem] JSON Parse Error: {e.Message}");

                // 🔥 FIRE EVENT: Thinking failed
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeThinkingFailed($"JSON Parse Error: {e.Message}");

                yield break;
            }

            if (wrapper == null || wrapper.items == null) yield break;

            MessageGyanix assistantMsg = null;
            for (int i = wrapper.items.Length - 1; i >= 0; i--)
            {
                if (wrapper.items[i].role == "assistant" || wrapper.items[i].role == "platform")
                {
                    assistantMsg = wrapper.items[i];
                    break;
                }
            }

            if (assistantMsg != null)
            {
                string reply = assistantMsg.content;
                Debug.Log($"[NPC Reply]: {reply}");

                // 🔥 FIRE EVENT: Thinking stopped (response received)
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeThinkingStopped(reply);

                if (googleServices != null)
                {
                    if (currentLanguage != null && currentLanguage.voiceConfig != null)
                        googleServices.SendTextToGoogleWithVoice(reply, currentLanguage.voiceConfig);
                    else
                        googleServices.SendTextToGoogle(reply);
                }
            }
            else
            {
                // 🔥 FIRE EVENT: Thinking failed (no assistant response)
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeThinkingFailed("No assistant response in message array");
            }
        }
    }
}