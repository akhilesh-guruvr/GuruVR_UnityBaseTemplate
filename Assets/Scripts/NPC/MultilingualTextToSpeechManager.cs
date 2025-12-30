using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleTextToSpeech.Scripts.Data;
using TMPro;
using System;
using ReadyPlayerAvatar = ReadyPlayerMe.Core;

namespace GoogleTextToSpeech.Scripts
{
    /// <summary>
    /// Enhanced Multilingual TTS Manager with event system
    /// </summary>
    public class MultilingualTextToSpeechManager : MonoBehaviour
    {
        [Header("Default Voice")]
        [SerializeField] private VoiceScriptableObject defaultVoice;
        [SerializeField] private TextToSpeech text_to_speech;

        [Header("Audio Output")]
        public ReadyPlayerAvatar.VoiceHandler voiceHandler;

        [Header("Debug")]
        [SerializeField] private bool enableEventLogs = true;

        private Action<AudioClip> _audioClipReceived;
        private Action<BadRequestData> _errorReceived;
        private string currentTextBeingSpoken = "";
        private Coroutine audioMonitorCoroutine;

        void Start()
        {
            // Auto-find TextToSpeech if not assigned
            if (text_to_speech == null)
            {
                text_to_speech = GetComponent<TextToSpeech>();
                if (text_to_speech == null)
                {
                    Debug.LogError("[MultilingualTTS] TextToSpeech component not found!");
                }
            }

            // Validate VoiceHandler
            if (voiceHandler == null)
            {
                Debug.LogWarning("[MultilingualTTS] VoiceHandler not assigned! TTS will not play audio.");
            }
            else if (voiceHandler.AudioSource == null)
            {
                Debug.LogWarning("[MultilingualTTS] VoiceHandler.AudioSource is null!");
            }
        }

        // ==========================================
        // Send with default voice
        // ==========================================
        public void SendTextToGoogle(string _text)
        {
            SendTextToGoogleWithVoice(_text, defaultVoice);
        }

        // ==========================================
        // Send with specific voice (for multilingual)
        // ==========================================
        public void SendTextToGoogleWithVoice(string _text, VoiceScriptableObject voice)
        {
            if (string.IsNullOrEmpty(_text))
            {
                Debug.LogWarning("[MultilingualTTS] Empty text received.");
                return;
            }

            if (voice == null)
            {
                Debug.LogWarning("[MultilingualTTS] Voice is null, using default.");
                voice = defaultVoice;
            }

            if (voice == null)
            {
                Debug.LogError("[MultilingualTTS] No voice configured! Please assign a default voice.");

                // 🔥 FIRE EVENT: TTS failed
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeTTSFailed("No voice configured");

                return;
            }

            // Store the text for event firing
            currentTextBeingSpoken = _text;

            _errorReceived = ErrorReceived;
            _audioClipReceived = AudioClipReceived;

            if (enableEventLogs)
                Debug.Log($"[MultilingualTTS] Requesting TTS for: \"{_text.Substring(0, Mathf.Min(50, _text.Length))}...\"");

            text_to_speech.GetSpeechAudioFromGoogle(_text, voice, _audioClipReceived, _errorReceived);
        }

        private void ErrorReceived(BadRequestData badRequestData)
        {
            string errorMessage = $"Error {badRequestData.error.code}: {badRequestData.error.message}";
            Debug.LogError($"[MultilingualTTS] {errorMessage}");

            // 🔥 FIRE EVENT: TTS failed
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeTTSFailed(errorMessage);
        }

        private void AudioClipReceived(AudioClip clip)
        {
            if (voiceHandler != null && voiceHandler.AudioSource != null)
            {
                voiceHandler.AudioSource.Stop();
                voiceHandler.AudioSource.clip = clip;
                voiceHandler.AudioSource.Play();

                if (enableEventLogs)
                    Debug.Log($"[MultilingualTTS] ✅ Playing audio clip (length: {clip.length:F2}s)");

                // 🔥 FIRE EVENT: Talking started
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeTalkingStarted(currentTextBeingSpoken);

                // Start monitoring for when audio finishes
                if (audioMonitorCoroutine != null)
                    StopCoroutine(audioMonitorCoroutine);

                audioMonitorCoroutine = StartCoroutine(MonitorAudioPlayback());
            }
            else
            {
                Debug.LogWarning("[MultilingualTTS] VoiceHandler or AudioSource not assigned!");

                // 🔥 FIRE EVENT: TTS failed
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeTTSFailed("VoiceHandler or AudioSource not assigned");
            }
        }

        /// <summary>
        /// Monitors audio playback and fires event when finished
        /// </summary>
        private IEnumerator MonitorAudioPlayback()
        {
            if (voiceHandler == null || voiceHandler.AudioSource == null)
                yield break;

            AudioSource audioSource = voiceHandler.AudioSource;

            // Wait while audio is playing
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            // Audio finished playing
            if (enableEventLogs)
                Debug.Log("[MultilingualTTS] Audio playback finished");

            // 🔥 FIRE EVENT: Talking stopped
            if (NPCEventSystem.Instance != null)
                NPCEventSystem.Instance.InvokeTalkingStopped();

            currentTextBeingSpoken = "";
        }

        /// <summary>
        /// Stops current audio playback (if any)
        /// </summary>
        public void StopCurrentAudio()
        {
            if (voiceHandler != null && voiceHandler.AudioSource != null && voiceHandler.AudioSource.isPlaying)
            {
                voiceHandler.AudioSource.Stop();

                if (enableEventLogs)
                    Debug.Log("[MultilingualTTS] Audio stopped manually");

                // 🔥 FIRE EVENT: Talking stopped
                if (NPCEventSystem.Instance != null)
                    NPCEventSystem.Instance.InvokeTalkingStopped();

                currentTextBeingSpoken = "";

                if (audioMonitorCoroutine != null)
                    StopCoroutine(audioMonitorCoroutine);
            }
        }

        /// <summary>
        /// Check if TTS is currently playing audio
        /// </summary>
        public bool IsCurrentlyTalking()
        {
            return voiceHandler != null &&
                   voiceHandler.AudioSource != null &&
                   voiceHandler.AudioSource.isPlaying;
        }

        /// <summary>
        /// Get the text that is currently being spoken
        /// </summary>
        public string GetCurrentText()
        {
            return currentTextBeingSpoken;
        }

        void OnDisable()
        {
            // Clean up coroutine
            if (audioMonitorCoroutine != null)
            {
                StopCoroutine(audioMonitorCoroutine);
                audioMonitorCoroutine = null;
            }
        }
    }
}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using GoogleTextToSpeech.Scripts.Data;
//using TMPro;
//using System;
//using ReadyPlayerAvatar = ReadyPlayerMe.Core;

//namespace GoogleTextToSpeech.Scripts
//{
//    public class MultilingualTextToSpeechManager : MonoBehaviour
//    {
//        [Header("Default Voice")]
//        [SerializeField] private VoiceScriptableObject defaultVoice;

//        [SerializeField] private TextToSpeech text_to_speech;

//        private Action<AudioClip> _audioClipReceived;
//        private Action<BadRequestData> _errorReceived;

//        public ReadyPlayerAvatar.VoiceHandler voiceHandler;

//        void Start()
//        {
//            // Auto-find TextToSpeech if not assigned
//            if (text_to_speech == null)
//            {
//                text_to_speech = GetComponent<TextToSpeech>();
//                if (text_to_speech == null)
//                {
//                    Debug.LogError("[MultilingualTTS] TextToSpeech component not found!");
//                }
//            }
//        }

//        // ==========================================
//        // Send with default voice
//        // ==========================================
//        public void SendTextToGoogle(string _text)
//        {
//            SendTextToGoogleWithVoice(_text, defaultVoice);
//        }

//        // ==========================================
//        // Send with specific voice (for multilingual)
//        // ==========================================
//        public void SendTextToGoogleWithVoice(string _text, VoiceScriptableObject voice)
//        {
//            if (string.IsNullOrEmpty(_text))
//            {
//                Debug.LogWarning("[MultilingualTTS] Empty text received.");
//                return;
//            }

//            if (voice == null)
//            {
//                Debug.LogWarning("[MultilingualTTS] Voice is null, using default.");
//                voice = defaultVoice;
//            }

//            if (voice == null)
//            {
//                Debug.LogError("[MultilingualTTS] No voice configured! Please assign a default voice.");
//                return;
//            }

//            _errorReceived = ErrorReceived;
//            _audioClipReceived = AudioClipReceived;

//            text_to_speech.GetSpeechAudioFromGoogle(_text, voice, _audioClipReceived, _errorReceived);
//        }

//        private void ErrorReceived(BadRequestData badRequestData)
//        {
//            Debug.LogError($"[MultilingualTTS] Error {badRequestData.error.code} : {badRequestData.error.message}");
//        }

//        private void AudioClipReceived(AudioClip clip)
//        {
//            if (voiceHandler != null && voiceHandler.AudioSource != null)
//            {
//                voiceHandler.AudioSource.Stop();
//                voiceHandler.AudioSource.clip = clip;
//                voiceHandler.AudioSource.Play();
//            }
//            else
//            {
//                Debug.LogWarning("[MultilingualTTS] VoiceHandler or AudioSource not assigned!");
//            }
//        }


//    }
//}