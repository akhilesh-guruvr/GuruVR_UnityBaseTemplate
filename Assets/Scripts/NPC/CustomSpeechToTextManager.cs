//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.IO;
//// Check if New Input System is enabled to prevent errors
//#if ENABLE_INPUT_SYSTEM 
//using UnityEngine.InputSystem;
//#endif

//namespace GoogleSpeechToText.Scripts
//{
//    public class CustomSpeechToTextManager : MonoBehaviour
//    {
//        [Header("Google Cloud API")]
//        [SerializeField] private string apiKey;

//        [Header("Dependencies")]
//        public CustomChatNPCSystem customChatManager;

//        [Header("Input Configuration")]
//        [Tooltip("Use this for Desktop testing (Legacy Input)")]
//        [SerializeField] private KeyCode desktopKey = KeyCode.Space;

//        [Header("Debounce Settings")]
//        [Tooltip("Minimum time in seconds between recording starts to prevent double-triggering")]
//        [SerializeField] private float inputDebounceTime = 0.3f;

//#if ENABLE_INPUT_SYSTEM
//        [Tooltip("Assign your XR Controller Action here (e.g., A button or Grip)")]
//        [SerializeField] private InputActionReference xrInputProperty;
//#endif

//        private AudioClip clip;
//        private byte[] bytes;
//        private bool recording = false;
//        private string micDeviceName = null;
//        private bool isInputEnabled = false;
//        private float lastInputTime = -999f; // Track last input to prevent bouncing

//        void Start()
//        {
//            InitializeManager();
//        }

//        void OnEnable()
//        {
//            // Re-initialize when scene reloads
//            InitializeManager();
//            EnableInputListeners();
//        }

//        void OnDisable()
//        {
//            DisableInputListeners();
//        }

//        void OnDestroy()
//        {
//            DisableInputListeners();
//        }

//        private void InitializeManager()
//        {
//            // Find chat manager
//            if (customChatManager == null)
//            {
//                customChatManager = FindFirstObjectByType<CustomChatNPCSystem>();
//                if (customChatManager == null)
//                {
//                    Debug.LogWarning("[SpeechToText] CustomChatNPCSystem not found in scene!");
//                }
//            }

//            // Setup Microphone
//            if (Microphone.devices.Length > 0)
//            {
//                micDeviceName = Microphone.devices[0];
//                Debug.Log($"[SpeechToText] Using Microphone: {micDeviceName}");
//            }
//            else
//            {
//                Debug.LogError("[SpeechToText] No Microphone detected!");
//            }
//        }

//        private void EnableInputListeners()
//        {
//            if (isInputEnabled) return;

//#if ENABLE_INPUT_SYSTEM
//            if (xrInputProperty != null && xrInputProperty.action != null)
//            {
//                xrInputProperty.action.Enable();
//                xrInputProperty.action.started += OnXRInputStarted;
//                xrInputProperty.action.canceled += OnXRInputCanceled;
//                isInputEnabled = true;
//                Debug.Log("[SpeechToText] XR Input listeners enabled.");
//            }
//            else
//            {
//                Debug.LogWarning("[SpeechToText] XR Input Action Reference is null!");
//            }
//#endif
//        }

//        private void DisableInputListeners()
//        {
//            if (!isInputEnabled) return;

//#if ENABLE_INPUT_SYSTEM
//            if (xrInputProperty != null && xrInputProperty.action != null)
//            {
//                xrInputProperty.action.started -= OnXRInputStarted;
//                xrInputProperty.action.canceled -= OnXRInputCanceled;
//                xrInputProperty.action.Disable();
//                isInputEnabled = false;
//                Debug.Log("[SpeechToText] XR Input listeners disabled.");
//            }
//#endif
//        }

//#if ENABLE_INPUT_SYSTEM
//        private void OnXRInputStarted(InputAction.CallbackContext ctx)
//        {
//            // Debounce check: Ignore if too soon after last input
//            if (Time.time - lastInputTime < inputDebounceTime)
//            {
//                Debug.LogWarning($"[SpeechToText] Input debounced (too fast: {Time.time - lastInputTime:F3}s)");
//                return;
//            }

//            lastInputTime = Time.time;
//            StartRecording();
//        }

//        private void OnXRInputCanceled(InputAction.CallbackContext ctx)
//        {
//            StopRecording();
//        }
//#endif

//        void Update()
//        {
//            // Desktop Legacy Input Support (with debouncing)
//            if (Input.GetKeyDown(desktopKey) && !recording)
//            {
//                if (Time.time - lastInputTime < inputDebounceTime)
//                {
//                    Debug.LogWarning($"[SpeechToText] Desktop input debounced");
//                    return;
//                }
//                lastInputTime = Time.time;
//                StartRecording();
//            }

//            if (Input.GetKeyUp(desktopKey) && recording)
//            {
//                StopRecording();
//            }
//        }

//        // ====================================================
//        // PUBLIC METHODS (For UI Buttons / Mobile Events)
//        // ====================================================

//        public void StartRecordingManual()
//        {
//            if (!recording) StartRecording();
//        }

//        public void StopRecordingManual()
//        {
//            if (recording) StopRecording();
//        }

//        // ====================================================
//        // INTERNAL LOGIC
//        // ====================================================

//        private void StartRecording()
//        {
//            if (recording)
//            {
//                Debug.LogWarning("[SpeechToText] Already recording, ignoring duplicate start request.");
//                return;
//            }

//            // Validate chat manager before starting
//            if (customChatManager == null)
//            {
//                Debug.LogError("[SpeechToText] Cannot start recording: CustomChatNPCSystem is null!");
//                customChatManager = FindFirstObjectByType<CustomChatNPCSystem>();
//                if (customChatManager == null)
//                {
//                    Debug.LogError("[SpeechToText] Failed to find CustomChatNPCSystem. Recording aborted.");
//                    return;
//                }
//            }

//            if (string.IsNullOrEmpty(micDeviceName))
//            {
//                Debug.LogError("[SpeechToText] No microphone available!");
//                return;
//            }

//            clip = Microphone.Start(micDeviceName, false, 10, 44100);
//            recording = true;
//            Debug.Log("[CustomSpeechToTextManager] Recording started...");
//        }

//        private void StopRecording()
//        {
//            if (!recording)
//            {
//                Debug.LogWarning("[SpeechToText] Not recording, ignoring stop request.");
//                return;
//            }

//            int position = Microphone.GetPosition(micDeviceName);
//            Microphone.End(micDeviceName);

//            // CASE 1: Recording was too short (Local Check)
//            if (position < 1000) // Less than ~0.1 seconds
//            {
//                Debug.LogWarning("[CustomSpeechToText] Recording too short.");
//                recording = false;

//                // Trigger the error voice immediately
//                if (customChatManager != null)
//                {
//                    customChatManager.SpeakInputErrorMessage();
//                }
//                else
//                {
//                    Debug.LogError("[SpeechToText] CustomChatNPCSystem is null during error handling!");
//                }
//                return;
//            }

//            float[] samples = new float[position * clip.channels];
//            clip.GetData(samples, 0);
//            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
//            recording = false;

//            // Get Language with null check
//            string langCode = "en-US";
//            if (customChatManager != null)
//            {
//                langCode = customChatManager.GetCurrentLanguageCode();
//            }
//            else
//            {
//                Debug.LogWarning("[SpeechToText] CustomChatNPCSystem is null, using default language: en-US");
//            }

//            Debug.Log($"[CustomSpeechToText] Processing... Language: {langCode}");

//            GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey, langCode,
//                (response) => {
//                    var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);

//                    // CASE 2: API returns success, but no words found (Cloud Check)
//                    if (speechResponse != null && speechResponse.results != null && speechResponse.results.Length > 0)
//                    {
//                        var transcript = speechResponse.results[0].alternatives[0].transcript;

//                        if (string.IsNullOrWhiteSpace(transcript))
//                        {
//                            if (customChatManager != null)
//                                customChatManager.SpeakInputErrorMessage();
//                            else
//                                Debug.LogError("[SpeechToText] CustomChatNPCSystem is null!");
//                        }
//                        else
//                        {
//                            Debug.Log($"[Transcript]: {transcript}");
//                            if (customChatManager != null)
//                            {
//                                customChatManager.SendChat(transcript);
//                            }
//                            else
//                            {
//                                Debug.LogError("[SpeechToText] CustomChatNPCSystem is null! Cannot send transcript.");
//                            }
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogWarning("[CustomSpeechToText] No speech recognized (Silence).");
//                        if (customChatManager != null)
//                            customChatManager.SpeakInputErrorMessage();
//                        else
//                            Debug.LogError("[SpeechToText] CustomChatNPCSystem is null!");
//                    }
//                },
//                (error) => {
//                    Debug.LogError($"[CustomSpeechToText] API Error: {error.error.message}");
//                });
//        }

//        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
//        {
//            using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
//            {
//                using (var writer = new BinaryWriter(memoryStream))
//                {
//                    writer.Write("RIFF".ToCharArray());
//                    writer.Write(36 + samples.Length * 2);
//                    writer.Write("WAVE".ToCharArray());
//                    writer.Write("fmt ".ToCharArray());
//                    writer.Write(16);
//                    writer.Write((ushort)1);
//                    writer.Write((ushort)channels);
//                    writer.Write(frequency);
//                    writer.Write(frequency * channels * 2);
//                    writer.Write((ushort)(channels * 2));
//                    writer.Write((ushort)16);
//                    writer.Write("data".ToCharArray());
//                    writer.Write(samples.Length * 2);

//                    foreach (var sample in samples)
//                    {
//                        writer.Write((short)(sample * short.MaxValue));
//                    }
//                }
//                return memoryStream.ToArray();
//            }
//        }
//    }
//}

using System.Collections;
using UnityEngine;
using System.IO;
using System;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GoogleSpeechToText.Scripts
{
    /// <summary>
    /// Enhanced Speech-to-Text manager with comprehensive event system
    /// </summary>
    public class SpeechToTextManager : MonoBehaviour
    {
        [Header("Google Cloud API")]
        [SerializeField] private string apiKey;

        [Header("Dependencies")]
        public GyanixChatNPCSystem customChatManager;

        [Header("Input Configuration")]
        [Tooltip("Use this for Desktop testing (Legacy Input)")]
        [SerializeField] private KeyCode desktopKey = KeyCode.Space;

        [Header("Debounce Settings")]
        [Tooltip("Minimum time in seconds between recording starts")]
        [SerializeField] private float inputDebounceTime = 0.3f;

        [Header("Recording Settings")]
        [Tooltip("Minimum recording duration in seconds (prevents accidental triggers)")]
        [SerializeField] private float minimumRecordingDuration = 0.1f;

        [Tooltip("Maximum recording duration in seconds")]
        [SerializeField] private int maxRecordingDuration = 10;

        [Tooltip("Recording frequency (Hz)")]
        [SerializeField] private int recordingFrequency = 44100;

#if ENABLE_INPUT_SYSTEM
        [Tooltip("Assign your XR Controller Action here")]
        [SerializeField] private InputActionReference xrInputProperty;
#endif

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Private variables
        private AudioClip clip;
        private byte[] bytes;
        private bool recording = false;
        private string micDeviceName = null;
        private bool isInputEnabled = false;
        private float lastInputTime = -999f;
        private float recordingStartTime = 0f;

        #region Unity Lifecycle

        void Start()
        {
            InitializeManager();
        }

        void OnEnable()
        {
            InitializeManager();
            EnableInputListeners();
        }

        void OnDisable()
        {
            DisableInputListeners();

            // Stop recording if active
            if (recording)
            {
                StopRecordingImmediate();
            }
        }

        void OnDestroy()
        {
            DisableInputListeners();
        }

        void Update()
        {
            // Desktop Legacy Input Support (with debouncing)
            if (Input.GetKeyDown(desktopKey) && !recording)
            {
                if (Time.time - lastInputTime < inputDebounceTime)
                {
                    if (enableDebugLogs)
                        Debug.LogWarning("[EnhancedSTT] Desktop input debounced");

                    if (NPCEventSystem.Instance != null)
                        NPCEventSystem.Instance.InvokeInputDebounced();
                    return;
                }

                lastInputTime = Time.time;
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokePlayerInputPressed();
                StartRecording();
            }

            if (Input.GetKeyUp(desktopKey) && recording)
            {
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokePlayerInputReleased();
                StopRecording();
            }
        }

        #endregion

        #region Initialization

        private void InitializeManager()
        {
            // Find chat manager
            if (customChatManager == null)
            {
                customChatManager = FindFirstObjectByType<GyanixChatNPCSystem>();
                if (customChatManager == null)
                {
                    Debug.LogWarning("[EnhancedSTT] GyanixChatNPCSystem not found!");
                    if (NPCEventSystem.Instance != null)
                        NPCEventSystem.Instance.InvokeCriticalError("STT_INIT", "GyanixChatNPCSystem not found");
                }
            }

            // Setup Microphone
            if (Microphone.devices.Length > 0)
            {
                micDeviceName = Microphone.devices[0];
                if (enableDebugLogs)
                    Debug.Log($"[EnhancedSTT] Using Microphone: {micDeviceName}");
            }
            else
            {
                Debug.LogError("[EnhancedSTT] No Microphone detected!");
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeCriticalError("STT_INIT", "No microphone detected");
            }
        }

        #endregion

        #region Input Handling

        private void EnableInputListeners()
        {
            if (isInputEnabled) return;

#if ENABLE_INPUT_SYSTEM
            if (xrInputProperty != null && xrInputProperty.action != null)
            {
                xrInputProperty.action.Enable();
                xrInputProperty.action.started += OnXRInputStarted;
                xrInputProperty.action.canceled += OnXRInputCanceled;
                isInputEnabled = true;

                if (enableDebugLogs)
                    Debug.Log("[EnhancedSTT] XR Input listeners enabled");
            }
            else
            {
                Debug.LogWarning("[EnhancedSTT] XR Input Action Reference is null!");
            }
#endif
        }

        private void DisableInputListeners()
        {
            if (!isInputEnabled) return;

#if ENABLE_INPUT_SYSTEM
            if (xrInputProperty != null && xrInputProperty.action != null)
            {
                xrInputProperty.action.started -= OnXRInputStarted;
                xrInputProperty.action.canceled -= OnXRInputCanceled;
                xrInputProperty.action.Disable();
                isInputEnabled = false;

                if (enableDebugLogs)
                    Debug.Log("[EnhancedSTT] XR Input listeners disabled");
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnXRInputStarted(InputAction.CallbackContext ctx)
        {
            // Debounce check
            if (Time.time - lastInputTime < inputDebounceTime)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[EnhancedSTT] Input debounced ({Time.time - lastInputTime:F3}s)");

                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeInputDebounced();
                return;
            }

            lastInputTime = Time.time;
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokePlayerInputPressed();
            StartRecording();
        }

        private void OnXRInputCanceled(InputAction.CallbackContext ctx)
        {
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokePlayerInputReleased();
            StopRecording();
        }
#endif

        #endregion

        #region Public Methods (For UI Buttons / Mobile)

        public void StartRecordingManual()
        {
            if (!recording)
            {
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokePlayerInputPressed();
                StartRecording();
            }
        }

        public void StopRecordingManual()
        {
            if (recording)
            {
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokePlayerInputReleased();
                StopRecording();
            }
        }

        public bool IsRecording()
        {
            return recording;
        }

        #endregion

        #region Recording Logic

        private void StartRecording()
        {
            if (recording)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EnhancedSTT] Already recording, ignoring duplicate start.");
                return;
            }

            // Validate chat manager
            if (customChatManager == null)
            {
                Debug.LogError("[EnhancedSTT] Cannot start: GyanixChatNPCSystem is null!");
                customChatManager = FindFirstObjectByType<GyanixChatNPCSystem>();

                if (customChatManager == null)
                {
                    if (NPCEventSystem.Instance != null)
                        NPCEventSystem.Instance.InvokeCriticalError("STT_RECORD", "GyanixChatNPCSystem not found");
                    return;
                }
            }

            // Validate microphone
            if (string.IsNullOrEmpty(micDeviceName))
            {
                Debug.LogError("[EnhancedSTT] No microphone available!");
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeSpeechRecognitionFailed("No microphone available");
                return;
            }

            // Start recording
            clip = Microphone.Start(micDeviceName, false, maxRecordingDuration, recordingFrequency);
            recording = true;
            recordingStartTime = Time.time;

            if (enableDebugLogs)
                Debug.Log("[EnhancedSTT] 🎤 Recording started");

            // Fire event
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeListeningStarted();
        }

        private void StopRecording()
        {
            if (!recording)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EnhancedSTT] Not recording, ignoring stop.");
                return;
            }

            float recordingDuration = Time.time - recordingStartTime;
            int position = Microphone.GetPosition(micDeviceName);
            Microphone.End(micDeviceName);

            if (enableDebugLogs)
                Debug.Log($"[EnhancedSTT] 🎤 Recording stopped (Duration: {recordingDuration:F2}s, Samples: {position})");

            // Fire event
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeListeningStopped();

            // CASE 1: Recording too short (Local validation)
            if (recordingDuration < minimumRecordingDuration || position < 1000)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EnhancedSTT] Recording too short");

                recording = false;

                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeSpeechRecognitionFailed("Recording too short");

                // Trigger error voice
                if (customChatManager != null)
                {
                    customChatManager.SpeakInputErrorMessage();
                }

                return;
            }

            // Extract audio data
            float[] samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;

            // Get language code
            string langCode = "en-US";
            if (customChatManager != null)
            {
                langCode = customChatManager.GetCurrentLanguageCode();
            }
            else
            {
                Debug.LogWarning("[EnhancedSTT] GyanixChatNPCSystem is null, using default: en-US");
            }

            if (enableDebugLogs)
                Debug.Log($"[EnhancedSTT] Processing with language: {langCode}");

            // Send to Google Speech-to-Text
            ProcessSpeechToText(langCode);
        }

        private void StopRecordingImmediate()
        {
            if (recording)
            {
                Microphone.End(micDeviceName);
                recording = false;

                if (enableDebugLogs)
                    Debug.Log("[EnhancedSTT] Recording force-stopped");
            }
        }

        #endregion

        #region Speech-to-Text Processing

        private void ProcessSpeechToText(string languageCode)
        {
            GoogleCloudSpeechToText.SendSpeechToTextRequest(
                bytes,
                apiKey,
                languageCode,
                (response) => OnSpeechRecognitionSuccess(response),
                (error) => OnSpeechRecognitionError(error)
            );
        }

        private void OnSpeechRecognitionError(BadRequestData error)
        {
            throw new NotImplementedException();
        }

        private void OnSpeechRecognitionSuccess(string response)
        {
            var speechResponse = JsonUtility.FromJson<GoogleSpeechResponse>(response);

            // CASE 2: API success, but no words detected
            if (speechResponse == null || speechResponse.results == null || speechResponse.results.Length == 0)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EnhancedSTT] No speech recognized (Silence)");

                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeSpeechRecognitionFailed("No speech detected");

                if (customChatManager != null)
                {
                    customChatManager.SpeakInputErrorMessage();
                }

                return;
            }

            // Extract transcript
            var transcript = speechResponse.results[0].alternatives[0].transcript;

            if (string.IsNullOrWhiteSpace(transcript))
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EnhancedSTT] Empty transcript received");

                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeSpeechRecognitionFailed("Empty transcript");

                if (customChatManager != null)
                {
                    customChatManager.SpeakInputErrorMessage();
                }

                return;
            }

            // SUCCESS
            if (enableDebugLogs)
                Debug.Log($"[EnhancedSTT] ✅ Transcript: \"{transcript}\"");

            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeSpeechTranscribed(transcript);

            // Send to chat manager
            if (customChatManager != null)
            {
                customChatManager.SendChat(transcript);
            }
            else
            {
                Debug.LogError("[EnhancedSTT] GyanixChatNPCSystem is null! Cannot send transcript.");
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeCriticalError("STT_SEND", "GyanixChatNPCSystem is null");
            }
        }

        private void OnSpeechRecognitionError(GoogleSpeechError error)
        {
            Debug.LogError($"[EnhancedSTT] API Error: {error.error.message}");
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeSpeechRecognitionFailed($"API Error: {error.error.message}");
        }

        #endregion

        #region Audio Encoding

        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + samples.Length * 2);
                    writer.Write("WAVE".ToCharArray());
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((ushort)1);
                    writer.Write((ushort)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16);
                    writer.Write("data".ToCharArray());
                    writer.Write(samples.Length * 2);

                    foreach (var sample in samples)
                    {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }
                return memoryStream.ToArray();
            }
        }

        #endregion
    }

    #region Google Speech Response Data Classes (Renamed to avoid conflicts)

    [System.Serializable]
    public class GoogleSpeechResponse
    {
        public GoogleSpeechResult[] results;
    }

    [System.Serializable]
    public class GoogleSpeechResult
    {
        public GoogleSpeechAlternative[] alternatives;
    }

    [System.Serializable]
    public class GoogleSpeechAlternative
    {
        public string transcript;
        public float confidence;
    }

    [System.Serializable]
    public class GoogleSpeechError
    {
        public GoogleErrorDetails error;
    }

    [System.Serializable]
    public class GoogleErrorDetails
    {
        public int code;
        public string message;
        public string status;
    }

    #endregion
}