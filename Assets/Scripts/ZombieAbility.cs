using UnityEngine;

/// <summary>
/// Attached to whichever player MatchManager assigns the Zombie role. Doubles move speed via
/// PrototypePlayerController.SpeedMultiplier, and tints the character's own material instances a
/// sickly green so the zombie is visually obvious (instances, not shared materials — otherwise
/// every player using the same StudentChan model would turn green too).
///
/// Actual detection/touch-elimination logic lives in MatchManager (it already needs the full
/// player list to check every zombie-vs-human pair, so duplicating that here per-instance would
/// just be two sources of truth for the same rule).
/// </summary>
public class ZombieAbility : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private Color zombieTint = new Color(0.4f, 0.75f, 0.35f);

    private void Awake()
    {
        var movement = GetComponent<PrototypePlayerController>();
        if (movement != null) movement.SpeedMultiplier = speedMultiplier;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            // .material (not .sharedMaterial) instances it — tints only this zombie, not every
            // other player sharing the same source material.
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                    mat.color = mat.color * zombieTint;
            }
        }
    }
}
