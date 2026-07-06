using UnityEngine;

/// <summary>
/// A lightweight, code-driven walk cycle for a Humanoid-rigged character — no baked animation
/// clip needed. There wasn't a walk animation bundled with the imported character model, so this
/// directly drives the leg/arm/spine bones (via the Animator's Humanoid Avatar) based on how fast
/// the parent is actually moving, rather than playing a pre-made clip.
///
/// Beyond a plain hip/shoulder swing, this bends knees and elbows (so legs/arms don't stay rigid
/// sticks), counter-twists the spine opposite the hip swing, and adds a small vertical body bob —
/// still a procedural approximation, not a polished animation, but reads as an actual walk rather
/// than a stiff pendulum.
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

    [Header("Bend")]
    [SerializeField] private float kneeMaxDegrees = 45f;
    [SerializeField] private float elbowMaxDegrees = 25f;
    [Range(0f, 3f)] [SerializeField] private float kneeLeadPhase = 0.7f; // knee bends slightly ahead of the hip's peak swing

    [Header("Body")]
    [SerializeField] private float spineTwistDegrees = 6f;
    [SerializeField] private float bobHeight = 0.045f;

    private Animator animator;
    private Transform leftUpperLeg, rightUpperLeg, leftLowerLeg, rightLowerLeg;
    private Transform leftUpperArm, rightUpperArm, leftLowerArm, rightLowerArm;
    private Transform spine;

    private Quaternion upperLegRestL, upperLegRestR, lowerLegRestL, lowerLegRestR;
    private Quaternion upperArmRestL, upperArmRestR, lowerArmRestL, lowerArmRestR;
    private Quaternion spineRest;
    private Vector3 bodyRestLocalPosition;

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
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);

        // The Humanoid Avatar's auto-mapping doesn't always find every bone (this rig already
        // missed the arms once) — fall back to searching the transform hierarchy by name so a
        // partial avatar mapping doesn't leave limbs stuck.
        if (leftUpperArm == null) leftUpperArm = FindBoneByName(transform, "LeftArm", "LeftUpperArm", "Arm_L", "L_Arm", "Shoulder_L");
        if (rightUpperArm == null) rightUpperArm = FindBoneByName(transform, "RightArm", "RightUpperArm", "Arm_R", "R_Arm", "Shoulder_R");
        if (leftUpperLeg == null) leftUpperLeg = FindBoneByName(transform, "LeftUpLeg", "LeftUpperLeg", "Leg_L", "L_Leg", "Thigh_L");
        if (rightUpperLeg == null) rightUpperLeg = FindBoneByName(transform, "RightUpLeg", "RightUpperLeg", "Leg_R", "R_Leg", "Thigh_R");
        if (leftLowerLeg == null) leftLowerLeg = FindBoneByName(transform, "LeftLeg", "LeftLowerLeg", "Knee_L", "L_Knee", "Calf_L", "Shin_L");
        if (rightLowerLeg == null) rightLowerLeg = FindBoneByName(transform, "RightLeg", "RightLowerLeg", "Knee_R", "R_Knee", "Calf_R", "Shin_R");
        if (leftLowerArm == null) leftLowerArm = FindBoneByName(transform, "LeftForeArm", "LeftLowerArm", "Elbow_L", "L_Elbow", "Forearm_L");
        if (rightLowerArm == null) rightLowerArm = FindBoneByName(transform, "RightForeArm", "RightLowerArm", "Elbow_R", "R_Elbow", "Forearm_R");
        if (spine == null) spine = FindBoneByName(transform, "Spine", "Torso", "Chest");

        if (leftUpperLeg == null || rightUpperLeg == null)
        {
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] Couldn't find leg bones (avatar mapping and name search both failed) — disabling procedural walk.");
            enabled = false;
            return;
        }
        if (leftUpperArm == null || rightUpperArm == null)
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] Couldn't find arm bones — legs will animate but arms will stay still.");
        if (leftLowerLeg == null || rightLowerLeg == null)
            Debug.LogWarning("[SimpleHumanoidWalkAnimator] Couldn't find knee bones — legs will swing but won't bend.");

        // A Humanoid Animator keeps re-applying its own internal "default pose" (all muscles at
        // zero, i.e. the T-pose bind pose) to whichever bones its avatar mapping recognizes, even
        // with no AnimatorController assigned — that fight is exactly what was locking her arms
        // back into a T-pose every frame regardless of what we wrote to them. Once we've cached
        // every bone reference we need, disable the Animator so it stops touching the skeleton at
        // all and our manual writes below are the only thing driving it.
        animator.enabled = false;

        upperLegRestL = leftUpperLeg.localRotation;
        upperLegRestR = rightUpperLeg.localRotation;
        if (leftLowerLeg != null) lowerLegRestL = leftLowerLeg.localRotation;
        if (rightLowerLeg != null) lowerLegRestR = rightLowerLeg.localRotation;
        if (leftUpperArm != null) upperArmRestL = leftUpperArm.localRotation;
        if (rightUpperArm != null) upperArmRestR = rightUpperArm.localRotation;
        if (leftLowerArm != null) lowerArmRestL = leftLowerArm.localRotation;
        if (rightLowerArm != null) lowerArmRestR = rightLowerArm.localRotation;
        if (spine != null) spineRest = spine.localRotation;

        bodyRestLocalPosition = transform.localPosition;
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

        leftUpperLeg.localRotation = upperLegRestL * Quaternion.Euler(swing * legSwingDegrees, 0f, 0f);
        rightUpperLeg.localRotation = upperLegRestR * Quaternion.Euler(-swing * legSwingDegrees, 0f, 0f);

        // Knees only ever bend forward — peak while that leg is mid-swing (lifting through), close
        // to straight while it's planted/pushing off. kneeLeadPhase shifts the peak a bit earlier
        // than the hip's own peak swing, which is roughly how a real knee leads a hip through a step.
        if (leftLowerLeg != null)
        {
            float kneeL = Mathf.Max(0f, Mathf.Sin(phase + kneeLeadPhase)) * kneeMaxDegrees * currentAmplitude;
            leftLowerLeg.localRotation = lowerLegRestL * Quaternion.Euler(-kneeL, 0f, 0f);
        }
        if (rightLowerLeg != null)
        {
            float kneeR = Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI + kneeLeadPhase)) * kneeMaxDegrees * currentAmplitude;
            rightLowerLeg.localRotation = lowerLegRestR * Quaternion.Euler(-kneeR, 0f, 0f);
        }

        if (leftUpperArm != null)
            leftUpperArm.localRotation = upperArmRestL * Quaternion.Euler(-swing * armSwingDegrees, 0f, 0f);
        if (rightUpperArm != null)
            rightUpperArm.localRotation = upperArmRestR * Quaternion.Euler(swing * armSwingDegrees, 0f, 0f);

        // Elbows: arm L is in the "forward swing" phase when swing < 0 (opposite the same-side
        // leg), so bend it there and mirror for arm R.
        if (leftLowerArm != null)
        {
            float elbowL = Mathf.Max(0f, -Mathf.Sin(phase + kneeLeadPhase)) * elbowMaxDegrees * currentAmplitude;
            leftLowerArm.localRotation = lowerArmRestL * Quaternion.Euler(-elbowL, 0f, 0f);
        }
        if (rightLowerArm != null)
        {
            float elbowR = Mathf.Max(0f, Mathf.Sin(phase + kneeLeadPhase)) * elbowMaxDegrees * currentAmplitude;
            rightLowerArm.localRotation = lowerArmRestR * Quaternion.Euler(-elbowR, 0f, 0f);
        }

        // Torso counter-twists opposite the hip swing, and the whole body bobs twice per stride
        // (once per footfall) — both cheap but do a lot to sell "walking" over "sliding".
        if (spine != null)
            spine.localRotation = spineRest * Quaternion.Euler(0f, -swing * spineTwistDegrees, 0f);

        float bob = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * currentAmplitude;
        transform.localPosition = bodyRestLocalPosition + Vector3.up * bob;
    }

    // Case-insensitive substring search for a transform matching any of the given hints — a
    // fallback for when Humanoid avatar bone mapping doesn't resolve a bone via GetBoneTransform.
    // Searches the SkinnedMeshRenderer's own bones[] array first (the bones that actually deform
    // the visible mesh) before falling back to the whole hierarchy, since a same-named helper/twist
    // bone elsewhere in the rig would otherwise silently animate nothing visible.
    static Transform FindBoneByName(Transform root, params string[] nameHints)
    {
        var skinnedMesh = root.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMesh != null && skinnedMesh.bones != null)
        {
            var match = MatchInSet(skinnedMesh.bones, nameHints);
            if (match != null) return match;
        }

        return MatchInSet(root.GetComponentsInChildren<Transform>(true), nameHints);
    }

    static Transform MatchInSet(System.Collections.Generic.IEnumerable<Transform> candidates, string[] nameHints)
    {
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
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
