//using UnityEngine;
//using System.Collections.Generic;
//using UnityEngine.Events;

//public class InteractionSequenceManager : MonoBehaviour
//{
//    [System.Serializable]
//    public class SequenceStage
//    {
//        public string stageName = "Stage 1";
//        public List<SocketValidator> socketsToMonitor;
//        public UnityEvent onStageComplete;
//    }

//    [Header("Game Configuration")]
//    [SerializeField] private List<SequenceStage> _stages;
//    public UnityEvent OnAllStagesComplete;

//    private int _currentStageIndex = 0;
//    private HashSet<SocketValidator> _currentStageCompletedSockets = new HashSet<SocketValidator>();

//    public void RegisterCompletion(SocketValidator socket)
//    {
//        if (_currentStageIndex >= _stages.Count) return;

//        var currentStage = _stages[_currentStageIndex];

//        if (currentStage.socketsToMonitor.Contains(socket))
//        {
//            _currentStageCompletedSockets.Add(socket);
//            CheckProgress();
//        }
//    }

//    public void DeregisterCompletion(SocketValidator socket)
//    {
//        if (_currentStageCompletedSockets.Contains(socket))
//        {
//            _currentStageCompletedSockets.Remove(socket);
//        }
//    }

//    private void CheckProgress()
//    {
//        var currentStage = _stages[_currentStageIndex];
//        if (_currentStageCompletedSockets.Count == currentStage.socketsToMonitor.Count)
//        {
//            CompleteCurrentStage();
//        }
//    }

//    private void CompleteCurrentStage()
//    {
//        var currentStage = _stages[_currentStageIndex];
//        currentStage.onStageComplete.Invoke();

//        _currentStageIndex++;
//        _currentStageCompletedSockets.Clear();

//        if (_currentStageIndex >= _stages.Count)
//        {
//            OnAllStagesComplete.Invoke();
//        }
//    }
//}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class InteractionSequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class SequenceStage
    {
        public string stageName = "Stage 1";
        public List<SocketValidator> socketsToMonitor;
        public UnityEvent onStageComplete;
    }

    [Header("Game Configuration")]
    [SerializeField] private List<SequenceStage> _stages;
    public UnityEvent OnAllStagesComplete;

    private int _currentStageIndex = 0;
    private HashSet<SocketValidator> _currentStageCompletedSockets = new HashSet<SocketValidator>();

    private void Start()
    {
        Debug.Log($"[SequenceManager {name}] START - Total Stages: {_stages.Count}");

        for (int i = 0; i < _stages.Count; i++)
        {
            Debug.Log($"[SequenceManager {name}] Stage {i} = '{_stages[i].stageName}', Sockets: {_stages[i].socketsToMonitor.Count}");
            foreach (var s in _stages[i].socketsToMonitor)
            {
                Debug.Log($"--- Socket '{s.name}' assigned to Stage: {_stages[i].stageName}");
            }
        }
    }

    public void RegisterCompletion(SocketValidator socket)
    {
        Debug.Log($"[SequenceManager {name}] RegisterCompletion RECEIVED from Socket {socket.name}");

        if (_currentStageIndex >= _stages.Count)
        {
            Debug.LogWarning($"[SequenceManager {name}] Register ignored — all stages completed already.");
            return;
        }

        var currentStage = _stages[_currentStageIndex];

        Debug.Log($"[SequenceManager {name}] Current Stage = {_currentStageIndex} ({currentStage.stageName})");
        Debug.Log($"[SequenceManager {name}] Stage sockets:");
        foreach (var s in currentStage.socketsToMonitor)
            Debug.Log($"   - {s.name}");

        if (currentStage.socketsToMonitor.Contains(socket))
        {
            Debug.Log($"[SequenceManager {name}] Socket {socket.name} IS part of this stage.");
            _currentStageCompletedSockets.Add(socket);
            Debug.Log($"[SequenceManager {name}] Completed count = {_currentStageCompletedSockets.Count}/{currentStage.socketsToMonitor.Count}");

            CheckProgress();
        }
        else
        {
            Debug.LogWarning($"[SequenceManager {name}] Socket {socket.name} is NOT part of this stage!");
        }
    }


    public void DeregisterCompletion(SocketValidator socket)
    {
        Debug.Log($"[SequenceManager {name}] DeregisterCompletion called by socket: {socket.name}");

        if (_currentStageCompletedSockets.Contains(socket))
        {
            _currentStageCompletedSockets.Remove(socket);
            Debug.Log($"[SequenceManager {name}] Socket '{socket.name}' removed from completed list.");
        }
        else
        {
            Debug.Log($"[SequenceManager {name}] Deregister called but socket '{socket.name}' was not marked as completed.");
        }
    }

    private void CheckProgress()
    {
        var currentStage = _stages[_currentStageIndex];
        Debug.Log($"[SequenceManager {name}] CheckProgress → {_currentStageCompletedSockets.Count}/{currentStage.socketsToMonitor.Count}");

        if (_currentStageCompletedSockets.Count == currentStage.socketsToMonitor.Count)
        {
            Debug.Log($"[SequenceManager {name}] Stage COMPLETE → Calling CompleteCurrentStage()");
            CompleteCurrentStage();
        }
        else
        {
            Debug.Log($"[SequenceManager {name}] Stage NOT complete yet.");
        }
    }

    private void CompleteCurrentStage()
    {
        var currentStage = _stages[_currentStageIndex];

        Debug.LogError($"[SequenceManager {name}] >>>>>> COMPLETECurrentStage CALLED FOR: {currentStage.stageName} <<<<<<");

        currentStage.onStageComplete.Invoke();

        _currentStageIndex++;
        _currentStageCompletedSockets.Clear();

        Debug.Log($"[SequenceManager {name}] Stage index now = {_currentStageIndex}/{_stages.Count}");

        if (_currentStageIndex >= _stages.Count)
        {
            Debug.LogError($"[SequenceManager {name}] >>>>>> ALL STAGES COMPLETE <<<<<<");
            OnAllStagesComplete.Invoke();
        }
        else
        {
            Debug.Log($"[SequenceManager {name}] Next stage = {_stages[_currentStageIndex].stageName}");
        }
    }

    // --------------------------------------
    // RESET PROGRESS (CALLED BY YOU)
    // --------------------------------------
    public void ResetProgress()
    {
        Debug.Log($"[SequenceManager {name}] RESET REQUESTED");

        Debug.Log($"[SequenceManager {name}] Before Reset: Stage = {_currentStageIndex}");

        _currentStageIndex = 0;
        _currentStageCompletedSockets.Clear();

        Debug.Log($"[SequenceManager {name}] After Reset: Stage = {_currentStageIndex}");
    }
}
