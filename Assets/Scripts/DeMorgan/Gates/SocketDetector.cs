//using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;
//using System.Collections;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;
//using UnityEngine.XR.Interaction.Toolkit.Interactors;

//public class SocketDetector : MonoBehaviour
//{
//    [Header("Socket Configuration")]
//    [SerializeField] private int expectedValue;           // Expected value for correct placement
//    [SerializeField] private GameObject indicatorObject;  // Game object to change material
//    [SerializeField] private Material correctMaterial;    // Material for correct placement
//    [SerializeField] private Material wrongMaterial;      // Material for wrong placement

//    [Header("Audio")]
//    [SerializeField] private AudioSource audioSource;
//    [SerializeField] private AudioClip correctVoiceover;
//    [SerializeField] private AudioClip wrongVoiceover;

//    [Header("Game Objects to Control")]
//    [SerializeField] private GameObject[] objectsToEnable;  // Objects to enable on correct placement
//    [SerializeField] private GameObject[] objectsToDisable; // Objects to disable on correct placement

//    private XRSocketInteractor socket;
//    private Renderer indicatorRenderer;
//    private bool isCorrectlyPlaced = false;

//    void Start()
//    {
//        socket = GetComponent<XRSocketInteractor>();
//        if (socket != null)
//        {
//            socket.selectEntered.AddListener(OnObjectPlaced);
//        }

//        if (indicatorObject != null)
//        {
//            indicatorRenderer = indicatorObject.GetComponent<Renderer>();
//        }
//    }

//    void OnObjectPlaced(SelectEnterEventArgs args)
//    {
//        var placedObject = args.interactableObject.transform.GetComponent<PlaceableObject>();
//        if (placedObject != null)
//        {
//            bool isCorrect = placedObject.value == expectedValue;

//            // Change indicator material
//            if (indicatorRenderer != null)
//            {
//                indicatorRenderer.material = isCorrect ? correctMaterial : wrongMaterial;
//            }

//            // Play voiceover
//            if (audioSource != null)
//            {
//                AudioClip clipToPlay = isCorrect ? correctVoiceover : wrongVoiceover;
//                if (clipToPlay != null)
//                {
//                    audioSource.PlayOneShot(clipToPlay);
//                }
//            }

//            // Release from socket
//            socket.interactionManager.SelectExit(socket, args.interactableObject);

//            if (isCorrect)
//            {
//                HandleCorrectPlacement(placedObject);
//            }
//            else
//            {
//                HandleWrongPlacement(placedObject);
//            }
//        }
//    }

//    private void HandleCorrectPlacement(PlaceableObject placedObject)
//    {
//        isCorrectlyPlaced = true;

//        // Disable grab interactable
//        var grabInteractable = placedObject.GetComponent<XRGrabInteractable>();
//        if (grabInteractable != null)
//        {
//            grabInteractable.enabled = false;
//        }

//        // Fix object position and physics
//        Transform objTransform = placedObject.transform;
//        objTransform.position = socket.attachTransform.position;
//        objTransform.rotation = Quaternion.identity;

//        Rigidbody rb = placedObject.GetComponent<Rigidbody>();
//        if (rb != null)
//        {
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;
//            rb.isKinematic = true;
//        }

//        // Enable specified objects
//        foreach (GameObject obj in objectsToEnable)
//        {
//            if (obj != null)
//            {
//                obj.SetActive(true);
//            }
//        }

//        // Disable specified objects
//        foreach (GameObject obj in objectsToDisable)
//        {
//            if (obj != null)
//            {
//                obj.SetActive(false);
//            }
//        }

//        // Disable socket after correct placement
//        socket.enabled = false;
//    }

//    private void HandleWrongPlacement(PlaceableObject placedObject)
//    {
//        // Return to initial position
//        StartCoroutine(ReturnToInitialPosition(placedObject));
//    }

//    private IEnumerator ReturnToInitialPosition(PlaceableObject placedObject)
//    {
//        // Wait a brief moment for audio/visual feedback
//        yield return new WaitForSeconds(0.5f);

//        // Move back to initial position
//        Transform objTransform = placedObject.transform;
//        Vector3 startPos = objTransform.position;
//        Vector3 endPos = placedObject.initialPosition;
//        float duration = 1.0f;
//        float time = 0f;

//        while (time < duration)
//        {
//            objTransform.position = Vector3.Lerp(startPos, endPos, time / duration);
//            time += Time.deltaTime;
//            yield return null;
//        }

//        objTransform.position = endPos;

//        // Reset material back to default if needed
//        if (indicatorRenderer != null && wrongMaterial != null)
//        {
//            // You might want to set back to a default material here
//            // indicatorRenderer.material = defaultMaterial;
//        }
//    }

//    public bool IsCorrectlyPlaced => isCorrectlyPlaced;
//}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketDetector : MonoBehaviour
{
    [Header("Core Settings")]
    public int expectedValue;

    [Header("GameObject Control")]
    public GameObject[] objectsToEnableOnCorrect;
    public GameObject[] objectsToDisableOnCorrect;
    public GameObject[] objectsToEnableOnWrong;
    public GameObject[] objectsToDisableOnWrong;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnCorrectPlacementEvent;
    public UnityEngine.Events.UnityEvent OnWrongPlacementEvent;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;

    void Start()
    {
        socketInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socketInteractor.selectEntered.AddListener(OnObjectPlaced);

        // If no audio source is assigned, try to get one from this GameObject
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void OnObjectPlaced(SelectEnterEventArgs args)
    {
        GameObject placedObject = args.interactableObject.transform.gameObject;
        PlaceableObject placeable = placedObject.GetComponent<PlaceableObject>();

        if (placeable != null && placeable.value == expectedValue)
        {
            HandleCorrectPlacement(placedObject);
        }
        else
        {
            HandleWrongPlacement(placedObject);
        }
    }

    void HandleCorrectPlacement(GameObject placedObject)
    {
        // Enable/Disable GameObjects for correct placement
        EnableGameObjects(objectsToEnableOnCorrect);
        DisableGameObjects(objectsToDisableOnCorrect);

        // Play correct sound
        PlayAudioClip(correctSound);

        // Invoke Unity Event for additional custom behavior
        OnCorrectPlacementEvent?.Invoke();

        Debug.Log($"Correct placement! Object {placedObject.name} placed in socket {gameObject.name}");
    }

    void HandleWrongPlacement(GameObject placedObject)
    {
        // Enable/Disable GameObjects for wrong placement
        EnableGameObjects(objectsToEnableOnWrong);
        DisableGameObjects(objectsToDisableOnWrong);

        // Play wrong sound
        PlayAudioClip(wrongSound);

        // Invoke Unity Event for additional custom behavior
        OnWrongPlacementEvent?.Invoke();

        Debug.Log($"Wrong placement! Object {placedObject.name} doesn't match expected value {expectedValue}");
    }

    void EnableGameObjects(GameObject[] objects)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    void DisableGameObjects(GameObject[] objects)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    void PlayAudioClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ResetSocket()
    {
        // Reset all objects to their default state if needed
        // You can customize this method based on your needs
        Debug.Log($"Socket {gameObject.name} reset");
    }

    void OnDestroy()
    {
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnObjectPlaced);
        }
    }
}