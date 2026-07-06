using UnityEngine;

/// <summary>
/// Fortnite-style over-the-shoulder third-person camera.
/// Mouse orbits around the player, the camera sits close with a shoulder offset,
/// and a spherecast pulls it in when geometry is behind it — so it stays with
/// you indoors instead of clipping through walls.
///
/// Pairs with PrototypePlayerController (which moves relative to this camera's yaw).
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Height above the target's origin that the camera looks at (head height).")]
    [SerializeField] private float pivotHeight = 1.6f;

    [Header("Framing")]
    [Tooltip("Default distance behind the player.")]
    [SerializeField] private float distance = 2.75f;
    [Tooltip("Sideways over-the-shoulder offset.")]
    [SerializeField] private float shoulderOffset = 0.7f;

    [Header("Mouse Look")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float pitchMin = -35f;
    [SerializeField] private float pitchMax = 70f;

    [Header("Collision")]
    [Tooltip("Layers the camera will pull in front of.")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [Tooltip("How far the camera keeps off a surface it hits.")]
    [SerializeField] private float collisionRadius = 0.25f;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        // Start facing the same way the player faces.
        if (target != null) yaw = target.eulerAngles.y;
        LockCursor(true);
    }

    private void Update()
    {
        // Esc releases the mouse; click re-captures it.
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

        if (Cursor.lockState != CursorLockMode.Locked) return;

        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * pivotHeight;

        // Where the camera *wants* to sit: shoulder offset + pulled back.
        Vector3 shoulder = rotation * (Vector3.right * shoulderOffset);
        Vector3 desiredDir = rotation * Vector3.back;
        Vector3 desiredPos = pivot + shoulder + desiredDir * distance;

        // Pull in if something is between the pivot and the desired position.
        Vector3 castOrigin = pivot + shoulder;
        if (Physics.SphereCast(castOrigin, collisionRadius, desiredDir,
                out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = castOrigin + desiredDir * (hit.distance - 0.05f);
        }

        transform.position = desiredPos;
        transform.rotation = rotation;
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>Assign the follow target at runtime (used by the setup script).</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) yaw = target.eulerAngles.y;
    }
}
