using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dynamic NPC animation system that reacts to NPC state changes via events
/// Supports multiple animation states: Idle, Listening, Thinking, Talking
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCDynamicAnimator : MonoBehaviour
{
    #region Animation State Enum
    public enum NPCAnimationState
    {
        Idle,       // Default state - NPC is doing nothing
        Listening,  // NPC is actively listening to player speech
        Thinking,   // NPC is processing/waiting for backend response
        Talking,    // NPC is speaking (audio playing)
        Greeting,   // Optional: Special animation for first greeting
        Gesturing   // Optional: Hand gestures while talking
    }
    #endregion

    #region Inspector Settings

    [Header("Animator Configuration")]
    [Tooltip("The Animator component (auto-detected if not assigned)")]
    [SerializeField] private Animator npcAnimator;

    [Header("Animation Parameter Names")]
    [Tooltip("Name of the Animator integer parameter for state (0=Idle, 1=Listening, 2=Thinking, 3=Talking)")]
    [SerializeField] private string stateParameterName = "NPCState";

    [Tooltip("Alternative: Use separate boolean triggers instead of integer state")]
    [SerializeField] private bool useSeparateBooleans = false;

    [Header("Boolean Parameter Names (if using separate booleans)")]
    [SerializeField] private string idleParameter = "IsIdle";
    [SerializeField] private string listeningParameter = "IsListening";
    [SerializeField] private string thinkingParameter = "IsThinking";
    [SerializeField] private string talkingParameter = "IsTalking";

    [Header("Transition Settings")]
    [Tooltip("Delay before transitioning from Talking → Idle after audio stops")]
    [SerializeField] private float talkingToIdleDelay = 0.3f;

    [Tooltip("Delay before transitioning from Thinking → Talking")]
    [SerializeField] private float thinkingToTalkingDelay = 0.1f;

    [Tooltip("Should NPC return to Idle immediately when player leaves range?")]
    [SerializeField] private bool resetOnDeactivation = true;

    [Header("Advanced Features")]
    [Tooltip("Enable smooth state transitions with crossfade")]
    [SerializeField] private bool useCrossfade = true;

    [Tooltip("Crossfade duration in seconds")]
    [SerializeField] private float crossfadeDuration = 0.2f;

    [Header("Debug")]
    [Tooltip("Show current animation state in inspector")]
    [SerializeField] private NPCAnimationState currentState = NPCAnimationState.Idle;

    [Tooltip("Enable detailed debug logs")]
    [SerializeField] private bool enableDebugLogs = true;

    #endregion

    #region Private Variables

    private NPCAnimationState previousState = NPCAnimationState.Idle;
    private float stateChangeTime = 0f;
    private bool isTransitioning = false;

    // State tracking for delayed transitions
    private float lastTalkingStopTime = 0f;
    private bool pendingIdleTransition = false;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Auto-find Animator
        if (npcAnimator == null)
        {
            npcAnimator = GetComponent<Animator>();
            if (npcAnimator == null)
            {
                Debug.LogError("[NPCDynamicAnimator] Animator component not found!");
                enabled = false;
                return;
            }
        }

        ValidateAnimatorParameters();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        // Handle delayed transitions
        HandlePendingTransitions();
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        if (NPCEventSystem.Instance == null)
        {
            Debug.LogWarning("[NPCDynamicAnimator] NPCEventSystem not found! Animations will not respond to events.");
            return;
        }

        // Proximity events
        NPCEventSystem.Instance.OnNPCActivated += HandleNPCActivated;
        NPCEventSystem.Instance.OnNPCDeactivated += HandleNPCDeactivated;

        // Listening events
        NPCEventSystem.Instance.OnListeningStarted += HandleListeningStarted;
        NPCEventSystem.Instance.OnListeningStopped += HandleListeningStopped;

        // Thinking events
        NPCEventSystem.Instance.OnThinkingStarted += HandleThinkingStarted;
        NPCEventSystem.Instance.OnThinkingStopped += HandleThinkingStopped;

        // Talking events
        NPCEventSystem.Instance.OnTalkingStarted += HandleTalkingStarted;
        NPCEventSystem.Instance.OnTalkingStopped += HandleTalkingStopped;

        // Persona initialization (optional greeting animation)
        NPCEventSystem.Instance.OnPersonaInitialized += HandlePersonaInitialized;

        if (enableDebugLogs)
            Debug.Log("[NPCDynamicAnimator] ✅ Subscribed to NPC events");
    }

    private void UnsubscribeFromEvents()
    {
        if (NPCEventSystem.Instance == null) return;

        NPCEventSystem.Instance.OnNPCActivated -= HandleNPCActivated;
        NPCEventSystem.Instance.OnNPCDeactivated -= HandleNPCDeactivated;
        NPCEventSystem.Instance.OnListeningStarted -= HandleListeningStarted;
        NPCEventSystem.Instance.OnListeningStopped -= HandleListeningStopped;
        NPCEventSystem.Instance.OnThinkingStarted -= HandleThinkingStarted;
        NPCEventSystem.Instance.OnThinkingStopped -= HandleThinkingStopped;
        NPCEventSystem.Instance.OnTalkingStarted -= HandleTalkingStarted;
        NPCEventSystem.Instance.OnTalkingStopped -= HandleTalkingStopped;
        NPCEventSystem.Instance.OnPersonaInitialized -= HandlePersonaInitialized;

        if (enableDebugLogs)
            Debug.Log("[NPCDynamicAnimator] ❌ Unsubscribed from NPC events");
    }

    #endregion

    #region Event Handlers

    private void HandleNPCActivated()
    {
        if (enableDebugLogs)
            Debug.Log("[NPCDynamicAnimator] NPC Activated - Returning to Idle");

        SetAnimationState(NPCAnimationState.Idle);
    }

    private void HandleNPCDeactivated()
    {
        if (resetOnDeactivation)
        {
            if (enableDebugLogs)
                Debug.Log("[NPCDynamicAnimator] NPC Deactivated - Forcing Idle");

            SetAnimationState(NPCAnimationState.Idle, true); // Force immediate
        }
    }

    private void HandleListeningStarted()
    {
        SetAnimationState(NPCAnimationState.Listening);
    }

    private void HandleListeningStopped()
    {
        // Don't immediately return to Idle - wait for Thinking state
        if (enableDebugLogs)
            Debug.Log("[NPCDynamicAnimator] Listening stopped, waiting for next state...");
    }

    private void HandleThinkingStarted(string userMessage)
    {
        SetAnimationState(NPCAnimationState.Thinking);
    }

    private void HandleThinkingStopped(string npcResponse)
    {
        // Small delay before transitioning to Talking
        Invoke(nameof(DelayedThinkingToTalking), thinkingToTalkingDelay);
    }

    private void DelayedThinkingToTalking()
    {
        // Only transition if we're still in Thinking state
        if (currentState == NPCAnimationState.Thinking)
        {
            // Talking animation will be triggered by OnTalkingStarted event
            // Just ensure we're ready
        }
    }

    private void HandleTalkingStarted(string text)
    {
        pendingIdleTransition = false; // Cancel any pending idle transition
        SetAnimationState(NPCAnimationState.Talking);
    }

    private void HandleTalkingStopped()
    {
        lastTalkingStopTime = Time.time;
        pendingIdleTransition = true; // Flag for delayed transition
    }

    private void HandlePersonaInitialized(string language)
    {
        // Optional: Play greeting animation
        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] Persona initialized in {language}");
    }

    #endregion

    #region Animation State Management

    /// <summary>
    /// Sets the NPC animation state with optional immediate mode
    /// </summary>
    public void SetAnimationState(NPCAnimationState newState, bool immediate = false)
    {
        if (npcAnimator == null) return;

        // Prevent redundant state changes
        if (currentState == newState && !immediate)
        {
            if (enableDebugLogs)
                Debug.Log($"[NPCDynamicAnimator] Already in {newState} state, skipping...");
            return;
        }

        previousState = currentState;
        currentState = newState;
        stateChangeTime = Time.time;

        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] State Change: {previousState} → {currentState}");

        // Apply to Animator
        if (useSeparateBooleans)
        {
            SetBooleanParameters(newState);
        }
        else
        {
            SetIntegerParameter(newState);
        }

        // Optional crossfade
        if (useCrossfade && !immediate)
        {
            ApplyCrossfade(newState);
        }
    }

    /// <summary>
    /// Sets animator integer parameter based on state
    /// </summary>
    private void SetIntegerParameter(NPCAnimationState state)
    {
        if (string.IsNullOrEmpty(stateParameterName)) return;

        int stateValue = (int)state;
        npcAnimator.SetInteger(stateParameterName, stateValue);

        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] Set {stateParameterName} = {stateValue} ({state})");
    }

    /// <summary>
    /// Sets separate boolean parameters for each state
    /// </summary>
    private void SetBooleanParameters(NPCAnimationState state)
    {
        // Reset all booleans first
        if (!string.IsNullOrEmpty(idleParameter))
            npcAnimator.SetBool(idleParameter, state == NPCAnimationState.Idle);

        if (!string.IsNullOrEmpty(listeningParameter))
            npcAnimator.SetBool(listeningParameter, state == NPCAnimationState.Listening);

        if (!string.IsNullOrEmpty(thinkingParameter))
            npcAnimator.SetBool(thinkingParameter, state == NPCAnimationState.Thinking);

        if (!string.IsNullOrEmpty(talkingParameter))
            npcAnimator.SetBool(talkingParameter, state == NPCAnimationState.Talking);

        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] Set boolean parameters for {state}");
    }

    /// <summary>
    /// Applies smooth crossfade transition between states
    /// </summary>
    private void ApplyCrossfade(NPCAnimationState state)
    {
        string stateName = state.ToString();
        npcAnimator.CrossFade(stateName, crossfadeDuration);

        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] Crossfading to {stateName} over {crossfadeDuration}s");
    }

    #endregion

    #region Transition Management

    private void HandlePendingTransitions()
    {
        // Handle delayed Talking → Idle transition
        if (pendingIdleTransition && currentState == NPCAnimationState.Talking)
        {
            if (Time.time - lastTalkingStopTime >= talkingToIdleDelay)
            {
                SetAnimationState(NPCAnimationState.Idle);
                pendingIdleTransition = false;
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually force a specific animation state (for testing or special cases)
    /// </summary>
    public void ForceState(NPCAnimationState state)
    {
        if (enableDebugLogs)
            Debug.Log($"[NPCDynamicAnimator] Force State: {state}");

        SetAnimationState(state, true);
    }

    /// <summary>
    /// Returns the current animation state
    /// </summary>
    public NPCAnimationState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Returns time since last state change
    /// </summary>
    public float GetTimeSinceStateChange()
    {
        return Time.time - stateChangeTime;
    }

    /// <summary>
    /// Check if NPC is currently in a specific state
    /// </summary>
    public bool IsInState(NPCAnimationState state)
    {
        return currentState == state;
    }

    #endregion

    #region Validation

    private void ValidateAnimatorParameters()
    {
        if (npcAnimator == null) return;

        if (useSeparateBooleans)
        {
            ValidateParameter(idleParameter, AnimatorControllerParameterType.Bool);
            ValidateParameter(listeningParameter, AnimatorControllerParameterType.Bool);
            ValidateParameter(thinkingParameter, AnimatorControllerParameterType.Bool);
            ValidateParameter(talkingParameter, AnimatorControllerParameterType.Bool);
        }
        else
        {
            ValidateParameter(stateParameterName, AnimatorControllerParameterType.Int);
        }
    }

    private void ValidateParameter(string paramName, AnimatorControllerParameterType expectedType)
    {
        if (string.IsNullOrEmpty(paramName)) return;

        bool found = false;
        foreach (AnimatorControllerParameter param in npcAnimator.parameters)
        {
            if (param.name == paramName)
            {
                if (param.type == expectedType)
                {
                    found = true;
                    break;
                }
                else
                {
                    Debug.LogError($"[NPCDynamicAnimator] Parameter '{paramName}' exists but is {param.type}, expected {expectedType}");
                    return;
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[NPCDynamicAnimator] Parameter '{paramName}' not found in Animator! Add it to your Animator Controller.");
        }
    }

    #endregion

    #region Debug Visualization

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || npcAnimator == null) return;

        // Draw colored sphere above NPC head based on current state
        Vector3 indicatorPos = transform.position + Vector3.up * 2.5f;
        Color stateColor = GetStateColor(currentState);

        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(indicatorPos, 0.3f);

        // Draw filled sphere for active state
        Gizmos.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.3f);
        Gizmos.DrawSphere(indicatorPos, 0.25f);
    }

    private Color GetStateColor(NPCAnimationState state)
    {
        switch (state)
        {
            case NPCAnimationState.Idle: return Color.gray;
            case NPCAnimationState.Listening: return Color.blue;
            case NPCAnimationState.Thinking: return Color.yellow;
            case NPCAnimationState.Talking: return Color.green;
            case NPCAnimationState.Greeting: return Color.cyan;
            default: return Color.white;
        }
    }

    #endregion
}

#region Animator Setup Guide
/*
 * ========================================
 * ANIMATOR CONTROLLER SETUP GUIDE
 * ========================================
 * 
 * METHOD 1: INTEGER STATE PARAMETER (Recommended)
 * -----------------------------------------------
 * 1. Create 4 animation states in your Animator:
 *    - Idle (default)
 *    - Listening
 *    - Thinking
 *    - Talking
 * 
 * 2. Add an Integer parameter named "NPCState" (or your custom name)
 * 
 * 3. Create transitions between states using conditions:
 *    - Any State → Idle: NPCState Equals 0
 *    - Any State → Listening: NPCState Equals 1
 *    - Any State → Thinking: NPCState Equals 2
 *    - Any State → Talking: NPCState Equals 3
 * 
 * 4. Disable "Has Exit Time" on transitions for instant switching
 * 
 * -----------------------------------------------
 * METHOD 2: SEPARATE BOOLEAN PARAMETERS
 * -----------------------------------------------
 * 1. Create 4 animation states as above
 * 
 * 2. Add 4 Boolean parameters:
 *    - IsIdle
 *    - IsListening
 *    - IsThinking
 *    - IsTalking
 * 
 * 3. Create transitions with conditions:
 *    - Idle → Listening: IsListening Equals true
 *    - Listening → Thinking: IsThinking Equals true
 *    - Thinking → Talking: IsTalking Equals true
 *    - Talking → Idle: IsIdle Equals true
 * 
 * 4. Check "useSeparateBooleans" in inspector
 * 
 * ========================================
 * ANIMATION TIPS
 * ========================================
 * - Idle: Subtle breathing, occasional blinks, weight shifts
 * - Listening: Lean forward slightly, head tilts, attentive pose
 * - Thinking: Hand on chin, looking up/away, contemplative
 * - Talking: Lip sync (if using blend shapes), hand gestures, eye contact
 */
#endregion