using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Auto-setup that runs the instant Play starts — no menu commands, no manual steps. Loads the
/// "YandereSimulatorMap" model, works out where its real floor is from measured bounds (not a
/// guess), adds colliders for anything the import didn't already collide, and spawns Player
/// (CharacterController + PrototypePlayerController) standing on it — with the StudentChan model
/// as its visual (Humanoid Animator + SimpleHumanoidWalkAnimator for a basic procedural walk) and
/// ThirdPersonCamera on Camera.main for a 3rd-person view.
///
/// Destroys any stale "Player" left over from a previous session before building a fresh one —
/// EnsurePlayer only builds the character when no Player exists yet, so without this, an old
/// leftover Player would silently win every run instead of ever creating the real character
/// (this exact bug happened once already).
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    const string MapResourcePath = "YandereSimulatorMap/YandereSimulatorMap";
    // The map reads tiny next to StudentChan (the building's own proportions are fine, it's just
    // scaled down relative to her) — bump the whole map up rather than shrinking her, since she's
    // the one whose size should stay "normal human". Adjust this if it's still off.
    const float MapScale = 2.2f;

    void Awake()
    {
        var stalePlayer = GameObject.Find("Player");
        if (stalePlayer != null) Object.Destroy(stalePlayer);
        var staleGround = GameObject.Find("TemporaryGround");
        if (staleGround != null) Object.Destroy(staleGround);
        var staleMap = GameObject.Find("Map");
        if (staleMap != null) Object.Destroy(staleMap);

        Vector3 spawn = BuildMap();
        EnsurePlayer(spawn);
    }

    Vector3 BuildMap()
    {
        var mapPrefab = Resources.Load<GameObject>(MapResourcePath);
        if (mapPrefab == null)
        {
            Debug.LogError($"[GameBootstrap] Couldn't find '{MapResourcePath}' under any Resources folder — " +
                "falling back to a temporary flat plane. Check Assets/Models/Resources/YandereSimulatorMap/ exists.");
            return BuildTemporaryGround();
        }

        var map = new GameObject("Map");
        map.transform.localScale = Vector3.one * MapScale;
        var instance = Object.Instantiate(mapPrefab, map.transform);
        instance.name = "YandereSimulatorMap_Model";
        instance.transform.localPosition = Vector3.zero;
        // This FBX was authored Z-up (its floor spans a huge X/Y footprint but is only 0.15 thick
        // along Z — confirmed from GameBootstrap_Diagnostics.txt, not a guess) and imported with no
        // axis correction, so it sits rotated 90° relative to Unity's Y-up world. Rotating -90° on X
        // maps its Z (up) onto Unity's Y (up) and its Y (depth) onto Unity's Z.
        instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("[GameBootstrap] Map model has no MeshRenderers — falling back to a temporary flat plane.");
            return BuildTemporaryGround();
        }

        Bounds aggregate = renderers[0].bounds;
        foreach (var r in renderers) aggregate.Encapsulate(r.bounds);

        string diagPath = Path.Combine(Application.dataPath, "..", "GameBootstrap_Diagnostics.txt");
        var diag = new StringBuilder();
        diag.AppendLine($"Renderer count: {renderers.Length}");
        diag.AppendLine($"Aggregate bounds: center={FormatV(aggregate.center)} min={FormatV(aggregate.min)} max={FormatV(aggregate.max)}");
        diag.AppendLine("Per-renderer bounds (largest footprint first):");
        foreach (var r in renderers.OrderByDescending(r => r.bounds.size.x * r.bounds.size.z).Take(30))
        {
            string matNames = string.Join("+", r.sharedMaterials.Where(m => m != null).Select(m => m.name));
            diag.AppendLine($"  {r.gameObject.name} [{matNames}]  center={FormatV(r.bounds.center)} size={FormatV(r.bounds.size)} min={FormatV(r.bounds.min)}");
        }
        File.WriteAllText(diagPath, diag.ToString());

        int added = 0;
        foreach (var r in renderers)
        {
            if (r.GetComponent<Collider>() != null) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var mc = r.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                added++;
            }
        }

        WetSurfaceApplier.Apply(instance);
        // Thinner than the character's outline (0.0025) — building edges should read as a subtle
        // line, not a heavy cartoon border like her silhouette.
        ToonOutlineApplier.Apply(instance, Color.black, 0.0008f);

        Vector3 spawn = FindSpawnPoint(aggregate);
        Debug.Log($"[GameBootstrap] Loaded map. Added {added} missing colliders. Spawning at {FormatV(spawn)}. " +
            "Full breakdown in GameBootstrap_Diagnostics.txt.");
        return spawn;
    }

    static Vector3 BuildTemporaryGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TemporaryGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        return new Vector3(0f, 1f, 0f);
    }

    // The geometric center of the map's bounding box is NOT guaranteed to be open ground — it can
    // land inside a wall/prop for anything but a single flat plane. Search outward in rings from
    // the center, raycasting down each candidate to find the real ground, and confirm the player's
    // capsule actually fits there (Physics.CheckCapsule) before using it.
    static Vector3 FindSpawnPoint(Bounds aggregate)
    {
        const float capsuleRadius = 0.5f;
        const float capsuleHeight = 2f;

        int rings = 12;
        float maxRadius = Mathf.Max(aggregate.extents.x, aggregate.extents.z);

        for (int ring = 0; ring <= rings; ring++)
        {
            float ringRadius = maxRadius * ring / rings;
            int pointsInRing = ring == 0 ? 1 : 8;

            for (int p = 0; p < pointsInRing; p++)
            {
                float angle = p * Mathf.PI * 2f / pointsInRing;
                float x = aggregate.center.x + Mathf.Cos(angle) * ringRadius;
                float z = aggregate.center.z + Mathf.Sin(angle) * ringRadius;

                Vector3 rayStart = new Vector3(x, aggregate.max.y + 5f, z);
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, aggregate.size.y + 10f))
                    continue;

                Vector3 candidateCenter = hit.point + Vector3.up * (capsuleHeight * 0.5f + 0.05f);
                Vector3 capsuleBottom = candidateCenter + Vector3.up * (capsuleRadius - capsuleHeight * 0.5f);
                Vector3 capsuleTop = candidateCenter + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);
                if (Physics.CheckCapsule(capsuleBottom, capsuleTop, capsuleRadius * 0.95f))
                    continue; // something's already occupying this spot — keep searching

                return hit.point + Vector3.up * 1f;
            }
        }

        Debug.LogWarning("[GameBootstrap] Couldn't find a confirmed-clear spawn point after searching — " +
            "falling back to the raw bounding-box center, which may still be inside geometry.");
        return new Vector3(aggregate.center.x, aggregate.min.y + 1f, aggregate.center.z);
    }

    static string FormatV(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

    static void EnsurePlayer(Vector3 spawn)
    {
        var player = new GameObject("Player");

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1, 0);
        cc.skinWidth = 0.08f;

        player.AddComponent<PrototypePlayerController>();

        var characterPrefab = Resources.Load<GameObject>("StudentChan/StudentChan");
        if (characterPrefab != null)
        {
            var bodyVisual = Object.Instantiate(characterPrefab, player.transform);
            bodyVisual.name = "BodyVisual";
            bodyVisual.transform.localPosition = Vector3.zero; // humanoid rigs are typically pivoted at the feet
            bodyVisual.transform.localRotation = Quaternion.identity;

            // The visual mesh isn't the movement collider — CharacterController is — so strip any
            // colliders the model came with to avoid them fighting with it.
            foreach (var col in bodyVisual.GetComponentsInChildren<Collider>())
                Object.Destroy(col);

            bodyVisual.AddComponent<SimpleHumanoidWalkAnimator>();
            ToonOutlineApplier.Apply(bodyVisual, Color.black);
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] 'StudentChan' character not found under Resources — using a placeholder capsule.");
            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "BodyVisual";
            bodyVisual.transform.SetParent(player.transform);
            bodyVisual.transform.localPosition = cc.center;
            Object.Destroy(bodyVisual.GetComponent<Collider>());
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var oldIso = mainCamera.GetComponent<IsometricFollowCamera>();
            if (oldIso != null) Object.Destroy(oldIso);
            var tpc = mainCamera.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.SetTarget(player.transform);
        }

        player.transform.position = spawn;
    }

    // Runs the instant Play starts — this is the only thing the user has to do.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<GameBootstrap>() != null) return;
        new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
    }
}
