using UnityEngine;

namespace DeMorgansGame.MetaXR
{
    /// <summary>
    /// Represents an output block that can be snapped to output sockets.
    /// Stores the block's value and initial position for reset functionality.
    /// </summary>
    public class OutputBlock : MonoBehaviour
    {
        [Header("Block Properties")]
        [Tooltip("The output value this block represents (0 or 1)")]
        public int outputValue;

        [HideInInspector]
        public Vector3 initialPosition;

        [HideInInspector]
        public Quaternion initialRotation;

        void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            Debug.Log($"Output Block {outputValue} initial state saved: {initialPosition}");
        }
    }
}