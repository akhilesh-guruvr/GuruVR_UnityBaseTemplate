using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

public class VRPushToTalk : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign a TextMeshProUGUI element to show mic status & levels.")]
    public Text statusText;

    [Header("Input Settings")]
    [Tooltip("Assign InputActionReference mapped to controller button (e.g., A button).")]
    public InputActionReference pushToTalkAction;

    [Header("Microphone Settings")]
    public int sampleRate = 16000;
    public int bufferSize = 1024;

    private string micDevice;
    private AudioClip micClip;
    private bool isRecording = false;
    private int lastPos = 0;

    // 🔌 Future integration hooks (for Vosk, etc.)
    public Action OnVoiceStart;
    public Action OnVoiceStop;
    public Action<float[]> OnVoiceData;

    void Start()
    {
        SetupMicrophone();
        SetupInput();

        if (statusText)
            statusText.text = "🎧 Ready. Press A to Talk.";
    }

    void Update()
    {
        if (isRecording && micClip != null)
            ProcessMicData();
    }

    #region === SETUP ===

    private void SetupMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            SetStatus("❌ No microphone found!");
            enabled = false;
            return;
        }

        micDevice = Microphone.devices[0];
        Debug.Log($"🎧 Using microphone: {micDevice}");
    }

    private void SetupInput()
    {
        if (pushToTalkAction == null)
        {
            SetStatus("⚠️ No InputActionReference assigned!");
            return;
        }

        // Bind button press/release events
        pushToTalkAction.action.started += ctx => StartRecording();
        pushToTalkAction.action.canceled += ctx => StopRecording();
        pushToTalkAction.action.Enable();

        Debug.Log("🎮 Push-to-Talk input ready!");
    }

    #endregion

    #region === RECORDING ===

    private void StartRecording()
    {
        if (isRecording) return;

        micClip = Microphone.Start(micDevice, true, 10, sampleRate);
        isRecording = true;
        lastPos = 0;

        SetStatus("🎙️ Recording...");

        OnVoiceStart?.Invoke(); // future hook for Vosk
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(micDevice);
        isRecording = false;

        SetStatus("🛑 Stopped. Press A to Talk Again.");

        OnVoiceStop?.Invoke();
    }

    private void ProcessMicData()
    {
        int pos = Microphone.GetPosition(micDevice);
        int diff = pos - lastPos;
        if (diff < 0) diff += micClip.samples;

        while (diff >= bufferSize)
        {
            float[] samples = new float[bufferSize];
            micClip.GetData(samples, lastPos);

            float level = 0;
            foreach (float s in samples) level += Mathf.Abs(s);
            level /= samples.Length;

            // Update mic loudness on screen
            SetStatus($"🎙️ Recording... (Level: {level:F3})");

            OnVoiceData?.Invoke(samples);

            lastPos += bufferSize;
            if (lastPos >= micClip.samples) lastPos = 0;
            diff -= bufferSize;
        }
    }

    #endregion

    private void SetStatus(string msg)
    {
        if (statusText)
            statusText.text = msg;
    }

    void OnDestroy()
    {
        if (pushToTalkAction != null)
        {
            pushToTalkAction.action.started -= ctx => StartRecording();
            pushToTalkAction.action.canceled -= ctx => StopRecording();
        }
    }
}
