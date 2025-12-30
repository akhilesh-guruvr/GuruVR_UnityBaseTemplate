//using UnityEngine;
//using Oculus.Interaction;

//public class ObjectIdentity : MonoBehaviour
//{
//    [Header("Identity Configuration")]
//    [Tooltip("The unique identifier string for this object (e.g., 'Key_01', 'Gear_A').")]
//    [SerializeField] private string _id;

//    private Vector3 _initialPosition;
//    private Quaternion _initialRotation;
//    private SnapInteractor _associatedSnapInteractor;

//    public string ID => _id;

//    private void Awake()
//    {
//        _initialPosition = transform.position;
//        _initialRotation = transform.rotation;
//        _associatedSnapInteractor = GetComponent<SnapInteractor>();
//    }

//    /// <summary>
//    /// Resets the object to its initial spawn location.
//    /// </summary>
//    public void ResetToInitialState()
//    {
//        // Temporarily disable interactor to break physical/magnetic bonds
//        if (_associatedSnapInteractor != null)
//            _associatedSnapInteractor.enabled = false;

//        transform.position = _initialPosition;
//        transform.rotation = _initialRotation;

//        if (_associatedSnapInteractor != null)
//        {
//            // Re-enable in next frame to allow physics to settle
//            StartCoroutine(ReEnableInteractorRoutine());
//        }
//    }

//    private System.Collections.IEnumerator ReEnableInteractorRoutine()
//    {
//        yield return new WaitForSeconds(0.1f);
//        if (_associatedSnapInteractor != null)
//            _associatedSnapInteractor.enabled = true;
//    }
//}

using UnityEngine;

public class ObjectIdentity : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID for this item (e.g., 'RedCube', 'Key_A').")]
    public string ID;
}