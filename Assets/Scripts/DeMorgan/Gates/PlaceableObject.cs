using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public int value;                        // The value to check against expectedValue
    [HideInInspector] public Vector3 initialPosition; // Store the initial spawn position

    void Start()
    {
        initialPosition = transform.position; // Capture the object's starting position
        Debug.Log($"PlaceableObject {value} initial position: {initialPosition}");
    }
}