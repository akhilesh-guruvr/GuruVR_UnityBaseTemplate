//using UnityEngine;
//using Oculus.Interaction;
//using System.Collections;

//namespace DeMorgansGame.MetaXR
//{
//    /// <summary>
//    /// Manages input block snapping with Meta XR SDK.
//    /// Validates correct input placement and handles incorrect placements.
//    /// </summary>
//    public class InputSocket : MonoBehaviour
//    {
//        [Header("Socket Configuration")]
//        [SerializeField]
//        private string expectedInput;

//        [SerializeField]
//        private int socketIndex;

//        [SerializeField]
//        private DeMorgansGameManager manager;

//        [Header("Audio")]
//        [SerializeField]
//        private AudioSource audioSource;

//        [SerializeField]
//        private AudioClip correctSound;

//        [SerializeField]
//        private AudioClip incorrectSound;

//        [Header("Meta XR Components")]
//        [SerializeField]
//        private InteractableUnityEventWrapper eventWrapper;

//        [SerializeField]
//        private SnapInteractable snapInteractable;

//        private bool isCompleted = false;
//        private InputBlock currentBlock;

//        void Start()
//        {
//            if (eventWrapper != null)
//            {
//                eventWrapper.WhenSelect.AddListener(OnBlockSnapped);
//                eventWrapper.WhenUnselect.AddListener(OnBlockRemoved);
//            }
//        }

//        /// <summary>
//        /// Called when a block is snapped to this socket via Meta XR snap interaction
//        /// </summary>
//        private void OnBlockSnapped()
//        {
//            if (isCompleted) return;

//            // Use helper method to get the interactor and snapped component
//            if (snapInteractable.TryGetFirstSelectingInteractor(out SnapInteractor interactor))
//            {
//                var inputBlock = interactor.GetComponent<InputBlock>();

//                if (inputBlock != null)
//                {
//                    currentBlock = inputBlock;
//                    ValidateAndHandlePlacement(inputBlock, interactor);
//                }
//            }
//        }

//        private void ValidateAndHandlePlacement(InputBlock inputBlock, SnapInteractor interactor)
//        {
//            bool isCorrect = inputBlock.inputValue == expectedInput && !inputBlock.isUsed;

//            if (isCorrect)
//            {
//                HandleCorrectPlacement(inputBlock, interactor);
//            }
//            else
//            {
//                HandleIncorrectPlacement(inputBlock, interactor);
//            }
//        }

//        private void HandleCorrectPlacement(InputBlock inputBlock, SnapInteractor interactor)
//        {
//            if (audioSource != null && correctSound != null)
//                audioSource.PlayOneShot(correctSound);

//            isCompleted = true;
//            inputBlock.isUsed = true;

//            // Disable the SnapInteractor on the block to prevent further interaction
//            interactor.enabled = false;

//            // Make the block kinematic
//            Rigidbody rb = inputBlock.GetComponent<Rigidbody>();
//            if (rb != null)
//            {
//                rb.isKinematic = true;
//            }

//            // Lock position to snap point
//            inputBlock.transform.SetPositionAndRotation(
//                snapInteractable.transform.position,
//                Quaternion.identity
//            );

//            // Disable this socket
//            snapInteractable.enabled = false;

//            // Notify manager
//            manager.OnInputCorrectlyPlaced(socketIndex, inputBlock.inputValue);
//        }

//        private void HandleIncorrectPlacement(InputBlock inputBlock, SnapInteractor interactor)
//        {
//            if (audioSource != null && incorrectSound != null)
//                audioSource.PlayOneShot(incorrectSound);

//            // Notify manager of incorrect attempt
//            manager.OnInputIncorrectlyPlaced(socketIndex, inputBlock.inputValue, expectedInput);

//            // Start return animation
//            StartCoroutine(ReturnBlockToStartPosition(inputBlock, interactor));
//        }

//        private IEnumerator ReturnBlockToStartPosition(InputBlock block, SnapInteractor interactor)
//        {
//            // Disable interaction during return
//            interactor.enabled = false;

//            // Brief pause for feedback
//            yield return new WaitForSeconds(0.5f);

//            // Animate back to start
//            Transform objTransform = block.transform;
//            Vector3 startPos = objTransform.position;
//            Vector3 endPos = block.initialPosition;
//            Quaternion startRot = objTransform.rotation;
//            Quaternion endRot = block.initialRotation;

//            float duration = 1.0f;
//            float time = 0f;

//            while (time < duration)
//            {
//                float t = time / duration;
//                objTransform.position = Vector3.Lerp(startPos, endPos, t);
//                objTransform.rotation = Quaternion.Lerp(startRot, endRot, t);
//                time += Time.deltaTime;
//                yield return null;
//            }

//            // Snap to final position
//            objTransform.position = endPos;
//            objTransform.rotation = endRot;

//            // Re-enable interaction
//            interactor.enabled = true;

//            currentBlock = null;
//        }

//        private void OnBlockRemoved()
//        {
//            if (!isCompleted && currentBlock != null)
//            {
//                // Handle premature removal if needed
//                currentBlock = null;
//            }
//        }

//        public void SetSocketEnabled(bool enabled)
//        {
//            if (snapInteractable != null)
//            {
//                snapInteractable.enabled = enabled;
//            }
//        }

//        private void OnDestroy()
//        {
//            if (eventWrapper != null)
//            {
//                eventWrapper.WhenSelect.RemoveListener(OnBlockSnapped);
//                eventWrapper.WhenUnselect.RemoveListener(OnBlockRemoved);
//            }
//        }
//    }
//}