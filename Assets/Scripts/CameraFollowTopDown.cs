using UnityEngine;

public class CameraFollowTopDown : MonoBehaviour
{
    public Transform target;

    [Header("Settings")]
    public float smoothSpeed = 0.125f;

    // High up, looking down-and-forward
    public Vector3 offset = new Vector3(0f, 15f, -8f);

    void FixedUpdate()
    {
        // 1. Auto-find Player if target is missing
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
            return;
        }

        // 2. Calculate Desired Position
        Vector3 desiredPosition = target.position + offset;

        // 3. Smooth Move
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}