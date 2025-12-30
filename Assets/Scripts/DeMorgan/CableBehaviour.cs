////using UnityEngine;

////public class CableBehaviour : MonoBehaviour
////{
////    public GameObject cablePrefab;            // Assign cable prefab in inspector
////    public float moveThreshold = 0.01f;

////    private Transform endPointA;
////    private Transform endPointB;
////    private Vector3 initialPosA;
////    private Vector3 initialPosB;
////    private bool hasSpawnedNext = false;
////    public CableSpawner spawner;
////    void Start()
////    {
////        spawner = GameObject.Find("CableManager").GetComponent<CableSpawner>();
////        // Get first two children as endpoints
////        if (transform.childCount < 2)
////        {
////            Debug.LogError("Cable prefab needs 2 children for endpoints.");
////            enabled = false;
////            return;
////        }

////        endPointA = transform.GetChild(0);
////        endPointB = transform.GetChild(1);

////        initialPosA = endPointA.position;
////        initialPosB = endPointB.position;

////    }

////    void Update()
////    {
////        if (hasSpawnedNext) return;

////        bool movedA = Vector3.Distance(endPointA.position, initialPosA) > moveThreshold;
////        bool movedB = Vector3.Distance(endPointB.position, initialPosB) > moveThreshold;

////        if (movedA && movedB)
////        {
////            spawner.myspawn();
////           // SpawnNextCable();
////           hasSpawnedNext = true;
////        }
////    }

////    void SpawnNextCable()
////    {
////        Instantiate(cablePrefab, transform.position, transform.rotation);
////        Debug.Log("Spawned next cable.");
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;

//public class CableSpawner : MonoBehaviour
//{
//    public GameObject cablePrefab;

//    // Track cables for cleanup
//    private static List<GameObject> allCables = new List<GameObject>();

//    // CALL THIS IN INSPECTOR: You can type X, Y, Z coordinates manually
//    public void SpawnAtLocation(Vector3 position)
//    {
//        if (cablePrefab != null)
//        {
//            // Spawn at the coordinate provided in the Inspector
//            GameObject cable = Instantiate(cablePrefab, position, Quaternion.identity);
//            allCables.Add(cable);
//        }
//        else
//        {
//            Debug.LogError("Assign a Cable Prefab in the Inspector!");
//        }
//    }

//    // CALL THIS IN INSPECTOR: To delete everything
//    public void DestroyAllCables()
//    {
//        foreach (GameObject cable in allCables)
//        {
//            if (cable != null) Destroy(cable);
//        }
//        allCables.Clear();
//    }
//}