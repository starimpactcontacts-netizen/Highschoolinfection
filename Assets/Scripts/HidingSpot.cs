using UnityEngine;

/// <summary>
/// Attach to any prop (locker, cabinet, etc.) the local player can click to hide in for up to
/// maxHideSeconds before being forcibly ejected. This is a separate, "harder" hide than
/// HumanAbility's crouch (C key) — it fully commits the player (frozen in place, invisible) rather
/// than just shrinking the detection radius while still moving. Reuses HumanAbility.SetHiding
/// under the hood so MatchManager's existing zombie-detection-radius logic applies unchanged; no
/// separate "hiding in a locker" detection rule needed.
///
/// Interaction is proximity + left-click, not a literal cursor-hover click — ThirdPersonCamera
/// locks the cursor to screen center during gameplay, so a collider-raycast click (OnMouseDown)
/// would only ever fire for whatever happens to be dead-center of the screen, which isn't a
/// reliable "point at object and click" model. Distance-based only for now — not gated on facing
/// the object, which is an accepted simplification.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HidingSpot : MonoBehaviour
{
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private float maxHideSeconds = 10f;
    [SerializeField] private Vector3 hideLocalOffset = new Vector3(0f, 0f, 0.4f);

    public bool Occupied { get; private set; }
    public float RemainingHideTime { get; private set; }

    private Transform occupant;
    private HumanAbility occupantAbility;
    private PrototypePlayerController occupantController;
    private CharacterController occupantCC;
    private Renderer[] occupantRenderers;
    private Vector3 preHidePosition;

    private void Update()
    {
        if (Occupied)
        {
            RemainingHideTime -= Time.deltaTime;
            if (RemainingHideTime <= 0f || Input.GetMouseButtonDown(0))
                Eject();
            return;
        }

        // Only the local, keyboard-controlled Player can interact — dummies don't decide to hide here.
        var player = GameObject.Find("Player");
        if (player == null) return;
        if (Vector3.Distance(player.transform.position, transform.position) > interactRange) return;

        if (Input.GetMouseButtonDown(0))
            Enter(player.transform);
    }

    private void Enter(Transform player)
    {
        var ability = player.GetComponent<HumanAbility>();
        if (ability == null) return; // zombies can't hide

        occupant = player;
        occupantAbility = ability;
        occupantController = player.GetComponent<PrototypePlayerController>();
        occupantCC = player.GetComponent<CharacterController>();
        occupantRenderers = player.GetComponentsInChildren<Renderer>();

        preHidePosition = player.position;

        if (occupantCC != null) occupantCC.enabled = false;
        player.position = transform.position + transform.TransformDirection(hideLocalOffset);
        if (occupantCC != null) occupantCC.enabled = true;

        if (occupantController != null) occupantController.enabled = false;
        foreach (var r in occupantRenderers) r.enabled = false;

        occupantAbility.SetHiding(true);
        // Disabling (not just relying on state) stops HumanAbility's own C-key toggle from firing
        // while occupied — this component owns the hidden state exclusively until Eject().
        occupantAbility.enabled = false;

        Occupied = true;
        RemainingHideTime = maxHideSeconds;
    }

    private void Eject()
    {
        if (occupantCC != null) occupantCC.enabled = false;
        if (occupant != null) occupant.position = preHidePosition;
        if (occupantCC != null) occupantCC.enabled = true;

        if (occupantController != null) occupantController.enabled = true;
        if (occupantRenderers != null)
            foreach (var r in occupantRenderers) if (r != null) r.enabled = true;

        if (occupantAbility != null)
        {
            occupantAbility.enabled = true;
            occupantAbility.SetHiding(false);
        }

        Occupied = false;
        occupant = null;
        occupantAbility = null;
        occupantController = null;
        occupantCC = null;
        occupantRenderers = null;
    }

    /// <summary>Used by GameHUD to find which spot (if any) the given player currently occupies.</summary>
    public static HidingSpot FindOccupiedBy(Transform player)
    {
        foreach (var spot in FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
            if (spot.Occupied && spot.occupant == player) return spot;
        return null;
    }

    /// <summary>
    /// Used by MatchManager.ConvertToZombie — if a Human gets touched while mid-hide (frozen,
    /// invisible), force them out first so the conversion doesn't leave a Zombie stuck in a
    /// locker's hidden state forever.
    /// </summary>
    public static void ForceEjectIfOccupying(Transform player)
    {
        var spot = FindOccupiedBy(player);
        if (spot != null) spot.Eject();
    }

    /// <summary>Used by GameHUD to show a "press to hide" prompt when a spot is in reach.</summary>
    public static HidingSpot FindNearbyAvailable(Transform player)
    {
        HidingSpot nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var spot in FindObjectsByType<HidingSpot>(FindObjectsSortMode.None))
        {
            if (spot.Occupied) continue;
            float dist = Vector3.Distance(player.position, spot.transform.position);
            if (dist <= spot.interactRange && dist < nearestDist)
            {
                nearest = spot;
                nearestDist = dist;
            }
        }
        return nearest;
    }
}
