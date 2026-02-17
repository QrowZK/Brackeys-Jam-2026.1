using UnityEngine;
using UnityEngine.InputSystem;

public class Inspect : MonoBehaviour
{
    public Transform target;

    [Header("Distance")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 15f;

    [Header("Rotation")]
    public float sensitivity = 0.2f;
    public float minY = -40f;
    public float maxY = 80f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            yaw += delta.x * sensitivity;
            pitch -= delta.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minY, maxY);
        }

        // Scroll zoom
        float scroll = Mouse.current.scroll.ReadValue().y;
        distance -= scroll * 0.01f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
