using UnityEngine;
using GoogleSpeechToText.Scripts;
using System.Collections;

/// <summary>
/// Enhanced NPC Proximity Activator with event system
/// Activates/Deactivates the entire NPC system when player enters/exits proximity
/// </summary>
public class NPCProximityActivator : MonoBehaviour
{
    [Header("Proximity Settings")]
    [Tooltip("Tag to identify the player (usually 'Player')")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Activation radius around NPC (meters)")]
    [SerializeField] private float activationRadius = 3f;

    [Tooltip("Should NPC greet player when they approach?")]
    [SerializeField] private bool autoGreetOnApproach = false;

    [Tooltip("Cooldown time (seconds) before NPC can greet again")]
    [SerializeField] private float greetingCooldown = 30f;

    private float lastGreetingTime = -999f;

    [Header("Visual Feedback")]
    [Tooltip("Show a sphere gizmo in the editor to visualize activation range")]
    [SerializeField] private bool showActivationRange = true;

    [Tooltip("Optional UI indicator to show when player can talk (e.g., 'Press A to Talk')")]
    [SerializeField] private GameObject uiIndicator;

    [Header("Audio Feedback (Optional)")]
    [Tooltip("Sound to play when NPC becomes available")]
    [SerializeField] private AudioClip activationSound;

    [Tooltip("Sound to play when NPC becomes unavailable")]
    [SerializeField] private AudioClip deactivationSound;

    [Header("NPC Components (Auto-detected if not assigned)")]
    [SerializeField] private GyanixChatNPCSystem npcChatSystem;
    [SerializeField] private SpeechToTextManager speechToTextManager;

    [Header("Debug")]
    [SerializeField] private bool enableEventLogs = true;

    private AudioSource audioSource;
    private bool isPlayerNearby = false;
    private bool npcWasInitialized = false;
    private BoxCollider triggerCollider;

    void Start()
    {
        // Setup trigger collider
        SetupTriggerCollider();

        // Auto-find NPC components if not assigned
        if (npcChatSystem == null)
        {
            npcChatSystem = GetComponent<GyanixChatNPCSystem>();
            if (npcChatSystem == null)
            {
                Debug.LogError("[NPCProximity] CustomChatNPCSystem not found! Please attach this script to the NPC GameObject.");
                enabled = false;
                return;
            }
        }

        if (speechToTextManager == null)
        {
            speechToTextManager = FindObjectOfType<SpeechToTextManager>();
            if (speechToTextManager == null)
            {
                Debug.LogWarning("[NPCProximity] EnhancedSpeechToTextManager not found in scene!");
            }
        }

        // Setup audio source for feedback sounds
        if (activationSound != null || deactivationSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // Initially disable the entire NPC system
        DisableNPCSystem();

        // Hide UI indicator initially
        if (uiIndicator != null)
            uiIndicator.SetActive(false);

        Debug.Log("[NPCProximity] NPC system initialized and disabled. Waiting for player...");
    }

    void SetupTriggerCollider()
    {
        // Get or add BoxCollider
        triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("[NPCProximity] Added BoxCollider to NPC");
        }

        // Configure as trigger
        triggerCollider.isTrigger = true;

        // Set size based on activation radius
        triggerCollider.size = new Vector3(activationRadius * 2, activationRadius * 2, activationRadius * 2);
        triggerCollider.center = Vector3.zero;

        Debug.Log($"[NPCProximity] Trigger collider configured with radius: {activationRadius}m");
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is the player
        if (other.CompareTag(playerTag))
        {
            ActivateNPC();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if the player is leaving
        if (other.CompareTag(playerTag))
        {
            DeactivateNPC();
        }
    }

    void ActivateNPC()
    {
        if (isPlayerNearby) return; // Already active

        isPlayerNearby = true;

        if (enableEventLogs)
        {
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("[NPCProximity] 🟢 PLAYER ENTERED RANGE");
            Debug.Log("[NPCProximity] 🟢 NPC SYSTEM ACTIVATED");
            Debug.Log("═══════════════════════════════════════════");
        }

        // 🔥 FIRE EVENT: Player entered range
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokePlayerEnterRange();

        // Enable the entire NPC chat system
        if (npcChatSystem != null)
        {
            npcChatSystem.enabled = true;

            // Wait a frame for the system to fully enable, then initialize
            StartCoroutine(InitializeNPCAfterEnable());
        }

        // Enable speech-to-text
        if (speechToTextManager != null)
        {
            speechToTextManager.enabled = true;
            if (enableEventLogs)
                Debug.Log("[NPCProximity] Speech-to-Text ENABLED");
        }

        // Show UI indicator
        if (uiIndicator != null)
        {
            uiIndicator.SetActive(true);
            if (enableEventLogs)
                Debug.Log("[NPCProximity] UI Indicator shown");
        }

        // Play activation sound
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
            if (enableEventLogs)
                Debug.Log("[NPCProximity] Activation sound played");
        }

        // 🔥 FIRE EVENT: NPC fully activated
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokeNPCActivated();
    }

    private IEnumerator InitializeNPCAfterEnable()
    {
        // Wait for CustomChatNPCSystem.Start() to complete
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f); // Give time for session to be ready

        if (enableEventLogs)
            Debug.Log("[NPCProximity] Checking if NPC needs initialization...");

        // Check if session is ready
        if (string.IsNullOrEmpty(ChatSessionManager.Token) || string.IsNullOrEmpty(ChatSessionManager.ChatId))
        {
            Debug.LogWarning("[NPCProximity] Session not ready yet. Waiting...");
            yield return new WaitForSeconds(1f);
        }

        // Initialize NPC persona (only once)
        if (!npcWasInitialized && npcChatSystem.initializeWithIntroduction)
        {
            if (enableEventLogs)
                Debug.Log("[NPCProximity] Calling InitializeNPCPersona()...");

            npcChatSystem.InitializeNPCPersona();
            npcWasInitialized = true;

            if (enableEventLogs)
                Debug.Log("[NPCProximity] NPC persona initialization triggered");
        }
        else if (autoGreetOnApproach && npcWasInitialized)
        {
            // Check cooldown before greeting
            if (Time.time - lastGreetingTime > greetingCooldown)
            {
                // Optional: Send a quick greeting for returning players
                string greeting = GetGreetingForCurrentLanguage();
                npcChatSystem.SendChat(greeting);
                lastGreetingTime = Time.time;

                if (enableEventLogs)
                    Debug.Log($"[NPCProximity] Sent greeting: {greeting}");
            }
            else
            {
                if (enableEventLogs)
                    Debug.Log($"[NPCProximity] Greeting on cooldown (wait {greetingCooldown - (Time.time - lastGreetingTime):F0}s)");
            }
        }
        else
        {
            if (enableEventLogs)
                Debug.Log("[NPCProximity] NPC already initialized or auto-greet disabled");
        }
    }

    void DeactivateNPC()
    {
        if (!isPlayerNearby) return; // Already inactive

        isPlayerNearby = false;

        if (enableEventLogs)
        {
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("[NPCProximity] 🔴 PLAYER LEFT RANGE");
            Debug.Log("[NPCProximity] 🔴 NPC SYSTEM DEACTIVATED");
            Debug.Log("═══════════════════════════════════════════");
        }

        // 🔥 FIRE EVENT: Player exited range
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokePlayerExitRange();

        // Disable the entire NPC system
        DisableNPCSystem();

        // Hide UI indicator
        if (uiIndicator != null)
        {
            uiIndicator.SetActive(false);
            if (enableEventLogs)
                Debug.Log("[NPCProximity] UI Indicator hidden");
        }

        // Play deactivation sound
        if (audioSource != null && deactivationSound != null)
        {
            audioSource.PlayOneShot(deactivationSound);
            if (enableEventLogs)
                Debug.Log("[NPCProximity] Deactivation sound played");
        }

        // 🔥 FIRE EVENT: NPC deactivated
        if (NPCEventSystem.Instance != null)
            NPCEventSystem.Instance.InvokeNPCDeactivated();
    }

    void DisableNPCSystem()
    {
        // Disable CustomChatNPCSystem
        if (npcChatSystem != null)
        {
            npcChatSystem.enabled = false;
            if (enableEventLogs)
                Debug.Log("[NPCProximity] CustomChatNPCSystem DISABLED");
        }

        // Disable Speech-to-Text
        if (speechToTextManager != null)
        {
            speechToTextManager.enabled = false;
            if (enableEventLogs)
                Debug.Log("[NPCProximity] Speech-to-Text DISABLED");
        }
    }

    // Get appropriate greeting based on current language
    private string GetGreetingForCurrentLanguage()
    {
        if (npcChatSystem == null) return "Hello again!";

        string langCode = npcChatSystem.GetCurrentLanguageCode().ToLower();

        if (langCode.Contains("hi")) // Hindi
            return "वापस आने के लिए धन्यवाद!"; // "Thanks for coming back!"
        else if (langCode.Contains("mr")) // Marathi
            return "परत आल्याबद्दल धन्यवाद!"; // "Thanks for coming back!"
        else // English
            return "Welcome back!";
    }

    // ==========================================
    // Public API for External Control
    // ==========================================

    public bool IsPlayerNearby()
    {
        return isPlayerNearby;
    }

    public void SetActivationRadius(float newRadius)
    {
        activationRadius = newRadius;
        if (triggerCollider != null)
        {
            triggerCollider.size = new Vector3(newRadius * 2, newRadius * 2, newRadius * 2);
            Debug.Log($"[NPCProximity] Activation radius changed to: {newRadius}m");
        }
    }

    public void ForceActivate()
    {
        ActivateNPC();
    }

    public void ForceDeactivate()
    {
        DeactivateNPC();
    }

    // ==========================================
    // Visual Debug
    // ==========================================

    void OnDrawGizmos()
    {
        if (!showActivationRange) return;

        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }

    void OnDrawGizmosSelected()
    {
        if (!showActivationRange) return;

        // Show solid sphere when selected
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawSphere(transform.position, activationRadius);

        // Show boundaries
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}

//using UnityEngine;
//using GoogleSpeechToText.Scripts;
//using System.Collections;

///// <summary>
///// Activates/Deactivates the entire NPC system when player enters/exits proximity
///// Attach this to the NPC GameObject
///// </summary>
//public class NPCProximityActivator : MonoBehaviour
//{
//    [Header("Proximity Settings")]
//    [Tooltip("Tag to identify the player (usually 'Player')")]
//    [SerializeField] private string playerTag = "Player";

//    [Tooltip("Activation radius around NPC (meters)")]
//    [SerializeField] private float activationRadius = 3f;

//    [Tooltip("Should NPC greet player when they approach?")]
//    [SerializeField] private bool autoGreetOnApproach = false;

//    [Tooltip("Cooldown time (seconds) before NPC can greet again")]
//    [SerializeField] private float greetingCooldown = 30f;

//    private float lastGreetingTime = -999f;

//    [Header("Visual Feedback")]
//    [Tooltip("Show a sphere gizmo in the editor to visualize activation range")]
//    [SerializeField] private bool showActivationRange = true;

//    [Tooltip("Optional UI indicator to show when player can talk (e.g., 'Press A to Talk')")]
//    [SerializeField] private GameObject uiIndicator;

//    [Header("Audio Feedback (Optional)")]
//    [Tooltip("Sound to play when NPC becomes available")]
//    [SerializeField] private AudioClip activationSound;

//    [Tooltip("Sound to play when NPC becomes unavailable")]
//    [SerializeField] private AudioClip deactivationSound;

//    [Header("NPC Components (Auto-detected if not assigned)")]
//    [SerializeField] private GyanixChatNPCSystem npcChatSystem;
//    [SerializeField] private SpeechToTextManager speechToTextManager;

//    private AudioSource audioSource;
//    private bool isPlayerNearby = false;
//    private bool npcWasInitialized = false;
//    private BoxCollider triggerCollider;

//    void Start()
//    {
//        // Setup trigger collider
//        SetupTriggerCollider();

//        // Auto-find NPC components if not assigned
//        if (npcChatSystem == null)
//        {
//            npcChatSystem = GetComponent<GyanixChatNPCSystem>();
//            if (npcChatSystem == null)
//            {
//                Debug.LogError("[NPCProximity] CustomChatNPCSystem not found! Please attach this script to the NPC GameObject.");
//                enabled = false;
//                return;
//            }
//        }

//        if (speechToTextManager == null)
//        {
//            speechToTextManager = FindObjectOfType<SpeechToTextManager>();
//            if (speechToTextManager == null)
//            {
//                Debug.LogWarning("[NPCProximity] CustomSpeechToTextManager not found in scene!");
//            }
//        }

//        // Setup audio source for feedback sounds
//        if (activationSound != null || deactivationSound != null)
//        {
//            audioSource = GetComponent<AudioSource>();
//            if (audioSource == null)
//                audioSource = gameObject.AddComponent<AudioSource>();

//            audioSource.playOnAwake = false;
//            audioSource.spatialBlend = 1f; // 3D sound
//        }

//        // Initially disable the entire NPC system
//        DisableNPCSystem();

//        // Hide UI indicator initially
//        if (uiIndicator != null)
//            uiIndicator.SetActive(false);

//        Debug.Log("[NPCProximity] NPC system initialized and disabled. Waiting for player...");
//    }

//    void SetupTriggerCollider()
//    {
//        // Get or add BoxCollider
//        triggerCollider = GetComponent<BoxCollider>();
//        if (triggerCollider == null)
//        {
//            triggerCollider = gameObject.AddComponent<BoxCollider>();
//            Debug.Log("[NPCProximity] Added BoxCollider to NPC");
//        }

//        // Configure as trigger
//        triggerCollider.isTrigger = true;

//        // Set size based on activation radius
//        triggerCollider.size = new Vector3(activationRadius * 2, activationRadius * 2, activationRadius * 2);
//        triggerCollider.center = Vector3.zero;

//        Debug.Log($"[NPCProximity] Trigger collider configured with radius: {activationRadius}m");
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        // Check if the object entering is the player
//        if (other.CompareTag(playerTag))
//        {
//            ActivateNPC();
//        }
//    }

//    void OnTriggerExit(Collider other)
//    {
//        // Check if the player is leaving
//        if (other.CompareTag(playerTag))
//        {
//            DeactivateNPC();
//        }
//    }

//    void ActivateNPC()
//    {
//        if (isPlayerNearby) return; // Already active

//        isPlayerNearby = true;
//        Debug.Log("═══════════════════════════════════════════");
//        Debug.Log("[NPCProximity] 🟢 PLAYER ENTERED RANGE");
//        Debug.Log("[NPCProximity] 🟢 NPC SYSTEM ACTIVATED");
//        Debug.Log("═══════════════════════════════════════════");

//        // Enable the entire NPC chat system
//        if (npcChatSystem != null)
//        {
//            npcChatSystem.enabled = true;

//            // Wait a frame for the system to fully enable, then initialize
//            StartCoroutine(InitializeNPCAfterEnable());
//        }

//        // Enable speech-to-text
//        if (speechToTextManager != null)
//        {
//            speechToTextManager.enabled = true;
//            Debug.Log("[NPCProximity] Speech-to-Text ENABLED");
//        }

//        // Show UI indicator
//        if (uiIndicator != null)
//        {
//            uiIndicator.SetActive(true);
//            Debug.Log("[NPCProximity] UI Indicator shown");
//        }

//        // Play activation sound
//        if (audioSource != null && activationSound != null)
//        {
//            audioSource.PlayOneShot(activationSound);
//            Debug.Log("[NPCProximity] Activation sound played");
//        }
//    }

//    private IEnumerator InitializeNPCAfterEnable()
//    {
//        // Wait for CustomChatNPCSystem.Start() to complete
//        yield return new WaitForEndOfFrame();
//        yield return new WaitForSeconds(0.5f); // Give time for session to be ready

//        Debug.Log("[NPCProximity] Checking if NPC needs initialization...");

//        // Check if session is ready
//        if (string.IsNullOrEmpty(ChatSessionManager.Token) || string.IsNullOrEmpty(ChatSessionManager.ChatId))
//        {
//            Debug.LogWarning("[NPCProximity] Session not ready yet. Waiting...");
//            yield return new WaitForSeconds(1f);
//        }

//        // Initialize NPC persona (only once)
//        if (!npcWasInitialized && npcChatSystem.initializeWithIntroduction)
//        {
//            Debug.Log("[NPCProximity] Calling InitializeNPCPersona()...");
//            npcChatSystem.InitializeNPCPersona();
//            npcWasInitialized = true;
//            Debug.Log("[NPCProximity] NPC persona initialization triggered");
//        }
//        else if (autoGreetOnApproach && npcWasInitialized)
//        {
//            // Check cooldown before greeting
//            if (Time.time - lastGreetingTime > greetingCooldown)
//            {
//                // Optional: Send a quick greeting for returning players
//                string greeting = GetGreetingForCurrentLanguage();
//                npcChatSystem.SendChat(greeting);
//                lastGreetingTime = Time.time;
//                Debug.Log($"[NPCProximity] Sent greeting: {greeting}");
//            }
//            else
//            {
//                Debug.Log($"[NPCProximity] Greeting on cooldown (wait {greetingCooldown - (Time.time - lastGreetingTime):F0}s)");
//            }
//        }
//        else
//        {
//            Debug.Log("[NPCProximity] NPC already initialized or auto-greet disabled");
//        }
//    }

//    void DeactivateNPC()
//    {
//        if (!isPlayerNearby) return; // Already inactive

//        isPlayerNearby = false;
//        Debug.Log("═══════════════════════════════════════════");
//        Debug.Log("[NPCProximity] 🔴 PLAYER LEFT RANGE");
//        Debug.Log("[NPCProximity] 🔴 NPC SYSTEM DEACTIVATED");
//        Debug.Log("═══════════════════════════════════════════");

//        // Disable the entire NPC system
//        DisableNPCSystem();

//        // Hide UI indicator
//        if (uiIndicator != null)
//        {
//            uiIndicator.SetActive(false);
//            Debug.Log("[NPCProximity] UI Indicator hidden");
//        }

//        // Play deactivation sound
//        if (audioSource != null && deactivationSound != null)
//        {
//            audioSource.PlayOneShot(deactivationSound);
//            Debug.Log("[NPCProximity] Deactivation sound played");
//        }
//    }

//    void DisableNPCSystem()
//    {
//        // Disable CustomChatNPCSystem
//        if (npcChatSystem != null)
//        {
//            npcChatSystem.enabled = false;
//            Debug.Log("[NPCProximity] CustomChatNPCSystem DISABLED");
//        }

//        // Disable Speech-to-Text
//        if (speechToTextManager != null)
//        {
//            speechToTextManager.enabled = false;
//            Debug.Log("[NPCProximity] Speech-to-Text DISABLED");
//        }
//    }

//    // Get appropriate greeting based on current language
//    private string GetGreetingForCurrentLanguage()
//    {
//        if (npcChatSystem == null) return "Hello again!";

//        string langCode = npcChatSystem.GetCurrentLanguageCode().ToLower();

//        if (langCode.Contains("hi")) // Hindi
//            return "वापस आने के लिए धन्यवाद!"; // "Thanks for coming back!"
//        else if (langCode.Contains("mr")) // Marathi
//            return "परत आल्याबद्दल धन्यवाद!"; // "Thanks for coming back!"
//        else // English
//            return "Welcome back!";
//    }

//    // Public methods for external control
//    public bool IsPlayerNearby()
//    {
//        return isPlayerNearby;
//    }

//    public void SetActivationRadius(float newRadius)
//    {
//        activationRadius = newRadius;
//        if (triggerCollider != null)
//        {
//            triggerCollider.size = new Vector3(newRadius * 2, newRadius * 2, newRadius * 2);
//            Debug.Log($"[NPCProximity] Activation radius changed to: {newRadius}m");
//        }
//    }

//    public void ForceActivate()
//    {
//        ActivateNPC();
//    }

//    public void ForceDeactivate()
//    {
//        DeactivateNPC();
//    }

//    // Visualize activation range in editor
//    void OnDrawGizmos()
//    {
//        if (!showActivationRange) return;

//        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, activationRadius);
//    }

//    void OnDrawGizmosSelected()
//    {
//        if (!showActivationRange) return;

//        // Show solid sphere when selected
//        Gizmos.color = new Color(0, 1, 0, 0.15f);
//        Gizmos.DrawSphere(transform.position, activationRadius);

//        // Show boundaries
//        Gizmos.color = Color.green;
//        Gizmos.DrawWireSphere(transform.position, activationRadius);
//    }
//}