//using UnityEngine;
//using UnityEngine.Events;
//using System.Collections.Generic;
//using System.Linq;
//using Oculus.Interaction;

//namespace DeMorgansGame.MetaXR
//{
//    [System.Serializable]
//    public class TruthTableEntry
//    {
//        public string input;
//        public int[] outputs = new int[2];
//    }

//    // ========== ENHANCED EVENT CLASSES ==========

//    [System.Serializable]
//    public class InputPlacementEvent : UnityEvent<int, string> { }

//    [System.Serializable]
//    public class InputIncorrectEvent : UnityEvent<int, string, string> { }

//    [System.Serializable]
//    public class OutputPlacementEvent : UnityEvent<int, int> { }

//    [System.Serializable]
//    public class OutputPairEvent : UnityEvent<int, int, bool> { }

//    [System.Serializable]
//    public class RowCompleteEvent : UnityEvent<int, string> { }

//    /// <summary>
//    /// Main game manager handling truth table validation and game flow.
//    /// Provides granular events for all game actions.
//    /// </summary>
//    public class DeMorgansGameManager : MonoBehaviour
//    {
//        [Header("Socket References")]
//        public InputSocket[] inputSockets;
//        public OutputSocket[] outputSockets;

//        [Header("Block References")]
//        public InputBlock[] inputBlocks;
//        public OutputBlock[] outputBlocks;

//        [Header("Success Objects")]
//        public GameObject[] successObjects;

//        [Header("Audio")]
//        public AudioSource audioSource;
//        public AudioClip correctSound;
//        public AudioClip wrongSound;

//        [Header("Truth Table")]
//        public List<TruthTableEntry> truthTableEntries;

//        [Header("Completion")]
//        public GameObject allCompletedObject;

//        // ========== ENHANCED EVENTS ==========

//        [Header("Input Events")]
//        [Tooltip("Fired when an input is correctly placed (socketIndex, inputValue)")]
//        public InputPlacementEvent OnInputCorrectEvent;

//        [Tooltip("Fired when an input is incorrectly placed (socketIndex, attemptedValue, expectedValue)")]
//        public InputIncorrectEvent OnInputIncorrectEvent;

//        [Header("Output Events")]
//        [Tooltip("Fired when an output block is placed (socketIndex, outputValue)")]
//        public OutputPlacementEvent OnOutputPlacedEvent;

//        [Tooltip("Fired when an output block is removed (socketIndex)")]
//        public UnityEvent<int> OnOutputRemovedEvent;

//        [Tooltip("Fired when both outputs are placed (socket0Value, socket1Value, bothCorrect)")]
//        public OutputPairEvent OnOutputPairCompleteEvent;

//        [Tooltip("Fired for each individual output validation (socketIndex, value, isCorrect)")]
//        public OutputPairEvent OnIndividualOutputValidatedEvent;

//        [Header("Row Completion Events")]
//        [Tooltip("Fired when a truth table row is completed correctly (rowIndex, inputValue)")]
//        public RowCompleteEvent OnRowCompletedEvent;

//        [Tooltip("Fired when a truth table row validation fails")]
//        public UnityEvent OnRowFailedEvent;

//        [Header("Game Completion Events")]
//        [Tooltip("Fired when all truth table rows are completed")]
//        public UnityEvent OnAllRowsCompletedEvent;

//        // ========== INTERNAL STATE ==========

//        private Dictionary<string, int[]> truthTable = new Dictionary<string, int[]>();
//        private Dictionary<int, int> placedOutputs = new Dictionary<int, int>();
//        private string currentInput = "";
//        private int currentInputIndex = -1;
//        private bool[] completedInputs = new bool[4];
//        private List<GameObject> allClonedBlocks = new List<GameObject>();

//        private void Awake()
//        {
//            InitializeTruthTable();
//            InitializeGameState();
//        }

//        private void InitializeTruthTable()
//        {
//            truthTable.Clear();
//            foreach (var entry in truthTableEntries)
//            {
//                if (!string.IsNullOrEmpty(entry.input) &&
//                    entry.outputs != null &&
//                    entry.outputs.Length == 2)
//                {
//                    if (!truthTable.ContainsKey(entry.input))
//                    {
//                        truthTable.Add(entry.input, entry.outputs);
//                    }
//                }
//            }
//        }

//        private void InitializeGameState()
//        {
//            foreach (var socket in outputSockets)
//            {
//                socket.SetSocketEnabled(false);
//            }

//            if (allCompletedObject != null)
//            {
//                allCompletedObject.SetActive(false);
//            }
//        }

//        // ========== INPUT HANDLING ==========

//        public void OnInputCorrectlyPlaced(int socketIndex, string inputValue)
//        {
//            currentInput = inputValue;
//            currentInputIndex = socketIndex;

//            Debug.Log($"Input {inputValue} correctly placed in socket {socketIndex}");

//            // Fire event
//            OnInputCorrectEvent?.Invoke(socketIndex, inputValue);

//            SetInputSystemEnabled(false, socketIndex);

//            foreach (var socket in outputSockets)
//            {
//                socket.SetSocketEnabled(true);
//                socket.ResetFeedback();
//            }
//        }

//        public void OnInputIncorrectlyPlaced(int socketIndex, string attemptedValue, string expectedValue)
//        {
//            Debug.Log($"Input socket {socketIndex}: Incorrect! Attempted={attemptedValue}, Expected={expectedValue}");

//            // Fire event
//            OnInputIncorrectEvent?.Invoke(socketIndex, attemptedValue, expectedValue);
//        }

//        // ========== OUTPUT HANDLING ==========

//        public void OnOutputBlockPlaced(int socketIndex, int value)
//        {
//            placedOutputs[socketIndex] = value;

//            Debug.Log($"Output block {value} placed in socket {socketIndex}");

//            // Fire event
//            OnOutputPlacedEvent?.Invoke(socketIndex, value);

//            CheckCompletion();
//        }

//        public void OnOutputBlockRemoved(int socketIndex)
//        {
//            if (placedOutputs.ContainsKey(socketIndex))
//            {
//                placedOutputs.Remove(socketIndex);

//                // Fire event
//                OnOutputRemovedEvent?.Invoke(socketIndex);
//            }
//        }

//        // ========== VALIDATION ==========

//        private void CheckCompletion()
//        {
//            if (placedOutputs.Count != 2 || string.IsNullOrEmpty(currentInput))
//            {
//                return;
//            }

//            if (truthTable.ContainsKey(currentInput))
//            {
//                int[] expectedOutputs = truthTable[currentInput];

//                bool output0Correct = placedOutputs.ContainsKey(0) &&
//                                     placedOutputs[0] == expectedOutputs[0];
//                bool output1Correct = placedOutputs.ContainsKey(1) &&
//                                     placedOutputs[1] == expectedOutputs[1];

//                // Fire individual validation events
//                OnIndividualOutputValidatedEvent?.Invoke(0, placedOutputs[0], output0Correct);
//                OnIndividualOutputValidatedEvent?.Invoke(1, placedOutputs[1], output1Correct);

//                // Update visual feedback
//                outputSockets[0].SetFeedbackMaterial(
//                    output0Correct ? outputSockets[0].correctMaterial : outputSockets[0].wrongMaterial
//                );
//                outputSockets[1].SetFeedbackMaterial(
//                    output1Correct ? outputSockets[1].correctMaterial : outputSockets[1].wrongMaterial
//                );

//                bool bothCorrect = output0Correct && output1Correct;

//                // Fire pair complete event
//                OnOutputPairCompleteEvent?.Invoke(
//                    placedOutputs[0],
//                    placedOutputs[1],
//                    bothCorrect
//                );

//                if (bothCorrect)
//                {
//                    HandleCorrectCompletion();
//                }
//                else
//                {
//                    HandleIncorrectCompletion();
//                }
//            }
//        }

//        private void HandleCorrectCompletion()
//        {
//            if (audioSource != null && correctSound != null)
//                audioSource.PlayOneShot(correctSound);

//            completedInputs[currentInputIndex] = true;

//            if (successObjects.Length > currentInputIndex &&
//                successObjects[currentInputIndex] != null)
//            {
//                successObjects[currentInputIndex].SetActive(true);
//            }

//            // Fire row completed event
//            OnRowCompletedEvent?.Invoke(currentInputIndex, currentInput);

//            if (AllInputsCompleted())
//            {
//                Invoke(nameof(OnAllInputsCompleted), 2f);
//            }
//            else
//            {
//                Invoke(nameof(ResetForNextInput), 2f);
//            }
//        }

//        private void HandleIncorrectCompletion()
//        {
//            if (audioSource != null && wrongSound != null)
//                audioSource.PlayOneShot(wrongSound);

//            // Fire row failed event
//            OnRowFailedEvent?.Invoke();

//            Invoke(nameof(ResetIncorrectOutputAttempt), 1.5f);
//        }

//        private void ResetIncorrectOutputAttempt()
//        {
//            foreach (var socket in outputSockets)
//            {
//                socket.ReturnBlockToStart();
//            }

//            placedOutputs.Clear();
//        }

//        // ========== GAME FLOW ==========

//        private bool AllInputsCompleted()
//        {
//            return completedInputs.All(c => c);
//        }

//        private void OnAllInputsCompleted()
//        {
//            Debug.Log("ALL TRUTH TABLE ROWS COMPLETED! GAME FINISHED.");

//            // Fire completion event
//            OnAllRowsCompletedEvent?.Invoke();

//            SetInputSystemEnabled(false);

//            foreach (var socket in outputSockets)
//            {
//                socket.SetSocketEnabled(false);
//            }

//            // Cleanup
//            foreach (var socket in outputSockets)
//            {
//                if (socket.HasSelection())
//                {
//                    Destroy(socket.GetSelectedObject());
//                }
//            }

//            foreach (GameObject clone in allClonedBlocks)
//            {
//                if (clone != null)
//                {
//                    Destroy(clone);
//                }
//            }
//            allClonedBlocks.Clear();

//            if (allCompletedObject != null)
//            {
//                allCompletedObject.SetActive(true);
//            }
//        }

//        private void ResetForNextInput()
//        {
//            foreach (var socket in outputSockets)
//            {
//                socket.RemoveAndReinstantiateBlock();
//                socket.SetSocketEnabled(false);
//            }

//            placedOutputs.Clear();
//            currentInput = "";
//            currentInputIndex = -1;

//            SetInputSystemEnabled(true);
//        }

//        private void SetInputSystemEnabled(bool enabled, int excludeSocketIndex = -1)
//        {
//            for (int i = 0; i < inputSockets.Length; i++)
//            {
//                if (!completedInputs[i])
//                {
//                    inputSockets[i].SetSocketEnabled(enabled && i != excludeSocketIndex);
//                }
//                else
//                {
//                    inputSockets[i].SetSocketEnabled(false);
//                }
//            }

//            foreach (var block in inputBlocks)
//            {
//                if (!block.isUsed)
//                {
//                    var interactor = block.GetComponent<SnapInteractor>();
//                    if (interactor != null)
//                    {
//                        interactor.enabled = enabled;
//                    }
//                }
//            }
//        }

//        // ========== UTILITY ==========

//        public void RegisterClone(GameObject clone)
//        {
//            if (clone != null && !allClonedBlocks.Contains(clone))
//            {
//                allClonedBlocks.Add(clone);
//                Debug.Log($"Clone {clone.name} registered for cleanup.");
//            }
//        }
//    }
//}