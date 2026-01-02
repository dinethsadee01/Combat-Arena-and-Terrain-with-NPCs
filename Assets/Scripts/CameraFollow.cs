using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(0, 8.0f, -8.0f);

    [Header("Aiming Settings")]
    public Vector3 lookOffset = new Vector3(0, 3.0f, 0);

    [Header("Smoothing")]
    public float smoothSpeed = 0.125f;
    public float rotationSpeed = 5f;

    void FixedUpdate()
    {
        // Auto-find Player
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
            return;
        }

        // 1. Calculate Position relative to Player's rotation
        Vector3 desiredPosition = target.TransformPoint(offset);

        // 2. Smoothly Move
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 3. Rotate to look at the Focus Point (Player + LookOffset)
        Vector3 focusPoint = target.position + lookOffset;
        var targetRotation = Quaternion.LookRotation(focusPoint - transform.position);

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}