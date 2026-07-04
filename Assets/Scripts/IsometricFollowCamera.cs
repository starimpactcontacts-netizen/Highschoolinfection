using UnityEngine;

/// <summary>
/// Third-person follow camera. Maintains a configurable position offset behind
/// the target and a fixed look rotation, easing into position with SmoothDamp.
/// Decoupled from input and player logic — it only reads the target's transform.
/// </summary>
public class IsometricFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offsets")]
    [Tooltip("Local-space offset from the target (x=side, y=height, z=distance behind).")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 6f, -7f);
    [Tooltip("Fixed camera rotation in euler angles.")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(35f, 0f, 0f);

    [Header("Smoothing")]
    [Tooltip("Approximate time to reach the target position. Lower = snappier.")]
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("If true, the offset rotates with the target's yaw. If false, offset stays world-aligned (classic isometric).")]
    [SerializeField] private bool followTargetYaw = false;

    private Vector3 currentVelocity;

    // LateUpdate so the camera moves after the player has finished moving this frame.
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = followTargetYaw
            ? target.position + target.rotation * positionOffset
            : target.position + positionOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime);

        transform.rotation = Quaternion.Euler(rotationOffset);
    }

    /// <summary>Assign or swap the follow target at runtime.</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;
}
