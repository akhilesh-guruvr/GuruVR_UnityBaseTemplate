using System.Collections;
using UnityEngine;

public class BlinkingObject : MonoBehaviour
{
    [Header("Blink Settings")]
    public float blinkInterval = 0.5f; // Half-cycle time (full blink = 2x this).
    public BlinkType blinkType = BlinkType.Visibility; // Choose effect type.

    [Header("Color Blink Options (for Intensity/Custom)")]
    public Color startColor = Color.white; // For Custom: starting color.
    public Color endColor = Color.black;   // For Custom: ending color.

    [Header("Glow Options")]
    public float maxGlowIntensity = 2f;    // Max emission multiplier (brighter than 1 = glow).
    public float minGlowIntensity = 0f;    // Min emission (0 = no glow).

    private Renderer objectRenderer;
    private Material blinkMaterial; // Instance for safe editing (avoids affecting shared mats).
    private bool isBlinking = false;

    public enum BlinkType
    {
        Visibility,  // Hard on/off.
        ColorIntensity, // Fade brightness (grayscale lerp).
        Glow,        // Emission pulse.
        CustomColor  // Lerp between two colors.
    }

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("No Renderer on " + gameObject.name);
            return;
        }

        // Create a material instance to avoid modifying shared materials.
        blinkMaterial = objectRenderer.material; // Unity auto-instances if needed.

        // For Glow: Ensure emission is supported.
        if (blinkType == BlinkType.Glow && !blinkMaterial.HasProperty("_EmissionColor"))
        {
            Debug.LogWarning("Material doesn't support emission. Use Standard shader with Emission enabled.");
        }
        StartBlinking();
    }

    /// <summary> Starts blinking with the selected type. </summary>
    public void StartBlinking()
    {
        if (!isBlinking)
        {
            isBlinking = true;
            StartCoroutine(BlinkRoutine());
            Debug.Log("Started " + blinkType + " blink on " + gameObject.name);
        }
    }

    /// <summary> Stops blinking and resets to base state. </summary>
    public void StopBlinking()
    {
        if (isBlinking)
        {
            isBlinking = false;
            StopCoroutine(BlinkRoutine());

            // Reset to default state based on type.
            switch (blinkType)
            {
                case BlinkType.Visibility:
                    objectRenderer.enabled = true;
                    break;
                case BlinkType.ColorIntensity:
                case BlinkType.CustomColor:
                    blinkMaterial.color = startColor; // Or original color.
                    break;
                case BlinkType.Glow:
                    SetEmissionIntensity(1f); // Back to base glow.
                    break;
            }
            Debug.Log("Stopped blinking on " + gameObject.name);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        float elapsed = 0f;
        while (isBlinking)
        {
            elapsed = 0f;

            // Animate from "start" to "end" state over interval.
            while (elapsed < blinkInterval)
            {
                float t = elapsed / blinkInterval; // 0 to 1 progress.

                switch (blinkType)
                {
                    case BlinkType.Visibility:
                        // Skip lerp for visibility (instant toggle).
                        break;

                    case BlinkType.ColorIntensity:
                        Color currentColor = Color.Lerp(startColor, Color.black, t);
                        blinkMaterial.color = currentColor;
                        break;

                    case BlinkType.CustomColor:
                        blinkMaterial.color = Color.Lerp(startColor, endColor, t);
                        break;

                    case BlinkType.Glow:
                        float intensity = Mathf.Lerp(maxGlowIntensity, minGlowIntensity, t);
                        SetEmissionIntensity(intensity);
                        break;
                }

                elapsed += Time.deltaTime;
                yield return null; // Wait one frame.
            }

            // For Visibility: Toggle at end of interval.
            if (blinkType == BlinkType.Visibility)
            {
                objectRenderer.enabled = !objectRenderer.enabled;
            }
            else
            {
                // Reverse for next half-cycle (end -> start).
                elapsed = 0f;
                while (elapsed < blinkInterval)
                {
                    float t = elapsed / blinkInterval;

                    switch (blinkType)
                    {
                        case BlinkType.ColorIntensity:
                            Color currentColor = Color.Lerp(Color.black, startColor, t);
                            blinkMaterial.color = currentColor;
                            break;

                        case BlinkType.CustomColor:
                            blinkMaterial.color = Color.Lerp(endColor, startColor, t);
                            break;

                        case BlinkType.Glow:
                            float intensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, t);
                            SetEmissionIntensity(intensity);
                            break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }

    /// <summary> Helper: Sets emission intensity (multiplies base emission color). </summary>
    private void SetEmissionIntensity(float intensity)
    {
        if (blinkMaterial.HasProperty("_EmissionColor"))
        {
            Color emission = blinkMaterial.GetColor("_EmissionColor");
            blinkMaterial.SetColor("_EmissionColor", emission * intensity);
        }
    }

    // Optional: Change type at runtime (e.g., from another script).
    public void SetBlinkType(BlinkType newType)
    {
        blinkType = newType;
        if (isBlinking) // Restart with new type.
        {
            StopBlinking();
            StartBlinking();
        }
    }
}