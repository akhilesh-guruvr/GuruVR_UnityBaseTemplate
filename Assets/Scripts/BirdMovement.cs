using UnityEngine;

public class BirdMovement : MonoBehaviour
{
    [Header("Flight Settings")]
    public float moveSpeed = 5f;        // forward flight speed
    public float turnSpeed = 2f;        // how smoothly it turns
    public float changeDirectionTime = 3f; // seconds before picking new direction
    public float flyRadius = 20f;       // how far from start point bird can go

    private Vector3 startPos;
    private Vector3 targetDir;
    private float timer;

    void Start()
    {
        startPos = transform.position;
        PickNewDirection();
    }

    void Update()
    {
        // Move bird forward
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // Rotate smoothly towards target direction
        Quaternion targetRotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Timer to change direction
        timer += Time.deltaTime;
        if (timer >= changeDirectionTime)
        {
            PickNewDirection();
            timer = 0f;
        }
    }

    void PickNewDirection()
    {
        // Pick a random direction but keep inside flyRadius
        Vector3 randomPos = startPos + Random.insideUnitSphere * flyRadius;
        randomPos.y = Mathf.Clamp(randomPos.y, startPos.y, startPos.y + flyRadius * 0.5f); // keep above ground
        targetDir = (randomPos - transform.position).normalized;
    }
}
