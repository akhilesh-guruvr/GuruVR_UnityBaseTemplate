//using UnityEngine;

//public class LerpToTarget : MonoBehaviour
//{
//    // Optional target GameObject to move
//    public GameObject targetObject;

//    // The position to lerp towards
//    public Vector3 targetPosition;

//    // Speed of the lerp
//    public float speed = 1.0f;

//    private float timeElapsed = 0.0f;
//    private Vector3 startPosition;
//    private bool isLerping = false;
//    private Transform lerpTransform;

//    void Start()
//    {
//        // Determine which object to move
//        lerpTransform = targetObject != null ? targetObject.transform : transform;
//        startPosition = lerpTransform.position;
//    }

//    void Update()
//    {
//        if (!isLerping) return;

//        timeElapsed += Time.deltaTime * speed;

//        lerpTransform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed);

//        if (Vector3.Distance(lerpTransform.position, targetPosition) < 0.01f)
//        {
//            lerpTransform.position = targetPosition;
//            isLerping = false;
//        }
//    }

//    // ------------------------------
//    // FUNCTIONS TO CALL FROM INSPECTOR
//    // ------------------------------

//    /// <summary>
//    /// Updates the target position (callable from Inspector).
//    /// </summary>
//    [ContextMenu("Update Target Position")]
//    public void UpdateTargetPosition()
//    {
//        // Reset start pos so a new lerp can start correctly
//        startPosition = lerpTransform.position;
//        timeElapsed = 0f;
//        Debug.Log("Target position updated to: " + targetPosition);
//    }

//    /// <summary>
//    /// Starts the lerp movement (callable from Inspector).
//    /// </summary>
//    [ContextMenu("Start Lerp")]
//    public void StartLerp()
//    {
//        startPosition = lerpTransform.position;
//        timeElapsed = 0f;
//        isLerping = true;
//        Debug.Log("Lerp started toward: " + targetPosition);
//    }

//    /// <summary>
//    /// Updates target position & starts lerp in one click.
//    /// </summary>
//    [ContextMenu("Update Position And Start Lerp")]
//    public void UpdateAndStart()
//    {
//        UpdateTargetPosition();
//        StartLerp();
//    }
//}

using UnityEngine;
using System.Collections;

public class LerpToTarget : MonoBehaviour
{
    public GameObject targetObject;

    public Vector3 targetPosition;
    public float speed = 1.0f;

    [Header("Delay before lerping starts")]
    public float startDelay = 0f;

    private float timeElapsed = 0.0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion targetRotation = Quaternion.Euler(0, 0, 0);

    private bool isLerping = false;
    private bool isWaiting = false;

    private Transform lerpTransform;

    void Start()
    {
        lerpTransform = targetObject != null ? targetObject.transform : transform;
    }

    void Update()
    {
        if (!isLerping || isWaiting) return;

        timeElapsed += Time.deltaTime * speed;

        // Lerp movement
        lerpTransform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed);

        // Lerp rotation to zero
        lerpTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed);

        // Stop when close enough
        if (Vector3.Distance(lerpTransform.position, targetPosition) < 0.01f &&
            Quaternion.Angle(lerpTransform.rotation, targetRotation) < 0.5f)
        {
            lerpTransform.position = targetPosition;
            lerpTransform.rotation = targetRotation;
            isLerping = false;
        }
    }

    // ------------------------------
    // INSPECTOR FUNCTIONS
    // ------------------------------

    [ContextMenu("Update Target Position")]
    public void UpdateTargetPosition()
    {
        startPosition = lerpTransform.position;
        startRotation = lerpTransform.rotation;
        timeElapsed = 0f;
    }

    [ContextMenu("Start Lerp")]
    public void StartLerp()
    {
        StartCoroutine(StartLerpWithDelay());
    }

    [ContextMenu("Update Position And Start Lerp")]
    public void UpdateAndStart()
    {
        UpdateTargetPosition();
        StartLerp();
    }

    // ------------------------------
    // DELAY HANDLER
    // ------------------------------

    private IEnumerator StartLerpWithDelay()
    {
        // Set initial values
        startPosition = lerpTransform.position;
        startRotation = lerpTransform.rotation;
        timeElapsed = 5f;

        // Apply delay if needed
        if (startDelay > 0f)
        {
            isWaiting = true;
            yield return new WaitForSeconds(startDelay);
            isWaiting = false;
        }

        // Begin lerp
        isLerping = true;
    }
}
