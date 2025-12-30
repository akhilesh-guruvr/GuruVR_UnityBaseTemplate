//using UnityEngine;

//public class MenuAnchorInstantiatorAtStart : MonoBehaviour
//{
//    [Header("Setup")]
//    [Tooltip("The Menu Prefab to instantiate (make sure it's a World Space Canvas).")]
//    public GameObject menuPrefab;

//    [Tooltip("Drag the OVRCameraRig from your scene here.")]
//    public OVRCameraRig ovrCameraRig;

//    [Header("Placement")]
//    [Tooltip("Choose the anchor to spawn the object at.")]
//    public InputSource targetSource = InputSource.RightHand;

//    [Tooltip("Local offset from the anchor (e.g., 0, 0.1, 0.3 for in front of the hand).")]
//    public Vector3 localOffset = new Vector3(0f, 0.1f, 0.3f);

//    [Tooltip("If checked, the instantiated menu will follow the hand/controller.")]
//    public bool parentToAnchor = true;

//    // Custom enum for cleaner selection in the Inspector
//    public enum InputSource { LeftHand, RightHand }

//    private GameObject currentMenuInstance;

//    void Start()
//    {
//        // 1. Basic validation
//        if (ovrCameraRig == null || menuPrefab == null)
//        {
//            Debug.LogError("OVRCameraRig or Menu Prefab is not assigned! Cannot instantiate menu.");
//            enabled = false;
//            return;
//        }

//        // 2. Determine the target anchor Transform
//        Transform targetAnchor = GetTargetAnchor();

//        if (targetAnchor == null)
//        {
//            Debug.LogError("Target Anchor not found for the selected source.");
//            enabled = false;
//            return;
//        }

//        // 3. Calculate position and rotation
//        // TransformPoint converts the local offset from the anchor's space into world space.
//        Vector3 spawnPosition = targetAnchor.TransformPoint(localOffset);
//        Quaternion spawnRotation = targetAnchor.rotation;

//        // --- Instantiate the menu ---
//        currentMenuInstance = Instantiate(
//            menuPrefab,
//            spawnPosition,
//            spawnRotation
//        );

//        // 4. Optional: Parent the menu to the anchor
//        if (parentToAnchor)
//        {
//            // SetParent(targetAnchor, true) means keep the current world position/rotation 
//            // after changing the parent (though we just spawned it, so it's fine).
//            currentMenuInstance.transform.SetParent(targetAnchor, true);
//        }

//        Debug.Log($"Menu instantiated and parented to {targetAnchor.name} upon scene start.");

//        // Ensure the menu is active
//        currentMenuInstance.SetActive(true);
//    }

//    private Transform GetTargetAnchor()
//    {
//        // Access the hand anchor properties of the OVRCameraRig
//        return targetSource == InputSource.RightHand
//            ? ovrCameraRig.rightHandAnchor
//            : ovrCameraRig.leftHandAnchor;
//    }
//}

using UnityEngine;
using UnityEngine.UI; // Needed to access the Button component

public class MenuAnchorInstantiatorAtStart : MonoBehaviour
{
    [Header("Setup")]
    public GameObject menuPrefab;
    public OVRCameraRig ovrCameraRig;

    [Header("Target A Toggling (Button 1)")]
    [Tooltip("The first GameObject to toggle visibility for and reposition.")]
    public GameObject targetObjectA;
    [Tooltip("Local offset from the HMD (centerEyeAnchor) for Target A.")]
    public Vector3 hmdOffsetA = new Vector3(0f, 0f, 1f);

    [Header("Target B Toggling (Button 2)")]
    [Tooltip("The second GameObject to toggle visibility for and reposition.")]
    public GameObject targetObjectB;
    [Tooltip("Local offset from the HMD (centerEyeAnchor) for Target B.")]
    public Vector3 hmdOffsetB = new Vector3(0.5f, 0f, 1f); // Slightly offset from A for distinction

    [Header("Placement (for Menu Prefab)")]
    public InputSource targetSource = InputSource.RightHand;
    public Vector3 localOffset = new Vector3(0f, 0.1f, 0.3f);
    public bool parentToAnchor = true;

    public enum InputSource { LeftHand, RightHand }

    private GameObject currentMenuInstance;

    void Start()
    {
        // 1. Validation
        if (ovrCameraRig == null || menuPrefab == null || targetObjectA == null || targetObjectB == null)
        {
            Debug.LogError("Required references (OVRCameraRig, Prefab, Target A, or Target B) are missing!");
            enabled = false;
            return;
        }

        // 2. Determine the target anchor Transform for the menu
        Transform targetAnchor = GetTargetAnchor();
        if (targetAnchor == null) { /* Error handling... */ }

        // 3. Instantiate the menu
        Vector3 spawnPosition = targetAnchor.TransformPoint(localOffset);
        currentMenuInstance = Instantiate(menuPrefab, spawnPosition, targetAnchor.rotation);

        // 4. Optional: Parent the menu
        if (parentToAnchor)
        {
            currentMenuInstance.transform.SetParent(targetAnchor, true);
        }

        currentMenuInstance.SetActive(true);

        // 5. Link the buttons to the separate toggle methods
        SetupMenuButtons();
    }

    private void SetupMenuButtons()
    {
        // ASSUMPTION: The buttons are the first two Button components found in the prefab hierarchy.
        Button[] buttons = currentMenuInstance.GetComponentsInChildren<Button>(true);

        if (buttons.Length >= 2)
        {
            // Button 1 links to Target A logic
            buttons[0].onClick.AddListener(() => ToggleTargetObjectA());

            // Button 2 links to Target B logic
            buttons[1].onClick.AddListener(() => ToggleTargetObjectB());

            Debug.Log($"Button 1 linked to {targetObjectA.name}, Button 2 linked to {targetObjectB.name}.");
        }
        else
        {
            Debug.LogWarning("Could not find at least two buttons on the instantiated menu prefab!");
        }
    }

    private Transform GetTargetAnchor()
    {
        return targetSource == InputSource.RightHand
            ? ovrCameraRig.rightHandAnchor
            : ovrCameraRig.leftHandAnchor;
    }

    // --- Toggle Functions ---

    /// <summary>
    /// Toggles the visibility of Target A and repositions it relative to the HMD.
    /// </summary>
    public void ToggleTargetObjectA()
    {
        ToggleAndReposition(targetObjectA, hmdOffsetA);
    }

    /// <summary>
    /// Toggles the visibility of Target B and repositions it relative to the HMD.
    /// </summary>
    public void ToggleTargetObjectB()
    {
        ToggleAndReposition(targetObjectB, hmdOffsetB);
    }

    // --- Core Reposition Logic (Reused by both toggles) ---

    private void ToggleAndReposition(GameObject target, Vector3 offset)
    {
        bool newState = !target.activeSelf;
        target.SetActive(newState);

        if (newState)
        {
            // Get the current HMD/Center position
            Transform hmdAnchor = ovrCameraRig.centerEyeAnchor;

            // Calculate new World Position using the HMD's position and the specified offset
            Vector3 newPosition = hmdAnchor.TransformPoint(offset);

            // Set the rotation to match the HMD's rotation (so it faces the user)
            Quaternion newRotation = hmdAnchor.rotation;

            // Apply the new transform
            target.transform.SetPositionAndRotation(newPosition, newRotation);

            Debug.Log($"{target.name} activated and placed at new HMD position.");
        }
        else
        {
            Debug.Log($"{target.name} deactivated.");
        }
    }
}