using UnityEngine;
using ReadyPlayerAvatar = ReadyPlayerMe.Core;

/// <summary>
/// Automatically transitions NPC between Idle and Talking animation states
/// based on whether TTS audio is currently playing.
/// </summary>
public class NPCTalkingAnimator : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The Animator component controlling the NPC animations")]
    [SerializeField] private Animator npcAnimator;

    [Tooltip("The VoiceHandler containing the AudioSource for TTS")]
    [SerializeField] private ReadyPlayerAvatar.VoiceHandler voiceHandler;

    [Header("Animation Settings")]
    [Tooltip("Name of the boolean parameter in Animator that controls Idle<->Talking")]
    [SerializeField] private string talkParameterName = "Talk";

    [Tooltip("Delay before transitioning back to Idle after audio stops (seconds)")]
    [SerializeField] private float idleTransitionDelay = 0.2f;

    private bool isTalking = false;
    private float audioStopTime = 0f;

    void Start()
    {
        // Auto-find components if not assigned
        if (npcAnimator == null)
        {
            npcAnimator = GetComponent<Animator>();
            if (npcAnimator == null)
            {
                Debug.LogError("[NPCTalkingAnimator] Animator component not found! Please assign it in the inspector.");
            }
        }

        if (voiceHandler == null)
        {
            voiceHandler = GetComponent<ReadyPlayerAvatar.VoiceHandler>();
            if (voiceHandler == null)
            {
                Debug.LogError("[NPCTalkingAnimator] VoiceHandler not found! Please assign it in the inspector.");
            }
        }

        // Verify the animator has the required parameter
        if (npcAnimator != null)
        {
            bool hasParameter = false;
            foreach (AnimatorControllerParameter param in npcAnimator.parameters)
            {
                if (param.name == talkParameterName && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasParameter = true;
                    break;
                }
            }

            if (!hasParameter)
            {
                Debug.LogError($"[NPCTalkingAnimator] Animator does not have a boolean parameter named '{talkParameterName}'!");
            }
        }
    }

    void Update()
    {
        if (npcAnimator == null || voiceHandler == null || voiceHandler.AudioSource == null)
            return;

        AudioSource audioSource = voiceHandler.AudioSource;
        bool isAudioPlaying = audioSource.isPlaying && audioSource.clip != null;

        // Transition TO Talking state
        if (isAudioPlaying && !isTalking)
        {
            SetTalkingState(true);
        }
        // Transition TO Idle state (with optional delay)
        else if (!isAudioPlaying && isTalking)
        {
            // Record when audio stopped
            if (audioStopTime == 0f)
            {
                audioStopTime = Time.time;
            }

            // Wait for delay before transitioning to idle
            if (Time.time - audioStopTime >= idleTransitionDelay)
            {
                SetTalkingState(false);
                audioStopTime = 0f;
            }
        }
        // Reset timer if audio starts again during delay period
        else if (isAudioPlaying && audioStopTime != 0f)
        {
            audioStopTime = 0f;
        }
    }

    /// <summary>
    /// Sets the animator's Talk parameter and updates internal state
    /// </summary>
    private void SetTalkingState(bool talking)
    {
        isTalking = talking;
        npcAnimator.SetBool(talkParameterName, talking);

        Debug.Log($"[NPCTalkingAnimator] NPC {(talking ? "started talking" : "stopped talking")}");
    }

    /// <summary>
    /// Public method to manually force talking state (optional, for external control)
    /// </summary>
    public void ForceTalkingState(bool talking)
    {
        SetTalkingState(talking);
        audioStopTime = 0f;
    }

    /// <summary>
    /// Returns whether the NPC is currently in talking state
    /// </summary>
    public bool IsTalking()
    {
        return isTalking;
    }

    // Debug visualization in Scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying && voiceHandler != null && voiceHandler.AudioSource != null)
        {
            // Draw a colored sphere above the NPC to show talking state
            Gizmos.color = isTalking ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
        }
    }
}