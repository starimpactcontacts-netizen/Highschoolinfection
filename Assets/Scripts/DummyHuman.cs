using UnityEngine;

/// <summary>
/// Local-testing-only stand-in for a networked Human player. Photon isn't in this project (needs
/// an account/App ID only the project owner can create), so a solo Play session only ever has one
/// real, keyboard-controlled player — and that player always registers first with MatchManager,
/// i.e. is always the Zombie. Without any real Humans, every local match would resolve instantly
/// ("zombie wins, 0 humans") with nothing to observe. These wander/occasionally-hide capsules give
/// the Zombie something to actually hunt so detection/elimination/timer/win-condition logic is
/// exercisable end to end before Photon exists.
///
/// Delete these once Photon spawns real networked Humans — they're test scaffolding, not part of
/// the shipped game.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DummyHuman : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float wanderRadius = 20f;
    [SerializeField] private float minHideSeconds = 4f;
    [SerializeField] private float maxHideSeconds = 10f;

    private CharacterController controller;
    private HumanAbility humanAbility;
    private Vector3 wanderTarget;
    private float gravity = -20f;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        humanAbility = GetComponent<HumanAbility>();
        if (humanAbility == null) humanAbility = gameObject.AddComponent<HumanAbility>();

        PickNewWanderTarget();
        StartCoroutine(HideCycle());
    }

    private void Update()
    {
        Vector3 toTarget = wanderTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < 1f)
        {
            PickNewWanderTarget();
        }
        else if (!humanAbility.IsHiding)
        {
            Vector3 moveDir = toTarget.normalized;
            transform.rotation = Quaternion.LookRotation(moveDir);
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void PickNewWanderTarget()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(offset.x, 0f, offset.y);
    }

    private System.Collections.IEnumerator HideCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minHideSeconds, maxHideSeconds));
            humanAbility.SetHiding(!humanAbility.IsHiding);
        }
    }
}
