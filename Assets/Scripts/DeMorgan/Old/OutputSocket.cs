//using UnityEngine;
//using Oculus.Interaction;
//using System.Collections;

//namespace DeMorgansGame.MetaXR
//{
//    /// <summary>
//    /// Manages output block snapping with Meta XR SDK.
//    /// Provides visual feedback and handles validation through the game manager.
//    /// </summary>
//    public class OutputSocket : MonoBehaviour
//    {
//        [Header("Socket Configuration")]
//        [SerializeField]
//        private int socketIndex;

//        [SerializeField]
//        private DeMorgansGameManager manager;

//        [Header("Visual Feedback")]
//        [SerializeField]
//        private GameObject feedbackObject;

//        [SerializeField]
//        public Material correctMaterial;

//        [SerializeField]
//        public Material wrongMaterial;

//        [SerializeField]
//        private Material defaultMaterial;

//        [Header("Block Prefabs for Reinstantiation")]
//        [SerializeField]
//        private GameObject[] outputBlockPrefabs;

//        [Header("Meta XR Components")]
//        [SerializeField]
//        private InteractableUnityEventWrapper eventWrapper;

//        [SerializeField]
//        private SnapInteractable snapInteractable;

//        private Renderer feedbackRenderer;
//        private OutputBlock currentBlock;

//        void Start()
//        {
//            if (eventWrapper != null)
//            {
//                eventWrapper.WhenSelect.AddListener(OnBlockSnapped);
//                eventWrapper.WhenUnselect.AddListener(OnBlockRemoved);
//            }

//            feedbackRenderer = feedbackObject?.GetComponent<Renderer>();
//            SetFeedbackMaterial(defaultMaterial);
//        }

//        private void OnBlockSnapped()
//        {
//            // Use helper method to get the interactor and component
//            if (snapInteractable.TryGetFirstSelectingInteractor(out SnapInteractor interactor))
//            {
//                var outputBlock = interactor.GetComponent<OutputBlock>();

//                if (outputBlock != null)
//                {
//                    currentBlock = outputBlock;
//                    manager.OnOutputBlockPlaced(socketIndex, outputBlock.outputValue);
//                }
//            }
//        }

//        private void OnBlockRemoved()
//        {
//            SetFeedbackMaterial(defaultMaterial);
//            currentBlock = null;
//            manager.OnOutputBlockRemoved(socketIndex);
//        }

//        /// <summary>
//        /// Returns the current block to its starting position (for incorrect placements)
//        /// </summary>
//        public void ReturnBlockToStart()
//        {
//            if (currentBlock != null && snapInteractable.TryGetFirstSelectingInteractor(out SnapInteractor interactor))
//            {
//                StartCoroutine(ReturnToInitialPositionCoroutine(currentBlock, interactor));
//            }

//            ResetFeedback();
//        }

//        private IEnumerator ReturnToInitialPositionCoroutine(OutputBlock block, SnapInteractor interactor)
//        {
//            // Disable interaction
//            interactor.enabled = false;

//            yield return new WaitForSeconds(0.1f);

//            Transform objTransform = block.transform;
//            Vector3 startPos = objTransform.position;
//            Vector3 endPos = block.initialPosition;
//            Quaternion startRot = objTransform.rotation;
//            Quaternion endRot = block.initialRotation;

//            float duration = 0.75f;
//            float time = 0f;

//            while (time < duration)
//            {
//                float t = time / duration;
//                objTransform.position = Vector3.Lerp(startPos, endPos, t);
//                objTransform.rotation = Quaternion.Lerp(startRot, endRot, t);
//                time += Time.deltaTime;
//                yield return null;
//            }

//            objTransform.position = endPos;
//            objTransform.rotation = endRot;

//            // Re-enable interaction
//            interactor.enabled = true;
//            currentBlock = null;
//        }

//        /// <summary>
//        /// Destroys current block and creates a new one at the original position
//        /// </summary>
//        public void RemoveAndReinstantiateBlock()
//        {
//            if (currentBlock != null)
//            {
//                int blockValue = currentBlock.outputValue;
//                Vector3 initialPosition = currentBlock.initialPosition;
//                Quaternion initialRotation = currentBlock.initialRotation;

//                // Destroy current block
//                Destroy(currentBlock.gameObject);
//                currentBlock = null;

//                // Create new block
//                if (blockValue < outputBlockPrefabs.Length && outputBlockPrefabs[blockValue] != null)
//                {
//                    GameObject newBlock = Instantiate(
//                        outputBlockPrefabs[blockValue],
//                        initialPosition,
//                        initialRotation
//                    );

//                    // Register with manager for cleanup
//                    manager.RegisterClone(newBlock);

//                    // Ensure SnapInteractor is enabled
//                    var interactor = newBlock.GetComponent<SnapInteractor>();
//                    if (interactor != null)
//                        interactor.enabled = true;
//                }
//            }

//            ResetFeedback();
//        }

//        public bool HasSelection()
//        {
//            return currentBlock != null;
//        }

//        public GameObject GetSelectedObject()
//        {
//            return currentBlock?.gameObject;
//        }

//        public void SetFeedbackMaterial(Material material)
//        {
//            if (feedbackRenderer != null && material != null)
//            {
//                feedbackRenderer.material = material;
//            }
//        }

//        public void ResetFeedback()
//        {
//            SetFeedbackMaterial(defaultMaterial);
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