using UnityEngine;
using UnityEditor;

/// <summary>
/// Sets up the imported "Anime Class Room" OBJ as the main playable space.
///
/// Run via: Tools → Set Up Anime Classroom (Main Space)
///
/// What it does (all reversible / re-runnable):
///   1. Places the model under an "AnimeClassroom" parent, floor on Y=0.
///   2. Adds mesh colliders so the floor/walls are solid, plus a safety floor
///      box so the Player can never fall through.
///   3. Spawns (or repositions) the Player standing in the middle of the room,
///      with the over-the-shoulder camera attached.
///   4. Disables the greybox "School" object if present, so the two levels
///      don't overlap. It is only SetActive(false) — re-enable it any time.
///
/// Measured room bounds (native scale): X -20.9..8.4, Y -1.1..9.2, Z -1.9..19.2.
/// The model's raw bounding box looks huge (~90 deep) only because of a few
/// stray sky-backdrop verts; the actual room is ~29 x 21 x 10 units.
///
/// Model: "Anime Class Room" (https://skfb.ly/oEvMs) by AnixMoonLight,
/// licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/).
/// </summary>
public class ClassroomPlacer
{
    const string ModelPath = "Assets/AnimeClassroom/AnimeClassroom.obj";

    // Lift so the model floor (local Y = -1.09) lands on world Y = 0.
    const float FloorLift = 1.09f;
    const float ModelScale = 1f; // bump down (e.g. 0.35) later if the room feels too big.

    // Centre of the actual room footprint, in model-local XZ (rotation kept at 0
    // so these map straight to world XZ after the Y lift).
    static readonly Vector2 RoomCenterXZ = new Vector2(-6.24f, 8.67f);
    const float RoomWidth = 29.3f;
    const float RoomDepth = 21.1f;

    [MenuItem("Tools/Set Up Anime Classroom (Main Space)")]
    public static void SetUp()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            EditorUtility.DisplayDialog("Model Not Found",
                "Couldn't load:\n" + ModelPath + "\n\n" +
                "Make sure the OBJ imported (check Assets/AnimeClassroom in the " +
                "Project window) and that there are no import errors in the Console.",
                "OK");
            return;
        }

        // --- 1. Place the model ---------------------------------------------
        var existing = GameObject.Find("AnimeClassroom");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject parent = new GameObject("AnimeClassroom");
        parent.transform.position = new Vector3(0f, FloorLift, 0f);
        parent.transform.localScale = Vector3.one * ModelScale;

        GameObject instance = PrefabUtility.InstantiatePrefab(model, parent.transform) as GameObject;
        if (instance == null) instance = Object.Instantiate(model, parent.transform);
        instance.name = "AnimeClassroom_Model";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        // --- 2. Colliders ----------------------------------------------------
        // Solid floor/walls/desks from the mesh itself...
        int colliders = 0;
        foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<MeshCollider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            colliders++;
        }
        // ...plus an invisible safety floor so the Player can't fall through.
        GameObject safety = GameObject.CreatePrimitive(PrimitiveType.Cube);
        safety.name = "SafetyFloor";
        safety.transform.SetParent(parent.transform, false);
        // Place in world space (undo the parent's Y lift so its top sits at Y=0).
        safety.transform.position = new Vector3(RoomCenterXZ.x, -0.05f, RoomCenterXZ.y);
        safety.transform.localScale = new Vector3(RoomWidth + 2f, 0.1f, RoomDepth + 2f);
        var safetyRenderer = safety.GetComponent<MeshRenderer>();
        if (safetyRenderer != null) safetyRenderer.enabled = false;

        // --- 3. Player + camera, standing in the room ------------------------
        Vector3 playerSpawn = new Vector3(RoomCenterXZ.x, 0.5f, RoomCenterXZ.y);
        GameObject player = EnsurePlayer(playerSpawn);

        // --- 4. Get the greybox out of the way (reversibly) ------------------
        var greybox = GameObject.Find("School");
        if (greybox != null) greybox.SetActive(false);

        Selection.activeGameObject = player;
        SceneView.FrameLastActiveSceneView();

        EditorUtility.DisplayDialog("Anime Classroom Ready",
            "The anime classroom is now the main space.\n\n" +
            "• Model placed with " + colliders + " mesh colliders + a safety floor.\n" +
            "• Player spawned in the middle of the room.\n" +
            (greybox != null
                ? "• Greybox 'School' was hidden (not deleted) so they don't overlap.\n"
                : "") +
            "\nPress Play to walk around!\n\n" +
            "If it looks untextured: select the model in the Project window, open " +
            "the Materials tab, choose 'Extract Textures' / 'Extract Materials', Apply.\n" +
            "If the room feels too big, tell me and I'll drop ModelScale.",
            "Nice");
    }

    /// <summary>Find the Player (or build a fresh one) and stand it at spawn.</summary>
    static GameObject EnsurePlayer(Vector3 spawn)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1, 0);

            player.AddComponent<PrototypePlayerController>();

            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "BodyVisual";
            bodyVisual.transform.SetParent(player.transform);
            bodyVisual.transform.localPosition = Vector3.zero;
            Object.DestroyImmediate(bodyVisual.GetComponent<Collider>());

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var oldIso = mainCamera.GetComponent<IsometricFollowCamera>();
                if (oldIso != null) Object.DestroyImmediate(oldIso);
                if (mainCamera.GetComponent<ThirdPersonCamera>() == null)
                    mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
                mainCamera.GetComponent<ThirdPersonCamera>().SetTarget(player.transform);
            }
        }

        // CharacterController fights direct transform writes; disable while moving.
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = spawn;
        if (controller != null) controller.enabled = true;

        return player;
    }
}
