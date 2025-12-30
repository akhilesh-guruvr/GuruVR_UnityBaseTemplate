//Author: Akhilesh Ravindra Mali

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonAction
    {
        public string buttonName;
        public Button button;
        [Tooltip("Frame index to jump to when this button is clicked (-1 for next sequential frame)")]
        public int targetFrameIndex = -1;
        [Tooltip("Specific objects to enable when this button is clicked")]
        public GameObject[] objectsToEnable;
        [Tooltip("Specific objects to disable when this button is clicked")]
        public GameObject[] objectsToDisable;
    }

    [System.Serializable]
    public class FrameData
    {
        public string frameName;
        [Tooltip("Objects to enable at the start of this frame")]
        public GameObject[] objectsToEnable;
        [Tooltip("Objects to disable at the start of this frame")]
        public GameObject[] objectsToDisable;
        [Tooltip("Audio clip to play (optional)")]
        public AudioClip audioClip;
        [Tooltip("Whether this frame has branching paths based on button interaction")]
        public bool hasBranchingPaths = false;
        [Tooltip("Button actions for branching paths")]
        public ButtonAction[] buttonActions;
        [Tooltip("Delay before proceeding to next frame (0 if button activation is required)")]
        public float autoAdvanceDelay;
        [Tooltip("Whether to wait for audio completion before auto-advancing")]
        public bool waitForAudioCompletion = true;
        [Tooltip("Whether this frame requires button interaction to proceed")]
        public bool requireButtonInteraction = true;
        public bool StartController = false;
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private FrameData[] frames;
    [SerializeField] private GameObject[] Panels;
    [SerializeField] private GameObject splashScreen;
    [SerializeField] private float splashScreenDuration = 2f;

    private int currentFrameIndex = -1;
    private bool isTransitioning = false;

    void Start()
    {
        FramesDisable();
        // Check if AudioSource is assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("AudioSource was not assigned. Added one automatically.");
            }
        }

        // Show splash screen first
        if (splashScreen != null)
        {
            splashScreen.SetActive(true);
            StartCoroutine(ShowSplashScreen());
        }
        else
        {
            StartSequence();
        }
    }

    IEnumerator ShowSplashScreen()
    {
        yield return new WaitForSeconds(splashScreenDuration);
        splashScreen.SetActive(false);
        StartSequence();
    }

    void StartSequence()
    {
        if (frames.Length > 0)
        {
            AdvanceToNextFrame();
        }
        else
        {
            Debug.LogWarning("No frames defined in the sequence.");
        }
    }

    public void AdvanceToNextFrame()
    {
        AdvanceToFrame(-1); // -1 means go to the next sequential frame
    }

    public void AdvanceToFrame(int targetFrameIndex)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // If targetFrameIndex is -1, advance to next sequential frame
        // Otherwise jump to the specific frame index
        if (targetFrameIndex == -1)
        {
            currentFrameIndex++;
        }
        else
        {
            currentFrameIndex = targetFrameIndex;
        }

        if (currentFrameIndex < frames.Length)
        {
            StartCoroutine(ProcessFrame(frames[currentFrameIndex]));
        }
        else
        {
            Debug.Log("Sequence completed!");
        }
    }

    IEnumerator ProcessFrame(FrameData frame)
    {
        Debug.Log("Processing frame: " + frame.frameName);

        // Disable objects first
        if (frame.objectsToDisable != null)
        {
            foreach (GameObject obj in frame.objectsToDisable)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // Enable objects
        if (frame.objectsToEnable != null)
        {
            foreach (GameObject obj in frame.objectsToEnable)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Play audio if available
        bool isPlayingAudio = false;
        if (frame.audioClip != null)
        {
            audioSource.clip = frame.audioClip;
            audioSource.Play();
            isPlayingAudio = true;
        }

        // Handle button actions and branching
        if (frame.hasBranchingPaths && frame.buttonActions != null && frame.buttonActions.Length > 0)
        {
            foreach (ButtonAction action in frame.buttonActions)
            {
                if (action.button != null)
                {
                    // Remove all existing listeners to avoid duplicates
                    action.button.onClick.RemoveAllListeners();

                    // Create a local copy of the action to avoid closure issues
                    ButtonAction localAction = action;

                    // Add the click handler with specific action behavior
                    action.button.onClick.AddListener(() => {
                        HandleButtonAction(localAction);
                    });
                }
            }

            // If it has branching, we always wait for a button press
            isTransitioning = false;
            yield break; // Exit the coroutine and wait for button interaction
        }
        // Set up regular non-branching buttons if available
        else if (frame.requireButtonInteraction && frame.buttonActions != null && frame.buttonActions.Length > 0)
        {
            foreach (ButtonAction action in frame.buttonActions)
            {
                if (action.button != null)
                {
                    action.button.onClick.RemoveAllListeners();
                    action.button.onClick.AddListener(AdvanceToNextFrame);
                }
            }

            // Wait for button press
            isTransitioning = false;
            yield break;
        }

        // Auto advance if no buttons or not requiring interaction
        if (!frame.requireButtonInteraction)
        {
            // Wait for audio to complete if required
            if (isPlayingAudio && frame.waitForAudioCompletion)
            {
                yield return new WaitUntil(() => !audioSource.isPlaying);
            }


            // Additional delay if specified
            if (frame.autoAdvanceDelay > 0)
            {
                yield return new WaitForSeconds(frame.autoAdvanceDelay);
            }

            // Auto-advance to next frame
            isTransitioning = false;
            AdvanceToNextFrame();
        }
        else
        {
            // Wait for button interaction
            isTransitioning = false;
        }

    }

    private void HandleButtonAction(ButtonAction action)
    {
        // Handle specific object enables/disables for this button
        if (action.objectsToDisable != null)
        {
            foreach (GameObject obj in action.objectsToDisable)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (action.objectsToEnable != null)
        {
            foreach (GameObject obj in action.objectsToEnable)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Advance to the target frame
        AdvanceToFrame(action.targetFrameIndex);
    }

    // Debug method to show current progress
    public void PrintCurrentFrameInfo()
    {
        if (currentFrameIndex >= 0 && currentFrameIndex < frames.Length)
        {
            Debug.Log($"Current frame: {currentFrameIndex} - {frames[currentFrameIndex].frameName}");
        }
        else
        {
            Debug.Log($"Current frame index: {currentFrameIndex} (invalid)");
        }
    }

    public void FramesDisable()
    {
        foreach(GameObject obj in Panels)
        {
            obj.SetActive(false);
        }
    }
}
