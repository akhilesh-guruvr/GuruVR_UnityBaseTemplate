//using System.Collections.Generic;
//using UnityEngine;

//public class CableSpawner : MonoBehaviour
//{
//    public GameObject cablePrefab;

//    // Static list to track ALL cables from ALL spawners
//    private static List<GameObject> allCables = new List<GameObject>();

//    void Start()
//    {
//        if (cablePrefab != null)
//        {
//            GameObject cable = Instantiate(cablePrefab, transform.position, transform.rotation);
//            allCables.Add(cable);
//        }
//        else
//        {
//            Debug.LogError("CableSpawner: No cable prefab assigned.");
//        }
//    }

//    public void myspawn()
//    {
//        GameObject cable = Instantiate(cablePrefab, transform.position, transform.rotation);
//        allCables.Add(cable);
//    }

//    // Static method to destroy all cables from any spawner
//    public static void DestroyAllCables()
//    {
//        foreach (GameObject cable in allCables)
//        {
//            if (cable != null)
//            {
//                Destroy(cable);
//            }
//        }
//        allCables.Clear();
//        Debug.Log("All cables destroyed!");
//    }

//    // Non-static wrapper so you can call it from Unity Events
//    public void DestroyAllCablesEvent()
//    {
//        DestroyAllCables();
//    }
//}
using System.Collections.Generic;
using UnityEngine;

public class CableSpawner : MonoBehaviour
{
    public GameObject cablePrefab;

    // Track cables for cleanup
    private static List<GameObject> allCables = new List<GameObject>();

    // CALL THIS IN INSPECTOR
    // Drag a GameObject (e.g., a "SpawnMarker") into the parameter slot
    public void SpawnAtLocation(Transform targetLocation)
    {
        if (cablePrefab != null && targetLocation != null)
        {
            // Spawns at the target's position AND rotation
            GameObject cable = Instantiate(cablePrefab, targetLocation.position, targetLocation.rotation);
            allCables.Add(cable);
        }
        else
        {
            Debug.LogWarning("Missing Cable Prefab or Target Location!");
        }
    }

    // CALL THIS IN INSPECTOR: To delete everything
    public void DestroyAllCables()
    {
        foreach (GameObject cable in allCables)
        {
            if (cable != null) Destroy(cable);
        }
        allCables.Clear();
    }
}