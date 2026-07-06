using UnityEngine;

/// <summary>
/// A lightweight, code-driven walk cycle for a Humanoid-rigged character — no baked animation
/// clip needed. There wasn't a walk animation bundled with the imported character model, so
/// this directly swings the leg/arm bones (via the Animator's Humanoid Avatar) based on how
/// fast the parent is actually moving, rather than playing a pre-made clip. It's a simple
/// procedural approximation, not a polished animation — legs/arms swing in a sine wave that
/// fades in with speed and fades out to a standing idle when stationary.
///
/// Requires an Animator on this GameObject (or a child) with a valid Humanoid Avatar. If the
/// character's rig isn't recognized as Humanoid (avatar invalid), this disables itself.
/// </summary>
public class SimpleHumanoidWalkAnimator : MonoBehaviour
{
    [Tooltip("Root whose world-position movement drives the walk cycle (defaults to this object's parent).")]
    [SerializeField] private Transform movementSource;

    [Header("Swing")]
    [SerializeField] private float strideFrequency = 2.2f;   // cycles per second at full speed
    [SerializeField] private float legSwingDegrees = 30f;
    [SerializeField] private float armSwingDegrees = 20f;
    [SerializeField] private float speedForFullSwing = 6f;    // world units/sec that maps to full amplitude
    [SerializeField] private float blendSpeed = 6f;           // how fast amplitude fades in/out

    private Animator animator;
    private Transform leftUpperLeg, rightUpperLeg, leftUpperArm, rightUpperArm;
    private Quaternion leftLegRest, rightLegRest, leftArmRest, rightArmRest;

    private Vector3 lastPosition;
    private float phase;
    private float currentAmplitude;
    private bool ready;

    private void Awake()
    {
        if (movementSource == null) movementSource = transform.parent != null ? transform.parent : transform;
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.isHuman)
        {
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] No valid Humanoid Animator found — disabling procedural walk.");
            enabled = false;
            return;
        }

        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

        // The Humanoid Avatar's auto-mapping doesn't always find every bone (arms especially, if
        // there are extra twist/roll bones confusing the heuristic) — fall back to searching the
        // actual transform hierarchy by name so a partial avatar mapping doesn't leave limbs stuck.
        if (leftUpperArm == null) leftUpperArm = FindBoneByName(transform, "LeftArm", "LeftUpperArm", "Arm_L", "L_Arm", "Shoulder_L");
        if (rightUpperArm == null) rightUpperArm = FindBoneByName(transform, "RightArm", "RightUpperArm", "Arm_R", "R_Arm", "Shoulder_R");
        if (leftUpperLeg == null) leftUpperLeg = FindBoneByName(transform, "LeftUpLeg", "LeftUpperLeg", "Leg_L", "L_Leg", "Thigh_L");
        if (rightUpperLeg == null) rightUpperLeg = FindBoneByName(transform, "RightUpLeg", "RightUpperLeg", "Leg_R", "R_Leg", "Thigh_R");

        if (leftUpperLeg == null || rightUpperLeg == null)
        {
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] Couldn't find leg bones (avatar mapping and name search both failed) — disabling procedural walk.");
            enabled = false;
            return;
        }
        if (leftUpperArm == null || rightUpperArm == null)
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] Couldn't find arm bones — legs will animate but arms will stay still.");

        // Humanoid rigs are driven by the Animator itself once one is present; without an
        // AnimatorController/clip assigned, Animator just holds bones at bind pose each frame,
        // so writing to them directly in LateUpdate (after Animator's own update) sticks.
        leftLegRest = leftUpperLeg.localRotation;
        rightLegRest = rightUpperLeg.localRotation;
        if (leftUpperArm != null) leftArmRest = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightArmRest = rightUpperArm.localRotation;

        lastPosition = movementSource.position;
        ready = true;
    }

    private void LateUpdate()
    {
        if (!ready) return;

        Vector3 delta = movementSource.position - lastPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPosition = movementSource.position;

        float targetAmplitude = Mathf.Clamp01(speed / speedForFullSwing);
        currentAmplitude = Mathf.MoveTowards(currentAmplitude, targetAmplitude, blendSpeed * Time.deltaTime);

        if (currentAmplitude > 0.001f)
            phase += Time.deltaTime * strideFrequency * Mathf.PI * 2f * Mathf.Max(currentAmplitude, 0.3f);

        float swing = Mathf.Sin(phase) * currentAmplitude;

        leftUpperLeg.localRotation = leftLegRest * Quaternion.Euler(swing * legSwingDegrees, 0f, 0f);
        rightUpperLeg.localRotation = rightLegRest * Quaternion.Euler(-swing * legSwingDegrees, 0f, 0f);

        if (leftUpperArm != null)
            leftUpperArm.localRotation = leftArmRest * Quaternion.Euler(-swing * armSwingDegrees, 0f, 0f);
        if (rightUpperArm != null)
            rightUpperArm.localRotation = rightArmRest * Quaternion.Euler(swing * armSwingDegrees, 0f, 0f);
    }

    // Case-insensitive substring search through the whole hierarchy under root for a transform
    // whose name matches any of the given hints — a fallback for when Humanoid avatar bone
    // mapping doesn't resolve a bone via GetBoneTransform.
    static Transform FindBoneByName(Transform root, params string[] nameHints)
    {
        foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
        {
            string name = candidate.name.ToLowerInvariant();
            foreach (var hint in nameHints)
            {
                if (name.Contains(hint.ToLowerInvariant()))
                    return candidate;
            }
        }
        return null;
    }
}
