using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Visual chat interface with message bubbles, input field, and chat history
/// Displays user input (text/voice) and NPC responses in a scrollable view
/// </summary>
public class NPCChatUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Main chat panel (parent of scroll view)")]
    [SerializeField] private GameObject chatPanel;

    [Tooltip("Scroll View component")]
    [SerializeField] private ScrollRect scrollView;

    [Tooltip("Content container inside scroll view (where messages spawn)")]
    [SerializeField] private Transform messageContainer;

    [Tooltip("Text input field for typing messages")]
    [SerializeField] private TMP_InputField textInputField;

    [Tooltip("Send button next to input field")]
    [SerializeField] private Button sendButton;

    [Tooltip("Voice input button (optional - shows recording state)")]
    [SerializeField] private Button voiceButton;

    [Tooltip("Text on voice button to show state")]
    [SerializeField] private TextMeshProUGUI voiceButtonText;

    [Header("Message Prefabs")]
    [Tooltip("Prefab for user messages (right-aligned, blue)")]
    [SerializeField] private GameObject userMessagePrefab;

    [Tooltip("Prefab for NPC messages (left-aligned, gray)")]
    [SerializeField] private GameObject npcMessagePrefab;

    [Tooltip("Prefab for system messages (centered, yellow)")]
    [SerializeField] private GameObject systemMessagePrefab;

    [Header("Display Settings")]
    [Tooltip("Show system messages (language changes, errors, etc.)")]
    [SerializeField] private bool showSystemMessages = false;

    [Tooltip("Show welcome message on start")]
    [SerializeField] private bool showWelcomeMessage = false;

    [Tooltip("Auto-scroll to bottom when new message arrives")]
    [SerializeField] private bool autoScroll = true;

    [Tooltip("Maximum number of messages to keep in history")]
    [SerializeField] private int maxMessages = 100;

    [Tooltip("Should chat panel be visible by default?")]
    [SerializeField] private bool showOnStart = true;

    [Tooltip("Show typing indicator when NPC is thinking")]
    [SerializeField] private bool showTypingIndicator = true;

    [Header("Typing Indicator")]
    [SerializeField] private GameObject typingIndicatorPrefab;
    private GameObject currentTypingIndicator;

    //[Header("Colors")]
    //[SerializeField] private Color userMessageColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue
    //[SerializeField] private Color npcMessageColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Gray
    //[SerializeField] private Color systemMessageColor = new Color(1f, 0.9f, 0.3f, 1f); // Yellow

    [Header("References (Auto-assigned)")]
    [SerializeField] private GyanixChatNPCSystem chatSystem;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    // Private variables
    private List<GameObject> messageObjects = new List<GameObject>();
    private bool isListening = false;
    private bool isInitialized = false;
    private string lastNPCMessage = ""; // Track last NPC message to prevent duplicates
    private string lastUserMessage = ""; // Track last user message to prevent duplicates
    private float lastUserMessageTime = 0f; // Time when last user message was added

    #region Unity Lifecycle

    void Start()
    {
        LogDebug("=== NPCChatUI Start() ===");

        // Validate all references
        ValidateReferences();

        // Auto-find chat system
        if (chatSystem == null)
        {
            LogDebug("Chat system not assigned, searching...");
            chatSystem = FindFirstObjectByType<GyanixChatNPCSystem>();

            if (chatSystem != null)
                LogDebug($"✅ Found CustomChatNPCSystem on: {chatSystem.gameObject.name}");
            else
                LogError("❌ CustomChatNPCSystem NOT FOUND in scene!");
        }
        else
        {
            LogDebug($"✅ Chat system already assigned: {chatSystem.gameObject.name}");
        }

        // Setup UI
        SetupUI();

        // Show/hide panel
        if (chatPanel != null)
        {
            chatPanel.SetActive(showOnStart);
            LogDebug($"Chat panel visibility: {showOnStart}");
        }

        // Subscribe to NPC events
        SubscribeToEvents();

        // Add welcome message (optional)
        if (showWelcomeMessage)
        {
            AddSystemMessage("💬 Chat started. You can type or use voice input.", true); // Force show
        }

        isInitialized = true;
        LogDebug("=== NPCChatUI Initialization Complete ===");
    }

    void OnEnable()
    {
        LogDebug("NPCChatUI OnEnable()");
        SubscribeToEvents();
    }

    void OnDisable()
    {
        LogDebug("NPCChatUI OnDisable()");
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        LogDebug("NPCChatUI OnDestroy()");
        UnsubscribeFromEvents();
    }

    #endregion

    #region Reference Validation

    private void ValidateReferences()
    {
        LogDebug("--- Validating References ---");

        if (chatPanel == null) LogError("❌ Chat Panel is NULL!");
        else LogDebug($"✅ Chat Panel: {chatPanel.name}");

        if (scrollView == null) LogError("❌ Scroll View is NULL!");
        else LogDebug($"✅ Scroll View: {scrollView.gameObject.name}");

        if (messageContainer == null) LogError("❌ Message Container is NULL!");
        else LogDebug($"✅ Message Container: {messageContainer.name}");

        if (textInputField == null) LogError("❌ Text Input Field is NULL!");
        else LogDebug($"✅ Text Input Field: {textInputField.gameObject.name}");

        if (sendButton == null) LogError("❌ Send Button is NULL!");
        else LogDebug($"✅ Send Button: {sendButton.gameObject.name}");

        if (userMessagePrefab == null) LogError("❌ User Message Prefab is NULL!");
        else LogDebug($"✅ User Message Prefab assigned");

        if (npcMessagePrefab == null) LogError("❌ NPC Message Prefab is NULL!");
        else LogDebug($"✅ NPC Message Prefab assigned");

        if (systemMessagePrefab == null) LogWarning("⚠️ System Message Prefab is NULL (will use NPC prefab instead)");
        else LogDebug($"✅ System Message Prefab assigned");

        LogDebug("--- Validation Complete ---");
    }

    #endregion

    #region UI Setup

    private void SetupUI()
    {
        LogDebug("--- Setting Up UI ---");

        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendButtonClicked);
            LogDebug("✅ Send button listener added");
        }
        else
        {
            LogError("❌ Cannot setup send button - reference is null!");
        }

        if (textInputField != null)
        {
            textInputField.onSubmit.RemoveAllListeners();
            textInputField.onSubmit.AddListener(OnInputFieldSubmit);
            LogDebug("✅ Input field submit listener added");
        }
        else
        {
            LogError("❌ Cannot setup input field - reference is null!");
        }

        if (voiceButton != null)
        {
            UpdateVoiceButtonState(false);
            LogDebug("✅ Voice button configured");
        }

        LogDebug("--- UI Setup Complete ---");
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        if (NPCEventSystem.Instance == null)
        {
            LogWarning("⚠️ NPCEventSystem.Instance is NULL - events will not work!");
            return;
        }

        LogDebug("Subscribing to NPC events...");

        // User input events
        NPCEventSystem.Instance.OnSpeechTranscribed += OnUserSpeechTranscribed;
        NPCEventSystem.Instance.OnListeningStarted += OnListeningStarted;
        NPCEventSystem.Instance.OnListeningStopped += OnListeningStopped;

        // NPC response events - ONLY subscribe to OnTalkingStarted (not OnThinkingStopped)
        NPCEventSystem.Instance.OnThinkingStarted += OnNPCThinkingStarted;
        NPCEventSystem.Instance.OnTalkingStarted += OnNPCTalkingStarted;

        // Error events
        NPCEventSystem.Instance.OnSpeechRecognitionFailed += OnSpeechError;
        NPCEventSystem.Instance.OnThinkingFailed += OnThinkingError;

        // Language events (optional)
        if (showSystemMessages)
        {
            NPCEventSystem.Instance.OnLanguageChanged += OnLanguageChanged;
            NPCEventSystem.Instance.OnPersonaInitialized += OnPersonaInitialized;
        }

        // Proximity events (optional)
        if (showSystemMessages)
        {
            NPCEventSystem.Instance.OnNPCActivated += OnNPCActivated;
            NPCEventSystem.Instance.OnNPCDeactivated += OnNPCDeactivated;
        }

        LogDebug("✅ Event subscriptions complete");
    }

    private void UnsubscribeFromEvents()
    {
        if (NPCEventSystem.Instance == null) return;

        LogDebug("Unsubscribing from NPC events...");

        NPCEventSystem.Instance.OnSpeechTranscribed -= OnUserSpeechTranscribed;
        NPCEventSystem.Instance.OnListeningStarted -= OnListeningStarted;
        NPCEventSystem.Instance.OnListeningStopped -= OnListeningStopped;
        NPCEventSystem.Instance.OnThinkingStarted -= OnNPCThinkingStarted;
        NPCEventSystem.Instance.OnTalkingStarted -= OnNPCTalkingStarted;
        NPCEventSystem.Instance.OnSpeechRecognitionFailed -= OnSpeechError;
        NPCEventSystem.Instance.OnThinkingFailed -= OnThinkingError;
        NPCEventSystem.Instance.OnLanguageChanged -= OnLanguageChanged;
        NPCEventSystem.Instance.OnPersonaInitialized -= OnPersonaInitialized;
        NPCEventSystem.Instance.OnNPCActivated -= OnNPCActivated;
        NPCEventSystem.Instance.OnNPCDeactivated -= OnNPCDeactivated;
    }

    #endregion

    #region Event Handlers

    private void OnUserSpeechTranscribed(string transcript)
    {
        LogDebug($"[EVENT] Speech Transcribed: '{transcript}'");
        LogDebug($"[EVENT] Last user message: '{lastUserMessage}'");
        LogDebug($"[EVENT] Time since last: {Time.time - lastUserMessageTime}s");

        // Add voice message to UI (duplicate prevention is inside AddUserMessage)
        AddUserMessage(transcript, true);
    }

    private void OnListeningStarted()
    {
        LogDebug("[EVENT] Listening Started");
        isListening = true;
        UpdateVoiceButtonState(true);

        // Only show system message if enabled
        if (showSystemMessages)
        {
            AddSystemMessage("🎤 Listening...");
        }
    }

    private void OnListeningStopped()
    {
        LogDebug("[EVENT] Listening Stopped");
        isListening = false;
        UpdateVoiceButtonState(false);

        // Remove "Listening..." message if it was shown
        if (showSystemMessages)
        {
            RemoveLastSystemMessage();
        }
    }

    private void OnNPCThinkingStarted(string userMessage)
    {
        LogDebug($"[EVENT] Thinking Started: '{userMessage}'");
        if (showTypingIndicator)
        {
            ShowTypingIndicator();
        }
    }

    private void OnNPCTalkingStarted(string text)
    {
        LogDebug($"[EVENT] NPC Talking: '{text.Substring(0, Mathf.Min(50, text.Length))}...'");

        // Hide typing indicator first
        HideTypingIndicator();

        // Check for duplicate - only add if different from last message
        if (text != lastNPCMessage)
        {
            AddNPCMessage(text);
            lastNPCMessage = text;
        }
        else
        {
            LogDebug("⚠️ Duplicate NPC message detected, skipping...");
        }
    }

    private void OnSpeechError(string error)
    {
        LogDebug($"[EVENT] Speech Error: {error}");
        if (showSystemMessages)
        {
            AddSystemMessage($"❌ Speech error: {error}");
        }
    }

    private void OnThinkingError(string error)
    {
        LogDebug($"[EVENT] Thinking Error: {error}");
        HideTypingIndicator();

        if (showSystemMessages)
        {
            AddSystemMessage($"❌ Error: {error}");
        }
    }

    private void OnLanguageChanged(string oldLang, string newLang)
    {
        LogDebug($"[EVENT] Language Changed: {oldLang} → {newLang}");
        if (showSystemMessages)
        {
            AddSystemMessage($"🌐 Language changed: {oldLang} → {newLang}");
        }
    }

    private void OnPersonaInitialized(string language)
    {
        LogDebug($"[EVENT] Persona Initialized: {language}");
        if (showSystemMessages)
        {
            AddSystemMessage($"🎭 NPC initialized in {language}");
        }
    }

    private void OnNPCActivated()
    {
        LogDebug("[EVENT] NPC Activated");
        if (chatPanel != null)
            chatPanel.SetActive(true);

        if (showSystemMessages)
        {
            AddSystemMessage("🟢 NPC activated - You can now interact!");
        }
    }

    private void OnNPCDeactivated()
    {
        LogDebug("[EVENT] NPC Deactivated");
        if (showSystemMessages)
        {
            AddSystemMessage("🔴 NPC deactivated - Move closer to interact");
        }
    }

    #endregion

    #region Message Display

    /// <summary>
    /// Add a user message to the chat
    /// </summary>
    public void AddUserMessage(string text, bool fromVoice = false)
    {
        LogDebug($"=== AddUserMessage Called ===");
        LogDebug($"Text: '{text}'");
        LogDebug($"From Voice: {fromVoice}");

        if (string.IsNullOrEmpty(text))
        {
            LogWarning("⚠️ Cannot add user message - text is null or empty");
            return;
        }

        // Duplicate prevention: Check if same message was added within last 0.5 seconds
        float currentTime = Time.time;
        if (text == lastUserMessage && (currentTime - lastUserMessageTime) < 0.5f)
        {
            LogWarning($"⚠️ Duplicate user message detected (within 0.5s), skipping...");
            return;
        }

        string prefix = fromVoice ? "" : "";
        GameObject messageObj = InstantiateMessage(userMessagePrefab, prefix + text);

        if (messageObj != null)
        {
            messageObjects.Add(messageObj);
            lastUserMessage = text;
            lastUserMessageTime = currentTime;
            LogDebug($"✅ User message added. Total messages: {messageObjects.Count}");
            CleanupOldMessages();
            ScrollToBottom();
        }
        else
        {
            LogError("❌ Failed to instantiate user message!");
        }
    }

    /// <summary>
    /// Add an NPC message to the chat
    /// </summary>
    public void AddNPCMessage(string text)
    {
        LogDebug($"=== AddNPCMessage Called ===");
        LogDebug($"Text: '{text.Substring(0, Mathf.Min(100, text.Length))}...'");

        if (string.IsNullOrEmpty(text))
        {
            LogWarning("⚠️ Cannot add NPC message - text is null or empty");
            return;
        }

        GameObject messageObj = InstantiateMessage(npcMessagePrefab, text);

        if (messageObj != null)
        {
            messageObjects.Add(messageObj);
            LogDebug($"✅ NPC message added. Total messages: {messageObjects.Count}");
            CleanupOldMessages();
            ScrollToBottom();
        }
        else
        {
            LogError("❌ Failed to instantiate NPC message!");
        }
    }

    /// <summary>
    /// Add a system message to the chat (status, errors, etc.)
    /// </summary>
    /// <param name="forceShow">If true, show even if showSystemMessages is false</param>
    public void AddSystemMessage(string text, bool forceShow = false)
    {
        // Check if system messages should be shown
        if (!showSystemMessages && !forceShow)
        {
            LogDebug($"System message suppressed: '{text}'");
            return;
        }

        LogDebug($"=== AddSystemMessage Called ===");
        LogDebug($"Text: '{text}'");

        if (string.IsNullOrEmpty(text))
        {
            LogWarning("⚠️ Cannot add system message - text is null or empty");
            return;
        }

        GameObject prefabToUse = systemMessagePrefab != null ? systemMessagePrefab : npcMessagePrefab;
        GameObject messageObj = InstantiateMessage(prefabToUse, text);

        if (messageObj != null)
        {
            messageObjects.Add(messageObj);
            LogDebug($"✅ System message added. Total messages: {messageObjects.Count}");
            CleanupOldMessages();
            ScrollToBottom();
        }
        else
        {
            LogError("❌ Failed to instantiate system message!");
        }
    }

    private GameObject InstantiateMessage(GameObject prefab, string text)
    {
        LogDebug($"--- InstantiateMessage ---");
        LogDebug($"Prefab: {(prefab != null ? prefab.name : "NULL")}");
        LogDebug($"Text length: {text.Length}");

        if (prefab == null)
        {
            LogError("❌ Prefab is NULL!");
            return null;
        }

        if (messageContainer == null)
        {
            LogError("❌ Message Container is NULL!");
            return null;
        }

        GameObject messageObj = Instantiate(prefab, messageContainer);
        LogDebug($"✅ GameObject instantiated: {messageObj.name}");

        // Find TextMeshProUGUI component
        TextMeshProUGUI textComponent = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = Color.black;
            LogDebug($"✅ Text component set");
        }
        else
        {
            LogError("❌ TextMeshProUGUI not found in prefab!");
        }

        // Find Image component for background
        Image backgroundImage = messageObj.GetComponent<Image>();
        if (backgroundImage != null)
        {
            //backgroundImage.color = color;
            LogDebug($"✅ Background color set");
        }

        return messageObj;
    }

    private void RemoveLastSystemMessage()
    {
        if (messageObjects.Count > 0)
        {
            GameObject lastMsg = messageObjects[messageObjects.Count - 1];

            TextMeshProUGUI textComp = lastMsg.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null && textComp.text.Contains("🎤"))
            {
                messageObjects.RemoveAt(messageObjects.Count - 1);
                Destroy(lastMsg);
                LogDebug("Removed 'Listening...' message");
            }
        }
    }

    #endregion

    #region Typing Indicator

    private void ShowTypingIndicator()
    {
        if (typingIndicatorPrefab == null || messageContainer == null)
        {
            LogDebug("ℹ️ Typing indicator prefab not assigned");
            return;
        }

        HideTypingIndicator();

        currentTypingIndicator = Instantiate(typingIndicatorPrefab, messageContainer);
        LogDebug("✅ Typing indicator shown");
        ScrollToBottom();
    }

    private void HideTypingIndicator()
    {
        if (currentTypingIndicator != null)
        {
            Destroy(currentTypingIndicator);
            currentTypingIndicator = null;
            LogDebug("Typing indicator hidden");
        }
    }

    #endregion

    #region Input Handling

    private void OnSendButtonClicked()
    {
        LogDebug("=== SEND BUTTON CLICKED ===");
        SendTextMessage();
    }

    private void OnInputFieldSubmit(string text)
    {
        LogDebug($"=== INPUT FIELD SUBMITTED ===");
        SendTextMessage();
    }

    private void SendTextMessage()
    {
        LogDebug("=== SendTextMessage() Called ===");

        if (textInputField == null)
        {
            LogError("❌ textInputField is NULL!");
            return;
        }

        if (chatSystem == null)
        {
            LogError("❌ chatSystem is NULL!");
            AddSystemMessage("❌ Error: Chat system not found!", true);
            return;
        }

        string text = textInputField.text.Trim();
        LogDebug($"Input text: '{text}'");

        if (string.IsNullOrEmpty(text))
        {
            LogWarning("⚠️ Empty message");
            return;
        }

        LogDebug($"✅ Sending: '{text}'");

        // Add to UI first
        AddUserMessage(text, false);

        // Send to NPC system (but don't add to UI again when event fires)
        LogDebug($"Calling chatSystem.SendChat()");
        chatSystem.SendChat(text);
        LogDebug("✅ Message sent");

        // Clear input
        textInputField.text = "";
        textInputField.ActivateInputField();
    }

    private void UpdateVoiceButtonState(bool listening)
    {
        if (voiceButtonText != null)
        {
            voiceButtonText.text = listening ? "🎤 Listening..." : "🎤 Voice";
        }

        if (voiceButton != null)
        {
            ColorBlock colors = voiceButton.colors;
            colors.normalColor = listening ? Color.red : Color.white;
            voiceButton.colors = colors;
        }
    }

    #endregion

    #region Utility

    private void ScrollToBottom()
    {
        if (!autoScroll || scrollView == null) return;
        StartCoroutine(ScrollToBottomCoroutine());
    }

    private IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();

        if (scrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollView.verticalNormalizedPosition = 0f;
        }
    }

    private void CleanupOldMessages()
    {
        while (messageObjects.Count > maxMessages)
        {
            GameObject oldMsg = messageObjects[0];
            messageObjects.RemoveAt(0);
            Destroy(oldMsg);
        }
    }

    public void ClearChat()
    {
        LogDebug("=== Clearing Chat ===");

        foreach (GameObject msg in messageObjects)
        {
            if (msg != null) Destroy(msg);
        }

        messageObjects.Clear();
        HideTypingIndicator();
        lastNPCMessage = "";
        lastUserMessage = "";
        lastUserMessageTime = 0f;

        LogDebug("✅ Chat cleared");
    }

    #endregion

    #region Public API

    public void ToggleChatPanel()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(!chatPanel.activeSelf);
        }
    }

    public void ShowChatPanel()
    {
        if (chatPanel != null) chatPanel.SetActive(true);
    }

    public void HideChatPanel()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
    }

    public bool IsChatVisible()
    {
        return chatPanel != null && chatPanel.activeSelf;
    }

    #endregion

    #region Debug Helpers

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NPCChatUI] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[NPCChatUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[NPCChatUI] {message}");
    }

    [ContextMenu("Test Add User Message")]
    public void TestAddUserMessage()
    {
        AddUserMessage("This is a test user message!", false);
    }

    [ContextMenu("Test Add NPC Message")]
    public void TestAddNPCMessage()
    {
        AddNPCMessage("This is a test NPC response!");
    }

    #endregion
}
