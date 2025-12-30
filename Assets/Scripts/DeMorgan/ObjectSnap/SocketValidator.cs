//using UnityEngine;
//using System.Collections;
//using Oculus.Interaction;
//using UnityEngine.Events;

//public class SocketValidator : MonoBehaviour
//{
//    [Header("References")]
//    [Tooltip("Drag the SnapInteractable component from this object here.")]
//    [SerializeField] private SnapInteractable _snapInteractable;

//    [Tooltip("Optional: Drag the Global Game Manager here.")]
//    [SerializeField] private InteractionSequenceManager _sequenceManager;

//    [Header("Validation")]
//    [Tooltip("The ID required for this socket. Can be changed dynamically via SetRequiredID().")]
//    [SerializeField] private string _requiredID;

//    [Header("Events")]
//    public UnityEvent OnValidSnap;
//    public UnityEvent OnInvalidSnap;
//    public UnityEvent OnObjectRemoved;

//    // --- SETUP ---
//    private void Reset() => _snapInteractable = GetComponent<SnapInteractable>();

//    private void OnEnable()
//    {
//        if (_snapInteractable)
//        {
//            _snapInteractable.WhenSelectingInteractorViewAdded += ValidateSnap;
//            _snapInteractable.WhenSelectingInteractorViewRemoved += HandleRemoval;
//        }
//    }

//    private void OnDisable()
//    {
//        if (_snapInteractable)
//        {
//            _snapInteractable.WhenSelectingInteractorViewAdded -= ValidateSnap;
//            _snapInteractable.WhenSelectingInteractorViewRemoved -= HandleRemoval;
//        }
//    }

//    // --- PUBLIC API ---

//    /// <summary>
//    /// Updates the Required ID for this socket dynamically.
//    /// Call this from UnityEvents (e.g., Button OnClick) and type the new ID in the Inspector.
//    /// </summary>
//    /// <param name="newID">The new string ID to validate against (e.g., "BlueKey").</param>
//    public void SetRequiredID(string newID)
//    {
//        _requiredID = newID;
//        Debug.Log($"[SocketValidator] Required ID updated to: {_requiredID}");
//    }

//    // --- CORE LOGIC ---

//    private void ValidateSnap(IInteractorView interactorView)
//    {
//        // 1. Get Interactor
//        SnapInteractor interactor = interactorView as SnapInteractor;
//        if (interactor == null) return;

//        // 2. Get Identity
//        ObjectIdentity identity = interactor.GetComponent<ObjectIdentity>();
//        if (identity == null) return;

//        // 3. Logic Check
//        if (identity.ID == _requiredID)
//        {
//            // --- CORRECT ---
//            Debug.Log($"[SocketValidator] Valid Snap: {identity.ID}");
//            OnValidSnap.Invoke();
//            if (_sequenceManager) _sequenceManager.RegisterCompletion(this);
//        }
//        else
//        {
//            // --- WRONG ---
//            Debug.Log($"[SocketValidator] Wrong ID: {identity.ID}. Expected: {_requiredID}. Rejecting...");
//            OnInvalidSnap.Invoke();

//            // Start Rejection Routine (Force Drop)
//            StartCoroutine(RejectObject(interactor));
//        }
//    }

//    private IEnumerator RejectObject(SnapInteractor interactor)
//    {
//        // 1. Force the SDK to let go
//        interactor.Unselect();

//        // 2. DISABLE the component immediately.
//        // This turns off the "magnet" so it cannot snap back instantly.
//        interactor.enabled = false;

//        // 3. Wait for a Cooldown (1 second)
//        // This gives the object time to fall/move away from the socket trigger.
//        yield return new WaitForSeconds(1.0f);

//        // 4. Re-enable
//        // Now the user can try to place it in a DIFFERENT socket.
//        if (interactor != null)
//        {
//            interactor.enabled = true;
//        }
//    }

//    private void HandleRemoval(IInteractorView interactorView)
//    {
//        OnObjectRemoved.Invoke();
//        if (_sequenceManager) _sequenceManager.DeregisterCompletion(this);
//    }
//}

using UnityEngine;
using System.Collections;
using Oculus.Interaction;
using UnityEngine.Events;

public class SocketValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SnapInteractable _snapInteractable;

    [SerializeField] private InteractionSequenceManager _sequenceManager;

    [Header("Validation")]
    [SerializeField] private string _requiredID;

    [Header("Events")]
    public UnityEvent OnValidSnap;
    public UnityEvent OnInvalidSnap;
    public UnityEvent OnObjectRemoved;


    private void Reset()
    {
        _snapInteractable = GetComponent<SnapInteractable>();
    }

    private void OnEnable()
    {
        if (_snapInteractable)
        {
            Debug.Log($"[SocketValidator {name}] ENABLED - Registering Snap Events");
            _snapInteractable.WhenSelectingInteractorViewAdded += ValidateSnap;
            _snapInteractable.WhenSelectingInteractorViewRemoved += HandleRemoval;
        }
        else
        {
            Debug.LogError($"[SocketValidator {name}] No SnapInteractable assigned!");
        }

        if (_sequenceManager == null)
        {
            Debug.LogWarning($"[SocketValidator {name}] No SequenceManager assigned!");
        }
        else
        {
            Debug.Log($"[SocketValidator {name}] Connected to SequenceManager: {_sequenceManager.name}");
        }
    }

    private void OnDisable()
    {
        if (_snapInteractable)
        {
            Debug.Log($"[SocketValidator {name}] DISABLED - Unregistering Snap Events");
            _snapInteractable.WhenSelectingInteractorViewAdded -= ValidateSnap;
            _snapInteractable.WhenSelectingInteractorViewRemoved -= HandleRemoval;
        }
    }


    public void SetRequiredID(string newID)
    {
        _requiredID = newID;
        Debug.Log($"[SocketValidator {name}] Required ID updated to: {_requiredID}");
    }


    private void ValidateSnap(IInteractorView interactorView)
    {
        Debug.Log($"[SocketValidator {name}] ValidateSnap TRIGGERED. SequenceManager = {_sequenceManager?.name}");

        SnapInteractor interactor = interactorView as SnapInteractor;
        if (interactor == null)
        {
            Debug.LogError($"[SocketValidator {name}] ERROR: interactorView is not SnapInteractor");
            return;
        }

        ObjectIdentity identity = interactor.GetComponent<ObjectIdentity>();
        if (identity == null)
        {
            Debug.LogError($"[SocketValidator {name}] ERROR: Object has NO ObjectIdentity");
            return;
        }

        Debug.Log($"[SocketValidator {name}] Incoming ID = {identity.ID}, Required = {_requiredID}");

        if (identity.ID == _requiredID)
        {
            Debug.Log($"[SocketValidator {name}] VALID SNAP → Calling RegisterCompletion on {_sequenceManager?.name}");

            OnValidSnap.Invoke();

            if (_sequenceManager == null)
            {
                Debug.LogError($"[SocketValidator {name}] ERROR: No SequenceManager assigned!");
            }
            else
            {
                _sequenceManager.RegisterCompletion(this);
            }
        }
        else
        {
            Debug.LogWarning($"[SocketValidator {name}] INVALID SNAP → ID {identity.ID} does not match {_requiredID}");

            OnInvalidSnap.Invoke();
            StartCoroutine(RejectObject(interactor));
        }
    }

    private IEnumerator RejectObject(SnapInteractor interactor)
    {
        Debug.Log($"[SocketValidator {name}] Rejecting Object...");

        interactor.Unselect();
        interactor.enabled = false;

        yield return new WaitForSeconds(1.0f);

        if (interactor != null)
        {
            interactor.enabled = true;
            Debug.Log($"[SocketValidator {name}] Reject Complete - Interactor Re-enabled");
        }
    }


    private void HandleRemoval(IInteractorView interactorView)
    {
        Debug.Log($"[SocketValidator {name}] Object Removed!");

        OnObjectRemoved.Invoke();

        if (_sequenceManager)
        {
            Debug.Log($"[SocketValidator {name}] Informing SequenceManager of removal.");
            _sequenceManager.DeregisterCompletion(this);
        }
    }

    public void AssignSequenceManager(InteractionSequenceManager newManager)
    {
        _sequenceManager = newManager;

        if (newManager == null)
        {
            Debug.LogWarning($"[SocketValidator {name}] SequenceManager has been cleared.");
        }
        else
        {
            Debug.Log($"[SocketValidator {name}] SequenceManager assigned: {newManager.name}");
        }
    }

}
