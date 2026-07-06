using UnityEngine;

/// <summary>
/// Attached to every player MatchManager assigns the Human role. Press C to toggle hiding —
/// MatchManager reads <see cref="IsHiding"/> to shrink the zombie's detection radius against this
/// player. Also shrinks the CharacterController height a bit while hiding, both as a visual crouch
/// cue and because a shorter capsule is a smaller physical target for the zombie's touch check.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class HumanAbility : MonoBehaviour
{
    [SerializeField] private KeyCode hideKey = KeyCode.C;
    [SerializeField] private float crouchHeight = 1.1f;

    public bool IsHiding { get; private set; }

    /// <summary>External override for non-keyboard-driven Humans (see DummyHuman).</summary>
    public void SetHiding(bool hiding)
    {
        if (hiding == IsHiding) return;
        IsHiding = hiding;
        controller.height = IsHiding ? crouchHeight : standingHeight;
        controller.center = IsHiding
            ? new Vector3(standingCenter.x, standingCenter.y - (standingHeight - crouchHeight) * 0.5f, standingCenter.z)
            : standingCenter;
    }

    private CharacterController controller;
    private float standingHeight;
    private Vector3 standingCenter;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
    }

    private void Update()
    {
        if (Input.GetKeyDown(hideKey))
            SetHiding(!IsHiding);
    }
}
