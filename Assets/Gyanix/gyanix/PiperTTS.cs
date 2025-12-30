using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class PiperTTS : MonoBehaviour
{
    [Header("Piper Setup")]
    [Tooltip("Folder containing piper.exe and models. Example: Assets/PiperTTS/StreamingAssets/Piper")]
    private string piperFolderPath = "Assets/Gyanix/StreamingAssets/Piper"; // if empty, will try Application.dataPath + "/PiperTTS/StreamingAssets/Piper"

    [Tooltip("Name of the .onnx model file (e.g. model.onnx)")]
    private string modelFilename = "en_US-joe-medium.onnx";

    private string piperExePath;
    private string modelPath;
    private AudioSource audioSource;

    private void Awake()
    {
        if (string.IsNullOrEmpty(piperFolderPath))
        {
            piperFolderPath = Path.Combine(Application.dataPath, "PiperTTS", "StreamingAssets", "Piper");
        }

        piperExePath = Path.Combine(piperFolderPath, "piper.exe");
        modelPath = Path.Combine(piperFolderPath, modelFilename);

        if (!File.Exists(piperExePath))
        {
            Debug.LogError("[PiperTTS] piper.exe not found at: " + piperExePath);
        }
        if (!File.Exists(modelPath))
        {
            Debug.LogError("[PiperTTS] model not found at: " + modelPath);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Public call — generates speech from text and plays it. The temporary wav file will be deleted after playback.
    /// </summary>
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[PiperTTS] Empty text, skipping.");
            return;
        }

        StartCoroutine(GenerateAndPlay(text));
    }

    private IEnumerator GenerateAndPlay(string text)
    {
        if (!File.Exists(piperExePath) || !File.Exists(modelPath))
        {
            Debug.LogError("[PiperTTS] Piper exe or model missing.");
            yield break;
        }

        // create temp filename in application.persistentDataPath to ensure Unity can load it
        string tempName = "piper_" + System.Guid.NewGuid().ToString("N") + ".wav";
        string outputWavPath = Path.Combine(Application.persistentDataPath, tempName);

        // start external process
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = piperExePath,
            Arguments = $"-m \"{modelPath}\" --output_file \"{outputWavPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process proc = new Process())
            {
                proc.StartInfo = psi;
                proc.Start();

                using (StreamWriter sw = proc.StandardInput)
                {
                    sw.WriteLine(text);
                }

                // Optional: capture errors for debugging
                string stderr = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                if (!string.IsNullOrEmpty(stderr))
                {
                    Debug.LogWarning("[PiperTTS] Piper stderr: " + stderr);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[PiperTTS] Error launching piper.exe: " + ex.Message);
            yield break;
        }

        // wait for file
        float waitTimeout = 5f;
        float t = 0f;
        while (!File.Exists(outputWavPath) && t < waitTimeout)
        {
            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (!File.Exists(outputWavPath))
        {
            Debug.LogError("[PiperTTS] Output WAV not created: " + outputWavPath);
            yield break;
        }

        // load and play
        string uri = "file://" + outputWavPath.Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[PiperTTS] Failed to load audio: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();

                // wait until finished
                yield return new WaitForSeconds(clip.length + 0.1f);
            }
        }

        // delete temporary file
        try
        {
            File.Delete(outputWavPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[PiperTTS] Could not delete temp file: " + ex.Message);
        }
    }
}
