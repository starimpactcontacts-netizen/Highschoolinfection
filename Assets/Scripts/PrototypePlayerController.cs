using UnityEngine;

/// <summary>
/// Prototype third-person mover. Reads WASD, translates it into camera-relative
/// movement, smoothly turns the character toward its heading, and drives a single
/// animator float ("Speed") for Idle/Walk blending. Requires a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PrototypePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float gravity = -20f;

    [Header("Turning")]
    [Tooltip("How quickly the character rotates toward its movement direction.")]
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("References")]
    [Tooltip("Camera used to make movement relative to view. Defaults to Camera.main.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Optional Animator with a float parameter for locomotion.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";

    private CharacterController controller;
    private float turnSmoothVelocity;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // --- Read raw input ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        Vector3 horizontalMove = Vector3.zero;

        if (inputDir.magnitude >= 0.1f)
        {
            // Angle of input relative to the camera's yaw.
            float cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraYaw;

            // Smoothly rotate the body toward the heading.
            float smoothedAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

            // Move along the (target) heading so strafing feels natural.
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            horizontalMove = moveDir.normalized * targetSpeed;
        }

        // --- Gravity / grounding ---
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small constant keeps us stuck to the floor
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalMove + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // --- Drive animator ---
        if (animator != null)
        {
            float planarSpeed = horizontalMove.magnitude;
            animator.SetFloat(speedParameter, planarSpeed);
        }
    }
}
