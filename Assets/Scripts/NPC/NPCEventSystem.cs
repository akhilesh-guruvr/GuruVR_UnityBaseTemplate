using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Centralized event system for NPC state changes with Inspector-assignable UnityEvents
/// Subscribe via code OR assign functions directly in Inspector
/// </summary>
public class NPCEventSystem : MonoBehaviour
{
    // Singleton pattern for easy access
    public static NPCEventSystem Instance { get; private set; }

    #region UnityEvent Declarations (Inspector-Assignable)

    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    [System.Serializable]
    public class TwoStringEvent : UnityEvent<string, string> { }

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    // ==================== PROXIMITY EVENTS ====================
    [Header("═══ PROXIMITY EVENTS ═══")]
    [Space(5)]

    [Tooltip("Fired when player enters NPC interaction range")]
    public UnityEvent onPlayerEnterRange;

    [Tooltip("Fired when player exits NPC interaction range")]
    public UnityEvent onPlayerExitRange;

    [Tooltip("Fired when NPC system is fully activated and ready")]
    public UnityEvent onNPCActivated;

    [Tooltip("Fired when NPC system is deactivated")]
    public UnityEvent onNPCDeactivated;

    // ==================== CHAT LIFECYCLE EVENTS ====================
    [Header("═══ AUTHENTICATION & SESSION EVENTS ═══")]
    [Space(5)]

    [Tooltip("Fired when authentication completes successfully (passes token)")]
    public StringEvent onAuthenticationSuccess;

    [Tooltip("Fired when authentication fails (passes error message)")]
    public StringEvent onAuthenticationFailed;

    [Tooltip("Fired when chat session is created (passes chat_id)")]
    public StringEvent onChatSessionCreated;

    [Tooltip("Fired when NPC persona is initialized (passes language name)")]
    public StringEvent onPersonaInitialized;

    // ==================== LANGUAGE EVENTS ====================
    [Header("═══ LANGUAGE EVENTS ═══")]
    [Space(5)]

    [Tooltip("Fired when language is changed (passes oldLanguage, newLanguage)")]
    public TwoStringEvent onLanguageChanged;

    [Tooltip("Fired when language detection is attempted (passes detectedLanguage, success)")]
    public StringEvent onLanguageDetectionAttempt;

    // ==================== LISTENING EVENTS (Speech-to-Text) ====================
    [Header("═══ LISTENING EVENTS (Speech-to-Text) ═══")]
    [Space(5)]

    [Tooltip("Fired when NPC starts listening to player (recording begins)")]
    public UnityEvent onListeningStarted;

    [Tooltip("Fired when NPC stops listening (recording ends)")]
    public UnityEvent onListeningStopped;

    [Tooltip("Fired when player speech is successfully transcribed (passes transcript)")]
    public StringEvent onSpeechTranscribed;

    [Tooltip("Fired when speech recognition fails (passes error reason)")]
    public StringEvent onSpeechRecognitionFailed;

    // ==================== THINKING EVENTS (Processing) ====================
    [Header("═══ THINKING EVENTS (Processing) ═══")]
    [Space(5)]

    [Tooltip("Fired when NPC starts processing player's message (passes user message)")]
    public StringEvent onThinkingStarted;

    [Tooltip("Fired when NPC finishes processing (passes NPC response)")]
    public StringEvent onThinkingStopped;

    [Tooltip("Fired when backend request fails (passes error message)")]
    public StringEvent onThinkingFailed;

    // ==================== TALKING EVENTS (Text-to-Speech) ====================
    [Header("═══ TALKING EVENTS (Text-to-Speech) ═══")]
    [Space(5)]

    [Tooltip("Fired when NPC starts speaking (passes text being spoken)")]
    public StringEvent onTalkingStarted;

    [Tooltip("Fired when NPC finishes speaking")]
    public UnityEvent onTalkingStopped;

    [Tooltip("Fired when TTS request fails (passes error message)")]
    public StringEvent onTTSFailed;

    // ==================== INPUT EVENTS ====================
    [Header("═══ INPUT EVENTS ═══")]
    [Space(5)]

    [Tooltip("Fired when player presses the talk button")]
    public UnityEvent onPlayerInputPressed;

    [Tooltip("Fired when player releases the talk button")]
    public UnityEvent onPlayerInputReleased;

    [Tooltip("Fired when input is debounced (ignored due to too-fast triggering)")]
    public UnityEvent onInputDebounced;

    // ==================== ERROR EVENTS ====================
    [Header("═══ ERROR EVENTS ═══")]
    [Space(5)]

    [Tooltip("Fired when any critical error occurs (passes errorType, errorMessage)")]
    public TwoStringEvent onCriticalError;

    #endregion

    #region C# Events (For Code Subscription)

    // Keep C# events for backwards compatibility with code-based subscriptions
    public event Action OnPlayerEnterRange;
    public event Action OnPlayerExitRange;
    public event Action OnNPCActivated;
    public event Action OnNPCDeactivated;
    public event Action<string> OnAuthenticationSuccess;
    public event Action<string> OnAuthenticationFailed;
    public event Action<string> OnChatSessionCreated;
    public event Action<string> OnPersonaInitialized;
    public event Action<string, string> OnLanguageChanged;
    public event Action<string, bool> OnLanguageDetectionAttempt;
    public event Action OnListeningStarted;
    public event Action OnListeningStopped;
    public event Action<string> OnSpeechTranscribed;
    public event Action<string> OnSpeechRecognitionFailed;
    public event Action<string> OnThinkingStarted;
    public event Action<string> OnThinkingStopped;
    public event Action<string> OnThinkingFailed;
    public event Action<string> OnTalkingStarted;
    public event Action OnTalkingStopped;
    public event Action<string> OnTTSFailed;
    public event Action OnPlayerInputPressed;
    public event Action OnPlayerInputReleased;
    public event Action OnInputDebounced;
    public event Action<string, string> OnCriticalError;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NPCEventSystem] Duplicate instance detected. Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes (optional)

        // Initialize all UnityEvents if null
        InitializeUnityEvents();

        Debug.Log("[NPCEventSystem] ✅ Event System Initialized");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeUnityEvents()
    {
        if (onPlayerEnterRange == null) onPlayerEnterRange = new UnityEvent();
        if (onPlayerExitRange == null) onPlayerExitRange = new UnityEvent();
        if (onNPCActivated == null) onNPCActivated = new UnityEvent();
        if (onNPCDeactivated == null) onNPCDeactivated = new UnityEvent();
        if (onAuthenticationSuccess == null) onAuthenticationSuccess = new StringEvent();
        if (onAuthenticationFailed == null) onAuthenticationFailed = new StringEvent();
        if (onChatSessionCreated == null) onChatSessionCreated = new StringEvent();
        if (onPersonaInitialized == null) onPersonaInitialized = new StringEvent();
        if (onLanguageChanged == null) onLanguageChanged = new TwoStringEvent();
        if (onLanguageDetectionAttempt == null) onLanguageDetectionAttempt = new StringEvent();
        if (onListeningStarted == null) onListeningStarted = new UnityEvent();
        if (onListeningStopped == null) onListeningStopped = new UnityEvent();
        if (onSpeechTranscribed == null) onSpeechTranscribed = new StringEvent();
        if (onSpeechRecognitionFailed == null) onSpeechRecognitionFailed = new StringEvent();
        if (onThinkingStarted == null) onThinkingStarted = new StringEvent();
        if (onThinkingStopped == null) onThinkingStopped = new StringEvent();
        if (onThinkingFailed == null) onThinkingFailed = new StringEvent();
        if (onTalkingStarted == null) onTalkingStarted = new StringEvent();
        if (onTalkingStopped == null) onTalkingStopped = new UnityEvent();
        if (onTTSFailed == null) onTTSFailed = new StringEvent();
        if (onPlayerInputPressed == null) onPlayerInputPressed = new UnityEvent();
        if (onPlayerInputReleased == null) onPlayerInputReleased = new UnityEvent();
        if (onInputDebounced == null) onInputDebounced = new UnityEvent();
        if (onCriticalError == null) onCriticalError = new TwoStringEvent();
    }

    #endregion

    #region Public Event Invokers (Call these from other scripts)

    // PROXIMITY
    public void InvokePlayerEnterRange()
    {
        onPlayerEnterRange?.Invoke();
        OnPlayerEnterRange?.Invoke();
        Debug.Log("[NPCEvents] 🟢 Player Entered Range");
    }

    public void InvokePlayerExitRange()
    {
        onPlayerExitRange?.Invoke();
        OnPlayerExitRange?.Invoke();
        Debug.Log("[NPCEvents] 🔴 Player Exited Range");
    }

    public void InvokeNPCActivated()
    {
        onNPCActivated?.Invoke();
        OnNPCActivated?.Invoke();
        Debug.Log("[NPCEvents] ✅ NPC Activated");
    }

    public void InvokeNPCDeactivated()
    {
        onNPCDeactivated?.Invoke();
        OnNPCDeactivated?.Invoke();
        Debug.Log("[NPCEvents] ❌ NPC Deactivated");
    }

    // AUTHENTICATION
    public void InvokeAuthenticationSuccess(string token)
    {
        onAuthenticationSuccess?.Invoke(token);
        OnAuthenticationSuccess?.Invoke(token);
        Debug.Log($"[NPCEvents] 🔐 Authentication Success (Token: {token.Substring(0, Mathf.Min(10, token.Length))}...)");
    }

    public void InvokeAuthenticationFailed(string error)
    {
        onAuthenticationFailed?.Invoke(error);
        OnAuthenticationFailed?.Invoke(error);
        Debug.LogError($"[NPCEvents] ❌ Authentication Failed: {error}");
    }

    public void InvokeChatSessionCreated(string chatId)
    {
        onChatSessionCreated?.Invoke(chatId);
        OnChatSessionCreated?.Invoke(chatId);
        Debug.Log($"[NPCEvents] 💬 Chat Session Created: {chatId}");
    }

    public void InvokePersonaInitialized(string language)
    {
        onPersonaInitialized?.Invoke(language);
        OnPersonaInitialized?.Invoke(language);
        Debug.Log($"[NPCEvents] 🎭 Persona Initialized ({language})");
    }

    // LANGUAGE
    public void InvokeLanguageChanged(string oldLang, string newLang)
    {
        onLanguageChanged?.Invoke(oldLang, newLang);
        OnLanguageChanged?.Invoke(oldLang, newLang);
        Debug.Log($"[NPCEvents] 🌐 Language Changed: {oldLang} → {newLang}");
    }

    public void InvokeLanguageDetectionAttempt(string detected, bool success)
    {
        onLanguageDetectionAttempt?.Invoke(detected);
        OnLanguageDetectionAttempt?.Invoke(detected, success);
        Debug.Log($"[NPCEvents] 🔍 Language Detection: {detected} ({(success ? "✅" : "❌")})");
    }

    // LISTENING
    public void InvokeListeningStarted()
    {
        onListeningStarted?.Invoke();
        OnListeningStarted?.Invoke();
        Debug.Log("[NPCEvents] 🎤 Listening Started");
    }

    public void InvokeListeningStopped()
    {
        onListeningStopped?.Invoke();
        OnListeningStopped?.Invoke();
        Debug.Log("[NPCEvents] 🎤 Listening Stopped");
    }

    public void InvokeSpeechTranscribed(string transcript)
    {
        onSpeechTranscribed?.Invoke(transcript);
        OnSpeechTranscribed?.Invoke(transcript);
        Debug.Log($"[NPCEvents] 📝 Speech Transcribed: \"{transcript}\"");
    }

    public void InvokeSpeechRecognitionFailed(string reason)
    {
        onSpeechRecognitionFailed?.Invoke(reason);
        OnSpeechRecognitionFailed?.Invoke(reason);
        Debug.LogWarning($"[NPCEvents] ⚠️ Speech Recognition Failed: {reason}");
    }

    // THINKING
    public void InvokeThinkingStarted(string userMessage)
    {
        onThinkingStarted?.Invoke(userMessage);
        OnThinkingStarted?.Invoke(userMessage);
        Debug.Log($"[NPCEvents] 🤔 Thinking Started (Message: \"{userMessage}\")");
    }

    public void InvokeThinkingStopped(string npcResponse)
    {
        onThinkingStopped?.Invoke(npcResponse);
        OnThinkingStopped?.Invoke(npcResponse);
        Debug.Log($"[NPCEvents] 🤔 Thinking Stopped (Response: \"{npcResponse.Substring(0, Mathf.Min(50, npcResponse.Length))}...\")");
    }

    public void InvokeThinkingFailed(string error)
    {
        onThinkingFailed?.Invoke(error);
        OnThinkingFailed?.Invoke(error);
        Debug.LogError($"[NPCEvents] ❌ Thinking Failed: {error}");
    }

    // TALKING
    public void InvokeTalkingStarted(string text)
    {
        onTalkingStarted?.Invoke(text);
        OnTalkingStarted?.Invoke(text);
        Debug.Log($"[NPCEvents] 🗣️ Talking Started: \"{text.Substring(0, Mathf.Min(50, text.Length))}...\"");
    }

    public void InvokeTalkingStopped()
    {
        onTalkingStopped?.Invoke();
        OnTalkingStopped?.Invoke();
        Debug.Log("[NPCEvents] 🗣️ Talking Stopped");
    }

    public void InvokeTTSFailed(string error)
    {
        onTTSFailed?.Invoke(error);
        OnTTSFailed?.Invoke(error);
        Debug.LogError($"[NPCEvents] ❌ TTS Failed: {error}");
    }

    // INPUT
    public void InvokePlayerInputPressed()
    {
        onPlayerInputPressed?.Invoke();
        OnPlayerInputPressed?.Invoke();
        Debug.Log("[NPCEvents] 🎮 Player Input Pressed");
    }

    public void InvokePlayerInputReleased()
    {
        onPlayerInputReleased?.Invoke();
        OnPlayerInputReleased?.Invoke();
        Debug.Log("[NPCEvents] 🎮 Player Input Released");
    }

    public void InvokeInputDebounced()
    {
        onInputDebounced?.Invoke();
        OnInputDebounced?.Invoke();
        Debug.LogWarning("[NPCEvents] ⏱️ Input Debounced");
    }

    // ERROR
    public void InvokeCriticalError(string errorType, string message)
    {
        onCriticalError?.Invoke(errorType, message);
        OnCriticalError?.Invoke(errorType, message);
        Debug.LogError($"[NPCEvents] 🚨 CRITICAL ERROR [{errorType}]: {message}");
    }

    #endregion

    #region Debug Helpers

    /// <summary>
    /// Logs all currently subscribed event listeners (for debugging)
    /// </summary>
    public void LogAllSubscribers()
    {
        Debug.Log("=== NPCEventSystem Subscribers ===");
        LogUnityEventCount("onPlayerEnterRange", onPlayerEnterRange);
        LogUnityEventCount("onListeningStarted", onListeningStarted);
        LogUnityEventCount("onThinkingStarted", onThinkingStarted);
        LogUnityEventCount("onTalkingStarted", onTalkingStarted);
    }

    private void LogUnityEventCount(string eventName, UnityEventBase unityEvent)
    {
        int count = unityEvent?.GetPersistentEventCount() ?? 0;
        Debug.Log($"  {eventName}: {count} listener(s) in Inspector");
    }

    #endregion
}

#region Usage Examples
/*
 * ═══════════════════════════════════════════════════════════════
 * HOW TO USE THIS EVENT SYSTEM (TWO METHODS)
 * ═══════════════════════════════════════════════════════════════
 * 
 * METHOD 1: INSPECTOR (Like UI Buttons) - RECOMMENDED FOR DESIGNERS
 * ═══════════════════════════════════════════════════════════════
 * 
 * 1. Select NPCEventSystem GameObject in Hierarchy
 * 2. Find the event you want in Inspector (e.g., "On Talking Started")
 * 3. Click "+" button to add a listener
 * 4. Drag the GameObject that has your script
 * 5. Select the function from dropdown
 * 
 * Example: Show subtitles when NPC talks
 * 
 *   On Talking Started (String Event)
 *   ┌──────────────────────────────┐
 *   │ Runtime:                     │
 *   │ ┌──────────────────────────┐ │
 *   │ │ [SubtitleManager]        │ │ ← Drag GameObject here
 *   │ │ SubtitleManager          │ │ ← Select script
 *   │ │ > ShowSubtitle (string)  │ │ ← Select function
 *   │ └──────────────────────────┘ │
 *   └──────────────────────────────┘
 * 
 * ═══════════════════════════════════════════════════════════════
 * METHOD 2: CODE (For Programmers)
 * ═══════════════════════════════════════════════════════════════
 * 
 * 1. SUBSCRIBE in OnEnable():
 * 
 *    void OnEnable()
 *    {
 *        NPCEventSystem.Instance.OnTalkingStarted += HandleNPCTalking;
 *        NPCEventSystem.Instance.OnListeningStarted += HandleNPCListening;
 *    }
 * 
 * 2. UNSUBSCRIBE in OnDisable():
 * 
 *    void OnDisable()
 *    {
 *        if (NPCEventSystem.Instance != null)
 *        {
 *            NPCEventSystem.Instance.OnTalkingStarted -= HandleNPCTalking;
 *            NPCEventSystem.Instance.OnListeningStarted -= HandleNPCListening;
 *        }
 *    }
 * 
 * 3. IMPLEMENT handlers:
 * 
 *    void HandleNPCTalking(string text)
 *    {
 *        Debug.Log($"NPC is saying: {text}");
 *    }
 * 
 * ═══════════════════════════════════════════════════════════════
 * WHICH METHOD TO USE?
 * ═══════════════════════════════════════════════════════════════
 * 
 * Use INSPECTOR (Method 1) when:
 * ✓ Designers need to hook up functionality
 * ✓ Simple on/off toggles (enable/disable GameObjects)
 * ✓ Playing sounds/animations
 * ✓ You want to see connections visually
 * 
 * Use CODE (Method 2) when:
 * ✓ Complex logic with conditionals
 * ✓ You need to process event data extensively
 * ✓ Dynamic subscription/unsubscription
 * ✓ You're a programmer who prefers code
 * 
 * You can use BOTH methods simultaneously!
 * ═══════════════════════════════════════════════════════════════
 */
#endregion