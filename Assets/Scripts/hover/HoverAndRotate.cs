using UnityEngine;

public class HoverAndRotate : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Hover Settings")]
    public bool enableHover = true;
    public Axis hoverAxis = Axis.Y;
    public float hoverAmplitude = 0.5f;
    public float hoverSpeed = 2f;

    [Header("Rotation Settings")]
    public bool enableRotation = true;
    public Axis rotationAxis = Axis.Y;
    public float rotationSpeed = 50f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        if (enableHover)
            ApplyHover();

        if (enableRotation)
            ApplyRotation();
    }

    void ApplyHover()
    {
        float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;

        Vector3 offset = Vector3.zero;

        switch (hoverAxis)
        {
            case Axis.X: offset = new Vector3(hoverOffset, 0, 0); break;
            case Axis.Y: offset = new Vector3(0, hoverOffset, 0); break;
            case Axis.Z: offset = new Vector3(0, 0, hoverOffset); break;
        }

        transform.localPosition = startPos + offset;
    }

    void ApplyRotation()
    {
        Vector3 axis = Vector3.zero;

        switch (rotationAxis)
        {
            case Axis.X: axis = Vector3.right; break;
            case Axis.Y: axis = Vector3.up; break;
            case Axis.Z: axis = Vector3.forward; break;
        }

        transform.Rotate(axis * rotationSpeed * Time.deltaTime);
    }
}
