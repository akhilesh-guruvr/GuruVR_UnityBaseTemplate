using UnityEngine;

public class UILoadingSpinnerSmooth : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Base rotation speed multiplier (degrees per second at max speed).")]
    public float baseSpeed = 300f;

    [Tooltip("Time it takes to complete one full 360° cycle (seconds).")]
    public float rotationDuration = 1.5f;

    [Tooltip("Rotate clockwise if true, counter-clockwise if false.")]
    public bool rotateClockwise = true;

    private RectTransform rectTransform;
    private float elapsedTime;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rectTransform == null)
            return;

        // Normalize time (0 → 1 → loop)
        elapsedTime += Time.deltaTime;
        float t = (elapsedTime % rotationDuration) / rotationDuration;

        // Smooth speed curve: ease in → fast mid → ease out
        // Using a sine curve for natural acceleration/deceleration
        float speedMultiplier = Mathf.Sin(t * Mathf.PI);  // goes 0→1→0 per rotation

        // Direction
        float direction = rotateClockwise ? -1f : 1f;

        // Apply rotation
        rectTransform.Rotate(0f, 0f, direction * baseSpeed * speedMultiplier * Time.deltaTime);
    }
}
