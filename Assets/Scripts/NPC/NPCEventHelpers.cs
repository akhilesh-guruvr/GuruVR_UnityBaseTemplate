using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Collection of simple helper scripts that can be called from NPCEventSystem via Inspector
/// Each method is public and can be dragged-and-dropped in Unity's event system
/// </summary>

// ═══════════════════════════════════════════════════════════════
// 1. SUBTITLE DISPLAY
// ═══════════════════════════════════════════════════════════════
public class NPCSubtitleDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private bool autoHide = true;

    private float hideTimer = 0f;

    /// <summary>Call this from OnTalkingStarted event</summary>
    public void ShowSubtitle(string text)
    {
        if (subtitlePanel != null)
            subtitlePanel.SetActive(true);

        if (subtitleText != null)
            subtitleText.text = text;

        hideTimer = displayDuration;

        Debug.Log($"[Subtitles] Showing: {text}");
    }

    /// <summary>Call this from OnTalkingStopped event</summary>
    public void HideSubtitle()
    {
        if (autoHide && subtitlePanel != null)
            subtitlePanel.SetActive(false);

        Debug.Log("[Subtitles] Hidden");
    }

    void Update()
    {
        if (autoHide && hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                HideSubtitle();
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// 2. UI INDICATOR (Speech Bubble, Icons, etc.)
// ═══════════════════════════════════════════════════════════════
public class NPCStateIndicator : MonoBehaviour
{
    [Header("State Icons")]
    [SerializeField] private GameObject listeningIcon;
    [SerializeField] private GameObject thinkingIcon;
    [SerializeField] private GameObject talkingIcon;

    /// <summary>Call from OnListeningStarted</summary>
    public void ShowListeningIcon()
    {
        HideAllIcons();
        if (listeningIcon != null)
            listeningIcon.SetActive(true);
    }

    /// <summary>Call from OnThinkingStarted</summary>
    public void ShowThinkingIcon(string message)
    {
        HideAllIcons();
        if (thinkingIcon != null)
            thinkingIcon.SetActive(true);
    }

    /// <summary>Call from OnTalkingStarted</summary>
    public void ShowTalkingIcon(string text)
    {
        HideAllIcons();
        if (talkingIcon != null)
            talkingIcon.SetActive(true);
    }

    /// <summary>Call from OnTalkingStopped or OnNPCDeactivated</summary>
    public void HideAllIcons()
    {
        if (listeningIcon != null) listeningIcon.SetActive(false);
        if (thinkingIcon != null) thinkingIcon.SetActive(false);
        if (talkingIcon != null) talkingIcon.SetActive(false);
    }
}

// ═══════════════════════════════════════════════════════════════
// 3. AUDIO FEEDBACK (Play Sounds)
// ═══════════════════════════════════════════════════════════════
public class NPCAudioFeedback : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip listeningSound;
    [SerializeField] private AudioClip thinkingSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip successSound;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>Call from OnListeningStarted</summary>
    public void PlayListeningSound()
    {
        if (listeningSound != null)
            audioSource.PlayOneShot(listeningSound);
    }

    /// <summary>Call from OnThinkingStarted</summary>
    public void PlayThinkingSound(string message)
    {
        if (thinkingSound != null)
            audioSource.PlayOneShot(thinkingSound);
    }

    /// <summary>Call from OnSpeechRecognitionFailed or OnTTSFailed</summary>
    public void PlayErrorSound(string error)
    {
        if (errorSound != null)
            audioSource.PlayOneShot(errorSound);
    }

    /// <summary>Call from OnSpeechTranscribed</summary>
    public void PlaySuccessSound(string transcript)
    {
        if (successSound != null)
            audioSource.PlayOneShot(successSound);
    }
}

// ═══════════════════════════════════════════════════════════════
// 4. LOADING SPINNER / PROGRESS INDICATOR
// ═══════════════════════════════════════════════════════════════
public class NPCLoadingSpinner : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject spinnerObject;
    [SerializeField] private RectTransform spinnerTransform;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 180f;

    private bool isSpinning = false;

    /// <summary>Call from OnThinkingStarted</summary>
    public void ShowSpinner(string message)
    {
        if (spinnerObject != null)
            spinnerObject.SetActive(true);

        isSpinning = true;
        Debug.Log("[Spinner] Thinking...");
    }

    /// <summary>Call from OnThinkingStopped or OnThinkingFailed</summary>
    public void HideSpinner(string response)
    {
        HideSpinner();
    }

    public void HideSpinner()
    {
        if (spinnerObject != null)
            spinnerObject.SetActive(false);

        isSpinning = false;
        Debug.Log("[Spinner] Hidden");
    }

    void Update()
    {
        if (isSpinning && spinnerTransform != null)
        {
            spinnerTransform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// 5. PARTICLE EFFECTS CONTROLLER
// ═══════════════════════════════════════════════════════════════
public class NPCParticleEffects : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem listeningParticles;
    [SerializeField] private ParticleSystem thinkingParticles;
    [SerializeField] private ParticleSystem talkingParticles;

    /// <summary>Call from OnListeningStarted</summary>
    public void PlayListeningEffect()
    {
        StopAllEffects();
        if (listeningParticles != null)
            listeningParticles.Play();
    }

    /// <summary>Call from OnThinkingStarted</summary>
    public void PlayThinkingEffect(string message)
    {
        StopAllEffects();
        if (thinkingParticles != null)
            thinkingParticles.Play();
    }

    /// <summary>Call from OnTalkingStarted</summary>
    public void PlayTalkingEffect(string text)
    {
        StopAllEffects();
        if (talkingParticles != null)
            talkingParticles.Play();
    }

    /// <summary>Call from OnTalkingStopped or OnNPCDeactivated</summary>
    public void StopAllEffects()
    {
        if (listeningParticles != null) listeningParticles.Stop();
        if (thinkingParticles != null) thinkingParticles.Stop();
        if (talkingParticles != null) talkingParticles.Stop();
    }
}

// ═══════════════════════════════════════════════════════════════
// 6. SIMPLE GAME OBJECT TOGGLE
// ═══════════════════════════════════════════════════════════════
public class NPCGameObjectToggle : MonoBehaviour
{
    [Header("Target GameObjects")]
    [SerializeField] private GameObject targetObject;

    /// <summary>Enable GameObject</summary>
    public void EnableObject()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    /// <summary>Disable GameObject</summary>
    public void DisableObject()
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }

    /// <summary>Toggle GameObject</summary>
    public void ToggleObject()
    {
        if (targetObject != null)
            targetObject.SetActive(!targetObject.activeSelf);
    }

    /// <summary>Enable with string parameter (for events with string)</summary>
    public void EnableObjectWithString(string ignored)
    {
        EnableObject();
    }

    /// <summary>Disable with string parameter</summary>
    public void DisableObjectWithString(string ignored)
    {
        DisableObject();
    }
}

// ═══════════════════════════════════════════════════════════════
// 7. LANGUAGE DISPLAY (Show Current Language)
// ═══════════════════════════════════════════════════════════════
public class NPCLanguageDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI languageText;
    [SerializeField] private Image languageFlag;

    [Header("Language Flags (Optional)")]
    [SerializeField] private Sprite englishFlag;
    [SerializeField] private Sprite hindiFlag;
    [SerializeField] private Sprite marathiFlag;

    /// <summary>Call from OnLanguageChanged</summary>
    public void UpdateLanguageDisplay(string oldLang, string newLang)
    {
        if (languageText != null)
            languageText.text = $"Language: {newLang}";

        if (languageFlag != null)
        {
            languageFlag.sprite = GetFlagForLanguage(newLang);
        }

        Debug.Log($"[LanguageDisplay] Updated to {newLang}");
    }

    /// <summary>Call from OnPersonaInitialized</summary>
    public void ShowInitialLanguage(string language)
    {
        if (languageText != null)
            languageText.text = $"Language: {language}";

        if (languageFlag != null)
        {
            languageFlag.sprite = GetFlagForLanguage(language);
        }
    }

    private Sprite GetFlagForLanguage(string language)
    {
        switch (language.ToLower())
        {
            case "english": return englishFlag;
            case "hindi": return hindiFlag;
            case "marathi": return marathiFlag;
            default: return null;
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// 8. ANALYTICS / DEBUG LOGGER
// ═══════════════════════════════════════════════════════════════
public class NPCAnalyticsLogger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool logToFile = false;
    [SerializeField] private string logFilePath = "npc_analytics.txt";

    private int totalInteractions = 0;
    private int languageSwitches = 0;
    private int errors = 0;

    /// <summary>Call from OnSpeechTranscribed</summary>
    public void LogInteraction(string transcript)
    {
        totalInteractions++;
        LogMessage($"[Interaction #{totalInteractions}] User said: \"{transcript}\"");
    }

    /// <summary>Call from OnLanguageChanged</summary>
    public void LogLanguageSwitch(string oldLang, string newLang)
    {
        languageSwitches++;
        LogMessage($"[Language Switch #{languageSwitches}] {oldLang} → {newLang}");
    }

    /// <summary>Call from OnCriticalError</summary>
    public void LogError(string errorType, string message)
    {
        errors++;
        LogMessage($"[ERROR #{errors}] {errorType}: {message}");
    }

    public void LogSessionStart()
    {
        LogMessage("=== NPC SESSION STARTED ===");
    }

    public void LogSessionEnd()
    {
        LogMessage($"=== NPC SESSION ENDED === (Interactions: {totalInteractions}, Switches: {languageSwitches}, Errors: {errors})");
    }

    private void LogMessage(string message)
    {
        string timestamped = $"[{System.DateTime.Now:HH:mm:ss}] {message}";

        if (logToConsole)
            Debug.Log(timestamped);

        if (logToFile)
        {
            System.IO.File.AppendAllText(logFilePath, timestamped + "\n");
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// 9. TUTORIAL / HINT DISPLAY
// ═══════════════════════════════════════════════════════════════
public class NPCTutorialHints : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Hints")]
    [SerializeField] private string listeningHint = "🎤 Speak now...";
    [SerializeField] private string thinkingHint = "⏳ Processing...";
    [SerializeField] private string errorHint = "❌ Try speaking again";

    /// <summary>Call from OnListeningStarted</summary>
    public void ShowListeningHint()
    {
        ShowHint(listeningHint);
    }

    /// <summary>Call from OnThinkingStarted</summary>
    public void ShowThinkingHint(string message)
    {
        ShowHint(thinkingHint);
    }

    /// <summary>Call from OnSpeechRecognitionFailed</summary>
    public void ShowErrorHint(string error)
    {
        ShowHint(errorHint);
    }

    /// <summary>Call from OnTalkingStopped</summary>
    public void HideHint()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private void ShowHint(string hint)
    {
        if (hintPanel != null)
            hintPanel.SetActive(true);

        if (hintText != null)
            hintText.text = hint;
    }
}

// ═══════════════════════════════════════════════════════════════
// 10. SIMPLE ANIMATOR TRIGGER (For Custom Animations)
// ═══════════════════════════════════════════════════════════════
public class NPCAnimationTrigger : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Trigger Names")]
    [SerializeField] private string greetingTrigger = "Greet";
    [SerializeField] private string celebrateTrigger = "Celebrate";
    [SerializeField] private string errorTrigger = "Error";

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>Call from OnPersonaInitialized</summary>
    public void PlayGreeting(string language)
    {
        if (animator != null)
            animator.SetTrigger(greetingTrigger);
    }

    /// <summary>Call from OnSpeechTranscribed</summary>
    public void PlayCelebration(string transcript)
    {
        if (animator != null)
            animator.SetTrigger(celebrateTrigger);
    }

    /// <summary>Call from OnSpeechRecognitionFailed</summary>
    public void PlayError(string error)
    {
        if (animator != null)
            animator.SetTrigger(errorTrigger);
    }
}

/*
 * ═══════════════════════════════════════════════════════════════
 * HOW TO USE THESE HELPER SCRIPTS:
 * ═══════════════════════════════════════════════════════════════
 * 
 * 1. Add desired helper script to any GameObject in your scene
 * 2. Configure the script's settings in Inspector
 * 3. Go to NPCEventSystem GameObject
 * 4. Find the event you want (e.g., "On Talking Started")
 * 5. Click "+" to add a listener
 * 6. Drag the GameObject with the helper script
 * 7. Select the public function from dropdown
 * 
 * Example Setup:
 * 
 * GameObject: "SubtitleUI"
 *   ├─ Canvas
 *   ├─ TextMeshPro
 *   └─ NPCSubtitleDisplay (script)
 * 
 * NPCEventSystem:
 *   On Talking Started:
 *     └─ SubtitleUI → NPCSubtitleDisplay.ShowSubtitle
 *   On Talking Stopped:
 *     └─ SubtitleUI → NPCSubtitleDisplay.HideSubtitle
 * 
 * That's it! No coding needed!
 * ═══════════════════════════════════════════════════════════════
 */