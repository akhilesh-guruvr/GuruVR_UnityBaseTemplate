using UnityEngine;

namespace DeMorgansGame.MetaXR
{
    /// <summary>
    /// Represents an input block that can be snapped to input sockets.
    /// Stores the block's value and initial position for reset functionality.
    /// </summary>
    public class InputBlock : MonoBehaviour
    {
        [Header("Block Properties")]
        [Tooltip("The input value this block represents (00, 01, 10, 11)")]
        public string inputValue;

        [HideInInspector]
        public Vector3 initialPosition;

        [HideInInspector]
        public Quaternion initialRotation;

        [HideInInspector]
        public bool isUsed = false;

        void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            Debug.Log($"Input Block {inputValue} initial state saved: {initialPosition}");
        }

        /// <summary>
        /// Resets the block to its initial state
        /// </summary>
        public void ResetToInitialState()
        {
            isUsed = false;
        }
    }
}